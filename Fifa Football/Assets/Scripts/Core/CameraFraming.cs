using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>
    /// How far the match camera should pull in or out so the action stays in view, like an operator following play: tight when the ball
    /// and the players around it are close together, wider when they spread out. Pure maths so it can be tested without a scene.
    /// </summary>
    public static class CameraFraming
    {
        /// <summary>Metres around the focus point that the normal view shows comfortably; a zoom of 1 frames about this much.</summary>
        public const float ReferenceRadius = 15f;
        /// <summary>Automatic zoom limits (below 1 is closer, above 1 is wider).</summary>
        public const float MinAuto = 0.7f, MaxAuto = 1.6f;
        /// <summary>The player's own zoom preference limits.</summary>
        public const float MinUser = 0.6f, MaxUser = 1.5f;
        /// <summary>Zoom used when the camera is told to look at one player.</summary>
        public const float FocusZoom = 0.55f;

        /// <summary>The automatic zoom that keeps every point within the reference radius of the focus (clamped).</summary>
        public static float AutoZoom(Vector2 focus, IList<Vector2> points)
        {
            float r = 0f;
            for (int i = 0; i < points.Count; i++) r = Mathf.Max(r, Vector2.Distance(focus, points[i]));
            return Mathf.Clamp(r / ReferenceRadius, MinAuto, MaxAuto);
        }

        public static float ClampUser(float z) { return Mathf.Clamp(z, MinUser, MaxUser); }

        /// <summary>One zoom button press: a tenth closer (-1) or wider (+1), kept on tenths so the label reads cleanly.</summary>
        public static float StepUser(float z, int direction)
        {
            return ClampUser(Mathf.Round((z + 0.1f * Mathf.Sign(direction)) * 10f) / 10f);
        }
    }
}
