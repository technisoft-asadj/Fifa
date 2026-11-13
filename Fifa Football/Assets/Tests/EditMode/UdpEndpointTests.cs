using System.Diagnostics;
using System.Net;
using NUnit.Framework;
using UnityEngine;
using FootballSim.Inputs;
using FootballSim.Net;

namespace FootballSim.Tests
{
    public class UdpEndpointTests
    {
        static bool WaitFor(UdpEndpoint e, byte[] buf, out int n, int ms = 1000)
        {
            var sw = Stopwatch.StartNew();
            EndPoint from;
            n = 0;
            while (sw.ElapsedMilliseconds < ms) { if (e.TryReceive(buf, out n, out from)) return true; }
            return false;
        }

        [Test]
        public void NothingWaitingMeansNothingReceived()
        {
            using (var a = new UdpEndpoint())
            {
                var buf = new byte[256];
                EndPoint from;
                Assert.IsFalse(a.TryReceive(buf, out int n, out from));
            }
        }

        [Test]
        public void SnapshotCrossesARealSocketIntact()
        {
            using (var server = new UdpEndpoint())
            using (var client = new UdpEndpoint())
            {
                var snap = new WorldSnapshot { tick = 4242, ballPos = new Vector3(1.5f, 0.11f, -20f), homeScore = 2, clockTenths = 300 };
                for (int i = 0; i < WorldSnapshot.PlayerCount; i++) snap.players[i] = new PlayerState { x = i, z = -i, yaw = i * 10f };
                var wire = new byte[NetCodec.SnapshotBytes];
                int len = NetCodec.Write(snap, wire);
                Assert.AreEqual(len, server.Send(wire, len, client.LocalEndPoint));

                var rx = new byte[1500];
                Assert.IsTrue(WaitFor(client, rx, out int n));
                Assert.AreEqual(NetCodec.SnapshotBytes, n);
                var back = new WorldSnapshot();
                NetCodec.Read(rx, 0, back);
                Assert.AreEqual(4242u, back.tick);
                Assert.AreEqual(2, back.homeScore);
                Assert.AreEqual(21f, back.players[21].x, 0.011f);
            }
        }

        [Test]
        public void InputFrameTravelsFromClientToServer()
        {
            using (var server = new UdpEndpoint())
            using (var client = new UdpEndpoint())
            {
                var wire = new byte[NetCodec.InputBytes];
                NetCodec.Write(new InputFrame { move = new Vector2(1f, 0f), sprint = true, pass = true }, wire);
                client.Send(wire, wire.Length, server.LocalEndPoint);
                var rx = new byte[64];
                Assert.IsTrue(WaitFor(server, rx, out int n));
                Assert.AreEqual(NetCodec.InputBytes, n);
                var f = NetCodec.ReadInput(rx);
                Assert.IsTrue(f.sprint && f.pass);
                Assert.AreEqual(1f, f.move.x, 0.01f);
            }
        }

        [Test]
        public void ServerCanReplyToWhoeverSentToIt()
        {
            using (var server = new UdpEndpoint())
            using (var client = new UdpEndpoint())
            {
                client.Send(new byte[] { 9 }, 1, server.LocalEndPoint);
                var rx = new byte[16];
                EndPoint from = null;
                var sw = Stopwatch.StartNew();
                int n = 0;
                while (sw.ElapsedMilliseconds < 1000 && !server.TryReceive(rx, out n, out from)) { }
                Assert.IsNotNull(from);
                server.Send(new byte[] { 7 }, 1, (IPEndPoint)from);
                Assert.IsTrue(WaitFor(client, rx, out n));
                Assert.AreEqual(7, rx[0]);
            }
        }
    }
}
