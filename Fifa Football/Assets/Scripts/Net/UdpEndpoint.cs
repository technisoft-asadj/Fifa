using System;
using System.Net;
using System.Net.Sockets;

namespace FootballSim.Net
{
    /// <summary>
    /// A non-blocking UDP socket for polling from Update. Datagrams are unreliable and may arrive late, twice or not at all,
    /// which is what <see cref="SnapshotInterpolator"/> is built to tolerate. No connection handling, encryption or NAT traversal.
    /// </summary>
    public sealed class UdpEndpoint : IDisposable
    {
        readonly Socket socket;

        /// <summary>Binds to the loopback address on the given port (0 = pick a free one).</summary>
        public UdpEndpoint(int port = 0)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            socket.Blocking = false;
        }

        public IPEndPoint LocalEndPoint => (IPEndPoint)socket.LocalEndPoint;

        public int Send(byte[] data, int length, IPEndPoint to)
        {
            try { return socket.SendTo(data, 0, length, SocketFlags.None, to); }
            catch (SocketException) { return 0; }
        }

        /// <summary>Returns true and the datagram size when one is waiting; false when there is nothing to read.</summary>
        public bool TryReceive(byte[] buffer, out int length, out EndPoint from)
        {
            length = 0;
            from = new IPEndPoint(IPAddress.Any, 0);
            if (socket.Available <= 0) return false;
            try { length = socket.ReceiveFrom(buffer, ref from); return true; }
            catch (SocketException) { return false; }
        }

        public void Dispose() { socket.Close(); }
    }
}
