using NUnit.Framework;
using UnityEngine;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class MatchSettingsStoreTests
    {
        static readonly string[] Keys = { MatchSettingsStore.MinutesKey, MatchSettingsStore.FormationKey, MatchSettingsStore.DifficultyKey };
        float?[] backup;

        [SetUp]
        public void Backup()
        {
            backup = new float?[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                backup[i] = PlayerPrefs.HasKey(Keys[i]) ? (float?)PlayerPrefs.GetFloat(Keys[i], PlayerPrefs.GetInt(Keys[i], 0)) : null;
                PlayerPrefs.DeleteKey(Keys[i]);
            }
        }

        [TearDown]
        public void Restore()
        {
            foreach (var k in Keys) PlayerPrefs.DeleteKey(k);
            // Real prefs were only ever written by the game through Save(); put the same values back
            if (backup[0].HasValue && backup[1].HasValue && backup[2].HasValue)
                MatchSettingsStore.Save(backup[0].Value, (int)backup[1].Value, (int)backup[2].Value);
        }

        [Test]
        public void DefaultsWhenNothingSaved()
        {
            var s = MatchSettingsStore.Load(5f, 4, 1, 3);
            Assert.AreEqual(5f, s.minutes);
            Assert.AreEqual(0, s.formation);
            Assert.AreEqual(1, s.difficulty);
        }

        [Test]
        public void SavedChoicesComeBack()
        {
            MatchSettingsStore.Save(10f, 2, 0);
            var s = MatchSettingsStore.Load(5f, 4, 1, 3);
            Assert.AreEqual(10f, s.minutes);
            Assert.AreEqual(2, s.formation);
            Assert.AreEqual(0, s.difficulty);
        }

        [Test]
        public void InvalidSavedValuesFallBackToDefaults()
        {
            MatchSettingsStore.Save(7f, 99, -4);
            var s = MatchSettingsStore.Load(5f, 4, 1, 3);
            Assert.AreEqual(5f, s.minutes, "7 minutes is not an offered length");
            Assert.AreEqual(0, s.formation, "formation index out of range");
            Assert.AreEqual(1, s.difficulty, "difficulty index out of range");
        }
    }
}
