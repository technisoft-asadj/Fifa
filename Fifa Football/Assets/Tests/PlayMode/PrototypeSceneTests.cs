using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FootballSim.Ball;
using FootballSim.Core;
using FootballSim.FootballAI;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.Tests
{
    /// <summary>Tests against the real saved FootballPrototype scene, not objects built in code.</summary>
    public class PrototypeSceneTests
    {
        IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("FootballPrototype", LoadSceneMode.Single);
            yield return null;
            Time.timeScale = 1f; // the start screen pauses the game
        }

        [TearDown]
        public void TearDown() { Time.timeScale = 1f; }

        [UnityTest]
        public IEnumerator SceneHasNoMissingScripts()
        {
            yield return LoadScene();
            int missing = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (var c in go.GetComponents<Component>())
                    if (c == null) { missing++; Debug.LogError("Missing script on " + go.name); }
            Assert.AreEqual(0, missing, "every serialised component must resolve (class name must match file name)");
        }

        [UnityTest]
        public IEnumerator SceneHasTwoTeamsOfElevenWithGoalkeepersAndTwoGoals()
        {
            yield return LoadScene();
            var teams = Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None);
            Assert.AreEqual(2, teams.Length);
            foreach (var t in teams)
            {
                Assert.AreEqual(11, t.players.Count);
                int keepers = 0; foreach (var p in t.players) if (p.isGoalkeeper) keepers++;
                Assert.AreEqual(1, keepers);
            }
            Assert.AreEqual(2, Object.FindObjectsByType<GoalTrigger>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator BallInsideAwayGoalScoresForHome()
        {
            yield return LoadScene();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            Assert.AreEqual(0, match.HomeScore);
            ball.ResetTo(new Vector3(0f, 1.2f, Pitch.HalfLength + 1.2f)); // inside the +Z goal mouth
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, match.HomeScore, "home attacks +Z");
            Assert.AreEqual(0, match.AwayScore);
        }

        [UnityTest]
        public IEnumerator PauseStopsTimeAndResumeRestoresIt()
        {
            yield return LoadScene();
            var hud = Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>();
            hud.TogglePause();
            Assert.IsFalse(hud.IsPaused, "cannot pause on the start screen");
            hud.BeginMatch();
            yield return null;
            hud.TogglePause();
            Assert.IsTrue(hud.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            hud.TogglePause();
            Assert.IsFalse(hud.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator TouchControlsPreviewWhilePausedAndClampSettings()
        {
            yield return LoadScene();
            var hud = Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>();
            var touch = Object.FindFirstObjectByType<FootballSim.Inputs.TouchControls>();
            touch.forceShow = true;
            hud.BeginMatch();
            yield return null; yield return null;
            Assert.IsTrue(touch.IsVisible, "visible during play");
            hud.TogglePause();
            yield return null; yield return null;
            Assert.IsTrue(touch.IsVisible, "previewed while paused so settings can be seen");
            float scale = touch.controlScale, opacity = touch.controlOpacity;
            touch.SetAppearance(9f, -1f);
            Assert.AreEqual(1.6f, touch.controlScale, 1e-4f);
            Assert.AreEqual(0.2f, touch.controlOpacity, 1e-4f);
            yield return null; yield return null;
            Assert.IsTrue(touch.IsVisible, "still visible after rebuild");
            touch.SetAppearance(scale, opacity); // restore saved prefs
            hud.TogglePause();
        }

        [UnityTest]
        public IEnumerator BallInsideHomeGoalScoresForAway()
        {
            yield return LoadScene();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            ball.ResetTo(new Vector3(0f, 1.2f, -Pitch.HalfLength - 1.2f));
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, match.AwayScore);
            Assert.AreEqual(0, match.HomeScore);
        }
    }
}
