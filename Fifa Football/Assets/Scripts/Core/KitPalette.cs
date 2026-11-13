using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>The shirt colours the player can pick for the home team, the matching away colour, and the saved choice.</summary>
    public static class KitPalette
    {
        public const string KitKey = "set_kit";
        public static readonly string[] Names = { "Red", "Blue", "Yellow", "Green", "White", "Black" };
        public static readonly Color[] Colors =
        {
            new Color(0.85f, 0.12f, 0.12f), new Color(0.12f, 0.28f, 0.85f), new Color(0.95f, 0.82f, 0.10f),
            new Color(0.10f, 0.62f, 0.22f), new Color(0.94f, 0.94f, 0.94f), new Color(0.12f, 0.12f, 0.14f)
        };
        public static int Count => Names.Length;

        /// <summary>The away team's colour: blue, unless the home kit is blue or black (then red or yellow) so the teams never clash.</summary>
        public static int AwayFor(int homeIndex)
        {
            if (homeIndex == 1) return 0;
            if (homeIndex == 5) return 2;
            return 1;
        }

        public static int Load()
        {
            try
            {
                int k = PlayerPrefs.GetInt(KitKey, 0);
                if (k >= 0 && k < Count) return k;
            }
            catch { }
            return 0;
        }

        public static void Save(int index)
        {
            if (index < 0 || index >= Count) return;
            try { PlayerPrefs.SetInt(KitKey, index); PlayerPrefs.Save(); } catch { }
        }
    }
}
