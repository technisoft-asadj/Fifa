using NUnit.Framework;
using UnityEngine;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class KitPaletteTests
    {
        [SetUp] public void Clean() { PlayerPrefs.DeleteKey(KitPalette.KitKey); }
        [TearDown] public void CleanAfter() { PlayerPrefs.DeleteKey(KitPalette.KitKey); }

        [Test]
        public void NamesAndColoursMatchInNumber()
        {
            Assert.AreEqual(KitPalette.Names.Length, KitPalette.Colors.Length);
            Assert.GreaterOrEqual(KitPalette.Count, 4);
        }

        [Test]
        public void AwayColourNeverEqualsOrClashesWithHome()
        {
            for (int h = 0; h < KitPalette.Count; h++)
            {
                int a = KitPalette.AwayFor(h);
                Assert.AreNotEqual(h, a, "home " + h);
                Assert.GreaterOrEqual(a, 0); Assert.Less(a, KitPalette.Count);
            }
            Assert.AreNotEqual(KitPalette.AwayFor(5), 5); // black home must not meet a dark away kit
            Assert.AreNotEqual(KitPalette.AwayFor(5), 1);
        }

        [Test]
        public void SavedChoiceIsRestored()
        {
            KitPalette.Save(3);
            Assert.AreEqual(3, KitPalette.Load());
        }

        [Test]
        public void MissingOrInvalidChoiceFallsBackToRed()
        {
            Assert.AreEqual(0, KitPalette.Load());
            PlayerPrefs.SetInt(KitPalette.KitKey, 99);
            Assert.AreEqual(0, KitPalette.Load());
            KitPalette.Save(-4); // ignored
            Assert.AreEqual(0, KitPalette.Load());
        }
    }
}
