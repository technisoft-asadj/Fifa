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
        public int Received { get; private set; }
        public int Dropped { get; private set; }

        public void Connect(IPEndPoint serverEndPoint)
        {
            server = serverEndPoint;
            endpoint = new UdpEndpoint();
        }

        void Update()
        {
            if (endpoint == null) return;
            if (Received == 0 && Time.unscaledTime >= nextHello)
            {
                endpoint.Send(new byte[] { 1 }, 1, server);
                nextHello = Time.unscaledTime + 1f;
            }
            while (endpoint.TryReceive(rx, out int n, out System.Net.EndPoint from))
            {
                if (n != NetCodec.SnapshotBytes) continue; // not a snapshot
                NetCodec.Read(rx, 0, incoming);
                if (Buffer.Add(incoming)) Received++; else Dropped++;
            }
        }

        void OnDestroy() { endpoint?.Dispose(); }
    }
}
