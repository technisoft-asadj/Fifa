using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>
    /// The player's lifetime record: matches, wins, draws, losses and goals, saved to PlayerPrefs after every match. Counts every
    /// full-time result of a real match (the human is home); training matches are not recorded. Not the same as a league or cup run,
    /// which <see cref="SaveData.ResetProgress"/> ends; <see cref="SaveData.ResetAll"/> also clears this.
    /// </summary>
    public static class CareerRecord
    {
        public const string Key = "career_record";
        const int Fields = 5;

        static bool restored;
        static int played, won, drawn, goalsFor, goalsAgainst;

        public static int Played { get { EnsureRestored(); return played; } }
        public static int Won { get { EnsureRestored(); return won; } }
        public static int Drawn { get { EnsureRestored(); return drawn; } }
        public static int Lost { get { EnsureRestored(); return played - won - drawn; } }
        public static int GoalsFor { get { EnsureRestored(); return goalsFor; } }
        public static int GoalsAgainst { get { EnsureRestored(); return goalsAgainst; } }

        public static void Record(int homeGoals, int awayGoals)
        {
            EnsureRestored();
            if (homeGoals < 0 || awayGoals < 0) return;
            played++;
            if (homeGoals > awayGoals) won++; else if (homeGoals == awayGoals) drawn++;
            goalsFor += homeGoals; goalsAgainst += awayGoals;
            Save();
        }

        public static void Clear() { restored = true; played = won = drawn = goalsFor = goalsAgainst = 0; Save(); }

        /// <summary>Two short lines for the start screen; empty before the first match.</summary>
        public static string Summary()
        {
            EnsureRestored();
            if (played == 0) return "";
            return $"W{won} D{drawn} L{Lost}\nGF {goalsFor}  GA {goalsAgainst}";
        }

        /// <summary>Forgets the in-memory copy so the next read loads from storage, as after a fresh launch. For tests.</summary>
        public static void ForgetInMemory() { restored = false; played = won = drawn = goalsFor = goalsAgainst = 0; }

        static void Save()
        {
            try
            {
                if (played == 0) PlayerPrefs.DeleteKey(Key);
                else PlayerPrefs.SetString(Key, $"{played},{won},{drawn},{goalsFor},{goalsAgainst}");
                PlayerPrefs.Save();
            }
            catch { }
        }

        static void EnsureRestored()
        {
            if (restored) return;
            restored = true;
            played = won = drawn = goalsFor = goalsAgainst = 0;
            string text;
            try { text = PlayerPrefs.GetString(Key, ""); } catch { return; }
            if (text.Length == 0) return;
            var parts = text.Split(',');
            bool ok = parts.Length == Fields;
            var v = new int[Fields];
            for (int i = 0; ok && i < Fields; i++) ok = int.TryParse(parts[i], out v[i]) && v[i] >= 0;
            // won + drawn cannot exceed played, and goals cannot be missing for matches that had any wins
            if (ok && (v[1] + v[2] > v[0])) ok = false;
            if (!ok)
            {
                Debug.LogWarning("Saved career record did not make sense and was discarded.");
                try { PlayerPrefs.DeleteKey(Key); } catch { }
                return;
            }
            played = v[0]; won = v[1]; drawn = v[2]; goalsFor = v[3]; goalsAgainst = v[4];
        }
    }
}
