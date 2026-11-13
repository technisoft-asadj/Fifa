using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FootballSim.Ball;
using FootballSim.Core;
using FootballSim.FootballAI;
using FootballSim.Match;

namespace FootballSim.Tests
{
    /// <summary>
    /// Evidence for the networking decision, not a pass/fail check: plays the same AI-vs-AI start twice from one seed with a
    /// fixed frame time and writes down whether the two runs end in exactly the same state. Skipped unless run_balance.flag exists.
    /// </summary>
    public class DeterminismProbeTests
    {
        struct Snapshot
        {
            public Vector3 ball; public int home, away, passes0, passes1; public double playerSum; public float clock;
        }

        static IEnumerator Run(int seed, int frames, System.Action<Snapshot> done)
        {
            SimRandom.Seed(seed);
            Time.captureFramerate = 60; // every frame lasts exactly 1/60 s, whatever the real speed
            yield return SceneManager.LoadSceneAsync("FootballPrototype", LoadSceneMode.Single);
            yield return null;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None)) t.ReleaseHumanControl();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            for (int i = 0; i < frames; i++) yield return null;
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            double sum = 0;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None))
                foreach (var p in t.players) sum += p.transform.position.x * 3.1 + p.transform.position.z * 1.7;
            Time.captureFramerate = 0;
            done(new Snapshot
            {
                ball = ball.transform.position, home = match.HomeScore, away = match.AwayScore,
                passes0 = match.Stats.passes[0], passes1 = match.Stats.passes[1], playerSum = sum, clock = match.GameMinute
            });
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator SameSeedTwiceGivesTheSameMatch()
        {
            Assume.That(File.Exists(Path.Combine(Application.dataPath, "..", "run_balance.flag")), "create run_balance.flag to run");

            var sb = new StringBuilder();
            sb.AppendLine("Determinism probe: AI vs AI from seed 42, captureFramerate 60, each case run twice and compared.");
            float originalFixed = Time.fixedDeltaTime;
            foreach (bool alignedPhysics in new[] { false, true })
            {
                // aligned = one physics step per rendered frame (fixedDeltaTime = 1/60), so step timing cannot differ between runs
                Time.fixedDeltaTime = alignedPhysics ? 1f / 60f : originalFixed;
                sb.AppendLine(alignedPhysics ? "-- fixedDeltaTime = 1/60 (one physics step per frame)" : $"-- default fixedDeltaTime = {originalFixed}");
                foreach (int frames in new[] { 2, 60, 300, 900 })
                {
                    Snapshot a = default, b = default;
                    yield return Run(42, frames, s => a = s);
                    yield return Run(42, frames, s => b = s);
                    bool same = a.ball == b.ball && a.home == b.home && a.away == b.away && a.passes0 == b.passes0 && a.passes1 == b.passes1
                                && a.playerSum == b.playerSum && Mathf.Approximately(a.clock, b.clock);
                    sb.AppendLine($"After {frames} frames: identical = {same}");
                    sb.AppendLine($"   run A: ball {a.ball:F4} score {a.home}-{a.away} passes {a.passes0}-{a.passes1} playerSum {a.playerSum:F5} minute {a.clock:F3}");
                    sb.AppendLine($"   run B: ball {b.ball:F4} score {b.home}-{b.away} passes {b.passes0}-{b.passes1} playerSum {b.playerSum:F5} minute {b.clock:F3}");
                }
            }
            Time.fixedDeltaTime = originalFixed;
            string path = Path.Combine(Application.dataPath, "..", "Documentation", "determinism_report.txt");
            File.WriteAllText(path, sb.ToString());
            Assert.Pass(sb.ToString());
        }
    }
}
