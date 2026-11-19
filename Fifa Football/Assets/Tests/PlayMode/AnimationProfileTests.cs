using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FootballSim.Players;

namespace FootballSim.Tests
{
    /// <summary>
    /// Editor-side cost of the animated bodies. Editor frame times are not phone frame times, but the difference between the three body
    /// modes on the same machine shows what the Animators and skinned meshes add, and it guards against a change that makes it much worse.
    /// </summary>
    public class AnimationProfileTests
    {
        const string SceneName = "FootballPrototype";

        // the match scene must not stay loaded for the next test class (see AnimatedPlayerTests)
        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            var keep = SceneManager.CreateScene("AnimationProfileTestsEmpty");
            SceneManager.SetActiveScene(keep);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (sc != keep && sc.isLoaded && sc.name == SceneName) yield return SceneManager.UnloadSceneAsync(sc);
            }
            yield return SceneManager.UnloadSceneAsync(keep);
        }

        static float AverageFrameMs(int frames, System.Collections.Generic.List<float> sink)
        {
            float sum = 0f;
            for (int i = 0; i < frames; i++) sum += sink[i];
            return sum / frames * 1000f;
        }

        IEnumerator Measure(System.Collections.Generic.List<float> frames, int count)
        {
            frames.Clear();
            float last = Time.realtimeSinceStartup;
            for (int i = 0; i < count; i++)
            {
                yield return null;
                float now = Time.realtimeSinceStartup;
                frames.Add(now - last); last = now;
            }
        }

        [UnityTest]
        public IEnumerator TwentyTwoAnimatedPlayersAddABoundedCostInTheEditor()
        {
            if (!AnimatedPlayerVisual.PrefabAvailable) Assert.Ignore("the animated player files are not in this checkout");
            FootballSim.UI.MatchHUD.SplashShown = true;
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            Time.timeScale = 1f;
            var hud = Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>();
            int oldVsync = QualitySettings.vSyncCount, oldFps = Application.targetFrameRate;
            var oldMode = PlayerVisual.Mode;
            var frames = new System.Collections.Generic.List<float>();
            try
            {
                QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1; // measure the work, not the frame cap
                hud.BeginMatch();
                PlayerVisual.SetMode(PlayerVisual.ModelMode.Off);
                for (int i = 0; i < 60; i++) yield return null;
                yield return Measure(frames, 240);
                float off = AverageFrameMs(240, frames);

                PlayerVisual.SetMode(PlayerVisual.ModelMode.All);
                for (int i = 0; i < 60; i++) yield return null;
                int animators = 0;
                foreach (var v in Object.FindObjectsByType<PlayerVisual>(FindObjectsSortMode.None)) if (v.AnimatedActive) animators++;
                yield return Measure(frames, 240);
                float all = AverageFrameMs(240, frames);

                Debug.Log($"[anim-profile] editor avg frame: simple bodies {off:0.00} ms, 22 animated {all:0.00} ms (+{all - off:0.00} ms), animated bodies active {animators}");
                Assert.AreEqual(22, animators, "all 22 players wear the animated body");
                Assert.Less(all - off, 12f, "22 animated bodies add under 12 ms per frame in the editor");
            }
            finally
            {
                PlayerVisual.SetMode(oldMode);
                QualitySettings.vSyncCount = oldVsync; Application.targetFrameRate = oldFps;
            }
        }
    }
}
