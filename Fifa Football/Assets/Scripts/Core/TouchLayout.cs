using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace FootballSim.Core
{
    public enum TouchButtonId { Shoot, Pass, Tackle, Switch, Sprint }

    /// <summary>
    /// Where each on-screen button sits, and the rules for moving them. Positions are stored for the right-handed layout, as the offset of the
    /// button centre from the bottom-right corner of the safe area (so x is negative); the left-handed layout mirrors them. Kept free of
    /// UI types so it can be tested without a scene.
    /// </summary>
    public static class TouchLayout
    {
        public static Vector2 DefaultPosition(TouchButtonId id)
        {
            switch (id)
            {
                case TouchButtonId.Shoot: return new Vector2(-210f, 210f);
                case TouchButtonId.Pass: return new Vector2(-460f, 140f);
                case TouchButtonId.Tackle: return new Vector2(-120f, 450f);
                case TouchButtonId.Switch: return new Vector2(-390f, 380f);
                default: return new Vector2(-640f, 100f); // Sprint
            }
        }

        public static float DefaultSize(TouchButtonId id)
        {
            switch (id)
            {
                case TouchButtonId.Shoot: return 190f;
                case TouchButtonId.Pass: case TouchButtonId.Tackle: return 150f;
                default: return 130f;
            }
        }

        /// <summary>Keeps a button fully inside the area (canvas units), so it can never be dragged off the screen or under the stick's far edge.</summary>
        public static Vector2 Clamp(Vector2 pos, float size, Vector2 area)
        {
            float half = size * 0.5f;
            float maxX = -half, minX = -(area.x - half);
            float minY = half, maxY = area.y - half;
            if (minX > maxX) minX = maxX; // an area narrower than the button: pin to the edge
            if (minY > maxY) maxY = minY;
            return new Vector2(Mathf.Clamp(pos.x, minX, maxX), Mathf.Clamp(pos.y, minY, maxY));
        }

        /// <summary>Two buttons overlap when their centres are closer than the sum of their radii (a drop that would hide a button is refused).</summary>
        public static bool Overlaps(Vector2 a, float sizeA, Vector2 b, float sizeB)
        {
            return Vector2.Distance(a, b) < (sizeA + sizeB) * 0.5f;
        }

        // ---- saving: one short string, "Shoot:-210,210;Pass:-460,140" (only buttons that were moved) ----
        public static string Serialize(IDictionary<TouchButtonId, Vector2> positions)
        {
            var sb = new StringBuilder();
            foreach (var kv in positions)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(kv.Key).Append(':')
                  .Append(kv.Value.x.ToString("0.#", CultureInfo.InvariantCulture)).Append(',')
                  .Append(kv.Value.y.ToString("0.#", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>Reads a saved layout; anything unreadable is skipped, so a damaged value falls back to the defaults.</summary>
        public static Dictionary<TouchButtonId, Vector2> Parse(string text)
        {
            var result = new Dictionary<TouchButtonId, Vector2>();
            if (string.IsNullOrEmpty(text)) return result;
            foreach (var part in text.Split(';'))
            {
                var kv = part.Split(':');
                if (kv.Length != 2) continue;
                if (!System.Enum.TryParse(kv[0], out TouchButtonId id) || !System.Enum.IsDefined(typeof(TouchButtonId), id)) continue;
                var xy = kv[1].Split(',');
                if (xy.Length != 2) continue;
                if (!float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                if (float.IsNaN(x) || float.IsNaN(y) || float.IsInfinity(x) || float.IsInfinity(y)) continue;
                result[id] = new Vector2(x, y);
            }
            return result;
        }
    }

    /// <summary>How the stick responds: sensitivity scales the thumb travel, so a small push can already be a full-speed run.</summary>
    public static class TouchTuning
    {
        public const float MinSensitivity = 0.6f, MaxSensitivity = 1.6f;

        public static float ClampSensitivity(float s) { return Mathf.Clamp(s, MinSensitivity, MaxSensitivity); }

        /// <summary>The stick value the game sees for a raw stick position (length 0 to 1) at the given sensitivity.</summary>
        public static Vector2 Shape(Vector2 raw, float sensitivity)
        {
            return Vector2.ClampMagnitude(raw * ClampSensitivity(sensitivity), 1f);
        }
    }
}
