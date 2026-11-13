using System.Collections.Generic;
using UnityEngine;
using FootballSim.Ball;
using FootballSim.Core;
using FootballSim.FootballAI;
using FootballSim.Players;

namespace FootballSim.Match
{
    /// <summary>One goal: the team it counts for, game minute, who last touched the ball, and whether that was a defender.</summary>
    public struct GoalRecord
    {
        public int team;
        public int minute;
        public string scorer;
        public bool ownGoal;

        public override string ToString() { return $"{minute}'  {(team == 0 ? "HOME" : "AWAY")}  {scorer}{(ownGoal ? " (og)" : "")}"; }
    }

    public enum RestartType { None, KickOff, ThrowIn, GoalKick, Corner, FreeKick, Penalty, Offside }

    /// <summary>Match clock, score, kick-off reset and out-of-bounds restarts. Gameplay rules only, no UI.</summary>
    public class MatchManager : MonoBehaviour
    {
        [SerializeField] BallController ball;
        [SerializeField] float matchMinutes = 5f;
        [SerializeField] float simulatedMatchMinutes = 90f;
        [SerializeField] Vector3 kickoffSpot = new Vector3(0f, 0.12f, 0f);
        [SerializeField] TeamController[] teams;
        [SerializeField] float restartDelay = 1.2f;

        /// <summary>Every goal in the order it was scored.</summary>
        public System.Collections.Generic.List<GoalRecord> Goals { get; } = new System.Collections.Generic.List<GoalRecord>();

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

        void OnEnable() { PlayerController.Fouled += OnPlayerFoul; PlayerController.Struck += OnPlayerStruck; }
        void OnDisable() { PlayerController.Fouled -= OnPlayerFoul; PlayerController.Struck -= OnPlayerStruck; }

        /// <summary>Possession, shots, passes and corners for both teams.</summary>
        public MatchStats Stats { get; } = new MatchStats();

        void OnPlayerStruck(PlayerController p, bool isShot, Vector3 dir)
        {
            if (Finished || ball == null) return;
            Stats.AddStrike(p.TeamId, isShot, ball.transform.position, dir, AttackSign(p.TeamId));
            SnapshotOffside(p);
        }

        /// <summary>True after the half-time break: the teams have changed ends.</summary>
        public bool SidesSwapped { get; private set; }

        /// <summary>+1 if the team currently attacks +Z, -1 if it attacks -Z. Home starts by attacking +Z.</summary>
        public float AttackSign(int team) { return (team == 0) != SidesSwapped ? 1f : -1f; }

        // ---- kick-off ----
        /// <summary>Team that takes the current/next kick-off: home to start, the conceding team after a goal, away to start the second half.</summary>
        public int KickoffTeam { get; private set; }
        /// <summary>True from the whistle until the ball is first played; the other team must stay out of the centre circle.</summary>
        public bool KickoffPending { get; private set; } = true;
        public const float CentreCircleRadius = 9.15f;
        float kickoffWait;

        /// <summary>Put the kick-off taker beside the ball and move the other team out of the centre circle.</summary>
        public void PrepareKickoff()
        {
            KickoffPending = true;
            kickoffWait = 0f;
            if (teams == null) return;
            foreach (var t in teams)
            {
                if (t == null) continue;
                if (t.teamId == KickoffTeam)
                {
                    PlayerController taker = null; float bd = float.MaxValue;
                    foreach (var p in t.players)
                    {
                        if (p.isGoalkeeper || !TeamController.IsOn(p)) continue;
                        float d = p.transform.position.sqrMagnitude;
                        if (d < bd) { bd = d; taker = p; }
                    }
                    if (taker != null)
                    {
                        taker.TeleportTo(new Vector3(0f, 1f, -t.attackDir * 0.8f), Quaternion.LookRotation(new Vector3(0f, 0f, t.attackDir)));
                        if (t.humanControlled) t.SetControlled(taker);
                    }
                }
                else KeepOutOfCentreCircle(t);
            }
        }

        /// <summary>Moves any player of this team who is inside the centre circle to its edge on the team's own half.</summary>
        void KeepOutOfCentreCircle(TeamController t)
        {
            float side = -t.attackDir; // this team's own half
            foreach (var p in t.players)
            {
                if (!TeamController.IsOn(p)) continue;
                Vector3 pos = p.transform.position;
                if (new Vector2(pos.x, pos.z).magnitude >= CentreCircleRadius) continue;
                pos.z = side * Mathf.Sqrt(Mathf.Max(0f, CentreCircleRadius * CentreCircleRadius - pos.x * pos.x)) + side * 0.3f;
                p.TeleportTo(pos, p.transform.rotation);
            }
        }

        void SwapSides()
        {
            KickoffTeam = 1; // away starts the second half
            SidesSwapped = !SidesSwapped;
            if (teams != null) foreach (var t in teams) if (t != null) t.attackDir = AttackSign(t.teamId);
            foreach (var g in Object.FindObjectsByType<GoalTrigger>(FindObjectsSortMode.None)) g.defendingTeam = 1 - g.defendingTeam;
            var cam = Object.FindFirstObjectByType<FootballSim.CameraSystem.BroadcastCamera>();
            if (cam != null) cam.SetViewSign(SidesSwapped ? -1f : 1f);
        }

        // ---- offside ----
        readonly HashSet<PlayerController> offsideRunners = new HashSet<PlayerController>();
        int offsideTeam = -1;
        PlayerController offsidePasser;
        float offsideSnapshotTime;
        bool exemptNextStrike; // throw-ins, goal kicks and corners cannot be offside

        /// <summary>Players of the passing team who were in an offside position when 'passer' played the ball.</summary>
        public IReadOnlyCollection<PlayerController> OffsideRunners => offsideRunners;

        /// <summary>Record which team-mates of 'passer' are offside right now. Called automatically on every pass or shot.</summary>
        public void SnapshotOffside(PlayerController passer)
        {
            offsideRunners.Clear();
            offsideTeam = -1;
            offsidePasser = passer;
            offsideSnapshotTime = Time.time;
            if (exemptNextStrike) { exemptNextStrike = false; return; }
            if (teams == null) return;
            TeamController own = null, opp = null;
            foreach (var t in teams)
            {
                if (t == null) continue;
                if (t.teamId == passer.TeamId) own = t; else opp = t;
            }
            if (own == null || opp == null) return;
            var defZ = new List<float>();
            foreach (var d in opp.players) if (TeamController.IsOn(d)) defZ.Add(d.transform.position.z);
            float dirSign = AttackSign(passer.TeamId);
            float line = OffsideRule.SecondLastDefenderZ(dirSign, defZ);
            float ballZ = ball.transform.position.z;
            foreach (var m in own.players)
                if (m != passer && TeamController.IsOn(m) && OffsideRule.IsOffside(dirSign, m.transform.position.z, ballZ, line))
                    offsideRunners.Add(m);
            if (offsideRunners.Count > 0) offsideTeam = passer.TeamId;
        }

        void CheckOffside()
        {
            if (offsideRunners.Count == 0) return;
            var toucher = ball.LastToucher as PlayerController;
            if (ball.LastTouchTeam != offsideTeam) { offsideRunners.Clear(); return; } // opponent got there first
            if (toucher == null) return;
            // The ball was played by an onside team-mate (not the passer): the earlier offside positions no longer count
            if (toucher != offsidePasser && !offsideRunners.Contains(toucher)) { offsideRunners.Clear(); return; }
            // Nobody touched it for a long time (rolling out, loose ball): the offside position no longer matters
            if (Time.time - offsideSnapshotTime > 6f) { offsideRunners.Clear(); return; }
            if (offsideRunners.Contains(toucher))
            {
                int team = offsideTeam;
                Vector3 spot = toucher.transform.position;
                offsideRunners.Clear();
                Stats.offsides[team]++;
                float limit = Pitch.HalfLength - 1f;
                spot = new Vector3(Mathf.Clamp(spot.x, -Pitch.HalfWidth + 1f, Pitch.HalfWidth - 1f), 0.12f, Mathf.Clamp(spot.z, -limit, limit));
                StartRestart(RestartType.Offside, 1 - team, spot, new Vector3(0f, 0f, -AttackSign(team)));
            }
            // any other touch by a team-mate means the pass was not to an offside player: stays pending only for the runners
        }

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
            Vector3 facing = new Vector3(0f, 0f, AttackSign(award));
            float foulerGoalZ = -AttackSign(foulerTeam) * Pitch.HalfLength; // the end the fouler defends
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

        /// <summary>True once the first half has ended (the break itself lasts HalfTimeSeconds).</summary>
        public bool HalfTimeReached { get; private set; }
        public const float HalfTimeSeconds = 4f;
        bool swapPending;
        /// <summary>Raised when the first half ends. Teams keep their ends in this prototype (no side swap yet).</summary>
        public event System.Action HalfTime;

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

            if (!HalfTimeReached && resetTimer <= 0f && restartTimer <= 0f && RealSecondsElapsed >= matchMinutes * 30f)
            {
                // Half-time: stop play for a few seconds, then restart from the centre spot
                HalfTimeReached = true;
                swapPending = true;
                offsideRunners.Clear();
                ball.Freeze(true);
                resetTimer = HalfTimeSeconds;
                HalfTime?.Invoke();
                return;
            }

            if (resetTimer > 0f)
            {
                resetTimer -= Time.deltaTime;
                if (resetTimer <= 0f)
                {
                    ball.Freeze(false);
                    if (swapPending) { swapPending = false; SwapSides(); } // change ends before the second-half kick-off
                    ball.LastTouchTeam = -1;
                    ball.LastToucher = null;
                    offsideRunners.Clear();
                    exemptNextStrike = true; // a goal can't be scored offside and kick-off is exempt too
                    ball.ResetTo(kickoffSpot);
                    LastRestart = RestartType.KickOff;
                    KickoffReady?.Invoke();
                    PrepareKickoff();
                }
                return;
            }

            if (restartTimer > 0f)
            {
                restartTimer -= Time.deltaTime;
                if (restartTimer <= 0f) ball.Freeze(false);
                return;
            }

            if (KickoffPending)
            {
                kickoffWait += Time.deltaTime;
                // The team not kicking off may not enter the centre circle (a human could walk in; AI already waits outside)
                if (teams != null) foreach (var t in teams) if (t != null && t.teamId != KickoffTeam) KeepOutOfCentreCircle(t);
                // ends when the ball is played, or after a failsafe so nobody waits forever for a taker who never kicks
                if (kickoffWait > 8f || (ball != null && (ball.LastTouchTeam != -1 || (ball.transform.position - kickoffSpot).sqrMagnitude > 1f))) KickoffPending = false;
            }
            if (ball != null) Stats.AddPossession(ball.LastTouchTeam, Time.deltaTime); // live play only
            CheckOffside();
            if (restartTimer > 0f) return; // an offside was just given
            CheckOutOfBounds();
        }

        public void OnGoal(int scoringTeam)
        {
            if (Finished || resetTimer > 0f) return;
            if (scoringTeam == 0) HomeScore++; else AwayScore++;
            var toucher = ball != null ? ball.LastToucher as PlayerController : null;
            Goals.Add(new GoalRecord
            {
                team = scoringTeam,
                minute = Mathf.Max(1, Mathf.CeilToInt(GameMinute)),
                scorer = toucher != null ? toucher.name : "Unknown",
                ownGoal = toucher != null && toucher.TeamId != scoringTeam
            });
            Debug.Log($"GOAL! Home {HomeScore} - {AwayScore} Away  ({GameMinute:0}')");
            resetTimer = 3f;
            KickoffTeam = 1 - scoringTeam; // the conceding team restarts
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
            float endSign = Mathf.Sign(p.z);                 // +1 = the +Z end
            int defendingTeam = AttackSign(0) > 0f ? (endSign > 0 ? 1 : 0) : (endSign > 0 ? 0 : 1); // the team attacking the other way defends this end
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
            ball.LastToucher = null;
            offsideRunners.Clear();
            exemptNextStrike = type == RestartType.ThrowIn || type == RestartType.GoalKick || type == RestartType.Corner;
            restartTimer = restartDelay;
            if (type == RestartType.Corner && (awardedTeam == 0 || awardedTeam == 1)) Stats.corners[awardedTeam]++;
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
