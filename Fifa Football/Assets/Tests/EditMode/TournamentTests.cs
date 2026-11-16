using NUnit.Framework;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class TournamentTests
    {
        [SetUp] public void Reset() { Tournament.Abandon(); }
        [TearDown] public void ResetAfter() { Tournament.Abandon(); }

        [Test]
        public void WinningThreeTimesWinsTheCupAgainstHarderOpponents()
        {
            Tournament.Start();
            Assert.AreEqual("Quarter-final", Tournament.CurrentStage);
            Assert.AreEqual(Difficulty.Amateur, Tournament.CurrentOpponent);
            Assert.AreEqual(Tournament.Outcome.Advanced, Tournament.RecordResult(1, 0));
            Assert.AreEqual("Semi-final", Tournament.CurrentStage);
            Assert.AreEqual(Difficulty.Professional, Tournament.CurrentOpponent);
            Assert.AreEqual(Tournament.Outcome.Advanced, Tournament.RecordResult(2, 1));
            Assert.AreEqual("Final", Tournament.CurrentStage);
            Assert.AreEqual(Difficulty.WorldClass, Tournament.CurrentOpponent);
            Assert.AreEqual(Tournament.Outcome.Champion, Tournament.RecordResult(3, 0));
            Assert.IsTrue(Tournament.Won);
            Assert.IsTrue(Tournament.Over);
            StringAssert.Contains("WON", Tournament.Summary());
            Assert.AreEqual(Tournament.Outcome.None, Tournament.RecordResult(1, 0), "nothing counts after the cup is won");
        }

        [Test]
        public void ALossEliminatesAndNothingCountsAfterwards()
        {
            Tournament.Start();
            Tournament.RecordResult(1, 0);
            Assert.AreEqual(Tournament.Outcome.Eliminated, Tournament.RecordResult(0, 2));
            Assert.IsTrue(Tournament.Eliminated);
            Assert.IsTrue(Tournament.Over);
            Assert.IsFalse(Tournament.Won);
            StringAssert.Contains("semi-final", Tournament.Summary());
            Assert.AreEqual(Tournament.Outcome.None, Tournament.RecordResult(5, 0));
            Assert.AreEqual(1, Tournament.Round, "the round does not move once out");
        }

        [Test]
        public void ADrawReplaysTheSameRound()
        {
            Tournament.Start();
            Assert.AreEqual(Tournament.Outcome.Replay, Tournament.RecordResult(1, 1));
            Assert.AreEqual(0, Tournament.Round);
            Assert.AreEqual("Quarter-final", Tournament.CurrentStage);
            Assert.AreEqual(1, Tournament.Results.Count);
            StringAssert.Contains("replay", Tournament.Results[0]);
            Assert.IsFalse(Tournament.Over);
        }

        [Test]
        public void ResultsAreIgnoredWhenNoCupIsRunning()
        {
            Assert.AreEqual(Tournament.Outcome.None, Tournament.RecordResult(3, 0));
            Assert.AreEqual("", Tournament.Summary());
        }

        [Test]
        public void ACupInProgressSurvivesARestart()
        {
            Tournament.Start();
            Tournament.RecordResult(1, 1); Tournament.RecordResult(2, 0);
            Tournament.ForgetInMemory();
            Assert.IsTrue(Tournament.Active);
            Assert.AreEqual(1, Tournament.Round);
            Assert.AreEqual(2, Tournament.Results.Count);
            Assert.AreEqual("Semi-final", Tournament.CurrentStage);
        }

        [Test]
        public void AnEliminatedAndAWonCupAreRestoredAsTheyWere()
        {
            Tournament.Start();
            Tournament.RecordResult(0, 1);
            Tournament.ForgetInMemory();
            Assert.IsTrue(Tournament.Eliminated);
            Tournament.Start();
            Tournament.RecordResult(1, 0); Tournament.RecordResult(1, 0); Tournament.RecordResult(1, 0);
            Tournament.ForgetInMemory();
            Assert.IsTrue(Tournament.Won);
            Tournament.Abandon();
            Tournament.ForgetInMemory();
            Assert.IsFalse(Tournament.Active);
        }

        [Test]
        public void NonsenseSavedDataIsDiscarded()
        {
            UnityEngine.PlayerPrefs.SetInt(Tournament.ActiveKey, 1);
            UnityEngine.PlayerPrefs.SetInt(Tournament.RoundKey, 3);
            UnityEngine.PlayerPrefs.SetInt(Tournament.EliminatedKey, 1); // out and also won: impossible
            UnityEngine.PlayerPrefs.SetString(Tournament.ResultsKey, "a|b|c|d");
            Tournament.ForgetInMemory();
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, "Saved tournament data did not make sense and was discarded.");
            Assert.IsFalse(Tournament.Active);
            Tournament.ForgetInMemory();
            Assert.IsFalse(Tournament.Active, "the bad data was cleared");
        }
    }
}
