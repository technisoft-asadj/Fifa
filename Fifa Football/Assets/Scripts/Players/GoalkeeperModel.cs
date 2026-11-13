using UnityEngine;

namespace FootballSim.Players
{
    public enum SaveOutcome { None, Catch, Parry }

    /// <summary>
    /// Goalkeeper save rules, kept free of scene objects so they can be unit tested: how far a keeper reaches, how likely
    /// a save is for a given shot, and whether it is held (caught) or pushed away (parried).
    /// </summary>
    public static class GoalkeeperModel
    {
        public const float StandReach = 1.3f;   // metres from the chest when standing
        public const float DiveReach = 2.3f;    // metres while diving
        public const float MaxHeight = 2.4f;    // balls above this are over the keeper's hands
        public const float CatchSpeedLimit = 13f; // faster shots can only be parried
        public const float MinShotSpeed = 3f;   // slower balls are just collected by walking into them

        /// <summary>Chance (0.15-0.92) that the keeper gets a hand to a shot: harder and further from the body is harder.</summary>
        public static float SaveChance(float shotSpeed, float distance, float reach)
        {
            float c = 0.9f - Mathf.Clamp((shotSpeed - 8f) * 0.025f, 0f, 0.45f) - Mathf.Clamp01(distance / Mathf.Max(0.01f, reach)) * 0.25f;
            return Mathf.Clamp(c, 0.15f, 0.92f);
        }

        /// <summary>roll is a random number in [0,1): below the chance the shot is saved, held when slow enough, else parried.</summary>
        public static SaveOutcome Decide(float shotSpeed, float distance, float reach, float roll)
        {
            if (shotSpeed < MinShotSpeed) return SaveOutcome.None;
            if (roll >= SaveChance(shotSpeed, distance, reach)) return SaveOutcome.None;
            return shotSpeed <= CatchSpeedLimit ? SaveOutcome.Catch : SaveOutcome.Parry;
        }
    }
}
