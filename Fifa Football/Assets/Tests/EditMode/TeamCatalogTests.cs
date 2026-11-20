using NUnit.Framework;
using UnityEngine;
using FootballSim.Core;
using FootballSim.Players;

namespace FootballSim.Tests
{
    public class TeamCatalogTests
    {
        [SetUp] public void Clean() { PlayerPrefs.DeleteKey(TeamSelection.HomeKey); PlayerPrefs.DeleteKey(TeamSelection.AwayKey); TeamSelection.ForgetInMemory(); }
        [TearDown] public void CleanAfter() { PlayerPrefs.DeleteKey(TeamSelection.HomeKey); PlayerPrefs.DeleteKey(TeamSelection.AwayKey); TeamSelection.ForgetInMemory(); }

        [Test]
        public void EveryClubHasElevenNamedPlayersAndANameAndShortName()
        {
            Assert.GreaterOrEqual(TeamCatalog.Count, 12);
            foreach (var c in TeamCatalog.Clubs)
            {
                Assert.AreEqual(11, c.players.Length, c.name);
                foreach (var p in c.players) Assert.IsFalse(string.IsNullOrWhiteSpace(p), c.name);
                Assert.IsFalse(string.IsNullOrEmpty(c.name));
                Assert.AreEqual(3, c.shortName.Length, c.name);
            }
        }

        [Test]
        public void GoalkeeperColoursDifferFromBothOutfieldShirtsAndFromEachOther()
        {
            Color[] shirts = { Color.red, Color.blue, Color.white, new Color(0.1f, 0.62f, 0.22f), new Color(0.95f, 0.82f, 0.1f), Color.black };
            foreach (var h in shirts)
                foreach (var a in shirts)
                {
                    TeamCatalog.GoalkeeperColors(h, a, out var gh, out var ga);
                    Assert.IsFalse(TeamCatalog.Clash(gh, h) || TeamCatalog.Clash(gh, a), "home keeper clashes");
                    Assert.IsFalse(TeamCatalog.Clash(ga, h) || TeamCatalog.Clash(ga, a), "away keeper clashes");
                    Assert.IsTrue(gh != ga, "the two keepers match");
                }
        }

        [Test]
        public void SelectionNeverPutsTheSameClubOnBothSides()
        {
            TeamSelection.Set(3, 3);
            Assert.AreEqual(3, TeamSelection.Home);
            Assert.AreEqual(-1, TeamSelection.Away);
            TeamSelection.Set(-1, 3);
            for (int i = 0; i < TeamCatalog.Count + 3; i++)
            {
                TeamSelection.Cycle(true, 1);
                Assert.AreNotEqual(TeamSelection.Away, TeamSelection.Home == -1 ? -99 : TeamSelection.Home, "same club on both sides");
            }
        }

        [Test]
        public void SelectionIsSaved()
        {
            TeamSelection.Set(2, 5);
            TeamSelection.ForgetInMemory();
            Assert.AreEqual(2, TeamSelection.Home);
            Assert.AreEqual(5, TeamSelection.Away);
        }

        [Test]
        public void ClubsWithAlikeShirtsGetADifferentAwayStrip()
        {
            TeamSelection.Set(3, 6); // Liverpool and Bayern: both red
            TeamKits.Resolve(0, out var home, out var away, out _, out _);
            Assert.IsFalse(TeamCatalog.Clash(home.shirt, away.shirt));
        }

        [Test]
        public void ShirtNumberComesFromTheSceneName()
        {
            Assert.AreEqual(9, TeamKits.ShirtNumber("Home_9"));
            Assert.AreEqual(11, TeamKits.ShirtNumber("Away_11"));
            Assert.AreEqual(0, TeamKits.ShirtNumber("Haaland"));
        }
    }
}
