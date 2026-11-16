using System.Net;
using NUnit.Framework;
using FootballSim.Net;

namespace FootballSim.Tests
{
    public class LanAddressTests
    {
        [Test]
        public void AnAddressWithoutAPortGetsTheDefaultPort()
        {
            Assert.IsTrue(LanAddress.TryParse(" 192.168.1.20 ", out var ep));
            Assert.AreEqual(IPAddress.Parse("192.168.1.20"), ep.Address);
            Assert.AreEqual(LanAddress.DefaultPort, ep.Port);
        }

        [Test]
        public void AnExplicitPortIsKept()
        {
            Assert.IsTrue(LanAddress.TryParse("10.0.0.5:9000", out var ep));
            Assert.AreEqual(9000, ep.Port);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        [TestCase("1")]
        [TestCase("1.2.3")]
        [TestCase("1.2.3.4.5")]
        [TestCase("256.1.1.1")]
        [TestCase("1.2.3.-4")]
        [TestCase("a.b.c.d")]
        [TestCase("example.com")]
        [TestCase("::1")]
        [TestCase("192.168.1.20:0")]
        [TestCase("192.168.1.20:70000")]
        [TestCase("192.168.1.20:abc")]
        [TestCase("192.168.1.20:")]
        public void NonsenseIsRefused(string text)
        {
            Assert.IsFalse(LanAddress.TryParse(text, out var ep));
            Assert.IsNull(ep);
        }

        [Test]
        public void OnlyPrivateRangesCountAsLan()
        {
            Assert.IsTrue(LanAddress.IsPrivate(IPAddress.Parse("10.1.2.3")));
            Assert.IsTrue(LanAddress.IsPrivate(IPAddress.Parse("172.16.0.1")));
            Assert.IsTrue(LanAddress.IsPrivate(IPAddress.Parse("172.31.255.255")));
            Assert.IsTrue(LanAddress.IsPrivate(IPAddress.Parse("192.168.0.9")));
            Assert.IsFalse(LanAddress.IsPrivate(IPAddress.Parse("172.32.0.1")));
            Assert.IsFalse(LanAddress.IsPrivate(IPAddress.Parse("8.8.8.8")));
            Assert.IsFalse(LanAddress.IsPrivate(IPAddress.Loopback));
            Assert.IsFalse(LanAddress.IsPrivate(null));
        }

        [Test]
        public void LocalAddressIsNullOrPrivateAndNeverThrows()
        {
            string a = LanAddress.LocalAddress();
            if (a != null) Assert.IsTrue(LanAddress.IsPrivate(IPAddress.Parse(a)), a);
        }
    }
}
