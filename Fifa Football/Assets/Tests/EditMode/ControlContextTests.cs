using NUnit.Framework;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class ControlContextTests
    {
        [Test]
        public void TheControlledPlayersSituationFollowsWhoHasTheBall()
        {
            Assert.AreEqual(ControlContext.Attack, ControlContextRules.Decide(false, 0, 0, 3f));
            Assert.AreEqual(ControlContext.Defend, ControlContextRules.Decide(false, 0, 1, 3f));
            Assert.AreEqual(ControlContext.Loose, ControlContextRules.Decide(false, 0, -1, 0f), "nobody has touched it yet");
            Assert.AreEqual(ControlContext.Loose, ControlContextRules.Decide(false, 0, 0, ControlContextRules.InFlightSpeed + 1f), "a ball in flight is nobody's");
            Assert.AreEqual(ControlContext.Attack, ControlContextRules.Decide(false, 1, 1, 3f), "the away side attacks the same way");
        }

        [Test]
        public void AKeeperAlwaysGetsTheKeeperButtons()
        {
            foreach (int poss in new[] { -1, 0, 1 })
                Assert.AreEqual(ControlContext.Keeper, ControlContextRules.Decide(true, 0, poss, 5f));
            var look = ControlContextRules.Look(ControlContext.Keeper);
            Assert.AreEqual("DIVE", look.tackle.label); Assert.AreEqual("KICK", look.shoot.label);
        }

        [Test]
        public void AttackingFavoursShootAndPassAndDefendingFavoursTheTackle()
        {
            var atk = ControlContextRules.Look(ControlContext.Attack);
            Assert.Greater(atk.shoot.size, 1f); Assert.Greater(atk.pass.size, 1f);
            Assert.Less(atk.tackle.alpha, 0.6f, "the tackle is not useful with the ball");
            var def = ControlContextRules.Look(ControlContext.Defend);
            Assert.Greater(def.tackle.size, 1.1f); Assert.AreEqual(1f, def.tackle.alpha);
            Assert.Less(def.pass.alpha, 0.6f);
            Assert.AreEqual("CLEAR", def.shoot.label);
            var loose = ControlContextRules.Look(ControlContext.Loose);
            Assert.AreEqual("SHOOT", loose.shoot.label); Assert.AreEqual(1f, loose.tackle.size);
        }
    }
}
