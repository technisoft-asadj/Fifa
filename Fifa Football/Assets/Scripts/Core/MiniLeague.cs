using System.Collections.Generic;

namespace FootballSim.Core
{
    /// <summary>
    /// A three-match mini-league against opponents of rising difficulty (3 points for a win, 1 for a draw). The state is static
    /// so it survives the scene reload between matches; it is not saved to disk, so quitting the game abandons the league.
    /// </summary>
    public static class MiniLeague
    {
        public const int Matches = 3;
        static readonly Difficulty[] Opponents = { Difficulty.Amateur, Difficulty.Professional, Difficulty.WorldClass };

        public static bool Active { get; private set; }
        /// <summary>Index of the next match to play (0-based); equals Matches when the league is over.</summary>
        public static int Round { get; private set; }
        public static int Points { get; private set; }
        static readonly List<string> results = new List<string>();
        public static IReadOnlyList<string> Results => results;

        public static bool Finished => Active && Round >= Matches;
        public static Difficulty CurrentOpponent => Opponents[System.Math.Min(Round, Matches - 1)];

        public static void Start()
        {
            Active = true; Round = 0; Points = 0; results.Clear();
        }

        public static void Abandon()
        {
            Active = false; Round = 0; Points = 0; results.Clear();
        }

        public static int PointsFor(int homeGoals, int awayGoals) { return homeGoals > awayGoals ? 3 : homeGoals == awayGoals ? 1 : 0; }

        /// <summary>Record the result of the match just played (human = home). Returns the points earned; ignored when no league is running.</summary>
        public static int RecordResult(int homeGoals, int awayGoals)
        {
            if (!Active || Finished) return 0;
            int p = PointsFor(homeGoals, awayGoals);
            Points += p;
            results.Add($"{homeGoals}-{awayGoals} vs {CurrentOpponent}");
            Round++;
            return p;
        }

        /// <summary>One line describing the league for the full-time panel.</summary>
        public static string Summary()
        {
            if (!Active) return "";
            if (Finished)
            {
                string verdict = Points >= 8 ? "CHAMPIONS" : Points >= 5 ? "RUNNERS-UP" : "BETTER LUCK NEXT TIME";
                return $"League finished: {Points}/{Matches * 3} points  -  {verdict}";
            }
            return $"League: {Round}/{Matches} played, {Points} points";
        }
    }
}
