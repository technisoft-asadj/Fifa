namespace FootballSim.Core
{
    /// <summary>
    /// Free practice: the opposing outfield players stand still as dummy defenders (their goalkeeper still plays) and the match is so long
    /// that full time is not reached in a session. Static so it survives the scene reload, like the league; it is not saved to disk, so
    /// every launch starts in single-match mode.
    /// </summary>
    public static class TrainingMode
    {
        public static bool Active;
        /// <summary>Match length used in training. Half time and full time still exist but are an hour away.</summary>
        public const float Minutes = 120f;
    }
}
