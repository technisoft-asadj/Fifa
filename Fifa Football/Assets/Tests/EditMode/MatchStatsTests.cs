using NUnit.Framework;
using UnityEngine;
using FootballSim.Core;
using FootballSim.Match;

namespace FootballSim.Tests
{
    public class MatchStatsTests
    {
        [Test]
        public void PossessionIsFiftyFiftyBeforeAnyTouchAndSumsToHundred()
        {
            var s = new MatchStats();
            Assert.AreEqual(50, s.PossessionPercent(0));
            Assert.AreEqual(50, s.PossessionPercent(1));
            s.AddPossession(0, 30f);
            s.AddPossession(1, 10f);
            s.AddPossession(-1, 99f); // nobody has touched it: ignored
            Assert.AreEqual(75, s.PossessionPercent(0));
            Assert.AreEqual(25, s.PossessionPercent(1));
            s.AddPossession(1, 3.3f);
            Assert.AreEqual(100, s.PossessionPercent(0) + s.PossessionPercent(1));
        }

        [Test]
        public void ShotsAndPassesAreCountedPerTeam()
        {
            var s = new MatchStats();
            s.AddStrike(0, false, Vector3.zero, Vector3.forward);
            s.AddStrike(0, true, new Vector3(0, 0, 40), Vector3.forward);          // straight at the +Z goal
            s.AddStrike(0, true, new Vector3(0, 0, 40), new Vector3(1, 0, 1));      // wide
            s.AddStrike(1, true, new Vector3(2, 0, -40), Vector3.back);             // away attacks -Z
            Assert.AreEqual(1, s.passes[0]);
            Assert.AreEqual(2, s.shots[0]);
            Assert.AreEqual(1, s.shotsOnTarget[0]);
            Assert.AreEqual(1, s.shots[1]);
            Assert.AreEqual(1, s.shotsOnTarget[1]);
            Assert.AreEqual(0, s.passes[1]);
        }

        [Test]
        public void OnTargetNeedsTheRightGoalAndBetweenThePosts()
        {
            var from = new Vector3(0, 0, 30);
            Assert.IsTrue(MatchStats.IsOnTarget(from, Vector3.forward, 0));
            Assert.IsFalse(MatchStats.IsOnTarget(from, Vector3.back, 0), "shooting at your own goal is not on target");
            Assert.IsFalse(MatchStats.IsOnTarget(from, Vector3.right, 0), "sideways never reaches the goal line");
            float dz = Pitch.HalfLength - 30f;
            Assert.IsTrue(MatchStats.IsOnTarget(from, new Vector3(Pitch.GoalHalfWidth - 0.1f, 0, dz), 0));
            Assert.IsFalse(MatchStats.IsOnTarget(from, new Vector3(Pitch.GoalHalfWidth + 0.1f, 0, dz), 0));
        }
    }
}
