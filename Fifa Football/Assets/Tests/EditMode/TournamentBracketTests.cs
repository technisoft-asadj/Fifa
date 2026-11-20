using NUnit.Framework;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class TournamentBracketTests
    {
        [TearDown] public void Cleanup() { Tournament.Abandon(); Tournament.ForgetInMemory(); }

        [Test]
        public void BracketIsEmptyWithNoCup()
        {
            Tournament.Abandon();
            Assert.AreEqual("", Tournament.Bracket());
        }

        [Test]
        public void BracketMarksWonRoundsAndTheNextOne()
        {
            Tournament.Start();
            Assert.AreEqual("[Quarter-final]  >  Semi-final  >  Final", Tournament.Bracket());
            Tournament.RecordResult(2, 0);
            Assert.AreEqual("Quarter-final (won)  >  [Semi-final]  >  Final", Tournament.Bracket());
        }

        [Test]
        public void BracketHasNoNextRoundOnceKnockedOut()
        {
            Tournament.Start();
            Tournament.RecordResult(0, 1);
            StringAssert.DoesNotContain("[", Tournament.Bracket());
        }
    }
}
