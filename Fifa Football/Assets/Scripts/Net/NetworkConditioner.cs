using System.Collections.Generic;
using FootballSim.Core;

namespace FootballSim.Net
{
    /// <summary>
    /// Pretends to be a bad network between two ends: drops packets, delays them by a base time plus random jitter, and so
    /// delivers them out of order when the jitter is larger than the gap between sends. Time is passed in (milliseconds), so tests
    /// are exact and repeatable for a given seed. For tests and local experiments; the real game never uses it.
    /// </summary>
    public sealed class NetworkConditioner
    {
        struct InFlight { public double deliverAtMs; public long order; public byte[] data; }

        readonly RandomStream rng;
        readonly List<InFlight> flying = new List<InFlight>();
        long counter;

        public float LossRate;      // 0..1 chance a packet never arrives
        public float BaseDelayMs;   // one-way latency
        public float JitterMs;      // extra random delay in [0, JitterMs)

        public int Sent { get; private set; }
        public int Lost { get; private set; }

        public NetworkConditioner(int seed, float lossRate, float baseDelayMs, float jitterMs)
        {
            rng = new RandomStream(seed); LossRate = lossRate; BaseDelayMs = baseDelayMs; JitterMs = jitterMs;
        }

        public void Send(byte[] data, int length, double nowMs)
        {
            Sent++;
            if (rng.Value < LossRate) { Lost++; return; }
            var copy = new byte[length];
            System.Array.Copy(data, copy, length);
            flying.Add(new InFlight { deliverAtMs = nowMs + BaseDelayMs + rng.Value * JitterMs, order = counter++, data = copy });
        }

        /// <summary>Removes and returns every packet whose delivery time has come, earliest first.</summary>
        public List<byte[]> Poll(double nowMs)
        {
            var ready = new List<InFlight>();
            for (int i = flying.Count - 1; i >= 0; i--)
                if (flying[i].deliverAtMs <= nowMs) { ready.Add(flying[i]); flying.RemoveAt(i); }
            ready.Sort((a, b) => a.deliverAtMs != b.deliverAtMs ? a.deliverAtMs.CompareTo(b.deliverAtMs) : a.order.CompareTo(b.order));
            var result = new List<byte[]>(ready.Count);
            foreach (var r in ready) result.Add(r.data);
            return result;
        }
    }
}
