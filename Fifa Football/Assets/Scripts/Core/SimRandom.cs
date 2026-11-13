namespace FootballSim.Core
{
    /// <summary>
    /// A small seedable random generator (xorshift64*). Instances are independent streams: drawing from one never changes another.
    /// </summary>
    public sealed class RandomStream
    {
        ulong state;

        public RandomStream(int seed) { Seed(seed); }

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
        public void Seed(int seed) { state = Initial(seed); }

        ulong Next()
        {
            state ^= state >> 12;
            state ^= state << 25;
            state ^= state >> 27;
            return state * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>A float in [0, 1).</summary>
        public float Value => (Next() >> 40) * (1f / (1 << 24));

        /// <summary>A float in [min, max).</summary>
        public float Range(float min, float max) { return min + (max - min) * Value; }

        /// <summary>An int in [min, max) like UnityEngine.Random.Range(int, int).</summary>
        public int Range(int min, int max)
        {
            if (max <= min) return min;
            return min + (int)(Next() % (ulong)(max - min));
        }
    }

    /// <summary>
    /// The random sources for gameplay decisions. The main stream drives the simulation (tackle success, shot error, keeper saves,
    /// fouls). The <see cref="Ai"/> stream drives only the AI's own choices (aim error, clearances), so that replaying recorded
    /// player inputs without the AI draws exactly the same simulation numbers as the original run. UnityEngine.Random is not
    /// used for gameplay; cosmetic randomness (animation variety) may keep using it.
    /// </summary>
    public static class SimRandom
    {
        static readonly RandomStream sim = new RandomStream(System.Environment.TickCount);
        public static readonly RandomStream Ai = new RandomStream(System.Environment.TickCount ^ 0x5BD1E995);

        /// <summary>Restart both streams: the same seed always produces the same numbers.</summary>
        public static void Seed(int seed) { sim.Seed(seed); Ai.Seed(seed ^ 0x5BD1E995); }

        public static float Value => sim.Value;
        public static float Range(float min, float max) { return sim.Range(min, max); }
        public static int Range(int min, int max) { return sim.Range(min, max); }
    }
}
