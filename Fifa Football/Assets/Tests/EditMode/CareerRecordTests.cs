using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class CareerRecordTests
    {
        [SetUp] public void Reset() { CareerRecord.Clear(); }
        [TearDown] public void ResetAfter() { CareerRecord.Clear(); }

        [Test]
        public void WinsDrawsLossesAndGoalsAddUp()
        {
            CareerRecord.Record(3, 1); CareerRecord.Record(0, 0); CareerRecord.Record(1, 2); CareerRecord.Record(2, 0);
            Assert.AreEqual(4, CareerRecord.Played);
            Assert.AreEqual(2, CareerRecord.Won); Assert.AreEqual(1, CareerRecord.Drawn); Assert.AreEqual(1, CareerRecord.Lost);
            Assert.AreEqual(6, CareerRecord.GoalsFor); Assert.AreEqual(3, CareerRecord.GoalsAgainst);
            Assert.AreEqual("W2 D1 L1\nGF 6  GA 3", CareerRecord.Summary());
        }

        [Test]
        public void NothingIsShownBeforeTheFirstMatchAndNegativeScoresAreIgnored()
        {
            Assert.AreEqual("", CareerRecord.Summary());
            CareerRecord.Record(-1, 0);
            Assert.AreEqual(0, CareerRecord.Played);
        }

        [Test]
        public void TheRecordSurvivesARestart()
        {
            CareerRecord.Record(2, 2); CareerRecord.Record(4, 0);
            CareerRecord.ForgetInMemory();
            Assert.AreEqual(2, CareerRecord.Played);
            Assert.AreEqual(1, CareerRecord.Won); Assert.AreEqual(1, CareerRecord.Drawn);
            Assert.AreEqual(6, CareerRecord.GoalsFor); Assert.AreEqual(2, CareerRecord.GoalsAgainst);
        }

        [TestCase("banana")]
        [TestCase("1,2,3")]
        [TestCase("2,2,1,0,0")]   // more wins and draws than matches
        [TestCase("1,-1,0,0,0")]
        public void NonsenseSavedDataIsDiscarded(string saved)
        {
            PlayerPrefs.SetString(CareerRecord.Key, saved);
            CareerRecord.ForgetInMemory();
            LogAssert.Expect(LogType.Warning, "Saved career record did not make sense and was discarded.");
            Assert.AreEqual(0, CareerRecord.Played);
            CareerRecord.ForgetInMemory();
            Assert.AreEqual(0, CareerRecord.Played, "the bad data was cleared");
            Assert.IsFalse(PlayerPrefs.HasKey(CareerRecord.Key));
        }

        [Test]
        public void ResetProgressKeepsTheRecordButResetAllClearsIt()
        {
            CareerRecord.Record(1, 0);
            SaveData.ResetProgress();
            Assert.AreEqual(1, CareerRecord.Played);
            SaveData.ResetAll();
            Assert.AreEqual(0, CareerRecord.Played);
            CareerRecord.ForgetInMemory();
            Assert.AreEqual(0, CareerRecord.Played);
        }
    }
}
