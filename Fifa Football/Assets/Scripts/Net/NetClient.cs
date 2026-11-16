using System.Net;
using UnityEngine;

namespace FootballSim.Net
{
    /// <summary>Receives snapshots from a <see cref="NetServer"/> and keeps them in an interpolation buffer. Says hello once per second until the first snapshot arrives.</summary>
    public class NetClient : MonoBehaviour
    {
        UdpEndpoint endpoint;
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

        readonly byte[] inputPacket = new byte[NetCodec.InputPacketBytes];
        readonly System.Collections.Generic.Dictionary<int, ushort> nextSequence = new System.Collections.Generic.Dictionary<int, ushort>();

        /// <summary>Sends one frame of input for the given player index with that player's token. Unreliable: a lost packet is superseded by the next one.</summary>
        public void SendInput(int playerIndex, uint token, FootballSim.Inputs.InputFrame frame)
        {
            if (endpoint == null) return;
            nextSequence.TryGetValue(playerIndex, out ushort seq);
            seq++;
            nextSequence[playerIndex] = seq;
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
            if (n != NetCodec.SnapshotBytes) return; // not a snapshot
            NetCodec.Read(data, 0, incoming);
            if (Buffer.Add(incoming)) { Received++; Delay.OnArrival(Time.unscaledTimeAsDouble, incoming.tick); } else Dropped++;
        }

        void OnDestroy()
        {
            if (endpoint != null && !left) endpoint.Send(new byte[] { NetCodec.LeavePacket }, 1, server); // polite goodbye when the object goes away
            endpoint?.Dispose();
        }
    }
}
