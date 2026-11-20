using System.Net;
using UnityEngine;

namespace FootballSim.Net
{
    /// <summary>Receives snapshots from a <see cref="NetServer"/> and keeps them in an interpolation buffer. Says hello once per second until the first snapshot arrives.</summary>
    public class NetClient : MonoBehaviour
    {
        INetTransport endpoint;
        IPEndPoint server;
        readonly byte[] rx = new byte[1500];
        readonly WorldSnapshot incoming = new WorldSnapshot();
        float nextHello;

        public readonly SnapshotInterpolator Buffer = new SnapshotInterpolator();
        /// <summary>Measures arrival jitter and suggests how far behind to draw.</summary>
        public readonly AdaptiveDelay Delay = new AdaptiveDelay();
        public int Received { get; private set; }
        public int Dropped { get; private set; }

        /// <summary>Set once the server's Welcome arrives: which player this client controls and the token that proves it.</summary>
        public bool Joined { get; private set; }
        public bool ServerFull { get; private set; }
        public int PlayerIndex { get; private set; }
        public uint Token { get; private set; }
        bool wantsJoin; float nextJoin;

        /// <summary>The newest input sequence the server says it has applied for our player, and the server tick that was true at. False until the first ack.</summary>
        public bool HasAck { get; private set; }
        public ushort AckedSequence { get; private set; }
        public uint AckedTick { get; private set; }

        // ---- ping: how long an input takes to be acknowledged (send time of each sequence number is remembered) ----
        readonly double[] sentAt = new double[256];
        readonly ushort[] sentSeq = new ushort[256];
        readonly bool[] sentOpen = new bool[256];
        /// <summary>Smoothed round-trip time in milliseconds, measured from an input to the server's ack of it. Includes up to one server tick.</summary>
        public float RttMs { get; private set; }
        public bool HasRtt { get; private set; }

        /// <summary>The score and game minute in the newest snapshot (the host's match), so a guest can show them.</summary>
        public bool HasMatchInfo { get; private set; }
        public int LatestHomeScore { get; private set; }
        public int LatestAwayScore { get; private set; }
        /// <summary>The match clock in game minutes, as the host shows it.</summary>
        public float LatestGameMinute { get; private set; }
        double lastSnapshotAt;
        /// <summary>Seconds since a snapshot last arrived; infinity before the first one.</summary>
        public float SecondsSinceSnapshot => Received == 0 ? float.PositiveInfinity : (float)(Time.unscaledTimeAsDouble - lastSnapshotAt);

        /// <summary>Asks the server for a player slot; repeats once a second until it answers.</summary>
        public void Join() { wantsJoin = true; nextJoin = 0f; }

        /// <summary>Sends input for the player the server assigned. Does nothing before the Welcome arrives.</summary>
        public void SendInput(FootballSim.Inputs.InputFrame frame) { if (Joined) SendInput(PlayerIndex, Token, frame); }

        /// <summary>Tells the server we are going so our slot is freed now. Unreliable: if it is lost the server's timeout still frees the slot. Stops hello and join.</summary>
        public void Leave()
        {
            if (endpoint == null) return;
            endpoint.Send(new byte[] { NetCodec.LeavePacket }, 1, server);
            wantsJoin = false; Joined = false; left = true;
        }
        bool left;

        public void Connect(IPEndPoint serverEndPoint)
        {
            server = serverEndPoint;
            // a loopback server needs a loopback socket; a server elsewhere can only be reached from a socket open to the network
            endpoint = new UdpEndpoint(0, !IPAddress.IsLoopback(serverEndPoint.Address));
        }

        /// <summary>Connects over a transport the caller built (the internet relay); <paramref name="serverAddress"/> is the stand-in address that transport uses for the host.</summary>
        public void Connect(INetTransport transport, IPEndPoint serverAddress) { server = serverAddress; endpoint = transport; }

        readonly byte[] inputPacket = new byte[NetCodec.InputPacketBytes];
        readonly System.Collections.Generic.Dictionary<int, ushort> nextSequence = new System.Collections.Generic.Dictionary<int, ushort>();

        /// <summary>Sends one frame of input for the given player index with that player's token. Unreliable: a lost packet is superseded by the next one.</summary>
        public void SendInput(int playerIndex, uint token, FootballSim.Inputs.InputFrame frame)
        {
            if (endpoint == null) return;
            nextSequence.TryGetValue(playerIndex, out ushort seq);
            seq++;
            nextSequence[playerIndex] = seq;
            sentSeq[seq & 255] = seq; sentAt[seq & 255] = Time.unscaledTimeAsDouble; sentOpen[seq & 255] = true;
            NetCodec.WriteInputPacket(inputPacket, playerIndex, token, seq, frame);
            endpoint.Send(inputPacket, inputPacket.Length, server);
        }

        /// <summary>Test hook: sends a packet with an explicit sequence number (to repeat or reorder).</summary>
        public void SendInputWithSequence(int playerIndex, uint token, ushort sequence, FootballSim.Inputs.InputFrame frame)
        {
            if (endpoint == null) return;
            NetCodec.WriteInputPacket(inputPacket, playerIndex, token, sequence, frame);
            endpoint.Send(inputPacket, inputPacket.Length, server);
        }

        void Update()
        {
            if (endpoint == null || left) return;
            if (Time.unscaledTime >= nextHello) // hello once a second doubles as the keep-alive that stops the server timing us out
            {
                endpoint.Send(new byte[] { NetCodec.HelloPacket }, 1, server);
                nextHello = Time.unscaledTime + 1f;
            }
            if (wantsJoin && !Joined && !ServerFull && Time.unscaledTime >= nextJoin)
            {
                endpoint.Send(new byte[] { NetCodec.JoinPacket }, 1, server);
                nextJoin = Time.unscaledTime + 1f;
            }
            while (endpoint.TryReceive(rx, out int n, out System.Net.EndPoint from))
            {
                if (Inbound != null) Inbound.Send(rx, n, Time.unscaledTimeAsDouble * 1000.0);
                else Handle(rx, n);
            }
            if (Inbound != null)
                foreach (var d in Inbound.Poll(Time.unscaledTimeAsDouble * 1000.0)) Handle(d, d.Length);
        }

        /// <summary>Test hook: when set, everything the server sends passes through this simulated bad network before the client sees it.</summary>
        public NetworkConditioner Inbound;

        void Handle(byte[] data, int n)
        {
            if (NetCodec.TryReadWelcome(data, n, out int idx, out uint token)) { PlayerIndex = idx; Token = token; Joined = true; return; }
            if (n == 1 && data[0] == NetCodec.FullPacket) { ServerFull = true; return; }
            if (NetCodec.TryReadAck(data, n, out ushort ackSeq, out uint ackTick))
            {
                // ignore a late, reordered ack that would move the confirmed point backwards
                if (!HasAck || NetCodec.IsNewer(ackSeq, AckedSequence)) { AckedSequence = ackSeq; AckedTick = ackTick; HasAck = true; }
                int slot = ackSeq & 255;
                if (sentOpen[slot] && sentSeq[slot] == ackSeq)
                {
                    sentOpen[slot] = false; // one sample per input
                    float sample = (float)((Time.unscaledTimeAsDouble - sentAt[slot]) * 1000.0);
                    RttMs = HasRtt ? RttMs + (sample - RttMs) * 0.2f : sample;
                    HasRtt = true;
                }
                return;
            }
            if (n != NetCodec.SnapshotBytes) return; // not a snapshot
            NetCodec.Read(data, 0, incoming);
            if (Buffer.Add(incoming))
            {
                Received++; Delay.OnArrival(Time.unscaledTimeAsDouble, incoming.tick);
                lastSnapshotAt = Time.unscaledTimeAsDouble;
                HasMatchInfo = true; LatestHomeScore = incoming.homeScore; LatestAwayScore = incoming.awayScore; LatestGameMinute = incoming.clockTenths / 10f;
            }
            else Dropped++;
        }

        void OnDestroy()
        {
            if (endpoint != null && !left) endpoint.Send(new byte[] { NetCodec.LeavePacket }, 1, server); // polite goodbye when the object goes away
            endpoint?.Dispose();
        }
    }
}
