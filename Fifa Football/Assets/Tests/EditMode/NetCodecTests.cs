using NUnit.Framework;
using UnityEngine;
using FootballSim.Inputs;
using FootballSim.Net;

namespace FootballSim.Tests
{
    public class NetCodecTests
    {
        static WorldSnapshot Sample()
        {
            var s = new WorldSnapshot { tick = 123456, ballPos = new Vector3(-12.34f, 0.11f, 48.76f), ballVel = new Vector3(3.21f, -0.5f, -20.04f), homeScore = 3, awayScore = 1, clockTenths = 873 };
            for (int i = 0; i < WorldSnapshot.PlayerCount; i++)
                s.players[i] = new PlayerState { x = -30f + i * 2.7f, z = 51f - i * 4.6f, yaw = i * 16.3f, speed = i * 0.37f, flags = (byte)(i % 4) };
            return s;
        }

        [Test]
        public void SnapshotRoundTripsWithinTheQuantisationStep()
        {
            var a = Sample(); var b = new WorldSnapshot();
            var buf = new byte[NetCodec.SnapshotBytes];
            int n = NetCodec.Write(a, buf);
            Assert.AreEqual(NetCodec.SnapshotBytes, n);
            NetCodec.Read(buf, 0, b);
            Assert.AreEqual(a.tick, b.tick);
            Assert.Less((a.ballPos - b.ballPos).magnitude, 0.01f);
            Assert.Less((a.ballVel - b.ballVel).magnitude, 0.01f);
            Assert.AreEqual(a.homeScore, b.homeScore); Assert.AreEqual(a.awayScore, b.awayScore); Assert.AreEqual(a.clockTenths, b.clockTenths);
            for (int i = 0; i < WorldSnapshot.PlayerCount; i++)
            {
                Assert.AreEqual(a.players[i].x, b.players[i].x, 0.006f);
                Assert.AreEqual(a.players[i].z, b.players[i].z, 0.006f);
                Assert.AreEqual(a.players[i].speed, b.players[i].speed, 0.06f);
                Assert.AreEqual(a.players[i].flags, b.players[i].flags);
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(a.players[i].yaw, b.players[i].yaw)), 0.75f);
            }
        }

        [Test]
        public void SnapshotSizeFitsTheBandwidthTarget()
        {
            // plan.md target: under 20 KB/s per client at a 20-30 Hz server tick. 28 bytes = IPv4 + UDP headers per packet.
            Assert.Less(NetCodec.SnapshotBytes, 200);
            float bytesPerSecondAt30Hz = (NetCodec.SnapshotBytes + 28) * 30f;
            Assert.Less(bytesPerSecondAt30Hz, 20000f);
        }

        [Test]
        public void InputFrameRoundTrips()
        {
            var f = new InputFrame { move = new Vector2(0.5f, -1f), moveIsWorldSpace = true, sprint = true, pass = true, tackle = true, aim = new Vector3(1f, 0f, 1f).normalized, power = 0.55f };
            var buf = new byte[NetCodec.InputBytes];
            Assert.AreEqual(NetCodec.InputBytes, NetCodec.Write(f, buf));
            var g = NetCodec.ReadInput(buf);
            Assert.AreEqual(f.move.x, g.move.x, 1f / 127f); Assert.AreEqual(f.move.y, g.move.y, 1f / 127f);
            Assert.IsTrue(g.moveIsWorldSpace && g.sprint && g.pass && g.tackle);
            Assert.IsFalse(g.shootHeld || g.shootReleased || g.switchPressed);
            Assert.Less(Vector3.Angle(f.aim, g.aim), 1.5f);
            Assert.AreEqual(f.power, g.power, 1f / 255f);
        }

        [Test]
        public void InputWithoutAimStaysWithoutAim()
        {
            var buf = new byte[NetCodec.InputBytes];
            NetCodec.Write(new InputFrame { move = new Vector2(1f, 0f) }, buf);
            Assert.AreEqual(Vector3.zero, NetCodec.ReadInput(buf).aim);
        }

        [Test]
        public void InputPacketRoundTripsWithIndexTokenAndSequence()
        {
            var buf = new byte[NetCodec.InputPacketBytes];
            var f = new InputFrame { move = new Vector2(0f, 1f), sprint = true };
            Assert.AreEqual(NetCodec.InputPacketBytes, NetCodec.WriteInputPacket(buf, 9, 0xDEADBEEF, 65535, f));
            Assert.IsTrue(NetCodec.TryReadInputPacket(buf, buf.Length, out int idx, out uint token, out ushort seq, out var g));
            Assert.AreEqual(9, idx); Assert.AreEqual(0xDEADBEEF, token); Assert.AreEqual((ushort)65535, seq);
            Assert.IsTrue(g.sprint); Assert.AreEqual(1f, g.move.y, 0.01f);
        }

        [Test]
        public void MalformedInputPacketsAreRefused()
        {
            var buf = new byte[NetCodec.InputPacketBytes];
            NetCodec.WriteInputPacket(buf, 1, 1, 1, default);
            Assert.IsFalse(NetCodec.TryReadInputPacket(buf, buf.Length - 1, out _, out _, out _, out _), "wrong length");
            buf[0] = 99;
            Assert.IsFalse(NetCodec.TryReadInputPacket(buf, buf.Length, out _, out _, out _, out _), "wrong type");
        }

        [Test]
        public void SequenceComparisonSurvivesTheCounterWrappingAround()
        {
            Assert.IsTrue(NetCodec.IsNewer(5, 4));
            Assert.IsFalse(NetCodec.IsNewer(4, 5));
            Assert.IsFalse(NetCodec.IsNewer(5, 5));
            Assert.IsTrue(NetCodec.IsNewer(2, 65534), "2 comes after 65534 once the counter wraps");
            Assert.IsFalse(NetCodec.IsNewer(65534, 2));
        }

        [Test]
        public void RemoteInputIgnoresOldAndRepeatedSequenceNumbers()
        {
            var r = new FootballSim.Net.RemoteInput(1234);
            Assert.IsTrue(r.ApplySequenced(new InputFrame { pass = true }, 10));
            Assert.IsFalse(r.ApplySequenced(new InputFrame { tackle = true }, 10), "repeat");
            Assert.IsFalse(r.ApplySequenced(new InputFrame { tackle = true }, 9), "older");
            Assert.IsTrue(r.PassPressed);
            Assert.IsFalse(r.PassPressed, "a one-shot is read once");
            Assert.IsFalse(r.TacklePressed, "the rejected frame's tackle must not leak in");
            Assert.AreEqual(1, r.Applied);
        }
    }
}
