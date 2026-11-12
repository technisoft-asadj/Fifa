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
        public IEnumerator LiveMatchAccumulatesStatsAndSummaryText()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            Time.timeScale = 4f;
            yield return new WaitForSeconds(24f); // 6 real seconds of AI play
            Time.timeScale = 1f;
            var s = match.Stats;
            Assert.Greater(s.possessionSeconds[0] + s.possessionSeconds[1], 1f, "someone must have had the ball");
            Assert.AreEqual(100, s.PossessionPercent(0) + s.PossessionPercent(1));
            Assert.Greater(s.passes[0] + s.passes[1] + s.shots[0] + s.shots[1], 0, "AI should pass or shoot within 24 game seconds");
            Assert.LessOrEqual(s.shotsOnTarget[0], s.shots[0]);
            Assert.LessOrEqual(s.shotsOnTarget[1], s.shots[1]);
            StringAssert.Contains("Possession", FootballSim.UI.MatchHUD.StatsSummary(match));
        }

        IEnumerator FoulWhenReady(MatchManager match, int team, Vector3 spot, object key)
        {
            float t = 0f;
            while ((match.InRestartHold || match.InGoalPause) && t < 8f) { t += Time.deltaTime; yield return null; }
            int before = match.HomeFouls + match.AwayFouls;
            match.AwardFoul(team, spot, key);
            Assert.AreEqual(before + 1, match.HomeFouls + match.AwayFouls, "foul must be accepted");
        }

        [UnityTest]
        public IEnumerator RedCardRemovesPlayerAndTeamPlaysOn()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            TeamController home = null;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None)) if (t.teamId == 0) home = t;
            yield return null;
            var victim = home.Controlled; // the human-controlled player: control must move to a team-mate
            Assert.IsNotNull(victim);
            for (int i = 0; i < 4; i++) yield return FoulWhenReady(match, 0, new Vector3(0f, 0f, 10f), victim);
            Assert.AreEqual(1, match.HomeReds);
            Assert.IsFalse(victim.gameObject.activeSelf, "sent-off player leaves the pitch");
            Assert.AreEqual(10, home.ActiveCount);
            Assert.IsNotNull(home.Controlled);
            Assert.AreNotSame(victim, home.Controlled);
            Assert.IsTrue(home.Controlled.gameObject.activeSelf);
            yield return new WaitForSeconds(2f); // play continues without errors
        }

        [UnityTest]
        public IEnumerator PenaltyClearsTheBoxAndPutsKeeperOnLine()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            var teams = Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None);
            yield return null;
            // Away (team 1) defends +Z; put two outfielders in that box first
            foreach (var t in teams)
            {
                int moved = 0;
                foreach (var p in t.players)
                    if (!p.isGoalkeeper && moved++ < 2) p.TeleportTo(new Vector3(4f * moved * (t.teamId == 0 ? 1 : -1), 1f, Pitch.HalfLength - 6f), Quaternion.identity);
            }
            yield return FoulWhenReady(match, 1, new Vector3(3f, 0f, Pitch.HalfLength - 8f), "awayDefender");
            Assert.AreEqual(RestartType.Penalty, match.LastRestart);
            Assert.AreEqual(Pitch.HalfLength - Pitch.PenaltySpotDistance, ball.transform.position.z, 0.2f);
            yield return null;
            int inBox = 0, keepersOnLine = 0;
            foreach (var t in teams)
                foreach (var p in t.players)
                {
                    if (p.isGoalkeeper && t.teamId == 1)
                    {
                        if (Mathf.Abs(p.transform.position.z - Pitch.HalfLength) < 1.5f && Mathf.Abs(p.transform.position.x) < 1f) keepersOnLine++;
                        continue;
                    }
                    if (Pitch.InPenaltyArea(p.transform.position, Pitch.HalfLength)) inBox++;
                }
            Assert.AreEqual(1, inBox, "only the taker stays in the box");
            Assert.AreEqual(1, keepersOnLine);
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
