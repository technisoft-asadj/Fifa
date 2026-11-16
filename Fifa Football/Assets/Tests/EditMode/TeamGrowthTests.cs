using NUnit.Framework;
using UnityEngine;
using FootballSim.Players;

namespace FootballSim.Tests
{
    public class TeamGrowthTests
    {
        [Test]
        public void LevelsComeFromWinsAndDrawsAndAreCapped()
        {
            Assert.AreEqual(0, TeamGrowth.LevelFor(0, 0));
            Assert.AreEqual(0, TeamGrowth.LevelFor(1, 1), "3 points is not yet a level");
            Assert.AreEqual(1, TeamGrowth.LevelFor(2, 0));
            Assert.AreEqual(1, TeamGrowth.LevelFor(0, 4));
            Assert.AreEqual(3, TeamGrowth.LevelFor(5, 2));
            Assert.AreEqual(TeamGrowth.MaxLevel, TeamGrowth.LevelFor(500, 500));
            Assert.AreEqual(0, TeamGrowth.LevelFor(-5, -5));
        }

        [Test]
        public void ApplyRaisesEveryAttributeByTheLevelAndStopsAt99()
        {
            var d = ScriptableObject.CreateInstance<PlayerData>();
            try
            {
                d.pace = 70; d.shooting = 97; d.strength = 99;
                TeamGrowth.Apply(d, 3);
                Assert.AreEqual(73, d.pace);
                Assert.AreEqual(99, d.shooting);
                Assert.AreEqual(99, d.strength);
                Assert.AreEqual(73, d.passing);
            }
            finally { Object.DestroyImmediate(d); }
        }

        [Test]
        public void LevelZeroAndNullChangeNothing()
        {
            var d = ScriptableObject.CreateInstance<PlayerData>();
            try
            {
                TeamGrowth.Apply(d, 0); TeamGrowth.Apply(d, -2); TeamGrowth.Apply(null, 4);
                Assert.AreEqual(70, d.pace); Assert.AreEqual(70, d.stamina);
            }
            finally { Object.DestroyImmediate(d); }
        }
    }
}
