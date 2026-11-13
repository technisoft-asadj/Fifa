namespace FootballSim.Core
{
    /// <summary>
    /// The random source for gameplay decisions (tackle success, shot error, keeper saves, fouls, AI aim). It is a small
    /// seedable generator so a match can be replayed or run on two machines from the same seed, which UnityEngine.Random
    /// does not guarantee once other systems also draw from it. Cosmetic randomness (animation variety) may keep using UnityEngine.Random.
    /// </summary>
    public static class SimRandom
    {
        static ulong state = Initial((int)System.Environment.TickCount);

        static ulong Initial(int seed)
        {
            // SplitMix64 scrambling so small or similar seeds give unrelated sequences; the state must never be zero
            ulong z = (ulong)(uint)seed + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return z == 0UL ? 0x2545F4914F6CDD1DUL : z;
        }

        /// <summary>Restart the sequence: the same seed always produces the same numbers.</summary>
        public static void Seed(int seed) { state = Initial(seed); }

        static ulong Next()
        {
            // xorshift64*
            state ^= state >> 12;
            state ^= state << 25;
            state ^= state >> 27;
            return state * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>A float in [0, 1).</summary>
        public static float Value => (Next() >> 40) * (1f / (1 << 24));

        /// <summary>A float in [min, max).</summary>
        public static float Range(float min, float max) { return min + (max - min) * Value; }

        /// <summary>An int in [min, max) like UnityEngine.Random.Range(int, int).</summary>
        public static int Range(int min, int max)
        {
            if (max <= min) return min;
            return min + (int)(Next() % (ulong)(max - min));
        }
    }
}
