using NUnit.Framework;
using UnityEngine;
using FootballSim.Net;

namespace FootballSim.Tests
{
    public class SnapshotInterpolatorTests
    {
        static WorldSnapshot Snap(uint tick, float x, float yaw = 0f, byte home = 0)
        {
            var s = new WorldSnapshot { tick = tick, ballPos = new Vector3(x, 0.11f, 0f), homeScore = home };
            for (int i = 0; i < WorldSnapshot.PlayerCount; i++) s.players[i] = new PlayerState { x = x + i, z = i, yaw = yaw };
            return s;
        }

        [Test]
        public void EmptyBufferHasNothingToSample()
        {
            Assert.IsFalse(new SnapshotInterpolator().Sample(5f, new WorldSnapshot()));
        }

        [Test]
        public void MidpointIsHalfwayBetweenTwoSnapshots()
        {
            var buf = new SnapshotInterpolator(); buf.Add(Snap(10, 0f)); buf.Add(Snap(12, 10f));
            var r = new WorldSnapshot();
            Assert.IsTrue(buf.Sample(11f, r));
            Assert.AreEqual(5f, r.ballPos.x, 1e-4f);
            Assert.AreEqual(5f + 3, r.players[3].x, 1e-4f);
            Assert.AreEqual(3f, r.players[3].z, 1e-4f);
        }

        [Test]
        public void SamplingOutsideTheHeldRangeClampsToTheEnds()
        {
            var buf = new SnapshotInterpolator(); buf.Add(Snap(10, 0f)); buf.Add(Snap(12, 10f));
            var r = new WorldSnapshot();
            buf.Sample(3f, r); Assert.AreEqual(0f, r.ballPos.x, 1e-4f);
            buf.Sample(99f, r); Assert.AreEqual(10f, r.ballPos.x, 1e-4f);
        }

        [Test]
        public void YawTakesTheShortWayAroundTheCircle()
        {
            var buf = new SnapshotInterpolator(); buf.Add(Snap(0, 0f, 350f)); buf.Add(Snap(2, 0f, 10f));
            var r = new WorldSnapshot();
            buf.Sample(1f, r);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(r.players[0].yaw, 0f)), 0.01f);
        }

        [Test]
        public void LateOrDuplicateSnapshotsAreIgnored()
        {
            var buf = new SnapshotInterpolator();
            Assert.IsTrue(buf.Add(Snap(10, 0f)));
            Assert.IsTrue(buf.Add(Snap(12, 10f)));
            Assert.IsFalse(buf.Add(Snap(11, 99f)));
            Assert.IsFalse(buf.Add(Snap(12, 99f)));
            Assert.AreEqual(2, buf.Count);
            var r = new WorldSnapshot(); buf.Sample(11f, r);
            Assert.AreEqual(5f, r.ballPos.x, 1e-4f);
        }

        [Test]
        public void ScoreChangesAtTheLaterSnapshotNotInBetween()
        {
            var buf = new SnapshotInterpolator(); buf.Add(Snap(10, 0f, 0f, 0)); buf.Add(Snap(12, 0f, 0f, 1));
            var r = new WorldSnapshot();
            buf.Sample(11.9f, r); Assert.AreEqual(0, r.homeScore);
            buf.Sample(12f, r); Assert.AreEqual(1, r.homeScore);
        }

        [Test]
        public void OnlyTheLastThirtyTwoSnapshotsAreKeptButSamplingStillWorks()
        {
            var buf = new SnapshotInterpolator();
            for (uint t = 0; t < 100; t++) buf.Add(Snap(t, t));
            var r = new WorldSnapshot();
            buf.Sample(90.5f, r);
            Assert.AreEqual(90.5f, r.ballPos.x, 1e-3f);
            buf.Sample(5f, r); // older than anything held: clamps to the oldest kept (tick 68)
            Assert.AreEqual(68f, r.ballPos.x, 1e-3f);
        }
    }
}
