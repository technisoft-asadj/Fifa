namespace FootballSim.Core
{
    public enum Difficulty { Beginner, Amateur, Professional, WorldClass, Legendary }

    /// <summary>
    /// AI behaviour tuning per difficulty. Difficulty changes decision quality (reaction time, aim error,
    /// shooting range, tackle timing), not raw speed or impossible accuracy.
    /// </summary>
    public readonly struct AiTuning
    {
        public readonly float reactionDelay;   // seconds between re-evaluating the chase target
        public readonly float aimErrorDeg;     // random aim error added to AI passes/shots
        public readonly float shootRange;      // metres from goal within which AI will shoot
        public readonly float tackleCooldown;  // seconds between AI tackle attempts

        public AiTuning(float reactionDelay, float aimErrorDeg, float shootRange, float tackleCooldown)
        {
            this.reactionDelay = reactionDelay; this.aimErrorDeg = aimErrorDeg;
            this.shootRange = shootRange; this.tackleCooldown = tackleCooldown;
        }

        /// <summary>Difficulty used for the current match (persists across rematches).</summary>
        public static Difficulty CurrentDifficulty = Difficulty.Professional;
        public static AiTuning Current => For(CurrentDifficulty);

        public static AiTuning For(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Beginner:     return new AiTuning(0.55f, 12f, 17f, 3.4f);
                case Difficulty.Amateur:      return new AiTuning(0.35f, 8f, 21f, 2.5f);
                case Difficulty.Professional: return new AiTuning(0.20f, 5f, 24f, 1.9f);
                case Difficulty.WorldClass:   return new AiTuning(0.10f, 3f, 28f, 1.4f);
                default:                      return new AiTuning(0.05f, 1.5f, 31f, 1.1f);
            }
        }
    }
}
