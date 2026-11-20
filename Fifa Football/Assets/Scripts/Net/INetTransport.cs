using System;
using System.Net;

namespace FootballSim.Net
{
    /// <summary>
    /// What <see cref="NetServer"/> and <see cref="NetClient"/> need from the wire: unreliable datagrams to and from addressed peers, polled from
    /// Update. Peers are identified by an <see cref="IPEndPoint"/>; a transport that has no real addresses (the relay) hands out a stand-in
    /// address per connection, so the join, token and timeout logic above it does not change.
    /// </summary>
    public interface INetTransport : IDisposable
    {
        /// <summary>Where this side listens. A transport without a real socket returns a stand-in.</summary>
        IPEndPoint LocalEndPoint { get; }

        /// <summary>Sends one datagram. Returns the bytes sent, or 0 when it could not be sent (not connected yet, peer gone).</summary>
        int Send(byte[] data, int length, IPEndPoint to);

        /// <summary>True and the datagram size when one is waiting; false when there is nothing to read.</summary>
        bool TryReceive(byte[] buffer, out int length, out EndPoint from);
    }
}
