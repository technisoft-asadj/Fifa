using FootballSim.Core;
using FootballSim.UI;
using NUnit.Framework;

namespace FootballSim.Tests
{
    public class UiLogicTests
    {
        [Test]
        public void LineupSwapMovesTwoOutfieldPlayersAndKeepsTheKeeper()
        {
            FootballSim.Players.TeamKits.ResetOrder();
            try
            {
                Assert.IsFalse(FootballSim.Players.TeamKits.SwapSlots(0, 5), "the keeper cannot be swapped");
                Assert.IsFalse(FootballSim.Players.TeamKits.SwapSlots(3, 3));
                Assert.IsFalse(FootballSim.Players.TeamKits.SwapSlots(3, 11));
                Assert.IsTrue(FootballSim.Players.TeamKits.SwapSlots(9, 10));
                Assert.AreEqual(11, FootballSim.Players.TeamKits.NumberInSlot(10), "slot 10 now wears the other forward's number");
                Assert.AreEqual(10, FootballSim.Players.TeamKits.NumberInSlot(11));
                Assert.AreEqual(1, FootballSim.Players.TeamKits.NumberInSlot(1));
                FootballSim.Players.TeamKits.ResetOrder();
                Assert.AreEqual(10, FootballSim.Players.TeamKits.NumberInSlot(10), "reset restores the default order");
            }
            finally { FootballSim.Players.TeamKits.ResetOrder(); }
        }

        [Test]
        public void TeamStrengthsFollowTheFormationAndTheTeamLevel()
        {
            MatchHUD.Strengths(1, 0, out int a433, out int m433, out int d433, out int o433);   // 4-3-3: three attackers
            MatchHUD.Strengths(4, 0, out int a532, out int m532, out int d532, out int o532);   // 5-3-2: five defenders
            Assert.Greater(a433, a532, "three attackers are stronger up front than two");
            Assert.Greater(d532, d433, "five defenders are stronger at the back than four");
            MatchHUD.Strengths(1, 6, out int aMax, out _, out _, out int oMax);
            Assert.Greater(aMax, a433, "a higher team level raises every strength");
            Assert.Greater(oMax, o433);
            foreach (int f in new[] { 0, 1, 2, 3, 4 })
            {
                MatchHUD.Strengths(f, 6, out int a, out int m, out int d, out int o);
                foreach (int v in new[] { a, m, d, o }) Assert.That(v, Is.InRange(1, 99));
            }
        }

        [Test]
        public void MatchesPayGemsForAWinMoreThanADrawAndADraw_MoreThanALoss()
        {
            Assert.AreEqual(50, PlayerProfile.GemsFor(2, 1));
            Assert.AreEqual(20, PlayerProfile.GemsFor(1, 1));
            Assert.AreEqual(10, PlayerProfile.GemsFor(0, 3));
        }
    }
}
