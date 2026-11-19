using UnityEngine;

namespace FootballSim.Players
{
    /// <summary>Data-driven attributes, separate from visuals. Values 1-99.</summary>
    [CreateAssetMenu(menuName = "FootballSim/Player Data")]
    public class PlayerData : ScriptableObject
    {
        public string displayName = "Player";
        [Range(1, 99)] public int pace = 70;
        [Range(1, 99)] public int acceleration = 70;
        [Range(1, 99)] public int sprintSpeed = 70;
        [Range(1, 99)] public int shooting = 70;
        [Range(1, 99)] public int passing = 70;
        [Range(1, 99)] public int dribbling = 70;
        [Range(1, 99)] public int ballControl = 70;
        [Range(1, 99)] public int agility = 70;
        [Range(1, 99)] public int stamina = 70;
        [Range(1, 99)] public int strength = 70;

        /// <summary>
        /// Everyone (both teams) runs this much faster than the real-life figures below. On a phone screen the pitch is small and the camera
        /// is far away, so real speeds look like slow motion; scaling all players together keeps the match balanced.
        /// </summary>
        public const float PaceScale = 1.25f;

        public float JogSpeed => Mathf.Lerp(3.2f, 4.5f, pace / 99f) * PaceScale;
        public float SprintSpeedMs => Mathf.Lerp(6.0f, 9.5f, sprintSpeed / 99f) * PaceScale;
        public float AccelerationMs2 => Mathf.Lerp(4f, 9f, acceleration / 99f) * PaceScale;
        public float TurnRateDegPerSec => Mathf.Lerp(360f, 720f, agility / 99f);
        public float MaxShotSpeed => Mathf.Lerp(18f, 32f, shooting / 99f);
        public float MaxPassSpeed => Mathf.Lerp(12f, 24f, passing / 99f);
        /// <summary>Random aim error in degrees at full power.</summary>
        public float ShotInaccuracyDeg => Mathf.Lerp(9f, 1.5f, shooting / 99f);
        public float PassInaccuracyDeg => Mathf.Lerp(7f, 1f, passing / 99f);
    }
}
