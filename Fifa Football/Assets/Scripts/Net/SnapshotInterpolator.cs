using UnityEngine;

namespace FootballSim.Net
{
    /// <summary>
    /// Client side of snapshot interpolation: keeps the last few snapshots by tick and returns the world at any fractional tick
    /// between them. Drawing a little in the past (the render delay) means there are almost always two snapshots to blend, so
    /// motion stays smooth even though snapshots arrive at 20-30 Hz. Packets that arrive late or twice are ignored.
    /// </summary>
    public class SnapshotInterpolator
    {
        const int Capacity = 32;
        readonly WorldSnapshot[] ring = new WorldSnapshot[Capacity];
        int count;
        uint newestTick;

        public int Count => count;
        public uint NewestTick => newestTick;

        /// <summary>Stores a copy of the snapshot. Returns false when it is older than, or the same as, the newest one already held.</summary>
        public bool Add(WorldSnapshot s)
        {
            if (count > 0 && s.tick <= newestTick) return false;
            var slot = ring[count % Capacity] ?? (ring[count % Capacity] = new WorldSnapshot());
            Copy(s, slot);
            count++;
            newestTick = s.tick;
            return true;
        }

        WorldSnapshot At(int i) { return ring[i % Capacity]; } // i is an absolute index; valid for the last Capacity added

        /// <summary>
        /// Fills <paramref name="result"/> with the world at <paramref name="tick"/> (fractional ticks allowed). Before the oldest
        /// held snapshot it returns that one; after the newest it holds the newest (no extrapolation). Returns false when empty.
        /// </summary>
        public bool Sample(float tick, WorldSnapshot result)
        {
            if (count == 0) return false;
            int oldest = Mathf.Max(0, count - Capacity);
            var last = At(count - 1);
            if (tick >= last.tick) { Copy(last, result); return true; }
            var first = At(oldest);
            if (tick <= first.tick) { Copy(first, result); return true; }
            for (int i = count - 1; i > oldest; i--)
            {
                var b = At(i); var a = At(i - 1);
                if (tick >= a.tick)
                {
                    Blend(a, b, (tick - a.tick) / (b.tick - a.tick), result);
                    return true;
                }
            }
            Copy(first, result);
            return true;
        }

        static void Blend(WorldSnapshot a, WorldSnapshot b, float t, WorldSnapshot r)
        {
            r.tick = a.tick; // the earlier snapshot's tick; fractional position is not stored
            r.ballPos = Vector3.Lerp(a.ballPos, b.ballPos, t);
            r.ballVel = Vector3.Lerp(a.ballVel, b.ballVel, t);
            for (int i = 0; i < WorldSnapshot.PlayerCount; i++)
            {
                var pa = a.players[i]; var pb = b.players[i];
                r.players[i] = new PlayerState
                {
                    x = Mathf.Lerp(pa.x, pb.x, t), z = Mathf.Lerp(pa.z, pb.z, t),
                    yaw = Mathf.LerpAngle(pa.yaw, pb.yaw, t), speed = Mathf.Lerp(pa.speed, pb.speed, t),
                    flags = t < 0.5f ? pa.flags : pb.flags
                };
            }
            // discrete values switch at the later snapshot's moment rather than blending
            var d = t < 1f ? a : b;
            r.homeScore = d.homeScore; r.awayScore = d.awayScore;
            r.clockTenths = (ushort)Mathf.RoundToInt(Mathf.Lerp(a.clockTenths, b.clockTenths, t));
        }

        static void Copy(WorldSnapshot from, WorldSnapshot to)
        {
            to.tick = from.tick; to.ballPos = from.ballPos; to.ballVel = from.ballVel;
            to.homeScore = from.homeScore; to.awayScore = from.awayScore; to.clockTenths = from.clockTenths;
            for (int i = 0; i < WorldSnapshot.PlayerCount; i++) to.players[i] = from.players[i];
        }
    }
}
