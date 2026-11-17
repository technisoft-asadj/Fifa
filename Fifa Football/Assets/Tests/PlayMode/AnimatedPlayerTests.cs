using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FootballSim.Players;

namespace FootballSim.Tests
{
    /// <summary>
    /// Phase 2 of the animation programme: the rigged player's locomotion. These need the Mixamo files (git-ignored); without them the tests are
    /// Inconclusive rather than failing, so a fresh checkout stays green.
    /// </summary>
    public class AnimatedPlayerTests
    {
        PlayerVisual.ModelMode savedMode;

        [SetUp] public void Save() { savedMode = PlayerVisual.Mode; }
        [TearDown] public void Restore() { PlayerVisual.Mode = savedMode; }

        IEnumerator LoadScene()
        {
            FootballSim.UI.MatchHUD.SplashShown = true;
            yield return SceneManager.LoadSceneAsync("FootballPrototype", LoadSceneMode.Single);
            yield return null;
            Time.timeScale = 1f;
        }

        static PlayerVisual HomeOutfielder()
        {
            foreach (var pv in Object.FindObjectsByType<PlayerVisual>(FindObjectsSortMode.None))
            {
                var pc = pv.GetComponent<PlayerController>();
                if (pc != null && pc.TeamId == 0 && !pc.isGoalkeeper) return pv;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator SimpleModeKeepsThePrimitiveBodyAndAnimatedAllShowsTheRiggedOneOnEveryPlayer()
        {
            if (!AnimatedPlayerVisual.PrefabAvailable) Assert.Inconclusive("Assets/Resources/Animated is not built (Mixamo files missing)");
            PlayerVisual.Mode = PlayerVisual.ModelMode.Off;
            yield return LoadScene();
            var pv = HomeOutfielder();
            Assert.IsFalse(pv.AnimatedActive, "Simple: primitive body");
            Assert.IsNotNull(pv.Body);

            PlayerVisual.SetMode(PlayerVisual.ModelMode.All);
            int animatedCount = 0, total = 0;
            foreach (var v in Object.FindObjectsByType<PlayerVisual>(FindObjectsSortMode.None)) { total++; if (v.AnimatedActive) animatedCount++; }
            Assert.AreEqual(22, total); Assert.AreEqual(22, animatedCount, "all 22 wear the rigged body");
            Assert.IsFalse(pv.transform.Find("Visual").gameObject.activeSelf, "the primitive body is hidden, not destroyed (it is the fallback)");
            Assert.IsNotNull(pv.Animated.Animator); Assert.IsTrue(pv.Animated.Animator.isHuman);

            PlayerVisual.SetMode(PlayerVisual.ModelMode.Off);
            Assert.IsFalse(pv.AnimatedActive); Assert.IsTrue(pv.transform.Find("Visual").gameObject.activeSelf, "back to the primitive body");
        }

        [UnityTest]
        public IEnumerator AnimatedOneModeFollowsTheControlledPlayerOnly()
        {
            if (!AnimatedPlayerVisual.PrefabAvailable) Assert.Inconclusive("Assets/Resources/Animated is not built (Mixamo files missing)");
            PlayerVisual.Mode = PlayerVisual.ModelMode.Off;
            yield return LoadScene();
            PlayerVisual.SetMode(PlayerVisual.ModelMode.One);
            int animatedCount = 0;
            PlayerVisual only = null;
            foreach (var v in Object.FindObjectsByType<PlayerVisual>(FindObjectsSortMode.None)) if (v.AnimatedActive) { animatedCount++; only = v; }
            Assert.LessOrEqual(animatedCount, 1, "at most the one controlled player");

            var pv = HomeOutfielder();
            var pc = pv.GetComponent<PlayerController>();
            var oldInput = pc.Input;
            pc.SetInput(oldInput, true);
            Assert.IsTrue(pv.AnimatedActive, "taking control gives the rigged body");
            pc.SetInput(oldInput, false);
            Assert.IsFalse(pv.AnimatedActive, "losing control gives the primitive body back");
        }

        /// <summary>Plays the locomotion tree at a fixed speed with the Animator stepped by hand, and returns the planted toe's backward speed in the model's own space.</summary>
        static float PlantedFootSpeed(Animator animator, float speed, float[] hipsJump = null)
        {
            animator.enabled = false;
            animator.SetFloat("Speed", speed);
            for (int i = 0; i < 90; i++) animator.Update(1f / 60f);            // settle into the cycle
            var model = animator.transform;
            var toeL = animator.GetBoneTransform(HumanBodyBones.LeftToes); var toeR = animator.GetBoneTransform(HumanBodyBones.RightToes);
            int n = 180; float dt = 1f / 60f;
            var z = new float[2][]; var y = new float[2][];
            for (int f = 0; f < 2; f++) { z[f] = new float[n]; y[f] = new float[n]; }
            for (int i = 0; i < n; i++)
            {
                animator.Update(dt);
                var l = model.InverseTransformPoint(toeL.position); var r = model.InverseTransformPoint(toeR.position);
                z[0][i] = l.z; y[0][i] = l.y; z[1][i] = r.z; y[1][i] = r.y;
            }
            var planted = new List<float>();
            for (int f = 0; f < 2; f++)
            {
                float minY = float.MaxValue; for (int i = 0; i < n; i++) minY = Mathf.Min(minY, y[f][i]);
                for (int i = 1; i < n - 1; i++)
                {
                    if (y[f][i] > minY + 0.025f) continue;
                    float v = -(z[f][i + 1] - z[f][i - 1]) / (2f * dt);
                    if (v > 0.3f) planted.Add(v);
                }
            }
            if (planted.Count == 0) return 0f;
            planted.Sort();
            return planted[planted.Count / 2];
        }

        [UnityTest]
        public IEnumerator PlantedFeetMoveBackAtTheSpeedTheBodyIsGoingAtSeveralSpeeds()
        {
            if (!AnimatedPlayerVisual.PrefabAvailable) Assert.Inconclusive("Assets/Resources/Animated is not built (Mixamo files missing)");
            PlayerVisual.Mode = PlayerVisual.ModelMode.Off;
            yield return LoadScene();
            PlayerVisual.SetMode(PlayerVisual.ModelMode.All);
            yield return null; yield return null;
            var animator = HomeOutfielder().Animated.Animator;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.IsNotNull(animator.runtimeAnimatorController, "controller assigned");
            var report = new System.Text.StringBuilder();
            // a jog, a quick run and a sprint from the game's own range (jog 3.2-4.5, sprint 6-9.5 m/s); the planted foot must travel back about as fast as the body goes forward
            foreach (float v in new[] { 3.2f, 4.5f, 6.5f, 8.0f })
            {
                float foot = PlantedFootSpeed(animator, v);
                float slide = Mathf.Abs(foot - v);
                report.AppendLine($"body {v:0.0} m/s -> planted foot {foot:0.00} m/s (slide {slide:0.00} m/s, {slide / v * 100f:0}%)");
                Assert.Less(slide, Mathf.Max(0.8f, v * 0.25f), $"at {v} m/s the planted foot slides {slide:0.00} m/s");
            }
            Debug.Log("[anim] foot slide\n" + report);
        }

        [UnityTest]
        public IEnumerator RampingTheSpeedFromStandingToSprintHasNoPops()
        {
            if (!AnimatedPlayerVisual.PrefabAvailable) Assert.Inconclusive("Assets/Resources/Animated is not built (Mixamo files missing)");
            PlayerVisual.Mode = PlayerVisual.ModelMode.Off;
            yield return LoadScene();
            PlayerVisual.SetMode(PlayerVisual.ModelMode.All);
            yield return null; yield return null;
            var animator = HomeOutfielder().Animated.Animator;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.IsNotNull(animator.runtimeAnimatorController, "controller assigned");
            animator.enabled = false;
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var toe = animator.GetBoneTransform(HumanBodyBones.LeftToes);
            var model = animator.transform;
            float dt = 1f / 60f, maxHipStep = 0f, maxToeStep = 0f, maxHipTurn = 0f;
            Vector3 lastHips = Vector3.zero, lastToe = Vector3.zero; Quaternion lastRot = Quaternion.identity; bool first = true;
            // 2 s standing, 3 s ramp to 9.5 m/s, 2 s sprint, 2 s back to a stop
            int steps = (int)(9f / dt);
            for (int i = 0; i < steps; i++)
            {
                float t = i * dt;
                float v = t < 2f ? 0f : t < 5f ? Mathf.Lerp(0f, 9.5f, (t - 2f) / 3f) : t < 7f ? 9.5f : Mathf.Lerp(9.5f, 0f, (t - 7f) / 2f);
                animator.SetFloat("Speed", v, 0.08f, dt);
                animator.Update(dt);
                var h = model.InverseTransformPoint(hips.position); var p = model.InverseTransformPoint(toe.position); var rot = Quaternion.Inverse(model.rotation) * hips.rotation;
                if (!first)
                {
                    maxHipStep = Mathf.Max(maxHipStep, (h - lastHips).magnitude);
                    maxToeStep = Mathf.Max(maxToeStep, (p - lastToe).magnitude);
                    maxHipTurn = Mathf.Max(maxHipTurn, Quaternion.Angle(rot, lastRot));
                }
                first = false; lastHips = h; lastToe = p; lastRot = rot;
            }
            Debug.Log($"[anim] ramp: max hip step {maxHipStep * 100f:0.0} cm/frame, max toe step {maxToeStep * 100f:0.0} cm/frame, max hip turn {maxHipTurn:0.0} deg/frame");
            Assert.Less(maxHipStep, 0.08f, "the hips must not jump between frames");
            Assert.Less(maxToeStep, 0.35f, "a foot swings fast in a sprint but must not teleport");
            Assert.Less(maxHipTurn, 15f, "no sudden spin of the hips");
        }
    }
}
