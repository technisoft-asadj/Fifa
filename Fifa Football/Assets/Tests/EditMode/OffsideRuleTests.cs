using NUnit.Framework;
using UnityEngine;
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
        public void SignOverloadsFollowTheAttackDirection()
        {
            // a team attacking -Z (e.g. home after the sides swap) uses the mirrored rule
            Assert.IsTrue(OffsideRule.IsOffside(-1f, -40f, -10f, -30f));
            Assert.IsFalse(OffsideRule.IsOffside(-1f, 40f, 10f, 30f), "that is the team's own half");
            Assert.AreEqual(-30f, OffsideRule.SecondLastDefenderZ(-1f, new[] { -50f, -30f, -10f }), 1e-4f);
            Assert.IsTrue(FootballSim.Match.MatchStats.IsOnTarget(new Vector3(0, 0, -30), Vector3.back, -1f));
            Assert.IsFalse(FootballSim.Match.MatchStats.IsOnTarget(new Vector3(0, 0, -30), Vector3.forward, -1f));
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
