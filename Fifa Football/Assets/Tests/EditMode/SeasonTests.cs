using System.Collections.Generic;
using NUnit.Framework;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class SeasonTests
    {
        [SetUp] public void Reset() { Season.Abandon(); }
        [TearDown] public void ResetAfter() { Season.Abandon(); }

        [Test]
        public void EveryPairOfTeamsMeetsExactlyOnceOverTheFiveRounds()
        {
            var seen = new HashSet<string>();
            for (int round = 0; round < Season.Rounds; round++)
            {
                var pairs = Season.Pairings(round);
                Assert.AreEqual(3, pairs.Count);
                var inRound = new HashSet<int>();
                foreach (var p in pairs)
                {
                    Assert.IsTrue(inRound.Add(p[0]) && inRound.Add(p[1]), $"round {round}: a team plays twice");
                    int lo = System.Math.Min(p[0], p[1]), hi = System.Math.Max(p[0], p[1]);
                    Assert.IsTrue(seen.Add(lo + "-" + hi), $"pair {lo}-{hi} meets twice");
                }
                Assert.AreEqual(Season.Teams, inRound.Count, "every team plays in every round");
            }
            Assert.AreEqual(15, seen.Count);
        }

        [Test]
        public void TheHumanMeetsTheOpponentsInOrderWeakestFirst()
        {
            for (int round = 0; round < Season.Rounds; round++)
            {
                int opponent = -1;
                foreach (var p in Season.Pairings(round)) if (p[0] == 0) opponent = p[1];
                Assert.AreEqual(Season.OpponentIndex(round), opponent);
            }
            Season.Start(1);
            Assert.AreEqual("Rovers", Season.CurrentOpponentName);
            Assert.AreEqual(Difficulty.Amateur, Season.CurrentOpponent);
        }

        [Test]
        public void ASimulatedMatchIsTheSameEveryTime()
        {
            Season.SimulateMatch(99, 2, 3, 4, out int a1, out int b1);
            Season.SimulateMatch(99, 2, 3, 4, out int a2, out int b2);
            Assert.AreEqual(a1, a2); Assert.AreEqual(b1, b2);
        }

        [Test]
        public void SimulatedGoalsLookLikeFootball()
        {
            int total = 0, matches = 0, maxGoals = 0;
            for (int seed = 0; seed < 600; seed++)
            {
                Season.SimulateMatch(seed, 0, 3, 4, out int a, out int b);
                total += a + b; matches++; maxGoals = System.Math.Max(maxGoals, System.Math.Max(a, b));
            }
            float perMatch = total / (float)matches;
            Assert.That(perMatch, Is.InRange(2.0f, 3.5f), $"average goals per match {perMatch}");
            Assert.LessOrEqual(maxGoals, 11);
        }

        [Test]
        public void TableAfterAFullSeasonAddsUpAndSortsBestFirst()
        {
            Season.Start(7);
            int[][] scores = { new[] { 2, 0 }, new[] { 1, 1 }, new[] { 0, 1 }, new[] { 3, 2 }, new[] { 1, 0 } };
            foreach (var s in scores) Season.RecordResult(s[0], s[1]);
            Assert.IsTrue(Season.Over);
            var table = Season.Table();
            Assert.AreEqual(Season.Teams, table.Count);
            int played = 0, won = 0, lost = 0, drawn = 0, gf = 0, ga = 0;
            foreach (var r in table)
            {
                Assert.AreEqual(5, r.played);
                Assert.AreEqual(r.points, r.won * 3 + r.drawn);
                played += r.played; won += r.won; lost += r.lost; drawn += r.drawn; gf += r.goalsFor; ga += r.goalsAgainst;
            }
            Assert.AreEqual(won, lost, "every win is somebody's loss");
            Assert.AreEqual(0, drawn % 2, "draws come in pairs");
            Assert.AreEqual(gf, ga, "goals for and against balance across the table");
            for (int i = 1; i < table.Count; i++) Assert.GreaterOrEqual(table[i - 1].points, table[i].points);
            var you = table.Find(r => r.name == "You");
            Assert.AreEqual(3 + 1 + 0 + 3 + 3, you.points, "W D L W W");
            Assert.AreEqual(7, you.goalsFor);
        }

        [Test]
        public void SummaryNamesTheNextOpponentThenTheFinalPlace()
        {
            Season.Start(3);
            StringAssert.Contains("Rovers", Season.Summary());
            for (int i = 0; i < Season.Rounds; i++) Season.RecordResult(4, 0);
            StringAssert.Contains("CHAMPIONS", Season.Summary());
            Assert.AreEqual(1, Season.Position());
            Assert.AreEqual(Season.Rounds, Season.Round);
            Season.RecordResult(9, 0);
            Assert.AreEqual(Season.Rounds, Season.Results.Count, "nothing counts after the season is over");
        }

        [Test]
        public void OnePointIsNotWrittenAsOnePoints()
        {
            Assert.AreEqual("1 point", Season.PointsText(1));
            Assert.AreEqual("0 points", Season.PointsText(0));
            Assert.AreEqual("4 points", Season.PointsText(4));
            Season.Start(2);
            Season.RecordResult(1, 1); // a draw is one point for you
            StringAssert.Contains("with 1 point.", Season.Summary());
            StringAssert.DoesNotContain("1 points", Season.Summary());
        }

        [Test]
        public void ASeasonInProgressSurvivesARestartAndGivesTheSameTable()
        {
            Season.Start(12345);
            Season.RecordResult(1, 0); Season.RecordResult(2, 2); Season.RecordResult(0, 3);
            string before = Season.TableText();
            Season.ForgetInMemory();
            Assert.IsTrue(Season.Active);
            Assert.AreEqual(3, Season.Round);
            Assert.AreEqual(before, Season.TableText());
        }

        [Test]
        public void NonsenseSavedDataIsDiscarded()
        {
            UnityEngine.PlayerPrefs.SetInt(Season.ActiveKey, 1);
            UnityEngine.PlayerPrefs.SetInt(Season.SeedKey, 5);
            UnityEngine.PlayerPrefs.SetString(Season.ResultsKey, "1-0|banana");
            Season.ForgetInMemory();
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, "Saved season data did not make sense and was discarded.");
            Assert.IsFalse(Season.Active);
            Season.ForgetInMemory();
            Assert.IsFalse(Season.Active, "the bad data was cleared");
        }
    }
}
