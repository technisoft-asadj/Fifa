using NUnit.Framework;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class MiniLeagueTests
    {
        [SetUp] public void Reset() { MiniLeague.Abandon(); }
        [TearDown] public void ResetAfter() { MiniLeague.Abandon(); }

        [Test]
        public void PointsAreThreeForAWinOneForADrawNoneForALoss()
        {
            Assert.AreEqual(3, MiniLeague.PointsFor(2, 1));
            Assert.AreEqual(1, MiniLeague.PointsFor(0, 0));
            Assert.AreEqual(0, MiniLeague.PointsFor(0, 4));
        }

        [Test]
        public void OpponentsGetHarderEachRound()
        {
            MiniLeague.Start();
            Assert.AreEqual(Difficulty.Amateur, MiniLeague.CurrentOpponent);
            MiniLeague.RecordResult(1, 0);
            Assert.AreEqual(Difficulty.Professional, MiniLeague.CurrentOpponent);
            MiniLeague.RecordResult(1, 0);
            Assert.AreEqual(Difficulty.WorldClass, MiniLeague.CurrentOpponent);
        }

        [Test]
        public void ThreeResultsFinishTheLeagueAndAddUpPoints()
        {
            MiniLeague.Start();
            Assert.AreEqual(3, MiniLeague.RecordResult(2, 0));
            Assert.AreEqual(1, MiniLeague.RecordResult(1, 1));
            Assert.AreEqual(0, MiniLeague.RecordResult(0, 1));
            Assert.IsTrue(MiniLeague.Finished);
            Assert.AreEqual(4, MiniLeague.Points);
            Assert.AreEqual(3, MiniLeague.Results.Count);
            StringAssert.Contains("4/9", MiniLeague.Summary());
            Assert.AreEqual(0, MiniLeague.RecordResult(5, 0), "no points after the league is over");
            Assert.AreEqual(4, MiniLeague.Points);
        }

        [Test]
        public void ResultsAreIgnoredWhenNoLeagueIsRunning()
        {
            Assert.AreEqual(0, MiniLeague.RecordResult(3, 0));
            Assert.AreEqual(0, MiniLeague.Points);
            Assert.AreEqual("", MiniLeague.Summary());
        }

        [Test]
        public void StartingAgainClearsThePreviousLeague()
        {
            MiniLeague.Start();
            MiniLeague.RecordResult(3, 0);
            MiniLeague.Start();
            Assert.AreEqual(0, MiniLeague.Points);
            Assert.AreEqual(0, MiniLeague.Round);
            Assert.AreEqual(0, MiniLeague.Results.Count);
        }

        [Test]
        public void ALeagueInProgressSurvivesARestart()
        {
            MiniLeague.Start();
            MiniLeague.RecordResult(2, 0); MiniLeague.RecordResult(1, 1);
            MiniLeague.ForgetInMemory(); // as if the game was closed and opened again
            Assert.IsTrue(MiniLeague.Active);
            Assert.AreEqual(2, MiniLeague.Round);
            Assert.AreEqual(4, MiniLeague.Points);
            Assert.AreEqual(2, MiniLeague.Results.Count);
            Assert.AreEqual(Difficulty.WorldClass, MiniLeague.CurrentOpponent);
        }

        [Test]
        public void AFinishedLeagueAndAnAbandonedOneAreRestoredAsTheyWere()
        {
            MiniLeague.Start();
            MiniLeague.RecordResult(1, 0); MiniLeague.RecordResult(1, 0); MiniLeague.RecordResult(1, 0);
            MiniLeague.ForgetInMemory();
            Assert.IsTrue(MiniLeague.Finished);
            Assert.AreEqual(9, MiniLeague.Points);
            MiniLeague.Abandon();
            MiniLeague.ForgetInMemory();
            Assert.IsFalse(MiniLeague.Active);
            Assert.AreEqual(0, MiniLeague.Round);
        }

        [Test]
        public void NonsenseSavedDataIsDiscarded()
        {
            UnityEngine.PlayerPrefs.SetInt(MiniLeague.ActiveKey, 1);
            UnityEngine.PlayerPrefs.SetInt(MiniLeague.RoundKey, 2);
            UnityEngine.PlayerPrefs.SetInt(MiniLeague.PointsKey, 99); // impossible after two matches
            UnityEngine.PlayerPrefs.SetString(MiniLeague.ResultsKey, "1-0 vs Amateur|1-0 vs Professional");
            MiniLeague.ForgetInMemory();
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, "Saved league data did not make sense and was discarded.");
            Assert.IsFalse(MiniLeague.Active);
            MiniLeague.ForgetInMemory();
            Assert.IsFalse(MiniLeague.Active, "the bad data was cleared, not left to trip again");
        }

        [Test]
        public void PerfectLeagueIsChampionsAndPoorOneIsNot()
        {
            MiniLeague.Start();
            MiniLeague.RecordResult(1, 0); MiniLeague.RecordResult(1, 0); MiniLeague.RecordResult(1, 0);
            StringAssert.Contains("CHAMPIONS", MiniLeague.Summary());
            MiniLeague.Start();
            MiniLeague.RecordResult(0, 1); MiniLeague.RecordResult(0, 1); MiniLeague.RecordResult(1, 1);
            StringAssert.DoesNotContain("CHAMPIONS", MiniLeague.Summary());
        }
    }
}
