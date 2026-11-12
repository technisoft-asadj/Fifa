namespace FootballSim.Core
{
    /// <summary>Shared pitch dimensions in metres. Home team attacks +Z.</summary>
    public static class Pitch
    {
        public const float HalfWidth = 34f;
        public const float HalfLength = 52.5f;
        public const float GoalHalfWidth = 3.66f;
        public const float PenaltyAreaHalfWidth = 20.16f;
        public const float PenaltyAreaDepth = 16.5f;
        public const float PenaltySpotDistance = 11f;

        /// <summary>True when a point is inside the penalty area in front of the goal at z = goalZ.</summary>
        public static bool InPenaltyArea(UnityEngine.Vector3 p, float goalZ)
        {
            return UnityEngine.Mathf.Abs(p.x) <= PenaltyAreaHalfWidth && UnityEngine.Mathf.Abs(p.z - goalZ) <= PenaltyAreaDepth
                && UnityEngine.Mathf.Abs(p.z) <= HalfLength;
        }
    }
}
