using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FootballSim.Ball;
using FootballSim.Core;
using FootballSim.Match;

namespace FootballSim.Tests
{
    public class BallAndMatchTests
    {
        GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("TestRoot");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(root.transform);
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(300, 1, 300);
        }

        [TearDown]
        public void TearDown() { Object.Destroy(root); }

        BallController MakeBall(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(root.transform);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.22f;
            go.AddComponent<Rigidbody>();
            return go.AddComponent<BallController>();
        }

        MatchManager MakeMatch(BallController ball)
        {
            var mm = new GameObject("Match").AddComponent<MatchManager>();
            mm.transform.SetParent(root.transform);
            typeof(MatchManager).GetField("ball", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(mm, ball);
            return mm;
        }

        [UnityTest]
        public IEnumerator DroppedBallBouncesAndComesToRest()
        {
            var ball = MakeBall(new Vector3(0, 3f, 0));
            yield return new WaitForSeconds(6f);
            Assert.Less(ball.Velocity.magnitude, 0.3f, "ball should settle");
            Assert.Less(ball.transform.position.y, 0.3f, "ball should be on the ground");
        }

        [UnityTest]
        public IEnumerator KickedBallTravelsAndSlowsDown()
        {
            var ball = MakeBall(new Vector3(0, 0.12f, 0));
            yield return new WaitForSeconds(0.5f);
            ball.Kick(Vector3.forward, 15f, Vector3.zero);
            yield return new WaitForSeconds(0.3f);
            float earlySpeed = ball.Velocity.magnitude;
            yield return new WaitForSeconds(3f);
            Assert.Greater(ball.transform.position.z, 8f, "ball should travel forward");
            Assert.Less(ball.Velocity.magnitude, earlySpeed, "ball should lose speed");
            Assert.Greater(ball.transform.position.y, -0.5f, "ball must not fall through ground");
        }

        [UnityTest]
        public IEnumerator KickSpeedIsClampedToMax()
        {
            var ball = MakeBall(new Vector3(0, 0.12f, 0));
            yield return new WaitForSeconds(0.3f);
            ball.Kick(Vector3.forward, 500f, Vector3.zero);
            yield return null;
            Assert.LessOrEqual(ball.Velocity.magnitude, 38.5f);
        }

        [UnityTest]
        public IEnumerator SpinCurvesTheBall()
        {
            var straight = MakeBall(new Vector3(-5, 0.12f, 0));
            var curved = MakeBall(new Vector3(5, 0.12f, 0));
            yield return new WaitForSeconds(0.3f);
            straight.Kick(Vector3.forward + Vector3.up * 0.15f, 25f, Vector3.zero);
            curved.Kick(Vector3.forward + Vector3.up * 0.15f, 25f, new Vector3(0, 40f, 0));
            yield return new WaitForSeconds(1.2f);
            float drift = Mathf.Abs((curved.transform.position.x - 5f) - (straight.transform.position.x + 5f));
            Assert.Greater(drift, 0.2f, "sidespin should deflect the ball sideways");
        }

        [UnityTest]
        public IEnumerator FrozenBallStaysPutAndThenMoves()
        {
            var ball = MakeBall(new Vector3(0, 0.12f, 0));
            yield return new WaitForSeconds(0.5f);
            ball.Freeze(true);
            Vector3 p = ball.transform.position;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(p, ball.transform.position);
            ball.Freeze(false);
            ball.Kick(Vector3.right, 10f, Vector3.zero);
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(ball.transform.position.x, p.x + 1f);
        }

        [UnityTest]
        public IEnumerator GoalsUpdateTheScoreAndIgnoreDuplicates()
        {
            var ball = MakeBall(new Vector3(0, 0.12f, 0));
            var mm = MakeMatch(ball);
            yield return null;
            mm.OnGoal(0);
            Assert.AreEqual(1, mm.HomeScore);
            mm.OnGoal(1); // ignored: kick-off reset in progress after a goal
            Assert.AreEqual(0, mm.AwayScore, "second goal during reset must not count");
        }

        [UnityTest]
        public IEnumerator BallLeavingTouchlineTriggersThrowInForOtherTeam()
        {
            var ball = MakeBall(new Vector3(0, 0.12f, 0));
            var mm = MakeMatch(ball);
            yield return new WaitForSeconds(0.3f);
            ball.LastTouchTeam = 0;
            ball.ResetTo(new Vector3(Pitch.HalfWidth + 1f, 0.12f, 10f));
            yield return null;
            yield return null;
            Assert.AreEqual(RestartType.ThrowIn, mm.LastRestart);
            Assert.AreEqual(Pitch.HalfWidth - 0.3f, ball.transform.position.x, 0.2f);
        }

        [UnityTest]
        public IEnumerator DefenderLastTouchOnEndLineGivesCorner()
        {
            var ball = MakeBall(new Vector3(0, 0.12f, 0));
            var mm = MakeMatch(ball);
            yield return new WaitForSeconds(0.3f);
            ball.LastTouchTeam = 1; // away defends the +Z end
            ball.ResetTo(new Vector3(20f, 0.12f, Pitch.HalfLength + 1f));
            yield return null;
            yield return null;
            Assert.AreEqual(RestartType.Corner, mm.LastRestart);
        }

        [UnityTest]
        public IEnumerator AttackerLastTouchOnEndLineGivesGoalKick()
        {
            var ball = MakeBall(new Vector3(0, 0.12f, 0));
            var mm = MakeMatch(ball);
            yield return new WaitForSeconds(0.3f);
            ball.LastTouchTeam = 0; // home attacks +Z
            ball.ResetTo(new Vector3(20f, 0.12f, Pitch.HalfLength + 1f));
            yield return null;
            yield return null;
            Assert.AreEqual(RestartType.GoalKick, mm.LastRestart);
        }
    }
}
