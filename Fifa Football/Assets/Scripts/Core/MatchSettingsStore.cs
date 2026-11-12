using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>Remembers the start-screen choices (match length, formation, difficulty) between sessions.</summary>
    public static class MatchSettingsStore
    {
        public const string MinutesKey = "set_minutes", FormationKey = "set_formation", DifficultyKey = "set_difficulty";
        static readonly float[] AllowedMinutes = { 3f, 5f, 10f };

        public struct Settings { public float minutes; public int formation; public int difficulty; }

        /// <summary>Saved choices, with anything missing or out of range replaced by the defaults.</summary>
        public static Settings Load(float defaultMinutes, int formationCount, int defaultDifficulty, int difficultyCount)
        {
            var s = new Settings { minutes = defaultMinutes, formation = 0, difficulty = defaultDifficulty };
            try
            {
                float m = PlayerPrefs.GetFloat(MinutesKey, defaultMinutes);
                foreach (var a in AllowedMinutes) if (Mathf.Approximately(a, m)) s.minutes = m;
                int f = PlayerPrefs.GetInt(FormationKey, 0);
                if (f >= 0 && f < formationCount) s.formation = f;
                int d = PlayerPrefs.GetInt(DifficultyKey, defaultDifficulty);
                if (d >= 0 && d < difficultyCount) s.difficulty = d;
            }
            catch { }
            return s;
        }

        public static void Save(float minutes, int formation, int difficulty)
        {
            try
            {
                PlayerPrefs.SetFloat(MinutesKey, minutes);
                PlayerPrefs.SetInt(FormationKey, formation);
                PlayerPrefs.SetInt(DifficultyKey, difficulty);
                PlayerPrefs.Save();
            }
            catch { }
        }
    }
}
