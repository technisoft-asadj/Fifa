using NUnit.Framework;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class AdsTests
    {
        class FakeAds : IAdProvider
        {
            public bool ready = true, watchToEnd = true, throws;
            public int shown;
            public bool RewardedReady => ready;
            public void ShowRewarded(System.Action<bool> done) { shown++; if (throws) throw new System.Exception("sdk failure"); done(watchToEnd); }
        }

        IAdProvider old;
        [SetUp] public void Save() { old = Ads.Provider; Ads.Provider = null; }
        [TearDown] public void Restore() { Ads.Provider = old; }

        [Test]
        public void WithNoProviderThereAreNoAdsAndNothingIsShown()
        {
            Assert.IsNull(Ads.Provider, "the game ships with no ad service");
            Assert.IsFalse(Ads.RewardedReady);
            bool? result = null;
            Ads.ShowRewarded(w => result = w);
            Assert.AreEqual(false, result, "the callback gets false at once");
        }

        [Test]
        public void AReadyProviderIsAskedAndItsAnswerIsPassedOn()
        {
            var fake = new FakeAds();
            Ads.Provider = fake;
            Assert.IsTrue(Ads.RewardedReady);
            bool? result = null;
            Ads.ShowRewarded(w => result = w);
            Assert.AreEqual(true, result); Assert.AreEqual(1, fake.shown);
            fake.watchToEnd = false; result = null;
            Ads.ShowRewarded(w => result = w);
            Assert.AreEqual(false, result, "skipping the ad earns nothing");
        }

        [Test]
        public void AProviderThatIsNotReadyIsNeverAskedAndOneThatFailsCountsAsNotWatched()
        {
            var fake = new FakeAds { ready = false };
            Ads.Provider = fake;
            bool? result = null;
            Ads.ShowRewarded(w => result = w);
            Assert.AreEqual(false, result); Assert.AreEqual(0, fake.shown);
            fake.ready = true; fake.throws = true; result = null;
            Ads.ShowRewarded(w => result = w);
            Assert.AreEqual(false, result, "an SDK exception must not crash the game or pay out");
        }

        [Test]
        public void BonusCoinsAddUpAndNonsenseAmountsAreIgnored()
        {
            PlayerProfile.Clear();
            PlayerProfile.AddBonusCoins(250);
            PlayerProfile.AddBonusCoins(-40);
            PlayerProfile.AddBonusCoins(0);
            Assert.AreEqual(250, PlayerProfile.Coins);
            PlayerProfile.Clear();
        }
    }
}
