using System.Collections.Generic;
using FootballSim.Core;
using NUnit.Framework;
using UnityEngine;

namespace FootballSim.Tests
{
    public class AimAssistTests
    {
        [Test]
        public void AShotFromNearTheGoalFacingRoughlyItGoesAtTheGoal()
        {
            Vector3 from = new Vector3(5f, 0f, 30f);
            Vector3 dir = AimAssist.ShotDirection(from, new Vector3(0.2f, 0f, 1f), 1f, Pitch.GoalHalfWidth);
            Vector3 end = from + dir * ((Pitch.HalfLength - from.z) / dir.z);
            Assert.LessOrEqual(Mathf.Abs(end.x), Pitch.GoalHalfWidth, "the ball crosses the goal line between the posts");
        }

        [Test]
        public void AShotAimedWellAwayFromTheGoalIsLeftAlone()
        {
            Vector3 from = new Vector3(0f, 0f, 10f);
            Vector3 dir = AimAssist.ShotDirection(from, Vector3.right, 1f, Pitch.GoalHalfWidth);
            Assert.AreEqual(1f, dir.x, 1e-4f, "a sideways push is a sideways shot");
        }

        [Test]
        public void TheAwayTeamShootsTowardsTheNegativeEnd()
        {
            Vector3 from = new Vector3(-2f, 0f, -30f);
            Vector3 dir = AimAssist.ShotDirection(from, new Vector3(0f, 0f, -1f), -1f, Pitch.GoalHalfWidth);
            Assert.Less(dir.z, 0f);
        }

        [Test]
        public void StandingStillWithNoAimShootsAtTheGoal()
        {
            Vector3 dir = AimAssist.ShotDirection(new Vector3(0f, 0f, 20f), Vector3.zero, 1f, Pitch.GoalHalfWidth);
            Assert.Greater(dir.z, 0.99f);
        }

        [Test]
        public void ThePassGoesToTheMateNearestTheAimLine()
        {
            var mates = new List<Vector3> { new Vector3(15f, 0f, 5f), new Vector3(1f, 0f, 14f), new Vector3(-20f, 0f, 2f) };
            Assert.AreEqual(1, AimAssist.PickPassTarget(Vector3.zero, Vector3.forward, mates));
        }

        [Test]
        public void NobodyInTheConeMeansNoTarget()
        {
            var mates = new List<Vector3> { new Vector3(0f, 0f, -12f) };
            Assert.AreEqual(-1, AimAssist.PickPassTarget(Vector3.zero, Vector3.forward, mates));
        }

        [Test]
        public void ABlockedMateIsSkippedAndOneBesideHimIsPicked()
        {
            var mates = new List<Vector3> { new Vector3(0f, 0f, 12f), new Vector3(6f, 0f, 12f) };
            Assert.AreEqual(1, AimAssist.PickPassTarget(Vector3.zero, Vector3.forward, mates, new List<bool> { true, false }));
        }

        [Test]
        public void PassPowerGrowsWithDistanceAndStaysInRange()
        {
            float shortPass = AimAssist.PassPower(5f, 20f), longPass = AimAssist.PassPower(35f, 20f);
            Assert.Less(shortPass, longPass);
            Assert.GreaterOrEqual(shortPass, 8f / 20f - 1e-4f);
            Assert.LessOrEqual(longPass, 1f);
        }
    }
}
