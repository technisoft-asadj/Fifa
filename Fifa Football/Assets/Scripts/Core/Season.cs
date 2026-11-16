using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>
    /// A one-round-robin season for six teams: the human team ("You") and five computer teams of rising strength. The human plays five
    /// matches, one against each opponent; in every round the other four teams play two matches among themselves, simulated from a saved
    /// seed. The table is recomputed from the seed and the human's own results, so only those need saving. Static so it survives the scene
    /// reload, saved to PlayerPrefs after every change, and saved data that does not add up is discarded.
    /// </summary>
    public static class Season
    {
        public const int Teams = 6, Rounds = 5;
        public const string ActiveKey = "se_active", SeedKey = "se_seed", ResultsKey = "se_results";
        public static readonly string[] Names = { "You", "Rovers", "Athletic", "United", "City", "Wanderers" };
        // team 0 is the human; 1..5 are computer teams, weakest to strongest
        static readonly Difficulty[] Strength = { Difficulty.Professional, Difficulty.Amateur, Difficulty.Amateur, Difficulty.Professional, Difficulty.Professional, Difficulty.WorldClass };
        static readonly float[] ExpectedGoals = { 1.4f, 0.9f, 1.0f, 1.3f, 1.4f, 1.8f };

        public class Row { public string name; public int played, won, drawn, lost, goalsFor, goalsAgainst, points; public int GoalDifference => goalsFor - goalsAgainst; }

        static bool restored, active;
        static int seed;
        static readonly List<string> results = new List<string>(); // the human's results, "h-a", one per round played

        public static bool Active { get { EnsureRestored(); return active; } }
        public static int Round { get { EnsureRestored(); return results.Count; } }
        public static bool Over => Active && results.Count >= Rounds;
        public static int Seed { get { EnsureRestored(); return seed; } }
        public static IReadOnlyList<string> Results { get { EnsureRestored(); return results; } }

        /// <summary>Opponent (team index 1..5) the human plays in the given round: the weakest first, the strongest last.</summary>
        public static int OpponentIndex(int round) { return round + 1; }

        public static string CurrentOpponentName { get { EnsureRestored(); return Names[OpponentIndex(System.Math.Min(results.Count, Rounds - 1))]; } }
        public static Difficulty CurrentOpponent { get { EnsureRestored(); return Strength[OpponentIndex(System.Math.Min(results.Count, Rounds - 1))]; } }

        public static void Start() { Start(System.Environment.TickCount); }
        public static void Start(int newSeed) { restored = true; active = true; seed = newSeed; results.Clear(); Save(); }
        public static void Abandon() { restored = true; active = false; seed = 0; results.Clear(); Save(); }

        /// <summary>Records the human's match for the current round (home = the human). A draw is a draw: this is a league. Ignored when no season runs.</summary>
        public static void RecordResult(int homeGoals, int awayGoals)
        {
            if (!Active || Over) return;
            results.Add($"{homeGoals}-{awayGoals}");
            Save();
        }

        /// <summary>
        /// The pairings of one round by the circle method: team 0 stays, the other five rotate one place per round, so over five rounds every
        /// pair of teams meets exactly once. The human (team 0) always meets <see cref="OpponentIndex"/>(round). Returns 3 pairs.
        /// </summary>
        public static List<int[]> Pairings(int round)
        {
            var line = new int[Teams];
            line[0] = 0;
            for (int i = 1; i < Teams; i++) line[i] = ((i + round) % (Teams - 1)) + 1;
            var pairs = new List<int[]>();
            for (int i = 0; i < Teams / 2; i++) pairs.Add(new[] { line[i], line[Teams - 1 - i] });
            return pairs;
        }

        /// <summary>A simulated score for a computer-vs-computer match; the same seed, round and pairing always give the same score.</summary>
        public static void SimulateMatch(int seed, int round, int teamA, int teamB, out int goalsA, out int goalsB)
        {
            var rng = new RandomStream(unchecked(seed * 31 + round * 1009 + teamA * 17 + teamB * 7919));
            goalsA = Poisson(rng, ExpectedGoals[teamA]);
            goalsB = Poisson(rng, ExpectedGoals[teamB]);
        }

        static int Poisson(RandomStream rng, float mean)
        {
            float limit = Mathf.Exp(-mean), p = 1f; int k = 0;
            do { k++; p *= rng.Value; } while (p > limit && k < 12);
            return k - 1;
        }

        /// <summary>The table after the rounds played so far, best first (points, then goal difference, then goals scored).</summary>
        public static List<Row> Table()
        {
            EnsureRestored();
            var rows = new Row[Teams];
            for (int i = 0; i < Teams; i++) rows[i] = new Row { name = Names[i] };
            for (int round = 0; round < results.Count; round++)
            {
                foreach (var pair in Pairings(round))
                {
                    int a = pair[0], b = pair[1], ga, gb;
                    if (a == 0 || b == 0)
                    {
                        ParseResult(results[round], out int h, out int aw);
                        // the human is always the home side in these results, and team 0 is always first in a pair
                        ga = h; gb = aw;
                    }
                    else SimulateMatch(seed, round, a, b, out ga, out gb);
                    Apply(rows[a], ga, gb); Apply(rows[b], gb, ga);
                }
            }
            var list = new List<Row>(rows);
            list.Sort((x, y) => y.points != x.points ? y.points.CompareTo(x.points)
                : y.GoalDifference != x.GoalDifference ? y.GoalDifference.CompareTo(x.GoalDifference)
                : y.goalsFor != x.goalsFor ? y.goalsFor.CompareTo(x.goalsFor) : string.CompareOrdinal(x.name, y.name));
            return list;
        }

        static void Apply(Row r, int scored, int conceded)
        {
            r.played++; r.goalsFor += scored; r.goalsAgainst += conceded;
            if (scored > conceded) { r.won++; r.points += 3; } else if (scored == conceded) { r.drawn++; r.points++; } else r.lost++;
        }

        static void ParseResult(string s, out int home, out int away)
        {
            var parts = s.Split('-');
            home = int.Parse(parts[0]); away = int.Parse(parts[1]);
        }

        /// <summary>The human's place in the table, 1 = top.</summary>
        public static int Position()
        {
            var t = Table();
            for (int i = 0; i < t.Count; i++) if (t[i].name == Names[0]) return i + 1;
            return t.Count;
        }

        /// <summary>"1 point", "0 points", "3 points".</summary>
        public static string PointsText(int n) { return n == 1 ? "1 point" : n + " points"; }

        public static string Summary()
        {
            if (!Active) return "";
            int pos = Position();
            string place = pos == 1 ? "1st" : pos == 2 ? "2nd" : pos == 3 ? "3rd" : pos + "th";
            var me = Table().Find(r => r.name == Names[0]);
            if (Over) return pos == 1 ? $"SEASON OVER  -  CHAMPIONS ({me.points} pts)" : $"Season over: {place} with {PointsText(me.points)}";
            return $"Season: {results.Count}/{Rounds} played, {place} with {PointsText(me.points)}. Next: {CurrentOpponentName}";
        }

        /// <summary>The table as text, one line per team, for a panel or a log.</summary>
        public static string TableText()
        {
            var sb = new System.Text.StringBuilder();
            int pos = 1;
            foreach (var r in Table())
                sb.AppendLine($"{pos++}. {r.name,-10} P{r.played} W{r.won} D{r.drawn} L{r.lost}  {r.goalsFor}-{r.goalsAgainst}  {r.points} pts");
            return sb.ToString().TrimEnd();
        }

        /// <summary>Forgets the in-memory state so the next read loads from storage again, as after a fresh launch. For tests.</summary>
        public static void ForgetInMemory() { restored = false; active = false; seed = 0; results.Clear(); }

        static void EnsureRestored()
        {
            if (restored) return;
            restored = true;
            try
            {
                if (PlayerPrefs.GetInt(ActiveKey, 0) != 1) return;
                int s = PlayerPrefs.GetInt(SeedKey, 0);
                string joined = PlayerPrefs.GetString(ResultsKey, "");
                var list = joined.Length == 0 ? new string[0] : joined.Split('|');
                bool sane = list.Length <= Rounds;
                foreach (var item in list)
                {
                    var parts = item.Split('-');
                    sane &= parts.Length == 2 && int.TryParse(parts[0], out int a) && int.TryParse(parts[1], out int b) && a >= 0 && b >= 0 && a < 100 && b < 100;
                }
                if (!sane) { Debug.LogWarning("Saved season data did not make sense and was discarded."); Save(); return; }
                active = true; seed = s; results.AddRange(list);
            }
            catch { }
        }

        static void Save()
        {
            try
            {
                if (!active) { PlayerPrefs.DeleteKey(ActiveKey); PlayerPrefs.DeleteKey(SeedKey); PlayerPrefs.DeleteKey(ResultsKey); }
                else
                {
                    PlayerPrefs.SetInt(ActiveKey, 1); PlayerPrefs.SetInt(SeedKey, seed);
                    PlayerPrefs.SetString(ResultsKey, string.Join("|", results));
                }
                PlayerPrefs.Save();
            }
            catch { }
        }
    }
}
