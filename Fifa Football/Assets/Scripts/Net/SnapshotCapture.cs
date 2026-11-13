using System.Collections.Generic;
using UnityEngine;
using FootballSim.Ball;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.Net
{
    /// <summary>Reads the live simulation into a <see cref="WorldSnapshot"/>. Players must be listed home team first, then away.</summary>
    public static class SnapshotCapture
    {
        public static void Fill(WorldSnapshot s, uint tick, MatchManager match, BallController ball, IList<PlayerController> players)
        {
            s.tick = tick;
            s.ballPos = ball.Position;
            s.ballVel = ball.Body.linearVelocity;
            for (int i = 0; i < WorldSnapshot.PlayerCount && i < players.Count; i++)
            {
                var p = players[i];
                byte flags = 0;
                if (p.IsDiving) flags |= PlayerState.Diving;
                if (p.Exhausted) flags |= PlayerState.Exhausted;
                s.players[i] = new PlayerState { x = p.transform.position.x, z = p.transform.position.z, yaw = p.transform.eulerAngles.y, speed = p.Velocity.magnitude, flags = flags };
            }
            s.homeScore = (byte)Mathf.Min(255, match.HomeScore);
            s.awayScore = (byte)Mathf.Min(255, match.AwayScore);
            s.clockTenths = (ushort)Mathf.Clamp(Mathf.RoundToInt(match.GameMinute * 10f), 0, ushort.MaxValue);
        }
    }
}
