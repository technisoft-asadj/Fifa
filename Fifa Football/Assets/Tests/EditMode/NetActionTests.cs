using FootballSim.Net;
using NUnit.Framework;

namespace FootballSim.Tests
{
    public class NetActionTests
    {
        [Test]
        public void AnActionPacksAndUnpacksWithItsSideAndCounter()
        {
            for (int action = 0; action <= 11; action++)
                foreach (bool right in new[] { true, false })
                    for (int serial = 0; serial < 8; serial++)
                    {
                        byte b = NetAction.Pack(action, right, serial);
                        Assert.AreEqual(action, NetAction.Action(b));
                        Assert.AreEqual(right, NetAction.Right(b));
                        Assert.AreEqual(serial, NetAction.Serial(b));
                    }
        }

        [Test]
        public void TheCounterWrapsAtEightWithoutTouchingTheAction()
        {
            Assert.AreEqual(1, NetAction.Serial(NetAction.Pack(5, true, 9)));
            Assert.AreEqual(5, NetAction.Action(NetAction.Pack(5, true, 9)));
        }

        [Test]
        public void TheActionByteSurvivesTheWireForEveryPlayer()
        {
            var s = new WorldSnapshot { tick = 7 };
            for (int i = 0; i < WorldSnapshot.PlayerCount; i++)
                s.players[i] = new PlayerState { x = i, z = -i, speed = 1f, act = NetAction.Pack(1 + i % 11, i % 2 == 0, i % 8) };
            var buf = new byte[NetCodec.SnapshotBytes];
            Assert.AreEqual(NetCodec.SnapshotBytes, NetCodec.Write(s, buf));
            var back = new WorldSnapshot();
            NetCodec.Read(buf, 0, back);
            for (int i = 0; i < WorldSnapshot.PlayerCount; i++) Assert.AreEqual(s.players[i].act, back.players[i].act, $"player {i}");
        }

        [Test]
        public void ActionsCostOneByteMorePerPlayerAndStayWithinTheBandwidthBudget()
        {
            Assert.AreEqual(4 + 6 + 6 + WorldSnapshot.PlayerCount * 8 + 4, NetCodec.SnapshotBytes);
            Assert.Less((NetCodec.SnapshotBytes + 28) * 30f, 20000f, "under 20 KB/s per client at 30 snapshots a second");
        }

        [Test]
        public void ABlendedSnapshotKeepsTheEarlierActionUntilTheLaterOneIsReached()
        {
            var a = new WorldSnapshot { tick = 10 }; var b = new WorldSnapshot { tick = 12 };
            a.players[3].act = NetAction.Pack(2, true, 1);
            b.players[3].act = NetAction.Pack(5, false, 2);
            var it = new SnapshotInterpolator();
            it.Add(a); it.Add(b);
            var r = new WorldSnapshot();
            it.Sample(11f, r);
            Assert.AreEqual(a.players[3].act, r.players[3].act, "half way: still the earlier one");
            it.Sample(12f, r);
            Assert.AreEqual(b.players[3].act, r.players[3].act, "at the later snapshot: the new action");
        }
    }
}
