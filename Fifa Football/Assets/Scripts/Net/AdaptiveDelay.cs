using UnityEngine;

namespace FootballSim.Net
{
    /// <summary>
    /// Chooses how many ticks behind the newest snapshot the client should draw. It watches how far each snapshot's arrival is from
    /// when it should have arrived (the server tick spacing at real time) and keeps a running mean of that error. The delay is the
    /// normal snapshot gap plus a few times the mean error, so a clean link stays near the minimum and a jittery one backs off.
    /// </summary>
    public class AdaptiveDelay
    {
        public float TicksPerSecond = 60f;
        public float MinTicks = 6f, MaxTicks = 30f;
        /// <summary>How many mean errors of safety margin to add on top of the snapshot gap.</summary>
        public float Margin = 4f;
        /// <summary>Weight of each new sample in the running mean (1/16).</summary>
        public float Smoothing = 1f / 16f;

        bool haveLast; double lastTime; uint lastTick;
        float meanError; float gapTicks = 3f;
        public int Samples { get; private set; }

        /// <summary>Mean arrival error in ticks.</summary>
        public float MeanErrorTicks => meanError;

        /// <summary>Records that the snapshot with this server tick arrived at this real time (seconds).</summary>
        public void OnArrival(double realTime, uint tick)
        {
            if (haveLast && tick > lastTick)
            {
                float tickDelta = tick - lastTick;
                float arrivalTicks = (float)((realTime - lastTime) * TicksPerSecond);
                float error = Mathf.Abs(arrivalTicks - tickDelta);
                meanError += (error - meanError) * (Samples == 0 ? 1f : Smoothing);
                gapTicks += (tickDelta - gapTicks) * Smoothing;
                Samples++;
            }
            if (!haveLast || tick > lastTick) { haveLast = true; lastTime = realTime; lastTick = tick; }
        }

        /// <summary>The render delay to use now, in ticks.</summary>
        public float Ticks => Mathf.Clamp(gapTicks + Margin * meanError, MinTicks, MaxTicks);
    }
}
