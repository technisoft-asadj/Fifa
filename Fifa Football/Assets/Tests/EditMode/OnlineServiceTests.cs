using System;
using NUnit.Framework;
using FootballSim.Net;

namespace FootballSim.Tests
{
    /// <summary>The parts of internet play that need no connection to Unity's services: how typed room codes are cleaned up and how failures are worded.</summary>
    public class OnlineServiceTests
    {
        [Test]
        public void RoomCodesAreTrimmedUpperCasedAndStrippedOfSpacesAndDashes()
        {
            Assert.AreEqual("ABC123", OnlineService.NormaliseCode("  abc-123 "));
            Assert.AreEqual("K7QX2M", OnlineService.NormaliseCode("k7 qx 2m"));
        }

        [Test]
        public void AnEmptyCodeStaysEmpty()
        {
            Assert.AreEqual("", OnlineService.NormaliseCode(null));
            Assert.AreEqual("", OnlineService.NormaliseCode("   "));
            Assert.AreEqual("", OnlineService.NormaliseCode("--"));
        }

        [Test]
        public void ErrorsAreWordedForPlayers()
        {
            Assert.AreEqual("Type the room code first.", OnlineService.Describe(new ArgumentException("Type the room code first.")));
            StringAssert.Contains("boom", OnlineService.Describe(new InvalidOperationException("boom")));
            var wrapped = new AggregateException(new ArgumentException("inner message"));
            Assert.AreEqual("inner message", OnlineService.Describe(wrapped));
        }

        [Test]
        public void TheRelayHostAddressIsAStandInNotARealAddress()
        {
            Assert.IsFalse(System.Net.IPAddress.IsLoopback(RelayTransport.HostAddress.Address));
            Assert.AreEqual(10, RelayTransport.HostAddress.Address.GetAddressBytes()[0]);
        }
    }
}
