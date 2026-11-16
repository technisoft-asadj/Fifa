using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>
    /// A three-round knockout (quarter-final, semi-final, final) against opponents of rising difficulty. Win to go through, lose and you are
    /// out. A drawn match is settled by a simulated penalty shoot-out when one is supplied (see <see cref="ShootOut"/>), otherwise it is
    /// played again; there is no extra time. Static so it survives the scene reload
    /// between matches, and saved to PlayerPrefs after every change; saved data that does not add up is discarded.
    /// </summary>
    public static class Tournament
    {
        public enum Outcome { None, Advanced, Replay, Eliminated, Champion }

        public const int Rounds = 3;
        public const string ActiveKey = "tn_active", RoundKey = "tn_round", EliminatedKey = "tn_out", ResultsKey = "tn_results";
        static readonly Difficulty[] Opponents = { Difficulty.Amateur, Difficulty.Professional, Difficulty.WorldClass };
        static readonly string[] Stages = { "Quarter-final", "Semi-final", "Final" };

        static bool restored, active, eliminated;
        static int round;
        static readonly List<string> results = new List<string>();

        public static bool Active { get { EnsureRestored(); return active; } }
        /// <summary>Index of the round being played (0 = quarter-final); equals Rounds once the cup is won.</summary>
        public static int Round { get { EnsureRestored(); return round; } }
        public static bool Eliminated { get { EnsureRestored(); return eliminated; } }
        public static bool Won => Active && !eliminated && round >= Rounds;
        /// <summary>The cup is over, one way or the other: no more matches to play.</summary>
        public static bool Over => Active && (eliminated || round >= Rounds);
        public static IReadOnlyList<string> Results { get { EnsureRestored(); return results; } }
        public static Difficulty CurrentOpponent { get { EnsureRestored(); return Opponents[Mathf.Min(round, Rounds - 1)]; } }
        public static string CurrentStage { get { EnsureRestored(); return Stages[Mathf.Min(round, Rounds - 1)]; } }

        public static void Start() { restored = true; active = true; eliminated = false; round = 0; results.Clear(); Save(); }
        public static void Abandon() { restored = true; active = false; eliminated = false; round = 0; results.Clear(); Save(); }

        /// <summary>Records the match just played (human = home). A win advances, a loss eliminates, a draw replays. Ignored when no cup is running.</summary>
        public static Outcome RecordResult(int homeGoals, int awayGoals, ShootOut.Result shootOut = null)
        {
            if (!Active || Over) return Outcome.None;
            string line = $"{CurrentStage}: {homeGoals}-{awayGoals} vs {CurrentOpponent}";
            Outcome o;
            bool homeThrough = homeGoals > awayGoals;
            bool decided = homeGoals != awayGoals;
            if (!decided && shootOut != null) { decided = true; homeThrough = shootOut.homeWins; line += $" ({shootOut})"; }
            if (!decided) { line += " (draw, replay)"; o = Outcome.Replay; }
            else if (homeThrough) { round++; o = round >= Rounds ? Outcome.Champion : Outcome.Advanced; }
            else { eliminated = true; o = Outcome.Eliminated; }
            results.Add(line);
            Save();
            return o;
        }

        public static string Summary()
        {
            if (!Active) return "";
            if (Won) return "TOURNAMENT WON  -  CHAMPIONS";
            if (eliminated) return $"Knocked out in the {CurrentStage.ToLower()}";
            return $"Tournament: {CurrentStage} next";
        }

        /// <summary>Forgets the in-memory state so the next read loads from storage again, as after a fresh launch. For tests.</summary>
        public static void ForgetInMemory() { restored = false; active = false; eliminated = false; round = 0; results.Clear(); }

        static void EnsureRestored()
        {
            if (restored) return;
            restored = true;
            try
            {
                if (PlayerPrefs.GetInt(ActiveKey, 0) != 1) return;
                int r = PlayerPrefs.GetInt(RoundKey, 0);
                bool out_ = PlayerPrefs.GetInt(EliminatedKey, 0) == 1;
                string joined = PlayerPrefs.GetString(ResultsKey, "");
                var list = joined.Length == 0 ? new string[0] : joined.Split('|');
                // every round passed or lost left at least one result line; an eliminated cup cannot also be won
                bool sane = r >= 0 && r <= Rounds && list.Length >= r + (out_ ? 1 : 0) && !(out_ && r >= Rounds) && list.Length <= 200;
                if (!sane) { Debug.LogWarning("Saved tournament data did not make sense and was discarded."); Save(); return; }
                active = true; round = r; eliminated = out_; results.AddRange(list);
            }
            catch { }
        }

        static void Save()
        {
            try
            {
                if (!active)
                {
                    PlayerPrefs.DeleteKey(ActiveKey); PlayerPrefs.DeleteKey(RoundKey); PlayerPrefs.DeleteKey(EliminatedKey); PlayerPrefs.DeleteKey(ResultsKey);
                }
                else
                {
                    PlayerPrefs.SetInt(ActiveKey, 1); PlayerPrefs.SetInt(RoundKey, round); PlayerPrefs.SetInt(EliminatedKey, eliminated ? 1 : 0);
                    PlayerPrefs.SetString(ResultsKey, string.Join("|", results));
                }
                PlayerPrefs.Save();
            }
            catch { }
        }
    }
}
