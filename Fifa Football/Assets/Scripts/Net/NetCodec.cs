using FootballSim.Inputs;
using UnityEngine;

namespace FootballSim.Net
{
    /// <summary>One player as sent over the wire. Positions are metres, yaw degrees, speed m/s.</summary>
    public struct PlayerState
    {
        public float x, z, yaw, speed;
        public byte flags;
        public const byte Diving = 1, Exhausted = 2;
    }

    /// <summary>The whole match state a client needs to draw one moment: 22 players, the ball, the score and the clock.</summary>
    public class WorldSnapshot
    {
        public const int PlayerCount = 22;
        public uint tick;
        public Vector3 ballPos, ballVel;
        public readonly PlayerState[] players = new PlayerState[PlayerCount];
        public byte homeScore, awayScore;
        /// <summary>Game minute in tenths (0..9000 = 900.0 min), enough for a 90-minute match with extra time.</summary>
        public ushort clockTenths;
    }

    /// <summary>
    /// Compact binary encoding of snapshots and input frames. Quantised: positions to 1 cm, velocities to 1 cm/s, yaw to 1.4 degrees,
    /// speed to 0.1 m/s. This is the size estimate for the bandwidth target (plan.md: under 20 KB/s per client), not a finished
    /// protocol: there is no versioning, delta compression or packet header yet, and nothing is sent over a network.
    /// </summary>
    public static class NetCodec
    {
        public const int SnapshotBytes = 4 + 6 + 6 + WorldSnapshot.PlayerCount * 7 + 2 + 2;
        public const int InputBytes = 5;

        static void PutShort(byte[] b, ref int o, float value, float step)
        {
            int q = Mathf.Clamp(Mathf.RoundToInt(value / step), short.MinValue, short.MaxValue);
            b[o++] = (byte)(q & 0xFF); b[o++] = (byte)((q >> 8) & 0xFF);
        }
        static float GetShort(byte[] b, ref int o, float step)
        {
            short q = (short)(b[o] | (b[o + 1] << 8)); o += 2;
            return q * step;
        }
        static void PutUInt(byte[] b, ref int o, uint v) { for (int i = 0; i < 4; i++) b[o++] = (byte)((v >> (8 * i)) & 0xFF); }
        static uint GetUInt(byte[] b, ref int o) { uint v = 0; for (int i = 0; i < 4; i++) v |= (uint)b[o++] << (8 * i); return v; }

        /// <summary>Writes the snapshot at buffer[offset..]; returns the number of bytes written (always SnapshotBytes).</summary>
        public static int Write(WorldSnapshot s, byte[] buffer, int offset = 0)
        {
            int o = offset;
            PutUInt(buffer, ref o, s.tick);
            PutShort(buffer, ref o, s.ballPos.x, 0.01f); PutShort(buffer, ref o, s.ballPos.y, 0.01f); PutShort(buffer, ref o, s.ballPos.z, 0.01f);
            PutShort(buffer, ref o, s.ballVel.x, 0.01f); PutShort(buffer, ref o, s.ballVel.y, 0.01f); PutShort(buffer, ref o, s.ballVel.z, 0.01f);
            for (int i = 0; i < WorldSnapshot.PlayerCount; i++)
            {
                var p = s.players[i];
                PutShort(buffer, ref o, p.x, 0.01f); PutShort(buffer, ref o, p.z, 0.01f);
                float yaw = Mathf.Repeat(p.yaw, 360f);
                buffer[o++] = (byte)(Mathf.RoundToInt(yaw / 360f * 256f) & 0xFF);
                buffer[o++] = (byte)Mathf.Clamp(Mathf.RoundToInt(p.speed * 10f), 0, 255);
                buffer[o++] = p.flags;
            }
            buffer[o++] = s.homeScore; buffer[o++] = s.awayScore;
            buffer[o++] = (byte)(s.clockTenths & 0xFF); buffer[o++] = (byte)(s.clockTenths >> 8);
            return o - offset;
        }

        public static void Read(byte[] buffer, int offset, WorldSnapshot s)
        {
            int o = offset;
            s.tick = GetUInt(buffer, ref o);
            float bx = GetShort(buffer, ref o, 0.01f), by = GetShort(buffer, ref o, 0.01f), bz = GetShort(buffer, ref o, 0.01f);
            s.ballPos = new Vector3(bx, by, bz);
            float vx = GetShort(buffer, ref o, 0.01f), vy = GetShort(buffer, ref o, 0.01f), vz = GetShort(buffer, ref o, 0.01f);
            s.ballVel = new Vector3(vx, vy, vz);
            for (int i = 0; i < WorldSnapshot.PlayerCount; i++)
            {
                var p = new PlayerState();
                p.x = GetShort(buffer, ref o, 0.01f);
                p.z = GetShort(buffer, ref o, 0.01f);
                p.yaw = buffer[o++] * 360f / 256f;
                p.speed = buffer[o++] * 0.1f;
                p.flags = buffer[o++];
                s.players[i] = p;
            }
            s.homeScore = buffer[o++]; s.awayScore = buffer[o++];
            s.clockTenths = (ushort)(buffer[o] | (buffer[o + 1] << 8));
        }

        // ---- input frame: move x, move y (sbyte/127), flags, aim yaw, power ----
        const byte WorldSpace = 1, Sprint = 2, Pass = 4, ShootHeld = 8, ShootReleased = 16, Tackle = 32, Switch = 64, HasAim = 128;

        public static int Write(InputFrame f, byte[] buffer, int offset = 0)
        {
            int o = offset;
            buffer[o++] = (byte)(sbyte)Mathf.Clamp(Mathf.RoundToInt(f.move.x * 127f), -127, 127);
            buffer[o++] = (byte)(sbyte)Mathf.Clamp(Mathf.RoundToInt(f.move.y * 127f), -127, 127);
            byte flags = 0;
            if (f.moveIsWorldSpace) flags |= WorldSpace;
            if (f.sprint) flags |= Sprint;
            if (f.pass) flags |= Pass;
            if (f.shootHeld) flags |= ShootHeld;
            if (f.shootReleased) flags |= ShootReleased;
            if (f.tackle) flags |= Tackle;
            if (f.switchPressed) flags |= Switch;
            bool hasAim = f.aim.sqrMagnitude > 1e-6f;
            if (hasAim) flags |= HasAim;
            buffer[o++] = flags;
            float yaw = hasAim ? Mathf.Repeat(Mathf.Atan2(f.aim.x, f.aim.z) * Mathf.Rad2Deg, 360f) : 0f;
            buffer[o++] = (byte)(Mathf.RoundToInt(yaw / 360f * 256f) & 0xFF);
            buffer[o++] = (byte)Mathf.Clamp(Mathf.RoundToInt(f.power * 255f), 0, 255);
            return o - offset;
        }

        public static InputFrame ReadInput(byte[] buffer, int offset = 0)
        {
            int o = offset;
            var f = new InputFrame();
            float mx = (sbyte)buffer[o++] / 127f;
            float my = (sbyte)buffer[o++] / 127f;
            f.move = new Vector2(mx, my);
            byte flags = buffer[o++];
            f.moveIsWorldSpace = (flags & WorldSpace) != 0; f.sprint = (flags & Sprint) != 0; f.pass = (flags & Pass) != 0;
            f.shootHeld = (flags & ShootHeld) != 0; f.shootReleased = (flags & ShootReleased) != 0;
            f.tackle = (flags & Tackle) != 0; f.switchPressed = (flags & Switch) != 0;
            float yaw = buffer[o++] * 360f / 256f;
            if ((flags & HasAim) != 0) f.aim = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
            f.power = buffer[o++] / 255f;
            return f;
        }
    }
}
