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
        public int InputReceived { get; private set; }
        /// <summary>Packets for a player the server never handed over, or carrying the wrong token.</summary>
        public int InputRejected { get; private set; }
        /// <summary>Packets with a valid token whose sequence number was old or repeated.</summary>
        public int InputStale { get; private set; }

        readonly Dictionary<int, RemoteInput> remotes = new Dictionary<int, RemoteInput>();
        readonly List<int> freeSlots = new List<int>();
        readonly Dictionary<IPEndPoint, int> assigned = new Dictionary<IPEndPoint, int>();
        readonly byte[] welcome = new byte[NetCodec.WelcomeBytes];
        public int JoinsRefused { get; private set; }
        /// <summary>A client we have heard nothing from for this long (seconds, real time) is dropped and its player goes back to the AI.</summary>
        public float clientTimeoutSeconds = 5f;
        public int ClientsTimedOut { get; private set; }
        /// <summary>Clients that said goodbye with a Leave packet.</summary>
        public int ClientsLeft { get; private set; }
        readonly Dictionary<IPEndPoint, float> lastHeard = new Dictionary<IPEndPoint, float>();
        readonly Dictionary<int, FootballSim.Inputs.IPlayerInput> originalInputs = new Dictionary<int, FootballSim.Inputs.IPlayerInput>();

        /// <summary>Player indices that joining clients may be given, in order. A client that sends Join gets the next free one (the same one again if it asks twice).</summary>
        public void OfferRemoteSlots(params int[] playerIndices) { freeSlots.AddRange(playerIndices); }

        /// <summary>Hands one player (index into the players list given to Init) to network control: his input now comes from client input packets.</summary>
        public RemoteInput RegisterRemote(int playerIndex)
        {
            var tokenBytes = new byte[4];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create()) rng.GetBytes(tokenBytes);
            var remote = new RemoteInput(System.BitConverter.ToUInt32(tokenBytes, 0));
            if (!originalInputs.ContainsKey(playerIndex)) originalInputs[playerIndex] = players[playerIndex].Input;
            remotes[playerIndex] = remote;
            players[playerIndex].SetInput(remote, false);
            return remote;
        }

        /// <summary>Gives a remote-controlled player back to the input he had before (the AI brain) and forgets his token.</summary>
        void ReleaseRemote(int playerIndex)
        {
            if (originalInputs.TryGetValue(playerIndex, out var original)) { players[playerIndex].SetInput(original, false); originalInputs.Remove(playerIndex); }
            remotes.Remove(playerIndex);
        }

        void DropSilentClients()
        {
            List<IPEndPoint> gone = null;
            foreach (var c in clients)
                if (lastHeard.TryGetValue(c, out float t) && Time.unscaledTime - t > clientTimeoutSeconds) (gone ?? (gone = new List<IPEndPoint>())).Add(c);
            if (gone == null) return;
            foreach (var c in gone) { RemoveClient(c); ClientsTimedOut++; }
        }

        void RemoveClient(IPEndPoint c)
        {
            clients.Remove(c); lastHeard.Remove(c);
            if (assigned.TryGetValue(c, out int slot)) { assigned.Remove(c); owners.Remove(slot); ReleaseRemote(slot); freeSlots.Insert(0, slot); }
        }

        readonly Dictionary<int, IPEndPoint> owners = new Dictionary<int, IPEndPoint>();
        /// <summary>Packets with the right token for a joined slot but from a different address than the one that joined.</summary>
        public int InputWrongAddress { get; private set; }

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
                if (n < 1) continue;
                if (rx[0] == NetCodec.LeavePacket) { if (clients.Contains(ip)) { RemoveClient(ip); ClientsLeft++; } continue; }
                lastHeard[ip] = Time.unscaledTime;
                if (rx[0] == NetCodec.HelloPacket)
                {
                    if (!clients.Contains(ip)) clients.Add(ip);
                }
                else if (rx[0] == NetCodec.JoinPacket)
                {
                    if (!clients.Contains(ip)) clients.Add(ip);
                    if (!assigned.TryGetValue(ip, out int slot))
                    {
                        if (freeSlots.Count == 0)
                        {
                            JoinsRefused++;
                            endpoint.Send(new byte[] { NetCodec.FullPacket }, 1, ip);
                            continue;
                        }
                        slot = freeSlots[0]; freeSlots.RemoveAt(0);
                        RegisterRemote(slot);
                        assigned[ip] = slot; owners[slot] = ip;
                    }
                    int welcomeLen = NetCodec.WriteWelcome(welcome, slot, remotes[slot].Token);
                    endpoint.Send(welcome, welcomeLen, ip);
                }
                else if (NetCodec.TryReadInputPacket(rx, n, out int index, out uint token, out ushort sequence, out var frame))
                {
                    // the player must be handed over AND the packet must carry that player's secret token; anything else is dropped
                    if (!remotes.TryGetValue(index, out var remote) || remote.Token != token) { InputRejected++; continue; }
                    // a slot given out by Join is bound to the address that joined; the same token from elsewhere is refused
                    if (owners.TryGetValue(index, out var owner) && !owner.Equals(ip)) { InputWrongAddress++; continue; }
                    if (!clients.Contains(ip)) clients.Add(ip);
                    if (remote.ApplySequenced(frame, sequence)) InputReceived++; else InputStale++;
                }
            }
            DropSilentClients();
            tick++;
            if (++frame % sendEvery != 0 || clients.Count == 0) return;
            SnapshotCapture.Fill(snapshot, tick, match, ball, players);
            int len = NetCodec.Write(snapshot, wire);
            foreach (var c in clients) { endpoint.Send(wire, len, c); Sent++; }
        }

        void OnDestroy() { endpoint?.Dispose(); }
    }
}
