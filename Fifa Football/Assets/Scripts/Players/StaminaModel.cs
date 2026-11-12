using UnityEngine;

namespace FootballSim.Players
{
    /// <summary>Pure stamina arithmetic. Stamina is 0..1; sprinting drains it, anything else refills it.</summary>
    public static class StaminaModel
    {
        public const float DrainSeconds = 20f;     // full bar of continuous sprint
        public const float RecoverSeconds = 14f;   // empty to full while not sprinting
        public const float ResumeThreshold = 0.25f; // after running dry, sprint is locked until this much is back

        public static float Step(float stamina, bool sprinting, float dt)
        {
            float delta = sprinting ? -dt / DrainSeconds : dt / RecoverSeconds;
            return Mathf.Clamp01(stamina + delta);
        }

        /// <summary>Whether sprint is allowed given current stamina and whether the player had run dry.</summary>
        public static bool UpdateExhausted(float stamina, bool wasExhausted)
        {
            if (stamina <= 0f) return true;
            if (stamina >= ResumeThreshold) return false;
            return wasExhausted;
        }
    }
}
