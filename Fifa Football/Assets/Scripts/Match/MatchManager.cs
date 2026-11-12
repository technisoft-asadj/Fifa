using System.Collections.Generic;
using UnityEngine;
using FootballSim.Ball;
using FootballSim.Core;
using FootballSim.FootballAI;
using FootballSim.Players;

namespace FootballSim.Match
{
    public enum RestartType { None, KickOff, ThrowIn, GoalKick, Corner, FreeKick, Penalty }

    /// <summary>Match clock, score, kick-off reset and out-of-bounds restarts. Gameplay rules only, no UI.</summary>
    public class MatchManager : MonoBehaviour
    {
        [SerializeField] BallController ball;
        [SerializeField] float matchMinutes = 5f;
        [SerializeField] float simulatedMatchMinutes = 90f;
        [SerializeField] Vector3 kickoffSpot = new Vector3(0f, 0.12f, 0f);
        [SerializeField] TeamController[] teams;
        [SerializeField] float restartDelay = 1.2f;

        public int HomeScore { get; private set; }
        public int AwayScore { get; private set; }
        public float RealSecondsElapsed { get; private set; }
        public bool Finished { get; private set; }
        public RestartType LastRestart { get; private set; }
        public float GameMinute => Mathf.Min(simulatedMatchMinutes, RealSecondsElapsed / (matchMinutes * 60f) * simulatedMatchMinutes);

        /// <summary>Raised after a goal once the ball has been put back on the centre spot.</summary>
        public event System.Action KickoffReady;

        /// <summary>Raised when a goal is scored; argument = scoring team (0 home, 1 away).</summary>
        public event System.Action<int> GoalScored;
        /// <summary>Raised once when the match clock runs out.</summary>
        public event System.Action FullTime;
        /// <summary>Raised when an out-of-bounds restart (throw-in, corner, goal kick) is awarded.</summary>
        public event System.Action<RestartType, int> RestartAwarded;

        /// <summary>Raised when a player is booked; arguments = team, total yellow cards for that team.</summary>
        public event System.Action<int, int> YellowCard;

        public int HomeFouls { get; private set; }
        public int AwayFouls { get; private set; }
        public int HomeYellows { get; private set; }
        public int AwayYellows { get; private set; }

        readonly Dictionary<object, int> foulsByPlayer = new Dictionary<object, int>();

        void OnEnable() { PlayerController.Fouled += OnPlayerFoul; }
        void OnDisable() { PlayerController.Fouled -= OnPlayerFoul; }

        void OnPlayerFoul(PlayerController fouler, PlayerController victim)
        {
            AwardFoul(fouler.TeamId, victim.transform.position, fouler);
        }

        /// <summary>Award a free kick to the team that was fouled. Every second foul by the same player is a yellow card.</summary>
        public void AwardFoul(int foulerTeam, Vector3 spot, object foulerKey)
        {
            if (Finished || resetTimer > 0f || restartTimer > 0f) return;
            if (foulerTeam == 0) HomeFouls++; else AwayFouls++;

            foulsByPlayer.TryGetValue(foulerKey, out int n);
            n++;
            foulsByPlayer[foulerKey] = n;
            if (n == 4)
            {
                // Second yellow = red. The player leaves the pitch (goalkeepers are not sent off yet: no substitute keeper logic).
                var pc = foulerKey as PlayerController;
                if (pc == null || !pc.isGoalkeeper)
                {
                    if (foulerTeam == 0) HomeReds++; else AwayReds++;
                    if (pc != null && teams != null)
                        foreach (var t in teams)
                            if (t != null && t.teamId == foulerTeam) t.SendOff(pc);
                    RedCard?.Invoke(foulerTeam, foulerTeam == 0 ? HomeReds : AwayReds);
                }
            }
            else if (n == 2)
            {
                if (foulerTeam == 0) HomeYellows++; else AwayYellows++;
                YellowCard?.Invoke(foulerTeam, foulerTeam == 0 ? HomeYellows : AwayYellows);
            }

            int award = 1 - foulerTeam;
            Vector3 facing = new Vector3(0f, 0f, award == 0 ? 1f : -1f);
            float foulerGoalZ = foulerTeam == 0 ? -Pitch.HalfLength : Pitch.HalfLength; // home defends -Z
            if (Pitch.InPenaltyArea(spot, foulerGoalZ))
            {
                Vector3 mark = new Vector3(0f, 0.12f, foulerGoalZ - Mathf.Sign(foulerGoalZ) * Pitch.PenaltySpotDistance);
                StartRestart(RestartType.Penalty, award, mark, facing);
                return;
            }
            float limit = Pitch.HalfLength - 1f;
            spot = new Vector3(Mathf.Clamp(spot.x, -Pitch.HalfWidth + 1f, Pitch.HalfWidth - 1f), 0.12f, Mathf.Clamp(spot.z, -limit, limit));
            StartRestart(RestartType.FreeKick, award, spot, facing);
        }

        /// <summary>Raised when a player is sent off; arguments = team, total red cards for that team.</summary>
        public event System.Action<int, int> RedCard;
        public int HomeReds { get; private set; }
        public int AwayReds { get; private set; }

        public float MatchMinutes => matchMinutes;
        public bool InGoalPause => resetTimer > 0f;
        public bool InRestartHold => restartTimer > 0f;

        /// <summary>Set the real-time match length (minutes). Call before the match starts.</summary>
        public void SetDurationMinutes(float minutes) { matchMinutes = Mathf.Max(0.5f, minutes); }

        float resetTimer = -1f;
        float restartTimer = -1f;

        void Update()
        {
            if (Finished) return;
            RealSecondsElapsed += Time.deltaTime;
            if (RealSecondsElapsed >= matchMinutes * 60f)
            {
                Finished = true;
                FullTime?.Invoke();
                return;
            }

            if (resetTimer > 0f)
            {
                resetTimer -= Time.deltaTime;
                if (resetTimer <= 0f)
                {
                    ball.LastTouchTeam = -1;
                    ball.ResetTo(kickoffSpot);
                    LastRestart = RestartType.KickOff;
                    KickoffReady?.Invoke();
                }
                return;
            }

            if (restartTimer > 0f)
            {
                restartTimer -= Time.deltaTime;
                if (restartTimer <= 0f) ball.Freeze(false);
                return;
            }

            CheckOutOfBounds();
        }

        public void OnGoal(int scoringTeam)
        {
            if (Finished || resetTimer > 0f) return;
            if (scoringTeam == 0) HomeScore++; else AwayScore++;
            Debug.Log($"GOAL! Home {HomeScore} - {AwayScore} Away  ({GameMinute:0}')");
            resetTimer = 3f;
            GoalScored?.Invoke(scoringTeam);
        }

        void CheckOutOfBounds()
        {
            if (ball == null) return;
            Vector3 p = ball.transform.position;
            float hw = Pitch.HalfWidth, hl = Pitch.HalfLength;
            if (Mathf.Abs(p.x) <= hw && Mathf.Abs(p.z) <= hl) return;

            // Inside the goal mouth: the goal trigger handles it
            bool inGoalMouth = Mathf.Abs(p.z) > hl && Mathf.Abs(p.x) < Pitch.GoalHalfWidth && p.y < 2.6f;
            if (inGoalMouth) return;

            int last = ball.LastTouchTeam;
            if (Mathf.Abs(p.x) > hw)
            {
                // Throw-in to the team that did not touch it last
                int award = last == 0 ? 1 : 0;
                Vector3 spot = new Vector3(Mathf.Sign(p.x) * (hw - 0.3f), 0.12f, Mathf.Clamp(p.z, -hl + 2f, hl - 2f));
                Vector3 facing = new Vector3(-Mathf.Sign(p.x), 0f, 0f);
                StartRestart(RestartType.ThrowIn, award, spot, facing);
                return;
            }

            // Crossed an end line (outside the goal mouth)
            float endSign = Mathf.Sign(p.z);                 // +1 = away goal end (home attacks +Z)
            int defendingTeam = endSign > 0 ? 1 : 0;
            if (last == defendingTeam)
            {
                // Defender touched last -> corner for attackers
                int award = 1 - defendingTeam;
                Vector3 spot = new Vector3(Mathf.Sign(p.x) * (hw - 0.5f), 0.12f, endSign * (hl - 0.5f));
                Vector3 facing = new Vector3(-Mathf.Sign(p.x), 0f, -endSign * 0.5f);
                StartRestart(RestartType.Corner, award, spot, facing);
            }
            else
            {
                // Attacker (or nobody) touched last -> goal kick for defenders
                Vector3 spot = new Vector3(0f, 0.12f, endSign * (hl - 5.5f));
                Vector3 facing = new Vector3(0f, 0f, -endSign);
                StartRestart(RestartType.GoalKick, defendingTeam, spot, facing);
            }
        }

        void StartRestart(RestartType type, int awardedTeam, Vector3 spot, Vector3 facing)
        {
            LastRestart = type;
            ball.ResetTo(spot);
            ball.Freeze(true);
            ball.LastTouchTeam = -1;
            restartTimer = restartDelay;
            bool penalty = type == RestartType.Penalty;
            PlayerController taker = null;
            if (teams != null)
            {
                foreach (var t in teams)
                    if (t != null && t.teamId == awardedTeam) taker = t.SendTakerTo(spot, facing, penalty);
                if (penalty)
                {
                    float goalZ = Mathf.Sign(facing.z) * Pitch.HalfLength;
                    foreach (var t in teams) if (t != null) t.ClearPenaltyArea(goalZ, taker);
                }
            }
            Debug.Log($"{type} for team {awardedTeam}");
            RestartAwarded?.Invoke(type, awardedTeam);
        }
    }
}
