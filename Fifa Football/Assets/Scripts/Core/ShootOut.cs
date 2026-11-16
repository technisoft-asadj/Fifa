using System.Collections.Generic;

namespace FootballSim.Core
{
    /// <summary>
    /// Penalty shoot-out rules, simulated rather than played: five kicks each taken alternately (home first), stopping early as soon as the
    /// other side can no longer catch up, then sudden death in pairs until one side leads after both have kicked. Each kick scores with a
    /// fixed chance per team. It exists so that a drawn tournament match can be settled; the player does not take or save these kicks.
    /// </summary>
    public static class ShootOut
    {
        public const int KicksPerSide = 5;
        public const int MaxSuddenDeathPairs = 50;

        public sealed class Result
        {
            public int homeGoals, awayGoals, homeKicks, awayKicks;
            public bool homeWins, suddenDeath;
            public readonly List<bool> sequence = new List<bool>(); // every kick in order, home first; true = scored
            public override string ToString() { return $"{homeGoals}-{awayGoals} on penalties{(suddenDeath ? " (sudden death)" : "")}"; }
        }

        /// <summary>Plays a whole shoot-out. <paramref name="homeChance"/> and <paramref name="awayChance"/> are the chance (0..1) each kick scores.</summary>
        public static Result Simulate(RandomStream rng, float homeChance, float awayChance)
        {
            var r = new Result();
            // regulation: alternate, stop when decided
            while (r.homeKicks < KicksPerSide || r.awayKicks < KicksPerSide)
            {
                if (r.homeKicks <= r.awayKicks && r.homeKicks < KicksPerSide) Kick(r, true, rng.Value < homeChance);
                else Kick(r, false, rng.Value < awayChance);
                int homeLeft = KicksPerSide - r.homeKicks, awayLeft = KicksPerSide - r.awayKicks;
                if (r.homeGoals > r.awayGoals + awayLeft) { r.homeWins = true; return r; }
                if (r.awayGoals > r.homeGoals + homeLeft) { r.homeWins = false; return r; }
            }
            if (r.homeGoals != r.awayGoals) { r.homeWins = r.homeGoals > r.awayGoals; return r; }
            // sudden death
            r.suddenDeath = true;
            for (int pair = 0; pair < MaxSuddenDeathPairs; pair++)
            {
                Kick(r, true, rng.Value < homeChance);
                Kick(r, false, rng.Value < awayChance);
                if (r.homeGoals != r.awayGoals) { r.homeWins = r.homeGoals > r.awayGoals; return r; }
            }
            r.homeWins = rng.Value < 0.5f; // not reachable in practice; a coin settles an endless tie so the cup can go on
            return r;
        }

        static void Kick(Result r, bool home, bool scored)
        {
            r.sequence.Add(scored);
            if (home) { r.homeKicks++; if (scored) r.homeGoals++; } else { r.awayKicks++; if (scored) r.awayGoals++; }
        }

        /// <summary>Chance that a kick against the AI of this difficulty scores (the human is home), and that the AI's own kick scores.</summary>
        public static void Chances(Difficulty opponent, out float homeChance, out float awayChance)
        {
            homeChance = 0.75f;
            awayChance = opponent == Difficulty.Amateur ? 0.65f : opponent == Difficulty.Professional ? 0.75f : 0.8f;
        }
    }
}
