using NUnit.Framework;
using UnityEngine;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class SaveDataTests
    {
        [SetUp] public void Clean() { SaveData.ResetAll(); }
        [TearDown] public void CleanAfter() { SaveData.ResetAll(); }

        [Test]
        public void ResetProgressEndsEveryCompetitionButKeepsSettings()
        {
            MatchSettingsStore.Save(10f, 2, 3);
            KitPalette.Save(1);
            MiniLeague.Start(); MiniLeague.RecordResult(1, 0);
            Tournament.Start(); Season.Start(4); Season.RecordResult(1, 0);
            TrainingMode.Active = true;
            SaveData.ResetProgress();
            Assert.IsFalse(MiniLeague.Active); Assert.IsFalse(Tournament.Active); Assert.IsFalse(Season.Active); Assert.IsFalse(TrainingMode.Active);
            MiniLeague.ForgetInMemory(); Tournament.ForgetInMemory(); Season.ForgetInMemory();
            Assert.IsFalse(MiniLeague.Active, "the saved league is gone too, not just the copy in memory");
            Assert.IsFalse(Tournament.Active); Assert.IsFalse(Season.Active);
            var s = MatchSettingsStore.Load(5f, 5, 2, 5);
            Assert.AreEqual(10f, s.minutes); Assert.AreEqual(2, s.formation); Assert.AreEqual(3, s.difficulty);
            Assert.AreEqual(1, PlayerPrefs.GetInt(KitPalette.KitKey, -1), "the shirt colour is a setting, not progress");
        }

        [Test]
        public void ResetAllAlsoPutsSettingsBackToTheirDefaults()
        {
            MatchSettingsStore.Save(10f, 2, 3);
            KitPalette.Save(1);
            PlayerPrefs.SetFloat(FootballSim.Inputs.TouchControls.PrefScale, 1.4f);
            PlayerPrefs.SetInt(FootballSim.Audio.MatchAudio.PrefKey, 0);
            SaveData.ResetAll();
            var s = MatchSettingsStore.Load(5f, 5, 2, 5);
            Assert.AreEqual(5f, s.minutes); Assert.AreEqual(0, s.formation); Assert.AreEqual(2, s.difficulty);
            Assert.IsFalse(PlayerPrefs.HasKey(KitPalette.KitKey));
            Assert.IsFalse(PlayerPrefs.HasKey(FootballSim.Inputs.TouchControls.PrefScale));
            Assert.IsFalse(PlayerPrefs.HasKey(FootballSim.Audio.MatchAudio.PrefKey));
        }

        [Test]
        public void KeysThatBelongToOtherThingsAreLeftAlone()
        {
            PlayerPrefs.SetInt("someone_elses_key", 42);
            try
            {
                SaveData.ResetAll();
                Assert.AreEqual(42, PlayerPrefs.GetInt("someone_elses_key", -1), "ResetAll must not be DeleteAll");
            }
            finally { PlayerPrefs.DeleteKey("someone_elses_key"); }
        }
    }
}
