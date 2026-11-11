using UnityEngine;
using FootballSim.Ball;

namespace FootballSim.Match
{
    /// <summary>
    /// Goal trigger placed behind the goal line. Reports to MatchManager which side scored.
    /// Lives in its own file: Unity can only serialise a MonoBehaviour into a scene when the class name
    /// matches the file name.
    /// </summary>
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
}
