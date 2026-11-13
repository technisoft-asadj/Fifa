using System.Collections.Generic;
using System.Net;
using UnityEngine;
using FootballSim.Ball;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.Net
{
    /// <summary>
    /// Runs next to a live match and streams snapshots to every client that has said hello. A spike, not a product: no accounts, no
    /// reliability, no input handling yet, and it only listens on the loopback address.
    /// </summary>
    public class NetServer : MonoBehaviour
    {
        UdpEndpoint endpoint;
        readonly List<IPEndPoint> clients = new List<IPEndPoint>();
        readonly WorldSnapshot snapshot = new WorldSnapshot();
        readonly byte[] wire = new byte[NetCodec.SnapshotBytes];
        readonly byte[] rx = new byte[64];
        MatchManager match; BallController ball; IList<PlayerController> players;
        int sendEvery = 3, frame;
        uint tick;

        public IPEndPoint LocalEndPoint => endpoint.LocalEndPoint;
        public int Sent { get; private set; }
        public int ClientCount => clients.Count;
        public uint Tick => tick;

        public void Init(MatchManager match, BallController ball, IList<PlayerController> players, int sendEveryFrames)
        {
            this.match = match; this.ball = ball; this.players = players; sendEvery = Mathf.Max(1, sendEveryFrames);
            endpoint = new UdpEndpoint();
        }

        void Update()
        {
            if (endpoint == null) return;
            while (endpoint.TryReceive(rx, out int n, out System.Net.EndPoint from))
            {
                var ip = (IPEndPoint)from;
                if (!clients.Contains(ip)) clients.Add(ip); // any datagram counts as hello
            }
            tick++;
            if (++frame % sendEvery != 0 || clients.Count == 0) return;
            SnapshotCapture.Fill(snapshot, tick, match, ball, players);
            int len = NetCodec.Write(snapshot, wire);
            foreach (var c in clients) { endpoint.Send(wire, len, c); Sent++; }
        }

        void OnDestroy() { endpoint?.Dispose(); }
    }
}
