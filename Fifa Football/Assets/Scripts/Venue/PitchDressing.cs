using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using FootballSim.Ball;
using FootballSim.Core;

namespace FootballSim.Venue
{
    /// <summary>
    /// What makes the pitch and the picture look like a real match: full pitch markings (centre circle, penalty and goal areas, arcs, spots), mown grass
    /// stripes, a panelled ball, goal nets, and a light colour grade. All made from code; nothing is downloaded.
    /// </summary>
    public static class PitchDressing
    {
        const float LineWidth = 0.18f, LineY = 0.03f;

        // ---------------------------------------------------------------- markings (added to the stadium's static mesh) ----
        public static void Markings(StadiumMeshKit kit, StadiumPalette palette)
        {
            int white = palette.Get(new Color(0.96f, 0.97f, 0.96f));
            float hl = Pitch.HalfLength, hw = Pitch.HalfWidth;
            // centre circle and spot
            Arc(kit, white, Vector3.zero, 9.15f, 0f, 360f, 64);
            Disc(kit, white, Vector3.zero, 0.22f);
            for (int end = -1; end <= 1; end += 2)
            {
                float gz = end * hl;
                // penalty area and goal area (their sides run from the goal line into the pitch)
                Box(kit, white, gz, -end, Pitch.PenaltyAreaHalfWidth, Pitch.PenaltyAreaDepth);
                Box(kit, white, gz, -end, 9.16f, 5.5f);
                // penalty spot and the arc outside the area
                float spotZ = gz - end * Pitch.PenaltySpotDistance;
                Disc(kit, white, new Vector3(0f, 0f, spotZ), 0.2f);
                float edge = Pitch.PenaltyAreaDepth - Pitch.PenaltySpotDistance; // 5.5 m from the spot to the area's front line
                float phi = Mathf.Acos(edge / 9.15f) * Mathf.Rad2Deg;
                // the arc bulges toward the pitch centre: angle 0 points from the spot toward the centre
                ArcOriented(kit, white, new Vector3(0f, 0f, spotZ), 9.15f, -end, -phi, phi, 24);
                // corner arcs
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    Vector3 c = new Vector3(sx * hw, 0f, gz);
                    float mid = Mathf.Atan2(-end, -sx) * Mathf.Rad2Deg; // the quarter circle opens toward the pitch
                    Arc(kit, white, c, 1f, mid - 45f, mid + 45f, 8);
                }
            }
        }

        // a rectangle on the goal line: width 2*halfW, reaching dir*depth into the pitch (dir = -1 goes toward -Z)
        static void Box(StadiumMeshKit kit, int color, float goalZ, int dir, float halfW, float depth)
        {
            float z0 = goalZ, z1 = goalZ + dir * depth;
            kit.Strip(new Vector3(-halfW, LineY, z1), new Vector3(halfW, LineY, z1), LineWidth, color);
            kit.Strip(new Vector3(-halfW, LineY, z0), new Vector3(-halfW, LineY, z1), LineWidth, color);
            kit.Strip(new Vector3(halfW, LineY, z0), new Vector3(halfW, LineY, z1), LineWidth, color);
        }

        static void Arc(StadiumMeshKit kit, int color, Vector3 c, float r, float fromDeg, float toDeg, int steps)
        {
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)steps) * Mathf.Deg2Rad;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * r, LineY, Mathf.Sin(a) * r);
                if (i > 0) kit.Strip(prev + Vector3.zero, p, LineWidth, color);
                prev = p;
            }
        }

        // an arc whose middle points along +/-Z (zDir), spanning from -phi to +phi either side of it
        static void ArcOriented(StadiumMeshKit kit, int color, Vector3 c, float r, int zDir, float fromDeg, float toDeg, int steps)
        {
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)steps) * Mathf.Deg2Rad;
                Vector3 p = c + new Vector3(Mathf.Sin(a) * r, LineY, zDir * Mathf.Cos(a) * r);
                if (i > 0) kit.Strip(prev, p, LineWidth, color);
                prev = p;
            }
        }

        static void Disc(StadiumMeshKit kit, int color, Vector3 c, float r)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 6f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                kit.Strip(c - d + Vector3.up * LineY, c + d + Vector3.up * LineY, r * 1.2f * 0.5f, color);
            }
        }

        // ---------------------------------------------------------------- goal nets ----
        public static void Nets(StadiumMeshKit kit, StadiumPalette palette)
        {
            int white = palette.Get(new Color(0.92f, 0.93f, 0.95f));
            const float t = 0.018f, depth = 2.1f, h = 2.44f, hw = Pitch.GoalHalfWidth, step = 0.3f;
            for (int end = -1; end <= 1; end += 2)
            {
                float z0 = end * Pitch.HalfLength, z1 = z0 + end * depth;
                // back: vertical and horizontal cords
                for (float x = -hw; x <= hw + 0.01f; x += step) kit.Box(new Vector3(x, h * 0.5f, z1), new Vector3(t, h, t), white);
                for (float y = 0f; y <= h + 0.01f; y += step) kit.Box(new Vector3(0f, y, z1), new Vector3(hw * 2f, t, t), white);
                // top: cords across and along
                for (float x = -hw; x <= hw + 0.01f; x += step) kit.Box(new Vector3(x, h, (z0 + z1) * 0.5f), new Vector3(t, t, depth), white);
                for (float z = 0f; z <= depth + 0.01f; z += step) kit.Box(new Vector3(0f, h, z0 + end * z), new Vector3(hw * 2f, t, t), white);
                // sides
                for (int s = -1; s <= 1; s += 2)
                {
                    for (float z = 0f; z <= depth + 0.01f; z += step) kit.Box(new Vector3(s * hw, h * 0.5f, z0 + end * z), new Vector3(t, h, t), white);
                    for (float y = 0f; y <= h + 0.01f; y += step) kit.Box(new Vector3(s * hw, y, (z0 + z1) * 0.5f), new Vector3(t, t, depth), white);
                }
            }
        }

        // ---------------------------------------------------------------- grass stripes ----
        public static void GrassStripes()
        {
            var pitch = GameObject.Find("Pitch");
            var r = pitch != null ? pitch.GetComponent<Renderer>() : null;
            if (r == null) return;
            const int w = 8, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "GrassStripes" };
            var px = new Color32[w * h];
            var rng = new System.Random(7);
            for (int y = 0; y < h; y++)
            {
                // sixteen mown bands along the pitch, alternately lighter and darker
                bool light = ((y * 16) / h) % 2 == 0;
                float baseV = light ? 1.0f : 0.84f;
                for (int x = 0; x < w; x++)
                {
                    float n = 1f + ((float)rng.NextDouble() - 0.5f) * 0.05f;
                    byte v = (byte)Mathf.Clamp(baseV * n * 255f, 0f, 255f);
                    px[y * w + x] = new Color32(v, v, v, 255);
                }
            }
            tex.SetPixels32(px); tex.Apply(true, true);
            var mat = r.material; // an instance: the asset stays as it is
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
        }

        // ---------------------------------------------------------------- ball ----
        public static void BallPanels()
        {
            var ball = Object.FindFirstObjectByType<BallController>();
            var r = ball != null ? ball.GetComponentInChildren<Renderer>() : null;
            if (r == null) return;
            const int w = 256, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, name = "BallPanels" };
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(245, 245, 245, 255);
            // twelve dark pentagon patches at the corners of an icosahedron
            float phi = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var dirs = new List<Vector3>();
            foreach (float a in new[] { -1f, 1f })
                foreach (float b in new[] { -phi, phi })
                {
                    dirs.Add(new Vector3(0f, a, b).normalized); dirs.Add(new Vector3(a, b, 0f).normalized); dirs.Add(new Vector3(b, 0f, a).normalized);
                }
            foreach (var d in dirs)
            {
                float lon = Mathf.Atan2(d.z, d.x), lat = Mathf.Asin(d.y);
                int cx = Mathf.RoundToInt((lon / (2f * Mathf.PI) + 0.5f) * w), cy = Mathf.RoundToInt((lat / Mathf.PI + 0.5f) * h);
                float rad = 0.34f; // angular radius in radians
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float lo = ((float)x / w - 0.5f) * 2f * Mathf.PI, la = ((float)y / h - 0.5f) * Mathf.PI;
                        Vector3 p = new Vector3(Mathf.Cos(la) * Mathf.Cos(lo), Mathf.Sin(la), Mathf.Cos(la) * Mathf.Sin(lo));
                        if (Vector3.Angle(p, d) * Mathf.Deg2Rad < rad) px[y * w + x] = new Color32(28, 28, 32, 255);
                    }
            }
            tex.SetPixels32(px); tex.Apply(true, true);
            var mat = r.material;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            mat.color = Color.white;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        }

        // ---------------------------------------------------------------- colour grade ----
        static GameObject fxObject;

        /// <summary>A gentle film-like grade: tone mapping, a touch of contrast and colour, a soft vignette and (on the stronger quality level) a little bloom.</summary>
        public static void ColourGrade(bool bloom)
        {
            if (fxObject != null) return;
            var cam = Camera.main;
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data == null) return;
            data.renderPostProcessing = true;
            fxObject = new GameObject("ColourGrade");
            var vol = fxObject.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 5f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = profile;
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.contrast.Override(10f); ca.saturation.Override(8f); ca.postExposure.Override(0.1f);
            var vg = profile.Add<Vignette>(true); vg.intensity.Override(0.22f); vg.smoothness.Override(0.5f);
            if (bloom) { var b = profile.Add<Bloom>(true); b.threshold.Override(1.05f); b.intensity.Override(0.3f); b.scatter.Override(0.6f); }
        }

        public static void Cleanup()
        {
            if (fxObject != null) Object.Destroy(fxObject);
            fxObject = null;
        }
    }
}
