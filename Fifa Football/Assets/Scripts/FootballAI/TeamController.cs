using System.Collections.Generic;
using UnityEngine;
using FootballSim.Ball;
using FootballSim.Core;
using FootballSim.Inputs;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.FootballAI
{
    /// <summary>
    /// Owns a team's players: formation positions, who chases the ball, and (for the human team)
    /// which player the human controls and player switching.
    /// </summary>
    public class TeamController : MonoBehaviour
    {
        public int teamId;
        [Tooltip("+1 = attacks +Z, -1 = attacks -Z")] public float attackDir = 1f;
        public bool humanControlled;
        public BallController ball;
        public MatchManager match;
        public FormationData formation;
        public PlayerInputSource humanInput;
        public FootballSim.CameraSystem.BroadcastCamera cameraRig;
        public List<PlayerController> players = new List<PlayerController>();
        public List<PlayerAI> brains = new List<PlayerAI>();

        public PlayerController Controlled { get; private set; }
        public PlayerController Chaser { get; private set; }

        public float OwnGoalZ => -attackDir * Pitch.HalfLength;
        public Vector3 OpponentGoalPos => new Vector3(0f, 0f, attackDir * Pitch.HalfLength);

        void OnEnable() { if (match != null) match.KickoffReady += ResetToKickoff; }
        void OnDisable() { if (match != null) match.KickoffReady -= ResetToKickoff; }

        /// <summary>Teleport the player closest to a point next to it (used for throw-ins, corners, goal kicks).</summary>
        public PlayerController SendTakerTo(Vector3 spot, Vector3 facing, bool outfieldOnly = false)
        {
            PlayerController best = null; float bd = float.MaxValue;
            foreach (var p in players)
            {
                if (!IsOn(p) || (outfieldOnly && p.isGoalkeeper)) continue;
                float d = (p.transform.position - spot).sqrMagnitude;
                if (d < bd) { bd = d; best = p; }
            }
            if (best == null) return null;
            Vector3 behind = spot - facing.normalized * 0.9f;
            behind.y = 1f;
            best.TeleportTo(behind, Quaternion.LookRotation(facing.normalized, Vector3.up));
            if (humanControlled && !best.isGoalkeeper) SetControlled(best);
            return best;
        }

        /// <summary>True while the player is on the pitch (not sent off).</summary>
        public static bool IsOn(PlayerController p) { return p != null && p.gameObject.activeSelf; }

        /// <summary>Players still on the pitch.</summary>
        public int ActiveCount { get { int n = 0; foreach (var p in players) if (IsOn(p)) n++; return n; } }

        /// <summary>Red card: remove a player from the pitch for the rest of the match. Slots keep their indices.</summary>
        public void SendOff(PlayerController p)
        {
            if (!players.Contains(p) || !IsOn(p)) return;
            if (p == Controlled)
            {
                PlayerController next = null; float bd = float.MaxValue;
                foreach (var q in players)
                {
                    if (q == p || q.isGoalkeeper || !IsOn(q)) continue;
                    float d = Dist(q);
                    if (d < bd) { bd = d; next = q; }
                }
                if (next != null) SetControlled(next);
            }
            p.gameObject.SetActive(false);
            if (Chaser == p) Chaser = null;
        }

        /// <summary>
        /// Penalty set-up: move everyone except the taker out of the penalty area at goalZ.
        /// The keeper defending that goal is put on the goal line.
        /// </summary>
        public void ClearPenaltyArea(float goalZ, PlayerController taker)
        {
            float inward = -Mathf.Sign(goalZ); // direction from the goal line toward the centre
            bool defending = Mathf.Approximately(Mathf.Sign(OwnGoalZ), Mathf.Sign(goalZ));
            foreach (var p in players)
            {
                if (!IsOn(p) || p == taker) continue;
                if (p.isGoalkeeper && defending)
                {
                    p.TeleportTo(new Vector3(0f, 1f, goalZ + inward * 0.6f), Quaternion.LookRotation(new Vector3(0, 0, inward)));
                    continue;
                }
                Vector3 pos = p.transform.position;
                if (!Pitch.InPenaltyArea(pos, goalZ)) continue;
                pos.z = goalZ + inward * (Pitch.PenaltyAreaDepth + 2.5f);
                p.TeleportTo(pos, Quaternion.LookRotation(new Vector3(0, 0, -inward)));
            }
        }

        void Start()
        {
            for (int i = 0; i < players.Count; i++) players[i].TeamId = teamId;
            for (int i = 0; i < players.Count; i++)
                players[i].SetInput(brains[i].Input, false);
            ResetToKickoff();
            if (humanControlled) SetControlled(ClosestOutfielderToBall());
        }

        void Update()
        {
            UpdateChaser();
            if (!humanControlled || Controlled == null) return;

            if (humanInput.SwitchPressed) SetControlled(ClosestOutfielderToBall());
            else
            {
                // Auto-switch when a teammate is clearly closer to a loose/opposing ball
                var best = ClosestOutfielderToBall();
                if (best != Controlled && Dist(Controlled) - Dist(best) > 6f) SetControlled(best);
            }
        }

        float Dist(PlayerController p)
        {
            Vector3 d = p.transform.position - ball.transform.position; d.y = 0f; return d.magnitude;
        }

        PlayerController ClosestOutfielderToBall()
        {
            PlayerController best = null; float bd = float.MaxValue;
            foreach (var p in players)
            {
                if (p.isGoalkeeper || !IsOn(p)) continue;
                float d = Dist(p);
                if (d < bd) { bd = d; best = p; }
            }
            return best;
        }

        PlayerController passReceiver;
        float passReceiverUntil;
        /// <summary>The team-mate an AI pass was aimed at; he becomes the chaser immediately so he runs onto the ball.</summary>
        public void SetPassReceiver(PlayerController receiver, float seconds)
        {
            passReceiver = receiver;
            passReceiverUntil = Time.time + seconds;
        }

        void UpdateChaser()
        {
            if (passReceiver != null && Time.time < passReceiverUntil && IsOn(passReceiver) && passReceiver != Controlled
                && ball.LastTouchTeam == teamId && !ball.Body.isKinematic)
            {
                Chaser = passReceiver;
                return;
            }
            passReceiver = null;
            PlayerController best = null; float bd = float.MaxValue;
            foreach (var p in players)
            {
                if (p.isGoalkeeper || p == Controlled || !IsOn(p)) continue;
                float d = Dist(p);
                if (d < bd) { bd = d; best = p; }
            }
            // On the human team the AI only chases when the human player is far from the ball
            if (humanControlled && Controlled != null && Dist(Controlled) < bd + 4f) best = null;
            Chaser = best;
        }

        /// <summary>Hand the human's player back to the AI so the whole team plays itself (demo / simulation mode).</summary>
        public void ReleaseHumanControl()
        {
            if (Controlled != null)
            {
                int i = players.IndexOf(Controlled);
                Controlled.SetInput(brains[i].Input, false);
                brains[i].enabled = true;
                Controlled = null;
            }
            humanControlled = false;
        }

        public void SetControlled(PlayerController p)
        {
            if (p == null || p == Controlled) return;
            if (Controlled != null)
            {
                int oi = players.IndexOf(Controlled);
                Controlled.SetInput(brains[oi].Input, false);
                brains[oi].enabled = true;
            }
            Controlled = p;
            if (cameraRig != null) cameraRig.SetPlayer(p.transform);
            int ni = players.IndexOf(p);
            brains[ni].enabled = false;
            brains[ni].Input.Clear();
            p.SetInput(humanInput, true);
        }

        /// <summary>World-space formation position for a slot, shifted toward the ball.</summary>
        public Vector3 HomePosition(int slot, bool kickoff = false)
        {
            Vector2 s = formation.slots[slot];
            float x = s.x * attackDir * (Pitch.HalfWidth - 6f);
            float z;
            if (kickoff) z = OwnGoalZ + attackDir * s.y * Pitch.HalfLength * 0.92f;
            else
            {
                z = OwnGoalZ + attackDir * s.y * Pitch.HalfLength * 1.6f;
                if (slot != 0) z += ball.transform.position.z * 0.35f;
                x += ball.transform.position.x * 0.15f * (slot == 0 ? 0f : 1f);
            }
            float limit = Pitch.HalfLength - 3f;
            z = Mathf.Clamp(z, -limit, limit);
            if (slot == 0) z = OwnGoalZ + attackDir * 1.5f;
            return new Vector3(Mathf.Clamp(x, -Pitch.HalfWidth + 2f, Pitch.HalfWidth - 2f), 1f, z);
        }

        public void ResetToKickoff()
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (!IsOn(players[i])) continue; // sent off
                Vector3 pos = HomePosition(i, true);
                players[i].TeleportTo(pos, Quaternion.LookRotation(new Vector3(0, 0, attackDir)));
            }
        }
    }
}
