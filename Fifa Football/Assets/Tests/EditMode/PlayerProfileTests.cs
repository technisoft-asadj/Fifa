using NUnit.Framework;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class PlayerProfileTests
    {
        System.Func<long> oldToday;

        [SetUp] public void Reset() { oldToday = PlayerProfile.Today; PlayerProfile.Clear(); }
        [TearDown] public void ResetAfter() { PlayerProfile.Today = oldToday; PlayerProfile.Clear(); }

        [Test]
        public void AWinPaysMoreThanADrawAndADrawMoreThanALoss()
        {
            PlayerProfile.Reward(2, 0, out int winCoins, out int winXp);
            PlayerProfile.Reward(1, 1, out int drawCoins, out int drawXp);
            PlayerProfile.Reward(0, 3, out int lossCoins, out int lossXp);
            Assert.Greater(winXp, drawXp); Assert.Greater(drawXp, lossXp);
            Assert.Greater(lossCoins, 0, "even a loss pays something");
            Assert.AreEqual(100 + 20, winCoins, "100 for the win and 10 per goal scored");
            Assert.AreEqual(40 + 10, drawCoins);
            Assert.AreEqual(20, lossCoins);
        }

        [Test]
        public void LevelRisesEveryHundredExperiencePoints()
        {
            Assert.AreEqual(1, PlayerProfile.Level);
            for (int i = 0; i < 2; i++) PlayerProfile.AwardMatch(1, 0); // 50 xp each
            Assert.AreEqual(100, PlayerProfile.Xp);
            Assert.AreEqual(2, PlayerProfile.Level);
            Assert.AreEqual(0, PlayerProfile.XpInLevel);
            PlayerProfile.AwardMatch(0, 1); // 15
            Assert.AreEqual(15, PlayerProfile.XpInLevel);
        }

        [Test]
        public void TheDailyRewardPaysOncePerDay()
        {
            long day = 19000;
            PlayerProfile.Today = () => day;
            Assert.IsTrue(PlayerProfile.DailyAvailable);
            Assert.AreEqual(PlayerProfile.DailyRewardCoins, PlayerProfile.ClaimDaily());
            Assert.IsFalse(PlayerProfile.DailyAvailable);
            Assert.AreEqual(0, PlayerProfile.ClaimDaily());
            day++;
            Assert.IsTrue(PlayerProfile.DailyAvailable);
            Assert.AreEqual(PlayerProfile.DailyRewardCoins, PlayerProfile.ClaimDaily());
            Assert.AreEqual(2 * PlayerProfile.DailyRewardCoins, PlayerProfile.Coins);
        }

        [Test]
        public void AProfileSurvivesARestart()
        {
            long day = 19000;
            PlayerProfile.Today = () => day;
            PlayerProfile.ClaimDaily();
            PlayerProfile.AwardMatch(3, 1);
            int coins = PlayerProfile.Coins, xp = PlayerProfile.Xp;
            PlayerProfile.ForgetInMemory();
            Assert.AreEqual(coins, PlayerProfile.Coins);
            Assert.AreEqual(xp, PlayerProfile.Xp);
            Assert.IsFalse(PlayerProfile.DailyAvailable, "today's reward stays claimed after a restart");
        }

        [Test]
        public void NonsenseSavedDataIsDiscarded()
        {
            UnityEngine.PlayerPrefs.SetInt(PlayerProfile.CoinsKey, -5);
            UnityEngine.PlayerPrefs.SetInt(PlayerProfile.XpKey, 10);
            PlayerProfile.ForgetInMemory();
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, "Saved player profile did not make sense and was discarded.");
            Assert.AreEqual(0, PlayerProfile.Coins);
            Assert.AreEqual(0, PlayerProfile.Xp);
        }

        [Test]
        public void NamesAreCleanedCutAndSaved()
        {
            Assert.IsFalse(PlayerProfile.HasName);
            Assert.AreEqual("Guest", PlayerProfile.Name, "no name yet reads as Guest");
            Assert.AreEqual("Guest", PlayerProfile.CleanName(null));
            Assert.AreEqual("Guest", PlayerProfile.CleanName("  <>!! "), "nothing usable left");
            Assert.AreEqual("Ali Khan-9_x", PlayerProfile.CleanName("  Ali Khan-9_x  "));
            Assert.AreEqual("Bob", PlayerProfile.CleanName("B<o>b"));
            Assert.AreEqual(16, PlayerProfile.CleanName("abcdefghijklmnopqrstuvwxyz").Length);
            PlayerProfile.SetName("Zed");
            Assert.IsTrue(PlayerProfile.HasName);
            PlayerProfile.ForgetInMemory();
            Assert.AreEqual("Zed", PlayerProfile.Name, "the name survives a restart");
            PlayerProfile.Clear();
            Assert.IsFalse(PlayerProfile.HasName, "clearing forgets the name");
        }

        [Test]
        public void ResetAllClearsTheProfile()
        {
            PlayerProfile.AwardMatch(2, 0);
            SaveData.ResetAll();
            PlayerProfile.ForgetInMemory();
            Assert.AreEqual(0, PlayerProfile.Coins);
            Assert.AreEqual(0, PlayerProfile.Xp);
        }
    }
}
