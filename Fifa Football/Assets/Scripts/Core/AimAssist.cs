using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>
    /// Aim help for touch controls, which have no aim stick: a shot is bent toward the goal when the player is roughly facing it,
    /// and a pass picks the best team-mate inside a cone around the direction the stick points. Pure maths, no scene access.
    /// </summary>
    public static class AimAssist
    {
        /// <summary>A shot snaps to the goal when the aim is within this many degrees of the direction to the goal centre.</summary>
        public const float ShotSnapDegrees = 55f;
        /// <summary>A pass looks for team-mates within this many degrees of the aim.</summary>
        public const float PassConeDegrees = 55f;
        public const float PassMinDistance = 3f, PassMaxDistance = 40f;
        /// <summary>The weakest a touch shot can be: a quick tap used to give only 25% power, which is a back-pass, not a shot.</summary>
        public const float MinTapShot = 0.6f;

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>Flat direction for a shot from <paramref name="from"/>. <paramref name="aim"/> is where the player pushes or faces.</summary>
        public static Vector3 ShotDirection(Vector3 from, Vector3 aim, float attackSign, float goalHalfWidth)
        {
            aim = Flat(aim);
            if (aim.sqrMagnitude < 0.0001f) aim = new Vector3(0f, 0f, attackSign);
            aim.Normalize();
            float goalZ = attackSign * Pitch.HalfLength;
            Vector3 toGoal = Flat(new Vector3(0f, 0f, goalZ) - from);
            if (toGoal.sqrMagnitude < 0.01f || Vector3.Angle(aim, toGoal) > ShotSnapDegrees) return aim;

            // aim at the point where the pushed direction crosses the goal line, kept inside the posts, so the player still chooses a side
            float x = 0f;
            if (aim.z * attackSign > 0.05f) x = from.x + aim.x / aim.z * (goalZ - from.z);
            float reach = goalHalfWidth * 0.8f;
            x = Mathf.Clamp(x, -reach, reach);
            Vector3 dir = Flat(new Vector3(x, 0f, goalZ) - from);
            return dir.sqrMagnitude < 0.0001f ? aim : dir.normalized;
        }

        /// <summary>
        /// Index of the team-mate a pass should go to, or -1 if nobody is inside the cone. Prefers mates close to the aim line and not too far.
        /// <paramref name="blocked"/> (optional) marks mates who must not be picked, such as a player in an offside position.
        /// </summary>
        public static int PickPassTarget(Vector3 from, Vector3 aim, IList<Vector3> mates, IList<bool> blocked = null)
        {
            aim = Flat(aim);
            if (aim.sqrMagnitude < 0.0001f || mates == null) return -1;
            aim.Normalize();
            int best = -1; float bestScore = float.MinValue;
            for (int i = 0; i < mates.Count; i++)
            {
                if (blocked != null && i < blocked.Count && blocked[i]) continue;
                Vector3 d = Flat(mates[i] - from);
                float dist = d.magnitude;
                if (dist < PassMinDistance || dist > PassMaxDistance) continue;
                float angle = Vector3.Angle(aim, d);
                if (angle > PassConeDegrees) continue;
                float score = -angle - dist * 0.35f;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        /// <summary>Pass power (0..1 of the player's maximum pass speed) for a pass over <paramref name="distance"/> metres.</summary>
        public static float PassPower(float distance, float maxPassSpeed)
        {
            float speed = Mathf.Clamp(6f + distance * 0.45f, 8f, Mathf.Max(8f, maxPassSpeed));
            return Mathf.Clamp01(speed / Mathf.Max(0.01f, maxPassSpeed));
        }
    }
}
