using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>
    /// The player's local profile: coins, experience, level and a once-a-day reward. Everything lives on the device (PlayerPrefs): there is no
    /// account, server or store behind it, so the coins are a progress meter, not a currency anyone can buy. Matches pay coins and
    /// experience (<see cref="AwardMatch"/>), saved data that does not make sense is discarded, and <see cref="ForgetInMemory"/> is a test hook.
    /// </summary>
    public static class PlayerProfile
    {
        public const string CoinsKey = "pf_coins", XpKey = "pf_xp", DailyKey = "pf_daily_day", NameKey = "pf_name";
        public const int MaxNameLength = 16;
        public const int XpPerLevel = 100, DailyRewardCoins = 250, MaxValue = 100000000;

        static bool restored;
        static int coins, xp;
        static long lastClaimDay = -1;
        static string name = "";

        /// <summary>The current day number (days since 1970, UTC). Tests replace it to move time forward.</summary>
        public static System.Func<long> Today = () => (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalDays;

        /// <summary>The name shown on the menu ("Guest" until the player picks one). There is no account behind it.</summary>
        public static string Name { get { EnsureRestored(); return name.Length == 0 ? "Guest" : name; } }
        /// <summary>True once the player has chosen a name or chosen to play as a guest.</summary>
        public static bool HasName { get { EnsureRestored(); return name.Length > 0; } }

        /// <summary>Letters, digits, space, underscore and hyphen only, trimmed and cut to 16 characters; an empty result means "Guest".</summary>
        public static string CleanName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Guest";
            var sb = new System.Text.StringBuilder();
            foreach (char c in raw)
                if (char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-') sb.Append(c);
            string cleaned = sb.ToString().Trim();
            if (cleaned.Length > MaxNameLength) cleaned = cleaned.Substring(0, MaxNameLength).Trim();
            return cleaned.Length == 0 ? "Guest" : cleaned;
        }

        /// <summary>Saves the cleaned name and returns it.</summary>
        public static string SetName(string raw)
        {
            EnsureRestored();
            name = CleanName(raw);
            Save();
            return name;
        }

        public static int Coins { get { EnsureRestored(); return coins; } }
        public static int Xp { get { EnsureRestored(); return xp; } }
        public static int Level { get { EnsureRestored(); return 1 + xp / XpPerLevel; } }
        public static int XpInLevel { get { EnsureRestored(); return xp % XpPerLevel; } }
        public static bool DailyAvailable { get { EnsureRestored(); return Today() > lastClaimDay; } }

        /// <summary>Pays the daily reward once per day. Returns the coins paid (0 if already claimed today).</summary>
        public static int ClaimDaily()
        {
            if (!DailyAvailable) return 0;
            lastClaimDay = Today();
            coins = Mathf.Min(MaxValue, coins + DailyRewardCoins);
            Save();
            return DailyRewardCoins;
        }

        /// <summary>What a finished match pays, as (coins, experience): a win pays most, a draw less, a loss a little; 10 coins per goal scored.</summary>
        public static void Reward(int homeGoals, int awayGoals, out int coinsEarned, out int xpEarned)
        {
            bool win = homeGoals > awayGoals, draw = homeGoals == awayGoals;
            coinsEarned = (win ? 100 : draw ? 40 : 20) + 10 * Mathf.Clamp(homeGoals, 0, 20);
            xpEarned = win ? 50 : draw ? 25 : 15;
        }

        /// <summary>Adds coins on top of what was already earned (the rewarded-ad bonus).</summary>
        public static void AddBonusCoins(int amount)
        {
            if (amount <= 0) return;
            EnsureRestored();
            coins = Mathf.Min(MaxValue, coins + amount);
            Save();
        }

        public static void AwardMatch(int homeGoals, int awayGoals)
        {
            EnsureRestored();
            Reward(homeGoals, awayGoals, out int c, out int x);
            coins = Mathf.Min(MaxValue, coins + c);
            xp = Mathf.Min(MaxValue, xp + x);
            Save();
        }

        public static void Clear()
        {
            restored = true; coins = 0; xp = 0; lastClaimDay = -1; name = "";
            try { PlayerPrefs.DeleteKey(CoinsKey); PlayerPrefs.DeleteKey(XpKey); PlayerPrefs.DeleteKey(DailyKey); PlayerPrefs.DeleteKey(NameKey); PlayerPrefs.Save(); } catch { }
        }

        /// <summary>Forgets the in-memory values so the next read loads from storage, as after a fresh launch. For tests.</summary>
        public static void ForgetInMemory() { restored = false; coins = 0; xp = 0; lastClaimDay = -1; name = ""; }

        static void EnsureRestored()
        {
            if (restored) return;
            restored = true;
            try
            {
                int c = PlayerPrefs.GetInt(CoinsKey, 0), x = PlayerPrefs.GetInt(XpKey, 0);
                string day = PlayerPrefs.GetString(DailyKey, "-1");
                if (c < 0 || c > MaxValue || x < 0 || x > MaxValue || !long.TryParse(day, out long d) || d < -1 || d > 10000000)
                {
                    Debug.LogWarning("Saved player profile did not make sense and was discarded.");
                    Clear();
                    return;
                }
                coins = c; xp = x; lastClaimDay = d;
                string saved = PlayerPrefs.GetString(NameKey, "");
                name = saved.Length == 0 ? "" : CleanName(saved);
            }
            catch { }
        }

        static void Save()
        {
            try
            {
                PlayerPrefs.SetInt(CoinsKey, coins); PlayerPrefs.SetInt(XpKey, xp); PlayerPrefs.SetString(DailyKey, lastClaimDay.ToString());
                if (name.Length > 0) PlayerPrefs.SetString(NameKey, name);
                PlayerPrefs.Save();
            }
            catch { }
        }
    }
}
