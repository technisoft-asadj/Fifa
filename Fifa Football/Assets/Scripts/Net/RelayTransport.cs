using System;
using System.Collections.Generic;
using System.Net;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;

namespace FootballSim.Net
{
    /// <summary>
    /// <see cref="INetTransport"/> over Unity Relay (Unity Transport, DTLS), so two phones on different networks anywhere in the world can play: both
    /// connect OUT to a relay server and the relay forwards the datagrams. The host creates an allocation and shares its join code; a guest joins with it.
    /// <para>Peers are named with stand-in addresses (10.255.x.y), one per relay connection, so <see cref="NetServer"/> and <see cref="NetClient"/> work unchanged.
    /// A guest talks to exactly one peer, the host, whatever address it passes to <see cref="Send"/>. Messages use the unreliable default pipeline,
    /// which is what the snapshot and input packets are designed for.</para>
    /// </summary>
    public sealed class RelayTransport : INetTransport
    {
        /// <summary>The address a guest should give <see cref="NetClient"/> for the host.</summary>
        public static readonly IPEndPoint HostAddress = new IPEndPoint(new IPAddress(new byte[] { 10, 255, 0, 1 }), 7777);

        /// <summary>Largest datagram handed up; a bigger message (never sent by this game) is dropped.</summary>
        public const int MaxDatagram = 1400;

        readonly bool isHost;
        NetworkDriver driver;
        NetworkConnection guestConnection;
        readonly Dictionary<IPEndPoint, NetworkConnection> byAddress = new Dictionary<IPEndPoint, NetworkConnection>();
        readonly Dictionary<NetworkConnection, IPEndPoint> byConnection = new Dictionary<NetworkConnection, IPEndPoint>();
        readonly List<NetworkConnection> scratch = new List<NetworkConnection>();
        readonly Queue<KeyValuePair<IPEndPoint, byte[]>> inbox = new Queue<KeyValuePair<IPEndPoint, byte[]>>();
        int nextPeer = 2;
        int pumpedFrame = -1;
        readonly Func<int> frameClock;

        /// <summary>Guest only: the connection to the host is up.</summary>
        public bool Connected { get; private set; }
        /// <summary>Guest: the host went away (or never answered and the relay gave up). Host: unused.</summary>
        public bool Lost { get; private set; }
        /// <summary>Host: how many guests are connected through the relay.</summary>
        public int GuestCount => byConnection.Count;

        public IPEndPoint LocalEndPoint => HostAddress;

        RelayTransport(bool isHost, Func<int> frameClock) { this.isHost = isHost; this.frameClock = frameClock ?? (() => UnityEngine.Time.frameCount); }

        /// <summary>Binds the host to the relay with the allocation it just created and starts listening for guests.</summary>
        public static RelayTransport Host(Allocation allocation, Func<int> frameClock = null)
        {
            var t = new RelayTransport(true, frameClock);
            var data = new RelayServerData(allocation, "dtls");
            t.Create(ref data);
            if (t.driver.Bind(NetworkEndpoint.AnyIpv4) != 0) { t.Dispose(); throw new InvalidOperationException("Could not bind to the relay server."); }
            if (t.driver.Listen() != 0) { t.Dispose(); throw new InvalidOperationException("Could not listen on the relay server."); }
            return t;
        }

        /// <summary>Binds a guest to the relay with the allocation it got from the join code and asks the host to connect.</summary>
        public static RelayTransport Join(JoinAllocation allocation, Func<int> frameClock = null)
        {
            var t = new RelayTransport(false, frameClock);
            var data = new RelayServerData(allocation, "dtls");
            t.Create(ref data);
            if (t.driver.Bind(NetworkEndpoint.AnyIpv4) != 0) { t.Dispose(); throw new InvalidOperationException("Could not bind to the relay server."); }
            t.guestConnection = t.driver.Connect();
            return t;
        }

        void Create(ref RelayServerData data)
        {
            var settings = new NetworkSettings();
            settings.WithRelayParameters(ref data);
            driver = NetworkDriver.Create(settings);
        }

        public int Send(byte[] data, int length, IPEndPoint to)
        {
            if (!driver.IsCreated || length <= 0 || length > MaxDatagram) return 0;
            NetworkConnection target;
            if (isHost) { if (to == null || !byAddress.TryGetValue(to, out target)) return 0; }
            else { if (!Connected || !guestConnection.IsCreated) return 0; target = guestConnection; }
            if (driver.BeginSend(target, out var writer) != 0) return 0;
            writer.WriteBytes(new Span<byte>(data, 0, length));
            return driver.EndSend(writer) >= 0 ? length : 0;
        }

        public bool TryReceive(byte[] buffer, out int length, out EndPoint from)
        {
            Pump();
            length = 0; from = HostAddress;
            if (inbox.Count == 0) return false;
            var item = inbox.Dequeue();
            length = Math.Min(item.Value.Length, buffer.Length);
            Buffer.BlockCopy(item.Value, 0, buffer, 0, length);
            from = item.Key;
            return true;
        }

        /// <summary>Moves the relay along once per frame (this also keeps the relay binding alive) and files incoming messages.</summary>
        void Pump()
        {
            int frame = frameClock();
            if (frame == pumpedFrame || !driver.IsCreated) return;
            pumpedFrame = frame;
            driver.ScheduleUpdate().Complete();
            if (isHost) PumpHost(); else PumpGuest();
        }

        void PumpHost()
        {
            NetworkConnection incoming;
            while ((incoming = driver.Accept()) != default(NetworkConnection))
            {
                var address = new IPEndPoint(new IPAddress(new byte[] { 10, 255, (byte)((nextPeer >> 8) & 255), (byte)(nextPeer & 255) }), 7777);
                nextPeer++;
                byAddress[address] = incoming; byConnection[incoming] = address;
            }
            scratch.Clear(); scratch.AddRange(byConnection.Keys);
            foreach (var c in scratch)
            {
                NetworkEvent.Type e;
                while ((e = driver.PopEventForConnection(c, out var stream)) != NetworkEvent.Type.Empty)
                {
                    if (e == NetworkEvent.Type.Data) Enqueue(byConnection[c], stream);
                    else if (e == NetworkEvent.Type.Disconnect)
                    {
                        var address = byConnection[c]; byConnection.Remove(c); byAddress.Remove(address);
                        break;
                    }
                }
            }
        }

        void PumpGuest()
        {
            if (!guestConnection.IsCreated) return;
            NetworkEvent.Type e;
            while ((e = guestConnection.PopEvent(driver, out var stream)) != NetworkEvent.Type.Empty)
            {
                if (e == NetworkEvent.Type.Connect) Connected = true;
                else if (e == NetworkEvent.Type.Data) Enqueue(HostAddress, stream);
                else if (e == NetworkEvent.Type.Disconnect) { Connected = false; Lost = true; guestConnection = default(NetworkConnection); break; }
            }
        }

        void Enqueue(IPEndPoint from, Unity.Collections.DataStreamReader stream)
        {
            int n = stream.Length;
            if (n <= 0 || n > MaxDatagram) return;
            var bytes = new byte[n];
            stream.ReadBytes(new Span<byte>(bytes, 0, n));
            inbox.Enqueue(new KeyValuePair<IPEndPoint, byte[]>(from, bytes));
        }

        public void Dispose()
        {
            if (!driver.IsCreated) return;
            if (!isHost && guestConnection.IsCreated) { try { driver.Disconnect(guestConnection); driver.ScheduleUpdate().Complete(); } catch { } }
            driver.Dispose();
        }
    }
}
