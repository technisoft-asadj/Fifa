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
using FootballSim.Inputs;
using FootballSim.Match;
using FootballSim.Net;
using FootballSim.Players;

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

        class StepCounter : MonoBehaviour { public int steps; void FixedUpdate() { steps++; } }

        struct Frame
        {
            public int steps; public Vector3 ball, ballVel; public double playerSum; public float time, fixedTime;
            public override string ToString() => $"steps {steps} ball {ball.x:F9},{ball.y:F9},{ball.z:F9} vel {ballVel.x:F7},{ballVel.y:F7},{ballVel.z:F7} players {playerSum:F9} t {time:F5} ft {fixedTime:F5}";
            public bool Same(Frame o) => steps == o.steps && ball == o.ball && ballVel == o.ballVel && playerSum == o.playerSum; // absolute Time.time legitimately differs between runs (reported, not compared)
        }

        static IEnumerator Trace(int seed, int frames, System.Collections.Generic.List<Frame> into)
        {
            SimRandom.Seed(seed);
            Time.captureFramerate = 60;
            yield return SceneManager.LoadSceneAsync("FootballPrototype", LoadSceneMode.Single);
            yield return null;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None)) t.ReleaseHumanControl();
            var ball = Object.FindFirstObjectByType<BallController>();
            var counter = ball.gameObject.AddComponent<StepCounter>();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                double sum = 0;
                foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None))
                    foreach (var p in t.players) sum += p.transform.position.x * 3.1 + p.transform.position.z * 1.7;
                into.Add(new Frame { steps = counter.steps, ball = ball.Position, ballVel = ball.Body.linearVelocity, playerSum = sum, time = Time.time, fixedTime = Time.fixedTime });
            }
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator FirstDivergenceIsLocated()
        {
            Assume.That(File.Exists(Path.Combine(Application.dataPath, "..", "run_balance.flag")), "create run_balance.flag to run");
            float originalFixed = Time.fixedDeltaTime;
            Time.fixedDeltaTime = 1f / 60f;
            var sb = new StringBuilder();
            sb.AppendLine("First-divergence probe: seed 42, fixedDeltaTime 1/60, 1200 frames, three runs (first is a warm-up).");
            var runs = new System.Collections.Generic.List<Frame>[3];
            for (int r = 0; r < 3; r++) { runs[r] = new System.Collections.Generic.List<Frame>(); yield return Trace(42, 1200, runs[r]); }
            for (int pair = 1; pair <= 2; pair++)
            {
                var a = runs[pair - 1 == 0 ? 1 : 1]; var b = runs[pair == 1 ? 2 : 0];
                sb.AppendLine(pair == 1 ? "== run 2 vs run 3 (both after warm-up)" : "== run 2 vs run 1 (warm-up)");
                int first = -1;
                for (int i = 0; i < a.Count; i++) if (!a[i].Same(b[i])) { first = i; break; }
                if (first < 0) { sb.AppendLine("identical for all frames"); continue; }
                sb.AppendLine($"first differing frame: {first}");
                for (int i = Mathf.Max(0, first - 2); i <= Mathf.Min(a.Count - 1, first + 2); i++)
                { sb.AppendLine($" f{i} A: {a[i]}"); sb.AppendLine($" f{i} B: {b[i]}"); }
            }
            Time.fixedDeltaTime = originalFixed;
            File.WriteAllText(Path.Combine(Application.dataPath, "..", "Documentation", "determinism_first_divergence.txt"), sb.ToString());
            Assert.Pass(sb.ToString());
        }

        static System.Collections.Generic.List<PlayerController> AllPlayers()
        {
            var teams = new System.Collections.Generic.List<TeamController>(Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None));
            teams.Sort((x, y) => x.teamId.CompareTo(y.teamId));
            var all = new System.Collections.Generic.List<PlayerController>();
            foreach (var t in teams) all.AddRange(t.players);
            return all;
        }

        /// <summary>mode 0: the AI plays and every player's input is recorded. mode 1: the AI is switched off and the recorded inputs are played back.</summary>
        static IEnumerator Play(int seed, int frames, int mode, System.Collections.Generic.List<RecordingInput> recordings, System.Collections.Generic.List<Frame> into)
        {
            SimRandom.Seed(seed);
            Time.captureFramerate = 60;
            yield return SceneManager.LoadSceneAsync("FootballPrototype", LoadSceneMode.Single);
            yield return null;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None)) t.ReleaseHumanControl();
            var players = AllPlayers();
            int start = Time.frameCount;
            for (int i = 0; i < players.Count; i++)
            {
                if (mode == 0) { var rec = new RecordingInput(players[i].Input, start); recordings.Add(rec); players[i].SetInput(rec, false); }
                else players[i].SetInput(new ReplayInput(recordings[i].Frames, start), false);
            }
            if (mode == 1) foreach (var brain in Object.FindObjectsByType<PlayerAI>(FindObjectsSortMode.None)) brain.enabled = false;
            var ball = Object.FindFirstObjectByType<BallController>();
            var counter = ball.gameObject.AddComponent<StepCounter>();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                double sum = 0;
                foreach (var p in players) sum += p.transform.position.x * 3.1 + p.transform.position.z * 1.7;
                into.Add(new Frame { steps = counter.steps, ball = ball.Position, ballVel = ball.Body.linearVelocity, playerSum = sum, time = Time.time, fixedTime = Time.fixedTime });
            }
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator ReplayingRecordedInputsReproducesTheMatch()
        {
            Assume.That(File.Exists(Path.Combine(Application.dataPath, "..", "run_balance.flag")), "create run_balance.flag to run");
            float originalFixed = Time.fixedDeltaTime;
            Time.fixedDeltaTime = 1f / 60f;
            const int frames = 900;
            var recordings = new System.Collections.Generic.List<RecordingInput>();
            var original = new System.Collections.Generic.List<Frame>();
            var replay = new System.Collections.Generic.List<Frame>();
            yield return Play(42, frames, 0, recordings, original);
            int recordedActions = 0;
            foreach (var r in recordings) foreach (var f in r.Frames) if (f.pass || f.shootReleased || f.tackle) recordedActions++;
            yield return Play(42, frames, 1, recordings, replay);
            Time.fixedDeltaTime = originalFixed;

            var sb = new StringBuilder();
            sb.AppendLine($"Replay probe: seed 42, 1/60 step, {frames} frames. Recorded {recordings.Count} players, {recordedActions} one-shot actions (pass/shot/tackle).");
            int first = -1;
            for (int i = 0; i < original.Count && i < replay.Count; i++) if (!original[i].Same(replay[i])) { first = i; break; }
            if (first < 0) sb.AppendLine("Replay matched the original in every frame (ball, ball velocity, players, physics steps).");
            else
            {
                sb.AppendLine($"first differing frame: {first}");
                for (int i = Mathf.Max(0, first - 1); i <= Mathf.Min(original.Count - 1, first + 1); i++)
                { sb.AppendLine($" f{i} original: {original[i]}"); sb.AppendLine($" f{i} replay:   {replay[i]}"); }
            }
            File.WriteAllText(Path.Combine(Application.dataPath, "..", "Documentation", "determinism_replay.txt"), sb.ToString());
            Assert.Greater(recordedActions, 0, "nothing was recorded; the probe is broken");
            Assert.AreEqual(-1, first, sb.ToString());
        }

        /// <summary>Server snapshots every 3rd frame (20 Hz) go through encode, decode and the interpolator; the client draws 6 frames (100 ms) in the past. Measures how far that picture is from the truth at the same moment.</summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator InterpolatedClientViewStaysCloseToTheServer()
        {
            Assume.That(File.Exists(Path.Combine(Application.dataPath, "..", "run_balance.flag")), "create run_balance.flag to run");
            float originalFixed = Time.fixedDeltaTime;
            Time.fixedDeltaTime = 1f / 60f;
            SimRandom.Seed(42);
            Time.captureFramerate = 60;
            yield return SceneManager.LoadSceneAsync("FootballPrototype", LoadSceneMode.Single);
            yield return null;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None)) t.ReleaseHumanControl();
            var players = AllPlayers();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();

            const int frames = 900, snapshotEvery = 3, delayFrames = 6;
            var truth = new System.Collections.Generic.List<WorldSnapshot>();
            var client = new SnapshotInterpolator();
            var wire = new byte[NetCodec.SnapshotBytes];
            var live = new WorldSnapshot(); var received = new WorldSnapshot(); var view = new WorldSnapshot();
            double sumPlayer = 0, sumBall = 0; float maxPlayer = 0f, maxBall = 0f; int samples = 0; long bytes = 0; int packets = 0;
            for (int f = 0; f < frames; f++)
            {
                yield return null;
                var now = new WorldSnapshot();
                SnapshotCapture.Fill(now, (uint)f, match, ball, players);
                truth.Add(now);
                if (f % snapshotEvery == 0)
                {
                    SnapshotCapture.Fill(live, (uint)f, match, ball, players);
                    bytes += NetCodec.Write(live, wire); packets++;
                    NetCodec.Read(wire, 0, received);
                    client.Add(received);
                }
                int shown = f - delayFrames;
                if (shown < snapshotEvery * 2) continue;
                client.Sample(shown, view);
                var real = truth[shown];
                for (int i = 0; i < WorldSnapshot.PlayerCount; i++)
                {
                    float e = new Vector2(view.players[i].x - real.players[i].x, view.players[i].z - real.players[i].z).magnitude;
                    sumPlayer += e; if (e > maxPlayer) maxPlayer = e;
                }
                float be = (view.ballPos - real.ballPos).magnitude;
                sumBall += be; if (be > maxBall) maxBall = be;
                samples++;
            }
            Time.captureFramerate = 0;
            Time.fixedDeltaTime = originalFixed;

            var sb = new StringBuilder();
            sb.AppendLine($"Interpolation probe: AI vs AI, seed 42, {frames} frames at 60 fps, snapshot every {snapshotEvery} frames (20 Hz), client draws {delayFrames} frames (100 ms) behind.");
            sb.AppendLine($"Sent {packets} snapshots, {bytes} bytes payload ({bytes / (frames / 60.0):F0} B/s before UDP/IP headers).");
            sb.AppendLine($"Player position error vs the server at the same moment: mean {sumPlayer / (samples * WorldSnapshot.PlayerCount):F4} m, max {maxPlayer:F3} m.");
            sb.AppendLine($"Ball position error: mean {sumBall / samples:F4} m, max {maxBall:F3} m.");
            File.WriteAllText(Path.Combine(Application.dataPath, "..", "Documentation", "interpolation_error.txt"), sb.ToString());
            Assert.Less(maxPlayer, 3f, sb.ToString());
        }
    }
}
