using UnityEngine;
using FootballSim.Ball;

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

    /// <summary>Match clock, score and kick-off reset. Gameplay rules only, no UI.</summary>
    public class MatchManager : MonoBehaviour
    {
        [SerializeField] BallController ball;
        [SerializeField] float matchMinutes = 5f;
        [SerializeField] float simulatedMatchMinutes = 90f;
        [SerializeField] Vector3 kickoffSpot = new Vector3(0f, 0.12f, 0f);

        public int HomeScore { get; private set; }
        public int AwayScore { get; private set; }
        public float RealSecondsElapsed { get; private set; }
        public bool Finished { get; private set; }
        public float GameMinute => Mathf.Min(simulatedMatchMinutes, RealSecondsElapsed / (matchMinutes * 60f) * simulatedMatchMinutes);

        /// <summary>Raised after a goal once the ball has been put back on the centre spot.</summary>
        public event System.Action KickoffReady;

        float resetTimer = -1f;

        void Update()
        {
            if (Finished) return;
            RealSecondsElapsed += Time.deltaTime;
            if (RealSecondsElapsed >= matchMinutes * 60f) Finished = true;

            if (resetTimer > 0f)
            {
                resetTimer -= Time.deltaTime;
                if (resetTimer <= 0f) { ball.ResetTo(kickoffSpot); KickoffReady?.Invoke(); }
            }
        }

        public void OnGoal(int scoringTeam)
        {
            if (Finished || resetTimer > 0f) return;
            if (scoringTeam == 0) HomeScore++; else AwayScore++;
            Debug.Log($"GOAL! Home {HomeScore} - {AwayScore} Away  ({GameMinute:0}')");
            resetTimer = 3f;
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.UpperCenter };
            GUI.Label(new Rect(0, 10, Screen.width, 40),
                $"HOME {HomeScore} - {AwayScore} AWAY    {GameMinute:00}'" + (Finished ? "  FULL TIME" : ""), style);
        }
    }
}
