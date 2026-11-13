using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FootballSim.Net;

namespace FootballSim.Tests
{
    public class NetworkConditionerTests
    {
        [Test]
        public void PacketsArriveAfterTheBaseDelayAndNotBefore()
        {
            var c = new NetworkConditioner(1, 0f, 50f, 0f);
            c.Send(new byte[] { 7 }, 1, 1000);
            Assert.AreEqual(0, c.Poll(1049).Count);
            var got = c.Poll(1050);
            Assert.AreEqual(1, got.Count);
            Assert.AreEqual(7, got[0][0]);
            Assert.AreEqual(0, c.Poll(2000).Count);
        }

        [Test]
        public void LossRateDropsRoughlyThatShareOfPackets()
        {
            var c = new NetworkConditioner(5, 0.2f, 0f, 0f);
            for (int i = 0; i < 2000; i++) c.Send(new byte[] { 1 }, 1, 0);
            Assert.AreEqual(2000, c.Sent);
            Assert.That(c.Lost, Is.InRange(300, 500));
            Assert.AreEqual(2000 - c.Lost, c.Poll(1).Count);
        }

        [Test]
        public void JitterLargerThanTheSendGapReordersPackets()
        {
            var c = new NetworkConditioner(3, 0f, 10f, 200f);
            for (int i = 0; i < 50; i++) c.Send(new byte[] { (byte)i }, 1, i * 10.0);
            var got = c.Poll(10000);
            Assert.AreEqual(50, got.Count);
            bool reordered = false;
            for (int i = 1; i < got.Count; i++) if (got[i][0] < got[i - 1][0]) reordered = true;
            Assert.IsTrue(reordered);
        }

        [Test]
        public void SameSeedGivesTheSameLosses()
        {
            int Run()
            {
                var c = new NetworkConditioner(9, 0.3f, 0f, 0f);
                for (int i = 0; i < 500; i++) c.Send(new byte[] { 1 }, 1, 0);
                return c.Lost;
            }
            Assert.AreEqual(Run(), Run());
        }

        // ---- what a bad network does to the picture the client draws ----

        // Server truth: a player who runs, turns and stops, 60 ticks per second, snapshot every 3rd tick.
        static float TruthX(int tick)
        {
            float t = tick / 60f;
            return t < 4f ? 6f * t : (t < 6f ? 24f + 4f * (t - 4f) : 32f);
        }

        static float WorstErrorMetres(float loss, float delayMs, float jitterMs, int renderDelayTicks, out int unusable)
        {
            var net = new NetworkConditioner(11, loss, delayMs, jitterMs);
            var client = new SnapshotInterpolator();
            var snap = new WorldSnapshot(); var rx = new WorldSnapshot(); var view = new WorldSnapshot();
            var wire = new byte[NetCodec.SnapshotBytes];
            float worst = 0f; unusable = 0;
            for (int tick = 0; tick < 600; tick++)
            {
                double nowMs = tick * (1000.0 / 60.0);
                if (tick % 3 == 0)
                {
                    snap.tick = (uint)tick;
                    for (int i = 0; i < WorldSnapshot.PlayerCount; i++) snap.players[i] = new PlayerState { x = TruthX(tick), z = 0f };
                    NetCodec.Write(snap, wire);
                    net.Send(wire, wire.Length, nowMs);
                }
                foreach (var p in net.Poll(nowMs)) { NetCodec.Read(p, 0, rx); client.Add(rx); }
                int shown = tick - renderDelayTicks;
                if (shown < 30 || client.Count < 2) continue;
                client.Sample(shown, view);
                if (client.NewestTick < (uint)shown) { unusable++; } // the buffer ran dry: the view is stuck on the newest snapshot
                worst = Mathf.Max(worst, Mathf.Abs(view.players[0].x - TruthX(shown)));
            }
            return worst;
        }

        [Test]
        public void CleanNetworkWithOneHundredMillisecondsOfRenderDelayIsAccurate()
        {
            Assert.Less(WorstErrorMetres(0f, 30f, 0f, 6, out _), 0.3f);
        }

        [Test]
        public void FivePercentLossAndJitterStillLooksRightWithEnoughRenderDelay()
        {
            float worst = WorstErrorMetres(0.05f, 40f, 40f, 12, out int unusable); // 200 ms behind
            Assert.Less(worst, 0.6f, $"worst error {worst} m, buffer ran dry {unusable} times");
        }

        [Test]
        public void TooLittleRenderDelayRunsTheBufferDryUnderJitter()
        {
            WorstErrorMetres(0.05f, 40f, 80f, 2, out int unusable); // 33 ms behind with 80 ms jitter
            Assert.Greater(unusable, 0);
        }
    }
}
