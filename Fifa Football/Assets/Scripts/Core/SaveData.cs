using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>
    /// Erasing what the game has saved. <see cref="ResetProgress"/> ends any league, tournament or season and leaves the player's settings
    /// alone; <see cref="ResetAll"/> also puts the start-screen choices, shirt colour, touch-control size and sound back to their defaults.
    /// Only this game's own keys are touched, never PlayerPrefs.DeleteAll.
    /// </summary>
    public static class SaveData
    {
        /// <summary>Ends the league, tournament, season and training mode, in memory and on disk.</summary>
        public static void ResetProgress()
        {
            MiniLeague.Abandon();
            Tournament.Abandon();
            Season.Abandon();
            TrainingMode.Active = false;
        }

        /// <summary>Progress and every saved setting.</summary>
        public static void ResetAll()
        {
            ResetProgress();
            CareerRecord.Clear();
            PlayerProfile.Clear();
            try
            {
                foreach (var key in new[]
                {
                    MatchSettingsStore.MinutesKey, MatchSettingsStore.FormationKey, MatchSettingsStore.DifficultyKey,
                    KitPalette.KitKey, FootballSim.Inputs.TouchControls.PrefScale, FootballSim.Inputs.TouchControls.PrefOpacity, FootballSim.CameraSystem.BroadcastCamera.PrefKey, FootballSim.Inputs.TouchControls.PrefLeft, FootballSim.Inputs.TouchControls.PrefFixed,
                    FootballSim.Audio.MatchAudio.PrefKey,
                })
                    PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
            catch { }
        }
    }
}
