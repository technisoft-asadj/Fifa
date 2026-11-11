using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FootballSim.Ball;
using FootballSim.Core;
using FootballSim.Inputs;
using FootballSim.Match;
using FootballSim.Players;

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

        [UnityTest]
        public IEnumerator FoulAwardsFreeKickToFouledTeam()
        {
            var ball = MakeBall(new Vector3(0, 0.12f, 0));
            var mm = MakeMatch(ball);
            yield return new WaitForSeconds(0.3f);
            mm.AwardFoul(0, new Vector3(10f, 0f, 20f), "playerA");
            Assert.AreEqual(RestartType.FreeKick, mm.LastRestart);
            Assert.AreEqual(1, mm.HomeFouls);
            Assert.AreEqual(10f, ball.transform.position.x, 0.2f);
            Assert.AreEqual(20f, ball.transform.position.z, 0.2f);
        }

        [UnityTest]
        public IEnumerator SecondFoulBySamePlayerIsYellowCard()
        {
            var ball = MakeBall(new Vector3(0, 0.12f, 0));
            var mm = MakeMatch(ball);
            int cards = 0;
            mm.YellowCard += (team, total) => cards++;
            yield return new WaitForSeconds(0.3f);
            mm.AwardFoul(1, new Vector3(0f, 0f, 0f), "playerB");
            Assert.AreEqual(0, cards, "first foul is only a foul");
            yield return new WaitForSeconds(1.5f); // restart hold ends
            mm.AwardFoul(1, new Vector3(0f, 0f, 5f), "playerB");
            Assert.AreEqual(1, cards, "second foul by same player is a yellow card");
            Assert.AreEqual(1, mm.AwayYellows);
        }

        [UnityTest]
        public IEnumerator FoulDuringRestartHoldIsIgnored()
        {
            var ball = MakeBall(new Vector3(0, 0.12f, 0));
            var mm = MakeMatch(ball);
            yield return new WaitForSeconds(0.3f);
            mm.AwardFoul(0, new Vector3(0f, 0f, 5f), "p1");
            mm.AwardFoul(0, new Vector3(0f, 0f, 9f), "p2");
            Assert.AreEqual(1, mm.HomeFouls, "second foul while ball is held must not count");
        }

        PlayerController MakePlayer(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(root.transform);
            go.transform.position = pos;
            Object.Destroy(go.GetComponent<CapsuleCollider>());
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = Vector3.zero;
            var pc = go.AddComponent<PlayerController>();
            go.AddComponent<PlayerVisual>();
            return pc;
        }

        [UnityTest]
        public IEnumerator PlayerVisualBuildsHumanoidAndHidesCapsule()
        {
            var pc = MakePlayer(new Vector3(0, 1f, 0));
            yield return null;
            Assert.IsFalse(pc.GetComponent<Renderer>().enabled, "placeholder capsule must be hidden");
            var visual = pc.transform.Find("Visual");
            Assert.IsNotNull(visual, "visual root");
            Assert.GreaterOrEqual(visual.GetComponentsInChildren<Renderer>().Length, 12, "head, torso, limbs, boots");
            Assert.IsNotNull(visual.Find("Body/HipL")); Assert.IsNotNull(visual.Find("Body/HipR"));
            Assert.AreEqual(0, visual.GetComponentsInChildren<Collider>().Length, "visuals must not add colliders");
        }

        [UnityTest]
        public IEnumerator RunningSwingsLegsAndIdleReturnsToRest()
        {
            var pc = MakePlayer(new Vector3(0, 1f, 0));
            var ai = new AIInputSource();
            pc.SetInput(ai, false);
            yield return null;
            var hip = pc.transform.Find("Visual/Body/HipR");
            ai.MoveVector = new Vector2(0f, 1f); ai.SprintFlag = true;
            float maxAngle = 0f;
            for (int i = 0; i < 60; i++)
            {
                yield return null;
                maxAngle = Mathf.Max(maxAngle, Mathf.Abs(Mathf.DeltaAngle(0f, hip.localEulerAngles.x)));
            }
            Assert.Greater(maxAngle, 15f, "legs should swing while sprinting");
            ai.MoveVector = Vector2.zero; ai.SprintFlag = false;
            yield return new WaitForSeconds(2f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f, hip.localEulerAngles.x)), 6f, "legs settle when standing");
        }

        [UnityTest]
        public IEnumerator KickAnimationMovesTheKickingLeg()
        {
            var pc = MakePlayer(new Vector3(0, 1f, 0));
            yield return null;
            var visual = pc.GetComponent<PlayerVisual>();
            var hip = pc.transform.Find("Visual/Body/HipR");
            visual.PlayKick(true);
            float maxAngle = 0f;
            for (int i = 0; i < 20; i++)
            {
                yield return null;
                maxAngle = Mathf.Max(maxAngle, Mathf.Abs(Mathf.DeltaAngle(0f, hip.localEulerAngles.x)));
            }
            Assert.Greater(maxAngle, 30f, "kick should swing the leg");
        }

        TouchControls MakeTouch()
        {
            var go = new GameObject("Touch");
            go.transform.SetParent(root.transform);
            var t = go.AddComponent<TouchControls>();
            t.forceShow = true;
            return t;
        }

        [UnityTest]
        public IEnumerator TouchControlsShowAndExposeInput()
        {
            var t = MakeTouch();
            yield return null;
            Assert.IsTrue(t.IsVisible, "forced visible while match is running");
            Assert.AreEqual(Vector2.zero, t.Move);
            t.StickForTest.SetValueForTest(new Vector2(0.5f, 1f));
            Assert.AreEqual(new Vector2(0.5f, 1f), t.Move);
            t.PassForTest.Press();
            Assert.IsTrue(t.PassPressed, "pass latch fires");
            Assert.IsFalse(t.PassPressed, "pass latch is consumed");
        }

        [UnityTest]
        public IEnumerator TouchShootHoldThenReleaseFiresOnce()
        {
            var t = MakeTouch();
            yield return null;
            t.ShootForTest.Press();
            Assert.IsTrue(t.ShootHeld);
            Assert.IsFalse(t.ShootReleased);
            t.ShootForTest.Release();
            Assert.IsFalse(t.ShootHeld);
            Assert.IsTrue(t.ShootReleased);
            Assert.IsFalse(t.ShootReleased);
        }

        [UnityTest]
        public IEnumerator TouchControlsHideWhenGameIsPaused()
        {
            var t = MakeTouch();
            yield return null;
            Assert.IsTrue(t.IsVisible);
            Time.timeScale = 0f;
            yield return null;
            bool hidden = !t.IsVisible;
            Time.timeScale = 1f;
            yield return null;
            Assert.IsTrue(hidden, "controls hidden while menus pause the game");
            Assert.IsTrue(t.IsVisible, "controls return when play resumes");
        }

        [UnityTest]
        public IEnumerator PlayerInputMergesTouchWithKeyboard()
        {
            var t = MakeTouch();
            var src = new GameObject("Src").AddComponent<PlayerInputSource>();
            src.transform.SetParent(root.transform);
            src.touch = t;
            yield return null;
            t.StickForTest.SetValueForTest(new Vector2(0f, 1f));
            Assert.AreEqual(new Vector2(0f, 1f), src.Move, "touch stick drives movement when no keys are pressed");
            t.PassForTest.Press();
            Assert.IsTrue(src.PassPressed);
            Assert.IsFalse(src.PassPressed);
        }
    }
}
