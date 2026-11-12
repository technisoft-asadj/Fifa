using NUnit.Framework;
using FootballSim.Match;

namespace FootballSim.Tests
{
    public class OffsideRuleTests
    {
        [Test]
        public void SecondLastDefenderIsSecondNearestHisOwnGoalLine()
        {
            // away defends +Z; the keeper at 50 is last, the defender at 30 is second-last
            Assert.AreEqual(30f, OffsideRule.SecondLastDefenderZ(0, new[] { 50f, 30f, 10f, -5f }), 1e-4f);
            // home defends -Z when away attack
            Assert.AreEqual(-30f, OffsideRule.SecondLastDefenderZ(1, new[] { -50f, -30f, -10f, 5f }), 1e-4f);
            Assert.AreEqual(12f, OffsideRule.SecondLastDefenderZ(0, new[] { 12f }), 1e-4f);
        }

        [Test]
        public void AheadOfBallAndSecondLastDefenderInOpponentHalfIsOffside()
        {
            Assert.IsTrue(OffsideRule.IsOffside(0, 40f, 10f, 30f));
            Assert.IsTrue(OffsideRule.IsOffside(1, -40f, -10f, -30f));
        }

        [Test]
        public void OwnHalfLevelWithOrBehindTheBallOrDefenderIsOnside()
        {
            Assert.IsFalse(OffsideRule.IsOffside(0, -5f, -20f, -30f), "own half is never offside");
            Assert.IsFalse(OffsideRule.IsOffside(0, 40f, 45f, 30f), "behind the ball");
            Assert.IsFalse(OffsideRule.IsOffside(0, 29f, 10f, 30f), "behind the second-last defender");
            Assert.IsFalse(OffsideRule.IsOffside(0, 30.2f, 10f, 30f), "within tolerance counts as level");
        }
    }
}
