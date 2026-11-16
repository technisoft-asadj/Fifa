using UnityEngine;

namespace FootballSim.Net
{
    /// <summary>
    /// A smooth playback clock for the client view, in server ticks. It runs at real time and is only nudged toward the target
    /// (newest snapshot tick minus the render delay), so uneven packet arrival no longer makes the picture jitter. A large error
    /// (a stall, a reconnect) snaps it straight to the target instead.
    /// </summary>
    public class RenderClock
    {
        public float TicksPerSecond = 60f;
        /// <summary>Error (ticks) above which the clock jumps to the target instead of easing.</summary>
        public float SnapTicks = 12f;
        /// <summary>How fast the error is pulled in, per second (2 = half the error gone in about 0.35 s).</summary>
        public float CorrectionPerSecond = 2f;

        public float Time { get; private set; }
        public bool Started { get; private set; }

        /// <summary>Advances by dt real seconds and eases toward <paramref name="target"/> (a tick number). Returns the new time.</summary>
        public float Step(float dt, float target)
        {
            if (!Started || Mathf.Abs(target - (Time + dt * TicksPerSecond)) > SnapTicks)
            {
                Time = target; Started = true;
                return Time;
            }
            Time += dt * TicksPerSecond;
            Time += (target - Time) * Mathf.Min(1f, dt * CorrectionPerSecond);
            return Time;
        }

        public void Reset() { Started = false; Time = 0f; }
    }
}
