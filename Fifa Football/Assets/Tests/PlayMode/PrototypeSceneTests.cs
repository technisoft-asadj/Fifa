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
            yield return new WaitForSeconds(40f); // 10 real seconds of AI play
            Time.timeScale = 1f;
            var s = match.Stats;
            Assert.Greater(s.possessionSeconds[0] + s.possessionSeconds[1], 1f, "someone must have had the ball");
            Assert.AreEqual(100, s.PossessionPercent(0) + s.PossessionPercent(1));

            // AI only passes under pressure, so whether it does in a short window is luck: force one pass instead
            TeamController away = null;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None)) if (t.teamId == 1) away = t;
            var ball = Object.FindFirstObjectByType<BallController>();
            float wait = 0f;
            while ((match.InRestartHold || match.InGoalPause) && wait < 8f) { wait += Time.deltaTime; yield return null; }
            PlayerController passer = null;
            foreach (var p in away.players) if (!p.isGoalkeeper && TeamController.IsOn(p)) { passer = p; break; }
            int before = s.passes[1];
            ball.ResetTo(passer.transform.position + passer.transform.forward * 0.4f + Vector3.up * 0.12f);
            away.brains[away.players.IndexOf(passer)].Input.QueuePass(passer.transform.forward, 0.5f);
            yield return null; yield return null;
            // At least one: a second away player (the AI chaser) may legitimately pass in the same two frames
            Assert.GreaterOrEqual(s.passes[1], before + 1, "a queued pass by an AI player is counted");
            Assert.LessOrEqual(s.shotsOnTarget[0], s.shots[0]);
            Assert.LessOrEqual(s.shotsOnTarget[1], s.shots[1]);
            StringAssert.Contains("Possession", FootballSim.UI.MatchHUD.StatsSummary(match));
            float lowest = 1f;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None))
                foreach (var p in t.players) lowest = Mathf.Min(lowest, p.Stamina);
            Assert.Less(lowest, 1f, "sprinting AI players must have used some stamina");
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

        static void AssertKickoffLayout(MatchManager match, TeamController[] teams)
        {
            foreach (var t in teams)
                foreach (var p in t.players)
                {
                    if (!TeamController.IsOn(p)) continue;
                    var pos = p.transform.position;
                    float fromCentre = new Vector2(pos.x, pos.z).magnitude;
                    if (t.teamId != match.KickoffTeam)
                    {
                        Assert.GreaterOrEqual(fromCentre, MatchManager.CentreCircleRadius - 0.1f, "non-kicking team stays out of the centre circle");
                        Assert.AreEqual(-t.attackDir, Mathf.Sign(pos.z), "and in its own half");
                    }
                }
        }

        [UnityTest]
        public IEnumerator KickoffGoesToTheTeamThatConcedesAndOtherTeamStaysOut()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            var teams = Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None);
            yield return null;
            Assert.AreEqual(0, match.KickoffTeam, "home starts");
            Assert.IsTrue(match.KickoffPending);
            AssertKickoffLayout(match, teams);
            foreach (var t in teams) if (t.teamId == 0)
            {
                float nearest = float.MaxValue;
                foreach (var p in t.players) if (!p.isGoalkeeper) nearest = Mathf.Min(nearest, p.transform.position.magnitude);
                Assert.Less(nearest, 1.5f, "home taker stands at the ball");
            }

            // Home scores: away must take the next kick-off
            match.OnGoal(0);
            Assert.AreEqual(1, match.KickoffTeam);
            float t0 = 0f;
            while (match.InGoalPause && t0 < 8f) { t0 += Time.deltaTime; yield return null; }
            yield return null;
            Assert.AreEqual(RestartType.KickOff, match.LastRestart);
            AssertKickoffLayout(match, teams);
            foreach (var t in teams) if (t.teamId == 1)
            {
                float nearest = float.MaxValue;
                foreach (var p in t.players) if (!p.isGoalkeeper) nearest = Mathf.Min(nearest, p.transform.position.magnitude);
                Assert.Less(nearest, 1.5f, "away taker stands at the ball");
            }
        }

        [UnityTest]
        public IEnumerator FullMatchPlaysThroughHalfTimeToFullTimeWithoutErrors()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            var teams = Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None);
            match.SetDurationMinutes(1.5f); // 90 game seconds, half-time at 45
            bool fullTime = false; match.FullTime += () => fullTime = true;
            Time.timeScale = 4f;
            float guard = 0f, lastClock = 0f, stuck = 0f; Vector3 lastBall = ball.transform.position;
            while (!fullTime && guard < 60f) // 90 game s is ~23 real s at 4x; 60 is a generous cap
            {
                guard += Time.unscaledDeltaTime;
                Assert.GreaterOrEqual(match.RealSecondsElapsed, lastClock, "clock never runs backwards");
                lastClock = match.RealSecondsElapsed;
                var bp = ball.transform.position;
                Assert.IsFalse(float.IsNaN(bp.x) || float.IsNaN(bp.y) || float.IsNaN(bp.z), "ball position is a number");
                Assert.Less(Mathf.Abs(bp.x), 80f); Assert.Less(Mathf.Abs(bp.z), 90f); Assert.Less(bp.y, 60f);
                foreach (var t in teams) foreach (var p in t.players)
                {
                    var pp = p.transform.position;
                    Assert.IsFalse(float.IsNaN(pp.x) || float.IsNaN(pp.z), "player position is a number");
                    Assert.Less(Mathf.Abs(pp.x), 120f); Assert.Less(Mathf.Abs(pp.z), 120f);
                }
                // The game must never stand still for long (deadlocked kick-off, ball stuck, etc.)
                stuck = (bp - lastBall).sqrMagnitude > 0.01f || match.InGoalPause || match.InRestartHold ? 0f : stuck + Time.unscaledDeltaTime;
                lastBall = bp;
                Assert.Less(stuck, 12f, "ball stopped moving in live play for 12 real seconds");
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.IsTrue(fullTime, "match reached full time");
            Assert.IsTrue(match.HalfTimeReached);
            Assert.IsTrue(match.SidesSwapped);
            Assert.AreEqual(match.HomeScore + match.AwayScore, match.Goals.Count, "every goal is logged");
            Assert.AreEqual(100, match.Stats.PossessionPercent(0) + match.Stats.PossessionPercent(1));
        }

        PlayerController FirstOutfielder(int teamId)
        {
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None))
                if (t.teamId == teamId) foreach (var p in t.players) if (!p.isGoalkeeper) return p;
            return null;
        }

        [UnityTest]
        public IEnumerator GoalIsRecordedWithScorerAndMinute()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            var scorer = FirstOutfielder(0);
            yield return null;
            ball.LastTouchTeam = 0; ball.LastToucher = scorer;
            ball.ResetTo(new Vector3(0f, 1.2f, Pitch.HalfLength + 1.2f));
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, match.Goals.Count);
            var g = match.Goals[0];
            Assert.AreEqual(0, g.team);
            Assert.AreEqual(scorer.name, g.scorer);
            Assert.GreaterOrEqual(g.minute, 1);
            Assert.IsFalse(g.ownGoal);
            StringAssert.Contains(scorer.name, FootballSim.UI.MatchHUD.StatsSummary(match));
        }

        PlayerController SecondOutfielder(int teamId)
        {
            int n = 0;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None))
                if (t.teamId == teamId) foreach (var p in t.players) if (!p.isGoalkeeper && ++n == 2) return p;
            return null;
        }

        [UnityTest]
        public IEnumerator GoalkeeperSavesSomeShotsAndCountsThem()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            var shooter = FirstOutfielder(0);
            yield return null;
            float[] targets = { 0f, 2.5f, -2.5f, 0f, 2.5f, -2.5f, 0f, 2.5f, -2.5f, 0f, 2.5f, -2.5f };
            int goals0 = match.HomeScore;
            foreach (float tx in targets)
            {
                float wait = 0f;
                while ((match.InRestartHold || match.InGoalPause) && wait < 8f) { wait += Time.deltaTime; yield return null; }
                ball.ResetTo(new Vector3(0f, 0.4f, 44f)); // close enough that a rolling ball does not lose its pace on the way
                ball.LastTouchTeam = 0; ball.LastToucher = shooter;
                Vector3 target = new Vector3(tx, 0.6f, Pitch.HalfLength + 0.5f);
                ball.Kick((target - ball.transform.position).normalized, 18f, Vector3.zero);
                yield return new WaitForSeconds(1.4f);
            }
            int conceded = match.HomeScore - goals0;
            Debug.Log($"Keeper test: 12 shots, {conceded} goals, {match.Stats.saves[1]} saves");
            Assert.GreaterOrEqual(match.Stats.saves[1], 3, $"the away keeper should save some of 12 shots (goals {conceded}, saves {match.Stats.saves[1]})");
            Assert.Less(conceded, 12, "not every shot may score");
        }

        [UnityTest]
        public IEnumerator PenaltiesAreMostlyScoredAndTheKeeperStillGetsSomeHands()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            var taker = FirstOutfielder(0);
            yield return null;
            float[] targets = { -3f, 3f, 0f, -2.5f, 2.5f, -3f, 3f, 0f, -2.5f, 2.5f };
            int goals0 = match.HomeScore, saves0 = match.Stats.saves[1];
            foreach (float tx in targets)
            {
                float wait = 0f;
                while ((match.InRestartHold || match.InGoalPause) && wait < 8f) { wait += Time.deltaTime; yield return null; }
                ball.ResetTo(new Vector3(0f, 0.12f, Pitch.HalfLength - Pitch.PenaltySpotDistance));
                ball.LastTouchTeam = 0; ball.LastToucher = taker;
                Vector3 target = new Vector3(tx, 0.7f, Pitch.HalfLength + 0.5f);
                ball.Kick((target - ball.transform.position).normalized, 22f, Vector3.zero);
                yield return new WaitForSeconds(1.5f);
            }
            int goals = match.HomeScore - goals0, saves = match.Stats.saves[1] - saves0;
            Debug.Log($"Penalty test: 10 penalties, {goals} goals, {saves} saves");
            Assert.GreaterOrEqual(goals, 3, $"most penalties should score (goals {goals}, saves {saves})");
            Assert.GreaterOrEqual(goals + saves, 6, $"each penalty should end as a goal or a save (goals {goals}, saves {saves})");
        }

        [UnityTest]
        public IEnumerator ChoosingAKitRecoloursBothTeams()
        {
            yield return LoadScene();
            var hud = Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>();
            yield return null;
            try
            {
                hud.SetHomeKit(2); // yellow
                yield return null;
                var home = FirstOutfielder(0); var away = FirstOutfielder(1);
                Color homeShirt = home.GetComponent<PlayerVisual>().Body.Find("Torso").GetComponent<Renderer>().sharedMaterial.color;
                Color awayShirt = away.GetComponent<PlayerVisual>().Body.Find("Torso").GetComponent<Renderer>().sharedMaterial.color;
                Assert.AreEqual(FootballSim.Core.KitPalette.Colors[2].r, homeShirt.r, 0.01f);
                Assert.AreEqual(FootballSim.Core.KitPalette.Colors[2].g, homeShirt.g, 0.01f);
                Assert.AreEqual(FootballSim.Core.KitPalette.Colors[FootballSim.Core.KitPalette.AwayFor(2)].b, awayShirt.b, 0.01f);
                Assert.AreNotEqual(homeShirt, awayShirt);
                Assert.AreEqual(2, FootballSim.Core.KitPalette.Load(), "the choice is remembered");
            }
            finally { FootballSim.Core.KitPalette.Save(0); }
        }

        [UnityTest]
        public IEnumerator GoalCreditsTheTeammateWhoTouchedItBeforeTheScorer()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            var scorer = FirstOutfielder(0); var provider = SecondOutfielder(0);
            yield return null;
            ball.LastTouchTeam = 0; ball.LastToucher = provider;
            ball.LastToucher = scorer;
            ball.ResetTo(new Vector3(0f, 1.2f, Pitch.HalfLength + 1.2f));
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, match.Goals.Count);
            Assert.AreEqual(scorer.name, match.Goals[0].scorer);
            Assert.AreEqual(provider.name, match.Goals[0].assist);
            StringAssert.Contains("assist " + provider.name, match.Goals[0].ToString());
        }

        [UnityTest]
        public IEnumerator NoAssistWhenScorerIsAloneOrPreviousTouchWasAnOpponent()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            var scorer = FirstOutfielder(0); var opponent = FirstOutfielder(1);
            yield return null;
            ball.LastTouchTeam = 1; ball.LastToucher = opponent;
            ball.LastTouchTeam = 0; ball.LastToucher = scorer; // won the ball from an opponent, then scored
            ball.ResetTo(new Vector3(0f, 1.2f, Pitch.HalfLength + 1.2f));
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, match.Goals.Count);
            Assert.IsNull(match.Goals[0].assist);
        }

        [UnityTest]
        public IEnumerator NonKickingTeamCannotEnterCentreCircleBeforeKickoff()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            yield return null;
            Assert.IsTrue(match.KickoffPending);
            var intruder = FirstOutfielder(1 - match.KickoffTeam);
            intruder.TeleportTo(new Vector3(1f, 1f, 1f), Quaternion.identity);
            yield return null;
            yield return null;
            Vector3 p = intruder.transform.position;
            Assert.GreaterOrEqual(new Vector2(p.x, p.z).magnitude, MatchManager.CentreCircleRadius - 0.05f, "intruder was left inside the circle at " + p);
        }

        [UnityTest]
        public IEnumerator FullTimePanelTextFitsItsBoxesWithManyGoals()
        {
            yield return LoadScene();
            var hud = Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>();
            var match = Object.FindFirstObjectByType<MatchManager>();
            for (int i = 0; i < 7; i++)
                match.Goals.Add(new GoalRecord { team = i % 2, minute = 10 + i * 10, scorer = "Home_11", ownGoal = i == 3 });
            typeof(FootballSim.UI.MatchHUD).GetMethod("OnFullTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(hud, null);
            Time.timeScale = 1f;
            yield return null;
            Canvas.ForceUpdateCanvases();
            var panel = hud.transform.Find("Canvas/FullTimePanel");
            if (panel == null) panel = FindDeep(hud.transform.root, "FullTimePanel");
            Assert.IsNotNull(panel);
            foreach (var name in new[] { "Goals", "Stats" })
            {
                var t = panel.Find(name).GetComponent<UnityEngine.UI.Text>();
                float rectH = ((RectTransform)t.transform).rect.height;
                Assert.LessOrEqual(t.preferredHeight, rectH + 0.5f, name + " text is taller than its box (" + t.preferredHeight + " > " + rectH + ")");
            }
            StringAssert.Contains("+3 more", panel.Find("Goals").GetComponent<UnityEngine.UI.Text>().text);
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        [UnityTest]
        public IEnumerator DefenderTouchingLastMakesItAnOwnGoal()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            var defender = FirstOutfielder(1); // away defends +Z
            yield return null;
            ball.LastTouchTeam = 1; ball.LastToucher = defender;
            ball.ResetTo(new Vector3(0f, 1.2f, Pitch.HalfLength + 1.2f));
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, match.HomeScore, "the goal counts for home");
            Assert.IsTrue(match.Goals[0].ownGoal);
            StringAssert.Contains("(og)", match.Goals[0].ToString());
        }

        [UnityTest]
        public IEnumerator SoundIsWiredAndMuteSwitchWorks()
        {
            yield return LoadScene();
            Assert.IsNotNull(Object.FindFirstObjectByType<FootballSim.Audio.MatchAudio>(), "HUD adds the audio player");
            bool was = FootballSim.Audio.MatchAudio.SoundOn;
            var hud = Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>();
            hud.ToggleSound();
            Assert.AreEqual(!was, FootballSim.Audio.MatchAudio.SoundOn);
            Assert.AreEqual(was ? 0f : 1f, AudioListener.volume);
            hud.ToggleSound();
            Assert.AreEqual(was, FootballSim.Audio.MatchAudio.SoundOn);
            Assert.AreEqual(was ? 1f : 0f, AudioListener.volume);
        }

        [UnityTest]
        public IEnumerator HalfTimeBreakStopsPlayThenRestartsFromCentre()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            match.SetDurationMinutes(0.5f); // half-time at 15 game seconds
            bool fired = false; match.HalfTime += () => fired = true;
            float t = 0f;
            while (!match.HalfTimeReached && t < 30f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(match.HalfTimeReached);
            Assert.IsTrue(fired, "HalfTime event raised");
            Assert.IsTrue(match.InGoalPause, "play is stopped during the break");
            Assert.IsTrue(ball.Body.isKinematic, "ball is held still");
            t = 0f;
            while (match.InGoalPause && t < 10f) { t += Time.deltaTime; yield return null; }
            Assert.IsFalse(match.InGoalPause);
            Assert.AreEqual(RestartType.KickOff, match.LastRestart);
            Assert.IsFalse(ball.Body.isKinematic);
            Assert.Less(ball.transform.position.magnitude, 1f, "restart from the centre spot");

            // Ends have changed: home now attacks -Z and a ball in the +Z goal counts for the AWAY team
            Assert.IsTrue(match.SidesSwapped);
            foreach (var tc in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None))
                Assert.AreEqual(tc.teamId == 0 ? -1f : 1f, tc.attackDir);
            Assert.AreEqual(-1f, match.AttackSign(0));
            Assert.AreEqual(-1f, Object.FindFirstObjectByType<FootballSim.CameraSystem.BroadcastCamera>().ViewSign);
            int homeBefore = match.HomeScore, awayBefore = match.AwayScore;
            yield return new WaitForSeconds(1.5f); // let any kick-off restart hold finish
            ball.ResetTo(new Vector3(0f, 1.2f, Pitch.HalfLength + 1.2f));
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(awayBefore + 1, match.AwayScore, "the +Z goal is now defended by home");
            Assert.AreEqual(homeBefore, match.HomeScore);
        }

        [UnityTest]
        public IEnumerator OffsideReceiverGivesFreeKickToDefenders()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            TeamController home = null, away = null;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None)) { if (t.teamId == 0) home = t; else away = t; }
            yield return null;
            // Away: everyone deep at z=45 except keeper at the line; home passer mid-pitch, one attacker at z=47
            foreach (var p in away.players) p.TeleportTo(new Vector3(p.isGoalkeeper ? 0f : (away.players.IndexOf(p) - 5) * 3f, 1f, p.isGoalkeeper ? 52f : 40f), Quaternion.identity);
            PlayerController passer = null, runner = null, onside = null;
            foreach (var p in home.players)
            {
                if (p.isGoalkeeper) continue;
                if (passer == null) passer = p; else if (runner == null) runner = p; else if (onside == null) onside = p;
            }
            passer.TeleportTo(new Vector3(0f, 1f, 5f), Quaternion.identity);
            runner.TeleportTo(new Vector3(10f, 1f, 47f), Quaternion.identity);
            onside.TeleportTo(new Vector3(-10f, 1f, 30f), Quaternion.identity);
            ball.ResetTo(new Vector3(0f, 0.12f, 5f));
            match.SnapshotOffside(passer);
            Assert.IsTrue(new System.Collections.Generic.List<PlayerController>(match.OffsideRunners).Contains(runner), "runner is beyond the second-last defender");
            Assert.IsFalse(new System.Collections.Generic.List<PlayerController>(match.OffsideRunners).Contains(onside), "onside player is not flagged");
            ball.LastTouchTeam = 0; ball.LastToucher = runner; // runner plays the ball
            yield return null; yield return null;
            Assert.AreEqual(RestartType.Offside, match.LastRestart);
            Assert.AreEqual(1, match.Stats.offsides[0]);
            Assert.AreEqual(47f, ball.transform.position.z, 0.5f, "free kick taken where the runner was");
        }

        [UnityTest]
        public IEnumerator OpponentTouchCancelsPendingOffside()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            TeamController home = null, away = null;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None)) { if (t.teamId == 0) home = t; else away = t; }
            yield return null;
            foreach (var p in away.players) p.TeleportTo(new Vector3(p.isGoalkeeper ? 0f : (away.players.IndexOf(p) - 5) * 3f, 1f, p.isGoalkeeper ? 52f : 40f), Quaternion.identity);
            PlayerController passer = null, runner = null;
            foreach (var p in home.players) { if (p.isGoalkeeper) continue; if (passer == null) passer = p; else if (runner == null) runner = p; }
            passer.TeleportTo(new Vector3(0f, 1f, 5f), Quaternion.identity);
            runner.TeleportTo(new Vector3(10f, 1f, 47f), Quaternion.identity);
            ball.ResetTo(new Vector3(0f, 0.12f, 5f));
            match.SnapshotOffside(passer);
            Assert.AreEqual(1, match.OffsideRunners.Count);
            ball.LastTouchTeam = 1; // a defender got to it first
            yield return null; yield return null;
            Assert.AreEqual(0, match.OffsideRunners.Count);
            ball.LastTouchTeam = 0; ball.LastToucher = runner;
            yield return null; yield return null;
            Assert.AreNotEqual(RestartType.Offside, match.LastRestart);
        }

        [UnityTest]
        public IEnumerator OnsideTeammateTouchingFirstCancelsPendingOffside()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>().BeginMatch();
            var match = Object.FindFirstObjectByType<MatchManager>();
            var ball = Object.FindFirstObjectByType<BallController>();
            TeamController home = null, away = null;
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None)) { if (t.teamId == 0) home = t; else away = t; }
            yield return null;
            foreach (var p in away.players) p.TeleportTo(new Vector3(p.isGoalkeeper ? 0f : (away.players.IndexOf(p) - 5) * 3f, 1f, p.isGoalkeeper ? 52f : 40f), Quaternion.identity);
            PlayerController passer = null, runner = null, onside = null;
            foreach (var p in home.players)
            {
                if (p.isGoalkeeper) continue;
                if (passer == null) passer = p; else if (runner == null) runner = p; else if (onside == null) onside = p;
            }
            passer.TeleportTo(new Vector3(0f, 1f, 5f), Quaternion.identity);
            runner.TeleportTo(new Vector3(10f, 1f, 47f), Quaternion.identity);
            onside.TeleportTo(new Vector3(-10f, 1f, 30f), Quaternion.identity);
            ball.ResetTo(new Vector3(0f, 0.12f, 5f));
            ball.LastTouchTeam = 0; ball.LastToucher = passer;
            match.SnapshotOffside(passer);
            Assert.AreEqual(1, match.OffsideRunners.Count);
            yield return null; yield return null;
            Assert.AreEqual(1, match.OffsideRunners.Count, "the passer's own touch must not cancel it");
            ball.LastTouchTeam = 0; ball.LastToucher = onside; // an onside team-mate plays the ball
            yield return null; yield return null;
            Assert.AreEqual(0, match.OffsideRunners.Count);
            ball.LastTouchTeam = 0; ball.LastToucher = runner; // runner touching it later is no longer offside
            yield return null; yield return null;
            Assert.AreNotEqual(RestartType.Offside, match.LastRestart);
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
