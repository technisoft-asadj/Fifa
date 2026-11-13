using NUnit.Framework;
using FootballSim.Players;

namespace FootballSim.Tests
{
    public class GoalkeeperModelTests
    {
        [Test]
        public void HarderShotsAreSavedLessOften()
        {
            Assert.Greater(GoalkeeperModel.SaveChance(8f, 0.5f, GoalkeeperModel.StandReach),
                           GoalkeeperModel.SaveChance(25f, 0.5f, GoalkeeperModel.StandReach));
        }

        [Test]
        public void ShotsFurtherFromTheBodyAreSavedLessOften()
        {
            Assert.Greater(GoalkeeperModel.SaveChance(15f, 0.2f, GoalkeeperModel.DiveReach),
                           GoalkeeperModel.SaveChance(15f, 2.2f, GoalkeeperModel.DiveReach));
        }

        [Test]
        public void ChanceStaysWithinBounds()
        {
            for (float speed = 0f; speed <= 40f; speed += 5f)
                for (float dist = 0f; dist <= 3f; dist += 0.5f)
                {
                    float c = GoalkeeperModel.SaveChance(speed, dist, GoalkeeperModel.DiveReach);
                    Assert.GreaterOrEqual(c, 0.15f); Assert.LessOrEqual(c, 0.92f);
                }
        }

        [Test]
        public void SlowShotsAreHeldAndFastShotsAreParried()
        {
            Assert.AreEqual(SaveOutcome.Catch, GoalkeeperModel.Decide(10f, 0.3f, GoalkeeperModel.StandReach, 0f));
            Assert.AreEqual(SaveOutcome.Parry, GoalkeeperModel.Decide(20f, 0.3f, GoalkeeperModel.StandReach, 0f));
        }

        [Test]
        public void ARollAboveTheChanceMeansNoSave()
        {
            Assert.AreEqual(SaveOutcome.None, GoalkeeperModel.Decide(15f, 0.5f, GoalkeeperModel.StandReach, 0.99f));
        }

        [Test]
        public void TooSlowBallsAreNotSavesAtAll()
        {
            Assert.AreEqual(SaveOutcome.None, GoalkeeperModel.Decide(GoalkeeperModel.MinShotSpeed - 0.1f, 0f, GoalkeeperModel.StandReach, 0f));
        }

        [Test]
        public void DivingExtendsReach()
        {
            Assert.Greater(GoalkeeperModel.DiveReach, GoalkeeperModel.StandReach);
        }
    }
}
