namespace FootballSim.Core
{
    /// <summary>
    /// Rolling frame-time statistics for the optional FPS readout (the baseline the animation phases are measured against).
    /// Keeps the last <see cref="Capacity"/> frame times; no allocation after construction.
    /// </summary>
    public class FrameStats
    {
        public const int Capacity = 300;
        readonly float[] times = new float[Capacity];
        int next, count;

        public int Count => count;

        public void Reset() { next = 0; count = 0; }

        /// <summary>Records one frame's duration in seconds. Zero, negative and non-finite values are ignored.</summary>
        public void Add(float seconds)
        {
            if (!(seconds > 0f) || float.IsInfinity(seconds)) return;
            times[next] = seconds;
            next = (next + 1) % Capacity;
            if (count < Capacity) count++;
        }

        /// <summary>Average frames per second over the stored frames (total frames / total time); 0 when empty.</summary>
        public float AverageFps
        {
            get
            {
                if (count == 0) return 0f;
                float sum = 0f;
                for (int i = 0; i < count; i++) sum += times[i];
                return count / sum;
            }
        }

        /// <summary>The slowest frame in milliseconds; 0 when empty.</summary>
        public float WorstMs
        {
            get
            {
                float worst = 0f;
                for (int i = 0; i < count; i++) if (times[i] > worst) worst = times[i];
                return worst * 1000f;
            }
        }

        /// <summary>"1% low" fps: the frame rate of the slowest 1% of stored frames (at least one frame); 0 when empty.</summary>
        public float LowFps
        {
            get
            {
                if (count == 0) return 0f;
                int take = System.Math.Max(1, count / 100);
                // the slowest `take` frames, found by repeated selection (take is tiny, so this stays cheap and allocation-free)
                float sum = 0f; float ceiling = float.MaxValue; int taken = 0;
                while (taken < take)
                {
                    float best = -1f; int bestIndex = -1; int ties = 0;
                    for (int i = 0; i < count; i++)
                    {
                        float t = times[i];
                        if (t > best && t < ceiling) { best = t; bestIndex = i; }
                    }
                    if (bestIndex < 0) break;
                    for (int i = 0; i < count; i++) if (times[i] == best) ties++;
                    int use = System.Math.Min(ties, take - taken);
                    sum += best * use; taken += use; ceiling = best;
                }
                return taken / sum;
            }
        }
    }
}
