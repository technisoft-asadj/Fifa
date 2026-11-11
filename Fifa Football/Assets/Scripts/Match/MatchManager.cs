using UnityEngine;
using FootballSim.Ball;
using FootballSim.Core;
using FootballSim.FootballAI;

namespace FootballSim.Match
{
    /// <summary>Goal trigger placed behind the goal line. Reports to MatchManager which side scored.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class GoalTrigger : MonoBehaviour
    {
        [Tooltip("Team that DEFENDS this goal (0 = home, 1 = away). The other team scores.")]
        public int defendingTeam;
        public MatchManager match;

        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (match != null && other.GetComponentInParent<BallController>() != null)
                match.OnGoal(1 - defendingTeam);
        }
    }

    public enum RestartType { None, KickOff, ThrowIn, GoalKick, Corner }

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

        float resetTimer = -1f;
        float restartTimer = -1f;

        void Update()
        {
            if (Finished) return;
            RealSecondsElapsed += Time.deltaTime;
            if (RealSecondsElapsed >= matchMinutes * 60f) Finished = true;

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
            if (teams != null)
                foreach (var t in teams)
                    if (t != null && t.teamId == awardedTeam) t.SendTakerTo(spot, facing);
            Debug.Log($"{type} for team {awardedTeam}");
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.UpperCenter };
            string restart = restartTimer > 0f ? $"  [{LastRestart}]" : "";
            GUI.Label(new Rect(0, 10, Screen.width, 40),
                $"HOME {HomeScore} - {AwayScore} AWAY    {GameMinute:00}'" + (Finished ? "  FULL TIME" : restart), style);
        }
    }
}
