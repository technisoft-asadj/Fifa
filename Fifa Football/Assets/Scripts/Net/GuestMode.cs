using System.Collections.Generic;
using System.Net;
using UnityEngine;
using FootballSim.Ball;
using FootballSim.CameraSystem;
using FootballSim.FootballAI;
using FootballSim.Inputs;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.Net
{
    /// <summary>
    /// Turns the running scene into a guest's screen: the local match is switched off and hidden, a client joins the server at the given
    /// address, and the broadcast camera follows the ball and the player the server assigned, drawn from snapshots only
    /// (<see cref="NetClientView"/>: plain capsules). <see cref="Leave"/> puts everything back. The local scoreboard is not driven by the
    /// server yet and stays at its own state.
    /// </summary>
    public class GuestMode
    {
        public NetSession.ClientHandle Session { get; private set; }
        public bool Active { get; private set; }

        readonly List<Behaviour> switchedOff = new List<Behaviour>();
        readonly List<Renderer> hidden = new List<Renderer>();
        readonly List<KeyValuePair<Rigidbody, bool>> ballKinematic = new List<KeyValuePair<Rigidbody, bool>>();
        BroadcastCamera camera;
        Transform oldBall, oldPlayer;
        bool cameraFollowing;
        NetClient watched;

        /// <param name="silenceLocalMatch">false leaves the local simulation running; only for a test that hosts and joins inside one scene.</param>
        /// <param name="transport">null joins over plain UDP at <paramref name="server"/>; the relay transport joins an internet room (then <paramref name="server"/> is <see cref="RelayTransport.HostAddress"/>).</param>
        public static GuestMode Enter(IPEndPoint server, IPlayerInput input, bool silenceLocalMatch = true, INetTransport transport = null)
        {
            var g = new GuestMode();
            if (silenceLocalMatch) g.SilenceLocalMatch();
            g.Session = NetSession.Join(server, transport, input, Vector3.zero);
            g.camera = Object.FindFirstObjectByType<BroadcastCamera>();
            if (g.camera != null)
            {
                g.oldBall = g.camera.Ball; g.oldPlayer = g.camera.Player;
                g.camera.SetTargets(g.Session.view.Ball, null);
                g.cameraFollowing = true;
            }
            g.watched = g.Session.client;
            g.Session.root.AddComponent<GuestCameraFollow>().Init(g);
            g.Active = true;
            return g;
        }

        void SilenceLocalMatch()
        {
            Off(Object.FindFirstObjectByType<MatchManager>());
            foreach (var t in Object.FindObjectsByType<TeamController>(FindObjectsSortMode.None)) Off(t);
            foreach (var p in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                Off(p);
                var ai = p.GetComponent<PlayerAI>(); if (ai != null) Off(ai);
                Hide(p.gameObject);
            }
            var ball = Object.FindFirstObjectByType<BallController>();
            if (ball != null)
            {
                Off(ball); Hide(ball.gameObject);
                if (ball.Body != null) { ballKinematic.Add(new KeyValuePair<Rigidbody, bool>(ball.Body, ball.Body.isKinematic)); ball.Body.isKinematic = true; }
            }
        }

        void Off(Behaviour b) { if (b != null && b.enabled) { b.enabled = false; switchedOff.Add(b); } }
        void Hide(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; hidden.Add(r); }
        }

        /// <summary>Called every frame by the session: once the server has assigned a player, the camera blends toward him.</summary>
        internal void Tick()
        {
            if (!Active || camera == null || Session == null || !watched.Joined) return;
            int i = watched.PlayerIndex;
            var bodies = Session.view.Bodies;
            if (i >= 0 && i < bodies.Length && camera.Player != bodies[i]) camera.SetTargets(null, bodies[i]);
        }

        /// <summary>Says goodbye, removes the network view and restores the local match exactly as it was.</summary>
        public void Leave()
        {
            if (!Active) return;
            Active = false;
            if (Session != null && Session.root != null) Object.Destroy(Session.root); // the client sends its Leave as it goes
            if (camera != null && cameraFollowing) camera.SetTargets(oldBall, oldPlayer);
            foreach (var b in switchedOff) if (b != null) b.enabled = true;
            foreach (var r in hidden) if (r != null) r.enabled = true;
            foreach (var kv in ballKinematic) if (kv.Key != null) kv.Key.isKinematic = kv.Value;
            switchedOff.Clear(); hidden.Clear(); ballKinematic.Clear();
        }
    }

    /// <summary>Drives <see cref="GuestMode.Tick"/> from the session object, so the guest stops with it.</summary>
    class GuestCameraFollow : MonoBehaviour
    {
        GuestMode guest;
        public void Init(GuestMode g) { guest = g; }
        void LateUpdate() { guest?.Tick(); }
    }
}
