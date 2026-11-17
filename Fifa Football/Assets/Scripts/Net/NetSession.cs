using System.Collections.Generic;
using System.Net;
using UnityEngine;
using FootballSim.Ball;
using FootballSim.Inputs;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.Net
{
    /// <summary>
    /// One call per side to start online play, so a menu screen needs no knowledge of the parts. <see cref="Host"/> puts a server next to
    /// the running match; <see cref="Join"/> creates a client that joins, a view that draws the match from snapshots, and an input sender.
    /// Both return the root object; destroying it shuts everything down (a client says goodbye as it goes).
    /// </summary>
    public static class NetSession
    {
        public class HostHandle { public GameObject root; public NetServer server; }
        public class ClientHandle { public GameObject root; public NetClient client; public NetClientView view; public NetInputSender sender; }

        /// <summary>Starts a server for the given match. <paramref name="remoteSlots"/> are the player indices joining clients may take.</summary>
        public static HostHandle Host(MatchManager match, BallController ball, IList<PlayerController> players, params int[] remoteSlots)
        {
            var root = new GameObject("NetHost");
            var server = root.AddComponent<NetServer>();
            server.Init(match, ball, players, 3);
            server.OfferRemoteSlots(remoteSlots);
            return new HostHandle { root = root, server = server };
        }

        /// <summary>
        /// Like <see cref="Host"/> but reachable from other devices on the network (all interfaces) on a fixed port, so a friend can type the address.
        /// Needs no account, relay or paid service: it is plain UDP between two devices on the same Wi-Fi. Falls back to a free port if
        /// <paramref name="port"/> is taken; read the real one from <c>server.LocalEndPoint</c>.
        /// </summary>
        public static HostHandle HostOnLan(MatchManager match, BallController ball, IList<PlayerController> players, int port, params int[] remoteSlots)
        {
            var root = new GameObject("NetHost");
            var server = root.AddComponent<NetServer>();
            try { server.Init(match, ball, players, 3, true, port); }
            catch (System.Net.Sockets.SocketException) { server.Init(match, ball, players, 3, true, 0); }
            server.OfferRemoteSlots(remoteSlots);
            return new HostHandle { root = root, server = server };
        }

        /// <summary>Starts a client that joins the server at <paramref name="server"/> and sends <paramref name="input"/> once it has a slot.</summary>
        public static ClientHandle Join(IPEndPoint server, IPlayerInput input, Vector3 viewOrigin)
        {
            var root = new GameObject("NetClientSession");
            root.transform.position = viewOrigin;
            var client = root.AddComponent<NetClient>();
            client.Connect(server); client.Join();
            var view = root.AddComponent<NetClientView>();
            view.client = client;
            var sender = root.AddComponent<NetInputSender>();
            sender.client = client; sender.SetSource(input);
            return new ClientHandle { root = root, client = client, view = view, sender = sender };
        }
    }
}
