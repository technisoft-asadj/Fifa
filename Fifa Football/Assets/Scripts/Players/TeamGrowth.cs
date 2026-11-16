using UnityEngine;
using FootballSim.Core;

namespace FootballSim.Players
{
    /// <summary>
    /// Growth of the home squad from results. The level comes from the saved <see cref="CareerRecord"/> (two points per win, one per draw,
    /// a level every <see cref="PointsPerLevel"/> points, capped), so it needs no save data of its own and goes back to zero with the record.
    /// Each level adds one point to every outfield attribute, up to 99. It is applied to a per-player copy, never to the shared asset.
    /// </summary>
    public static class TeamGrowth
    {
        public const int PointsPerLevel = 4, MaxLevel = 6;

        public static int Points(int won, int drawn) { return Mathf.Max(0, won) * 2 + Mathf.Max(0, drawn); }
        public static int LevelFor(int won, int drawn) { return Mathf.Clamp(Points(won, drawn) / PointsPerLevel, 0, MaxLevel); }
        public static int CurrentLevel => LevelFor(CareerRecord.Won, CareerRecord.Drawn);

        /// <summary>Adds <paramref name="levels"/> points to each outfield attribute of <paramref name="d"/>, capped at 99.</summary>
        public static void Apply(PlayerData d, int levels)
        {
            if (d == null || levels <= 0) return;
            d.pace = Up(d.pace, levels); d.acceleration = Up(d.acceleration, levels); d.sprintSpeed = Up(d.sprintSpeed, levels);
            d.shooting = Up(d.shooting, levels); d.passing = Up(d.passing, levels); d.dribbling = Up(d.dribbling, levels);
            d.ballControl = Up(d.ballControl, levels); d.agility = Up(d.agility, levels); d.stamina = Up(d.stamina, levels);
            d.strength = Up(d.strength, levels);
        }

        static int Up(int v, int levels) { return Mathf.Min(99, v + levels); }
    }
}
