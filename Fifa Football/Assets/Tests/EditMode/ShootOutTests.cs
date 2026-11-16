using NUnit.Framework;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class ShootOutTests
    {
        [Test]
        public void ThePerfectHomeSideWinsFiveToZeroAndStopsEarly()
        {
            var r = ShootOut.Simulate(new RandomStream(1), 1f, 0f);
            Assert.IsTrue(r.homeWins);
            Assert.AreEqual(3, r.homeGoals, "home leads 3-0 with 2 kicks left for away, so it is over");
            Assert.IsTrue(r.homeKicks == 3 && r.awayKicks == 3);
            Assert.AreEqual(0, r.awayGoals);
            Assert.IsFalse(r.suddenDeath);
            Assert.AreEqual(6, r.sequence.Count, "H,A,H,A,H,A: after six kicks away has two left and cannot catch up");
        }

        [Test]
        public void WhenBothScoreEverythingItGoesToSuddenDeathAndOnlyTheCapEndsIt()
        {
            var r = ShootOut.Simulate(new RandomStream(2), 1f, 1f);
            Assert.IsTrue(r.suddenDeath);
            Assert.AreEqual(r.homeKicks, r.awayKicks);
            Assert.AreEqual(r.homeGoals, r.awayGoals, "an endless tie stays level and a coin settles it");
            Assert.AreEqual(ShootOut.KicksPerSide + ShootOut.MaxSuddenDeathPairs, r.homeKicks);
        }

        [Test]
        public void TheAwaySideCanWinInRegulationToo()
        {
            var r = ShootOut.Simulate(new RandomStream(3), 0f, 1f);
            Assert.IsFalse(r.homeWins);
            Assert.AreEqual(3, r.awayGoals, "away leads 3-0 with only 2 home kicks left after six kicks");
            Assert.AreEqual(0, r.homeGoals);
            Assert.AreEqual(6, r.sequence.Count);
        }

        [Test]
        public void ResultIsAlwaysDecidedAndKickCountsAreConsistent()
        {
            for (int seed = 0; seed < 500; seed++)
            {
                var r = ShootOut.Simulate(new RandomStream(seed), 0.75f, 0.7f);
                Assert.AreEqual(r.sequence.Count, r.homeKicks + r.awayKicks);
                Assert.LessOrEqual(r.homeKicks - r.awayKicks, 1);
                Assert.GreaterOrEqual(r.homeKicks - r.awayKicks, 0, "home kicks first, so it is never behind in kick count");
                if (!r.suddenDeath) Assert.AreEqual(r.homeWins, r.homeGoals > r.awayGoals, $"seed {seed}");
                else { Assert.AreEqual(r.homeKicks, r.awayKicks); Assert.AreEqual(r.homeWins, r.homeGoals > r.awayGoals, $"seed {seed}"); }
                Assert.AreNotEqual(r.homeGoals, r.awayGoals, $"seed {seed} must have a winner");
            }
        }

        [Test]
        public void SameSeedGivesTheSameShootOutAndStrongerSideWinsMoreOften()
        {
            var a = ShootOut.Simulate(new RandomStream(42), 0.75f, 0.65f);
            var b = ShootOut.Simulate(new RandomStream(42), 0.75f, 0.65f);
            Assert.AreEqual(a.ToString(), b.ToString());
            int strongWins = 0;
            for (int seed = 0; seed < 1000; seed++) if (ShootOut.Simulate(new RandomStream(seed), 0.9f, 0.5f).homeWins) strongWins++;
            Assert.Greater(strongWins, 800, "a 90% side should beat a 50% side most of the time");
        }

        [Test]
        public void ChancesGetHarderWithTheOpponent()
        {
            ShootOut.Chances(Difficulty.Amateur, out float h1, out float a1);
            ShootOut.Chances(Difficulty.WorldClass, out float h2, out float a2);
            Assert.AreEqual(h1, h2);
            Assert.Less(a1, a2);
        }

        [Test]
        public void ATournamentDrawIsSettledByTheShootOutNotReplayed()
        {
            Tournament.Abandon(); Tournament.Start();
            try
            {
                var won = ShootOut.Simulate(new RandomStream(1), 1f, 0f);
                Assert.AreEqual(Tournament.Outcome.Advanced, Tournament.RecordResult(1, 1, won));
                Assert.AreEqual(1, Tournament.Round);
                StringAssert.Contains("penalties", Tournament.Results[0]);
                var lost = ShootOut.Simulate(new RandomStream(1), 0f, 1f);
                Assert.AreEqual(Tournament.Outcome.Eliminated, Tournament.RecordResult(0, 0, lost));
                Assert.IsTrue(Tournament.Eliminated);
            }
            finally { Tournament.Abandon(); }
        }

        [Test]
        public void ShootOutIsIgnoredWhenTheMatchWasNotADraw()
        {
            Tournament.Abandon(); Tournament.Start();
            try
            {
                var lost = ShootOut.Simulate(new RandomStream(1), 0f, 1f);
                Assert.AreEqual(Tournament.Outcome.Advanced, Tournament.RecordResult(2, 1, lost), "a 2-1 win goes through whatever a stray shoot-out says");
            }
            finally { Tournament.Abandon(); }
        }
    }
}
