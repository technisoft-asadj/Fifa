using System.Net;
using System.Net.Sockets;

namespace FootballSim.Net
{
    /// <summary>
    /// The address plumbing a host/join screen needs: turning what a person typed ("192.168.1.20" or "192.168.1.20:7777") into an endpoint,
    /// and finding this device's own LAN address to show to the other player.
    /// </summary>
    public static class LanAddress
    {
        public const int DefaultPort = 7777;

        /// <summary>Parses "a.b.c.d" or "a.b.c.d:port". IPv4 only; hostnames are refused so a typo can't trigger a DNS lookup.</summary>
        public static bool TryParse(string text, out IPEndPoint endPoint)
        {
            endPoint = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            int port = DefaultPort;
            int colon = text.LastIndexOf(':');
            string host = text;
            if (colon >= 0)
            {
                host = text.Substring(0, colon);
                if (!int.TryParse(text.Substring(colon + 1), out port) || port < 1 || port > 65535) return false;
            }
            var parts = host.Split('.');
            if (parts.Length != 4) return false; // IPAddress.TryParse would also accept "1" or "1.2"
            foreach (var p in parts)
                if (p.Length == 0 || p.Length > 3 || !int.TryParse(p, out int octet) || octet < 0 || octet > 255) return false;
            if (!IPAddress.TryParse(host, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork) return false;
            endPoint = new IPEndPoint(ip, port);
            return true;
        }

        /// <summary>This device's first usable private IPv4 address, or null when it has none (offline, airplane mode).</summary>
        public static string LocalAddress()
        {
            try
            {
                // Connecting a UDP socket sends nothing; it only makes the OS choose the interface it would use.
                using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    s.Connect(new IPEndPoint(IPAddress.Parse("192.0.2.1"), 9)); // TEST-NET-1, never routed
                    var ip = ((IPEndPoint)s.LocalEndPoint).Address;
                    return IsPrivate(ip) ? ip.ToString() : null;
                }
            }
            catch { return null; }
        }

        public static bool IsPrivate(IPAddress ip)
        {
            if (ip == null || ip.AddressFamily != AddressFamily.InterNetwork) return false;
            var b = ip.GetAddressBytes();
            return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
        }
    }
}
