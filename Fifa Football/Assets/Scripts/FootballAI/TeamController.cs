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
        public List<PlayerController> players = new List<PlayerController>();
        public List<PlayerAI> brains = new List<PlayerAI>();

        public PlayerController Controlled { get; private set; }
        public PlayerController Chaser { get; private set; }

        public float OwnGoalZ => -attackDir * Pitch.HalfLength;
        public Vector3 OpponentGoalPos => new Vector3(0f, 0f, attackDir * Pitch.HalfLength);

        void OnEnable() { if (match != null) match.KickoffReady += ResetToKickoff; }
        void OnDisable() { if (match != null) match.KickoffReady -= ResetToKickoff; }

        void Start()
        {
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
                if (p.isGoalkeeper) continue;
                float d = Dist(p);
                if (d < bd) { bd = d; best = p; }
            }
            return best;
        }

        void UpdateChaser()
        {
            PlayerController best = null; float bd = float.MaxValue;
            foreach (var p in players)
            {
                if (p.isGoalkeeper || p == Controlled) continue;
                float d = Dist(p);
                if (d < bd) { bd = d; best = p; }
            }
            // On the human team the AI only chases when the human player is far from the ball
            if (humanControlled && Controlled != null && Dist(Controlled) < bd + 4f) best = null;
            Chaser = best;
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
                Vector3 pos = HomePosition(i, true);
                players[i].TeleportTo(pos, Quaternion.LookRotation(new Vector3(0, 0, attackDir)));
            }
        }
    }
}
