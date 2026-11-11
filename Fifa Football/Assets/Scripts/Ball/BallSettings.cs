using UnityEngine;

namespace FootballSim.Ball
{
    /// <summary>Configurable ball physics values. Create via Assets > Create > FootballSim > Ball Settings.</summary>
    [CreateAssetMenu(menuName = "FootballSim/Ball Settings")]
    public class BallSettings : ScriptableObject
    {
        public float mass = 0.43f;
        public float radius = 0.11f;
        public float airDrag = 0.012f;          // quadratic air resistance coefficient
        public float magnusStrength = 0.0045f;  // spin curve effect
        public float rollingResistance = 0.55f; // deceleration (m/s^2) while rolling on ground
        public float spinDecay = 0.6f;          // per-second spin loss
        public float maxSpeed = 38f;
    }
}
