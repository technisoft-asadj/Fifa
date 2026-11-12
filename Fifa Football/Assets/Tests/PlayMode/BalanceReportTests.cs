using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FootballSim.Core;
using FootballSim.FootballAI;
using FootballSim.Match;

namespace FootballSim.Tests
{
    /// <summary>
    /// Not a pass/fail check: plays full AI-vs-AI matches and writes the match statistics to a file so game balance
    /// (goals, fouls, cards, offsides ...) can be read as numbers. Skipped unless run_balance.flag exists.
    /// </summary>
    public class BalanceReportTests
    {
        [UnityTest]
        public IEnumerator PlayAiMatchesAndWriteReport()
        {
            // Slow (~75 s per match), so it only runs when a file named run_balance.flag exists in the project root.
            // (Unity's runner would not start a test marked [Explicit] when selected by name.)
            Assume.That(File.Exists(Path.Combine(Application.dataPath, "..", "run_balance.flag")), "create run_balance.flag to run");

            const int matches = 2;
            var sb = new StringBuilder();
            sb.AppendLine("AI vs AI, 5-minute matches (= 90 simulated minutes), 4x speed, difficulty " + AiTuning.CurrentDifficulty);
            for (int m = 0; m < matches; m++)
            {
                yield return SceneManager.LoadSceneAsync("FootballPrototype", LoadSceneMode.Single);
                yield return null;
                var hud = Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>();
                foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None)) t.ReleaseHumanControl();
                hud.BeginMatch();
                var match = Object.FindFirstObjectByType<MatchManager>();
                bool done = false; match.FullTime += () => done = true;
                Time.timeScale = 4f;
                float guard = 0f;
                while (!done && guard < 150f) { guard += Time.unscaledDeltaTime; yield return null; }
                Time.timeScale = 1f;
                var s = match.Stats;
                sb.AppendLine($"Match {m + 1}: finished={done}  score {match.HomeScore}-{match.AwayScore}  " +
                              $"shots {s.shots[0]}({s.shotsOnTarget[0]})-{s.shots[1]}({s.shotsOnTarget[1]})  passes {s.passes[0]}-{s.passes[1]}  " +
                              $"poss {s.PossessionPercent(0)}-{s.PossessionPercent(1)}  corners {s.corners[0]}-{s.corners[1]}  offsides {s.offsides[0]}-{s.offsides[1]}  " +
                              $"fouls {match.HomeFouls}-{match.AwayFouls}  yellows {match.HomeYellows}-{match.AwayYellows}  reds {match.HomeReds}-{match.AwayReds}");
                foreach (var g in match.Goals) sb.AppendLine("   " + g);
            }
            string path = Path.Combine(Application.dataPath, "..", "Documentation", "balance_report.txt");
            File.WriteAllText(path, sb.ToString());
            Assert.Pass(sb.ToString());
        }
    }
}
