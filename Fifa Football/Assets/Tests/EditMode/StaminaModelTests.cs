using NUnit.Framework;
using FootballSim.Players;

namespace FootballSim.Tests
{
    public class StaminaModelTests
    {
        [Test]
        public void SprintDrainsInTwentySecondsAndRecoversInFourteen()
        {
            float s = 1f;
            for (int i = 0; i < 200; i++) s = StaminaModel.Step(s, true, 0.1f);
            Assert.AreEqual(0f, s, 1e-3f);
            for (int i = 0; i < 140; i++) s = StaminaModel.Step(s, false, 0.1f);
            Assert.AreEqual(1f, s, 1e-3f);
        }

        [Test]
        public void StaminaStaysWithinZeroAndOne()
        {
            Assert.AreEqual(0f, StaminaModel.Step(0f, true, 5f));
            Assert.AreEqual(1f, StaminaModel.Step(1f, false, 5f));
        }

        [Test]
        public void ExhaustionLocksSprintUntilQuarterRecovered()
        {
            Assert.IsTrue(StaminaModel.UpdateExhausted(0f, false));
            Assert.IsTrue(StaminaModel.UpdateExhausted(0.1f, true), "still locked below 25%");
            Assert.IsFalse(StaminaModel.UpdateExhausted(0.26f, true));
            Assert.IsFalse(StaminaModel.UpdateExhausted(0.1f, false), "not exhausted unless it ran dry");
        }
    }
}
