using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>
    /// A three-match mini-league against opponents of rising difficulty (3 points for a win, 1 for a draw). The state is static so it
    /// survives the scene reload between matches, and it is saved to PlayerPrefs after every change so quitting the game keeps it.
    /// Saved data that does not make sense (wrong counts, out-of-range values) is discarded and the league starts fresh.
    /// </summary>
    public static class MiniLeague
    {
        public const int Matches = 3;
        public const string ActiveKey = "lg_active", RoundKey = "lg_round", PointsKey = "lg_points", ResultsKey = "lg_results";
        static readonly Difficulty[] Opponents = { Difficulty.Amateur, Difficulty.Professional, Difficulty.WorldClass };

        static bool restored, active;
        static int round, points;
        static readonly List<string> results = new List<string>();

        public static bool Active { get { EnsureRestored(); return active; } }
        /// <summary>Index of the next match to play (0-based); equals Matches when the league is over.</summary>
        public static int Round { get { EnsureRestored(); return round; } }
        public static int Points { get { EnsureRestored(); return points; } }
        public static IReadOnlyList<string> Results { get { EnsureRestored(); return results; } }

        public static bool Finished => Active && round >= Matches;
        public static Difficulty CurrentOpponent { get { EnsureRestored(); return Opponents[System.Math.Min(round, Matches - 1)]; } }

        public static void Start()
        {
            restored = true;
            active = true; round = 0; points = 0; results.Clear();
            Save();
        }

        public static void Abandon()
        {
            restored = true;
            active = false; round = 0; points = 0; results.Clear();
            Save();
        }

        public static int PointsFor(int homeGoals, int awayGoals) { return homeGoals > awayGoals ? 3 : homeGoals == awayGoals ? 1 : 0; }

        /// <summary>Record the result of the match just played (human = home). Returns the points earned; ignored when no league is running.</summary>
        public static int RecordResult(int homeGoals, int awayGoals)
        {
            if (!Active || Finished) return 0;
            int p = PointsFor(homeGoals, awayGoals);
            points += p;
            results.Add($"{homeGoals}-{awayGoals} vs {CurrentOpponent}");
            round++;
            Save();
            return p;
        }

        /// <summary>One line describing the league for the full-time panel.</summary>
        public static string Summary()
        {
            if (!Active) return "";
            if (Finished)
            {
                string verdict = points >= 8 ? "CHAMPIONS" : points >= 5 ? "RUNNERS-UP" : "BETTER LUCK NEXT TIME";
                return $"League finished: {points}/{Matches * 3} points  -  {verdict}";
            }
            return $"League: {round}/{Matches} played, {points} points";
        }

        /// <summary>Forgets the in-memory state so the next read loads from storage again, as after a fresh launch. For tests.</summary>
        public static void ForgetInMemory()
        {
            restored = false; active = false; round = 0; points = 0; results.Clear();
        }

        static void EnsureRestored()
        {
            if (restored) return;
            restored = true;
            try
            {
                if (PlayerPrefs.GetInt(ActiveKey, 0) != 1) return;
                int r = PlayerPrefs.GetInt(RoundKey, 0), p = PlayerPrefs.GetInt(PointsKey, 0);
                string joined = PlayerPrefs.GetString(ResultsKey, "");
                var list = joined.Length == 0 ? new string[0] : joined.Split('|');
                bool sane = r >= 0 && r <= Matches && list.Length == r && p >= 0 && p <= r * 3;
                if (!sane) { Debug.LogWarning("Saved league data did not make sense and was discarded."); Save(); return; }
                active = true; round = r; points = p; results.AddRange(list);
            }
            catch { }
        }

        static void Save()
        {
            try
            {
                if (!active)
                {
                    PlayerPrefs.DeleteKey(ActiveKey); PlayerPrefs.DeleteKey(RoundKey); PlayerPrefs.DeleteKey(PointsKey); PlayerPrefs.DeleteKey(ResultsKey);
                }
                else
                {
                    PlayerPrefs.SetInt(ActiveKey, 1); PlayerPrefs.SetInt(RoundKey, round); PlayerPrefs.SetInt(PointsKey, points);
                    PlayerPrefs.SetString(ResultsKey, string.Join("|", results));
                }
                PlayerPrefs.Save();
            }
            catch { }
        }
    }
}
