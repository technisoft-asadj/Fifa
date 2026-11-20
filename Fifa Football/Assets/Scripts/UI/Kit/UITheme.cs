using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.UI
{
    public enum UIIcon { Ball, Trophy, People, Shirt, Gear, Coin, Gem, Gift, Back, Home, Check, Person, Globe, Play, Close,
        Bolt, Clock, Bars, Music, Monitor, Gamepad, Camera, Info, Speaker, Mic, Phone, Crown, Star, Chevron, ArrowLeft, Refresh, Plus, Shield, Join, Whistle }

    /// <summary>
    /// The look shared by every screen: palette, plus sprites generated in code (rounded rectangle with a baked highlight, circle, vertical
    /// gradient and simple icons), so the menus need no art files, nothing is downloaded and the sprites are created once and cached.
    /// Dark navy and electric blue glass, gold for the main action and rewards.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color NavyDeep = new Color(0.02f, 0.05f, 0.12f, 1f);
        public static readonly Color Navy = new Color(0.04f, 0.10f, 0.22f, 1f);
        public static readonly Color Glass = new Color(0.05f, 0.12f, 0.27f, 0.92f);
        public static readonly Color Blue = new Color(0.16f, 0.50f, 1f, 1f);
        public static readonly Color BlueDark = new Color(0.10f, 0.28f, 0.62f, 1f);
        public static readonly Color Gold = new Color(1f, 0.78f, 0.12f, 1f);
        public static readonly Color GoldText = new Color(0.25f, 0.14f, 0f, 1f);
        public static readonly Color Purple = new Color(0.52f, 0.22f, 0.88f, 1f);
        public static readonly Color Green = new Color(0.20f, 0.72f, 0.24f, 1f);
        public static readonly Color Slate = new Color(0.28f, 0.34f, 0.46f, 1f);
        public static readonly Color Red = new Color(0.85f, 0.18f, 0.2f, 1f);
        public static readonly Color Muted = new Color(1f, 1f, 1f, 0.7f);

        static TMPro.TMP_FontAsset gameFont; static bool fontTried;

        /// <summary>The menu typeface (Kenney Future Narrow, CC0, from ThirdParty/Kenney), built as a dynamic TextMeshPro font with the default font as fallback. Null if the file is missing.</summary>
        public static TMPro.TMP_FontAsset Font
        {
            get
            {
                if (fontTried) return gameFont;
                fontTried = true;
                try
                {
                    var f = Resources.Load<Font>("KenneyFutureNarrow");
                    if (f != null)
                    {
                        gameFont = TMPro.TMP_FontAsset.CreateFontAsset(f, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, TMPro.AtlasPopulationMode.Dynamic, true);
                        try { if (gameFont != null && TMPro.TMP_Settings.defaultFontAsset != null) { if (gameFont.fallbackFontAssetTable == null) gameFont.fallbackFontAssetTable = new List<TMPro.TMP_FontAsset>(); gameFont.fallbackFontAssetTable.Add(TMPro.TMP_Settings.defaultFontAsset); } } catch (System.Exception e) { Debug.LogWarning("Menu font fallback: " + e.Message); }
                    }
                }
                catch (System.Exception e) { Debug.LogWarning("Menu font: " + e.Message); }
                return gameFont;
            }
        }

        static readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();

        /// <summary>A picture from Resources/UI (the logo, the ball, the hero player, the club crests), or null when it is missing.</summary>
        public static Sprite Art(string path)
        {
            if (art.TryGetValue(path, out var s) && s != null) return s;
            var tex = Resources.Load<Texture2D>("UI/" + path);
            s = tex == null ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            art[path] = s;
            return s;
        }

        /// <summary>The crest picture for a club index (null for the plain team or a missing file).</summary>
        public static Sprite ClubCrest(int club)
        {
            if (club < 0) return null;
            return Art("Crests/crest_" + club.ToString("00"));
        }

        const int Size = 64;
        static Sprite rounded, circle, gradient;
        static readonly Dictionary<UIIcon, Sprite> icons = new Dictionary<UIIcon, Sprite>();

        /// <summary>White rounded rectangle, lighter at the top with a bright rim; tint it with <c>Image.color</c>. Use it as a Sliced image.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded == null)
                    rounded = Make("UI_Rounded", (u, v) =>
                    {
                        float d = RoundedBoxDistance(u, v, 1f, 0.34f); // inside < 0
                        float a = Mathf.Clamp01(0.5f - d * Size * 0.5f);
                        float shade = 0.80f + 0.20f * ((v + 1f) * 0.5f); // v up: lighter at the top
                        float rim = Mathf.Clamp01(1f - (-d) * Size * 0.5f / 2.5f); // within ~2.5 px of the edge
                        shade = Mathf.Min(1.15f, shade + 0.18f * rim * rim);
                        return new Color(shade, shade, shade, a);
                    }, new Vector4(22, 22, 22, 22));
                return rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                    circle = Make("UI_Circle", (u, v) =>
                    {
                        float r = Mathf.Sqrt(u * u + v * v);
                        float a = Mathf.Clamp01((1f - r) * Size * 0.5f);
                        float shade = 0.85f + 0.15f * ((v + 1f) * 0.5f);
                        return new Color(shade, shade, shade, a);
                    }, Vector4.zero);
                return circle;
            }
        }

        /// <summary>Opaque white; stretch it over the screen and tint it, or use <see cref="Gradient"/>.</summary>
        public static Sprite Gradient
        {
            get
            {
                if (gradient == null)
                {
                    var tex = new Texture2D(4, 64, TextureFormat.RGBA32, false) { name = "UI_Gradient", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                    for (int y = 0; y < 64; y++)
                    {
                        float t = y / 63f; // 0 bottom, 1 top
                        var c = Color.Lerp(new Color(0.12f, 0.36f, 0.85f, 0.55f), new Color(0.01f, 0.03f, 0.09f, 0.92f), t);
                        for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
                    }
                    tex.Apply(false, true);
                    gradient = Sprite.Create(tex, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f), 100f);
                }
                return gradient;
            }
        }

        public static Sprite Icon(UIIcon kind)
        {
            if (icons.TryGetValue(kind, out var s) && s != null) return s;
            s = Make("UI_Icon_" + kind, (u, v) => IconCoverage(kind, u, v), Vector4.zero, 128);
            icons[kind] = s;
            return s;
        }

        // ---- generation ----

        static Sprite Make(string name, System.Func<float, float, Color> pixel, Vector4 border, int size = Size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, pixel((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f));
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>Signed distance (in the -1..1 square) to a rounded box of half-size <paramref name="half"/> and corner radius <paramref name="radius"/>.</summary>
        static float RoundedBoxDistance(float u, float v, float half, float radius)
        {
            float qx = Mathf.Abs(u) - (half - radius), qy = Mathf.Abs(v) - (half - radius);
            float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }


        static float DistToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay, t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy));
            float qx = ax + t * dx - px, qy = ay + t * dy - py;
            return Mathf.Sqrt(qx * qx + qy * qy);
        }

        /// <summary>True if (u, v) is inside a regular pentagon of circumradius <paramref name="radius"/> centred at (cx, cy); <paramref name="rotation"/> is the angle (radians, clockwise from up) of one corner.</summary>
        static bool InPentagon(float u, float v, float cx, float cy, float radius, float rotation)
        {
            float dx = u - cx, dy = v - cy, rr = Mathf.Sqrt(dx * dx + dy * dy);
            if (rr < 1e-5f) return true;
            float ang = Mathf.Atan2(dx, dy) - rotation; // clockwise from up
            float sector = Mathf.PI * 0.4f;
            float m = Mathf.Repeat(ang + sector * 0.5f, sector) - sector * 0.5f; // -36..36 degrees from the nearest corner
            // the nearest edge's normal is 36 degrees minus |m| away from the point's direction; inside means closer than the edge along it
            return rr * Mathf.Cos(Mathf.PI * 0.2f - Mathf.Abs(m)) < radius * Mathf.Cos(Mathf.PI * 0.2f);
        }

        static Color Solid(bool on, float soft = 0f) { return new Color(1f, 1f, 1f, on ? 1f : 0f); }

        /// <summary>White-on-transparent coverage of a simple pictogram at (u, v) in -1..1, v up. Anti-aliased by the texture filter.</summary>
        static Color IconCoverage(UIIcon kind, float u, float v)
        {
            bool on = false;
            float r = Mathf.Sqrt(u * u + v * v);
            switch (kind)
            {
                case UIIcon.Ball:
                {
                    if (r > 0.92f) return new Color(1f, 1f, 1f, Mathf.Clamp01((0.95f - r) * 40f));
                    var dark = new Color(0.1f, 0.1f, 0.12f, 1f);
                    if (InPentagon(u, v, 0f, 0f, 0.3f, 0f)) return dark;
                    for (int k = 0; k < 5; k++)
                    {
                        float a = k * Mathf.PI * 0.4f;
                        float cx = Mathf.Sin(a) * 0.82f, cy = Mathf.Cos(a) * 0.82f;
                        if (InPentagon(u, v, cx, cy, 0.24f, a + Mathf.PI)) return dark;
                        // seam from the centre patch's corner out to the next patch
                        float sx = Mathf.Sin(a) * 0.3f, sy = Mathf.Cos(a) * 0.3f, ex = Mathf.Sin(a) * 0.6f, ey = Mathf.Cos(a) * 0.6f;
                        if (DistToSegment(u, v, sx, sy, ex, ey) < 0.035f) return dark;
                    }
                    if (r > 0.86f) return new Color(0.75f, 0.77f, 0.8f, 1f); // rim shading
                    return new Color(1f, 1f, 1f, 1f);
                }
                case UIIcon.Trophy:
                {
                    bool cup = v > -0.15f && v < 0.8f && Mathf.Abs(u) < 0.5f - (0.8f - v) * 0.08f - Mathf.Max(0f, (0.1f - v)) * 0.9f;
                    bool stem = Mathf.Abs(u) < 0.09f && v > -0.55f && v <= -0.1f;
                    bool baseP = Mathf.Abs(u) < 0.4f && v > -0.85f && v <= -0.55f;
                    float hr = Mathf.Sqrt((Mathf.Abs(u) - 0.66f) * (Mathf.Abs(u) - 0.66f) + (v - 0.42f) * (v - 0.42f));
                    bool handle = hr < 0.24f && hr > 0.12f && Mathf.Abs(u) > 0.48f;
                    on = cup || stem || baseP || handle;
                    break;
                }
                case UIIcon.People:
                {
                    bool Head(float cx, float cy, float rad) { return (u - cx) * (u - cx) + (v - cy) * (v - cy) < rad * rad; }
                    bool Body(float cx, float cy, float rx, float ry) { float a = (u - cx) / rx, b = (v - cy) / ry; return a * a + b * b < 1f && v < cy + ry * 0.2f; }
                    // three people: one in front, two behind him
                    on = Head(-0.62f, 0.3f, 0.2f) || Body(-0.62f, -0.4f, 0.36f, 0.5f)
                       || Head(0.62f, 0.3f, 0.2f) || Body(0.62f, -0.4f, 0.36f, 0.5f)
                       || Head(0f, 0.42f, 0.27f) || Body(0f, -0.5f, 0.5f, 0.55f);
                    break;
                }
                case UIIcon.Globe:
                {
                    // a globe: outer ring, equator, two latitude lines, centre meridian and a curved meridian
                    float e = Mathf.Sqrt((u / 0.45f) * (u / 0.45f) + v * v);
                    bool ring = r < 0.92f && r > 0.8f;
                    bool inside = r <= 0.86f;
                    bool lines = Mathf.Abs(v) < 0.06f || Mathf.Abs(Mathf.Abs(v) - 0.5f) < 0.05f || Mathf.Abs(u) < 0.06f || Mathf.Abs(e - 0.9f) < 0.07f;
                    on = ring || (inside && lines);
                    break;
                }
                case UIIcon.Person:
                {
                    bool head = u * u + (v - 0.34f) * (v - 0.34f) < 0.3f * 0.3f;
                    float a = u / 0.78f, b = (v + 0.72f) / 0.62f;
                    on = head || (a * a + b * b < 1f && v < -0.1f);
                    break;
                }
                case UIIcon.Shirt:
                {
                    bool body = Mathf.Abs(u) < 0.44f && v > -0.85f && v < 0.62f;
                    bool sleeve = Mathf.Abs(u) >= 0.4f && Mathf.Abs(u) < 0.92f && v > 0.05f && v < 0.64f && v < 0.64f - (Mathf.Abs(u) - 0.4f) * 0.55f + 0.2f && v > 0.64f - 0.55f - (Mathf.Abs(u) - 0.4f) * 0.0f;
                    bool shoulder = Mathf.Abs(u) < 0.92f && v >= 0.38f && v < 0.64f && v < 0.64f - (Mathf.Abs(u) - 0.44f) * 0.5f;
                    bool collar = u * u + (v - 0.64f) * (v - 0.64f) < 0.2f * 0.2f;
                    on = (body || sleeve || shoulder) && !collar;
                    break;
                }
                case UIIcon.Gear:
                {
                    float ang = Mathf.Atan2(v, u);
                    float tooth = Mathf.Cos(ang * 8f) > 0.15f ? 0.92f : 0.74f;
                    on = r < tooth && r > 0.3f;
                    break;
                }
                case UIIcon.Coin:
                    on = r < 0.9f && !(r > 0.62f && r < 0.7f) && !(Mathf.Abs(u) < 0.07f && Mathf.Abs(v) < 0.4f) ;
                    break;
                case UIIcon.Gem:
                {
                    float a = Mathf.Abs(u);
                    on = (v >= 0.15f && v < 0.7f && a < 0.8f - (0.7f - v) * 0.0f - (v - 0.15f) * 0.45f)
                      || (v < 0.15f && v > -0.85f && a < 0.8f * (v + 0.85f) / 1.0f);
                    break;
                }
                case UIIcon.Gift:
                {
                    bool box = Mathf.Abs(u) < 0.75f && v > -0.8f && v < 0.25f;
                    bool lid = Mathf.Abs(u) < 0.85f && v >= 0.25f && v < 0.5f;
                    bool ribbonV = Mathf.Abs(u) < 0.12f && v > -0.8f && v < 0.5f;
                    bool bow = ((Mathf.Abs(u) - 0.26f) * (Mathf.Abs(u) - 0.26f) + (v - 0.62f) * (v - 0.62f) < 0.2f * 0.2f);
                    on = (box || lid || bow) && !(ribbonV && false);
                    if (ribbonV) return new Color(1f, 1f, 1f, 0.5f);
                    break;
                }
                case UIIcon.Back:
                {
                    float d = Mathf.Abs(Mathf.Abs(v) * 1.0f - (u + 0.5f) * 1.0f) ;
                    on = u > -0.55f && u < 0.35f && v > -0.65f && v < 0.65f && Mathf.Abs(Mathf.Abs(v) - (0.65f - (u + 0.55f) * 0.72f)) < 0.17f;
                    break;
                }
                case UIIcon.Home:
                {
                    bool roof = v > 0.05f && v < 0.85f && Mathf.Abs(u) < (0.85f - v) * 1.05f;
                    bool house = Mathf.Abs(u) < 0.55f && v > -0.8f && v <= 0.05f && !(Mathf.Abs(u) < 0.15f && v < -0.25f);
                    on = roof || house;
                    break;
                }
                case UIIcon.Play:
                    // a right-pointing triangle
                    on = u > -0.5f && u < 0.7f && Mathf.Abs(v) < 0.75f * (0.7f - u) / 1.2f;
                    break;
                case UIIcon.Close:
                    // a cross of two diagonal bars
                    on = r < 0.85f && (Mathf.Abs(u - v) < 0.2f || Mathf.Abs(u + v) < 0.2f);
                    break;
                case UIIcon.Check:
                    on = (Mathf.Abs((v + 0.1f) + (u + 0.45f) * 1.0f) < 0.15f && u < -0.2f && u > -0.7f)
                      || (Mathf.Abs((v + 0.55f) - (u + 0.2f) * 1.1f) < 0.15f && u > -0.2f && u < 0.8f);
                    break;
                case UIIcon.Bolt:
                    on = InPoly(u, v, 0.18f, 0.95f, -0.5f, -0.1f, -0.04f, -0.1f, -0.2f, -0.95f, 0.5f, 0.12f, 0.04f, 0.12f);
                    break;
                case UIIcon.Clock:
                    on = (r < 0.92f && r > 0.76f) || DistToSegment(u, v, 0f, 0f, 0f, 0.55f) < 0.075f || DistToSegment(u, v, 0f, 0f, 0.4f, -0.1f) < 0.075f;
                    break;
                case UIIcon.Bars:
                    on = (u > -0.8f && u < -0.45f && v > -0.8f && v < -0.15f) || (u > -0.17f && u < 0.18f && v > -0.8f && v < 0.3f) || (u > 0.45f && u < 0.8f && v > -0.8f && v < 0.8f);
                    break;
                case UIIcon.Music:
                {
                    float hx = (u + 0.3f) / 0.34f, hy = (v + 0.55f) / 0.26f;
                    bool head = hx * hx + hy * hy < 1f;
                    bool stem = u > -0.08f && u < 0.03f && v > -0.55f && v < 0.8f;
                    bool flag = InPoly(u, v, 0.03f, 0.8f, 0.55f, 0.45f, 0.5f, 0.0f, 0.03f, 0.3f);
                    on = head || stem || flag;
                    break;
                }
                case UIIcon.Monitor:
                {
                    bool frame = Mathf.Abs(u) < 0.88f && v > -0.5f && v < 0.7f && !(Mathf.Abs(u) < 0.74f && v > -0.38f && v < 0.58f);
                    bool stand = Mathf.Abs(u) < 0.09f && v > -0.72f && v <= -0.5f;
                    bool baseP = Mathf.Abs(u) < 0.42f && v > -0.86f && v <= -0.72f;
                    on = frame || stand || baseP;
                    break;
                }
                case UIIcon.Gamepad:
                {
                    float bx = u / 0.92f, by = (v + 0.05f) / 0.55f;
                    bool body = bx * bx + by * by < 1f;
                    bool cross = (Mathf.Abs(u + 0.42f) < 0.11f && Mathf.Abs(v + 0.05f) < 0.32f) || (Mathf.Abs(u + 0.42f) < 0.32f && Mathf.Abs(v + 0.05f) < 0.11f);
                    bool b1 = (u - 0.35f) * (u - 0.35f) + (v + 0.2f) * (v + 0.2f) < 0.012f + 0.02f, b2 = (u - 0.58f) * (u - 0.58f) + (v - 0.05f) * (v - 0.05f) < 0.032f;
                    on = body && !cross && !b1 && !b2;
                    break;
                }
                case UIIcon.Camera:
                {
                    bool body = Mathf.Abs(u) < 0.88f && v > -0.55f && v < 0.5f;
                    bool bump = Mathf.Abs(u) < 0.32f && v >= 0.5f && v < 0.72f;
                    float lr = Mathf.Sqrt(u * u + (v + 0.02f) * (v + 0.02f));
                    bool ring = lr > 0.3f && lr < 0.48f; bool lens = lr < 0.2f;
                    on = ((body || bump) && !ring) || lens;
                    break;
                }
                case UIIcon.Info:
                {
                    bool dot = u * u + (v - 0.45f) * (v - 0.45f) < 0.11f * 0.11f;
                    bool bar = Mathf.Abs(u) < 0.09f && v > -0.55f && v < 0.2f;
                    on = r < 0.92f && !dot && !bar;
                    break;
                }
                case UIIcon.Speaker:
                {
                    bool box = u > -0.85f && u < -0.45f && Mathf.Abs(v) < 0.3f;
                    bool cone = InPoly(u, v, -0.45f, 0.3f, 0.05f, 0.75f, 0.05f, -0.75f, -0.45f, -0.3f);
                    float wr = Mathf.Sqrt((u - 0.05f) * (u - 0.05f) + v * v), wa = Mathf.Abs(Mathf.Atan2(v, u - 0.05f));
                    bool waves = wa < 0.75f && ((wr > 0.4f && wr < 0.52f) || (wr > 0.72f && wr < 0.84f));
                    on = box || cone || waves;
                    break;
                }
                case UIIcon.Mic:
                {
                    bool capsule = Mathf.Abs(u) < 0.26f && v > -0.1f && v < 0.78f || u * u + (v + 0.1f) * (v + 0.1f) < 0.26f * 0.26f || u * u + (v - 0.78f) * (v - 0.78f) < 0.26f * 0.26f;
                    float ar = Mathf.Sqrt(u * u + (v + 0.1f) * (v + 0.1f));
                    bool cradle = ar > 0.43f && ar < 0.53f && v < -0.1f;
                    bool stem = Mathf.Abs(u) < 0.05f && v > -0.9f && v < -0.55f;
                    bool bs = Mathf.Abs(u) < 0.3f && v > -0.95f && v < -0.85f;
                    on = capsule || cradle || stem || bs;
                    break;
                }
                case UIIcon.Phone:
                {
                    bool frame = Mathf.Abs(u) < 0.42f && Mathf.Abs(v) < 0.88f && !(Mathf.Abs(u) < 0.32f && v > -0.7f && v < 0.7f);
                    bool dot = u * u + (v + 0.79f) * (v + 0.79f) < 0.012f;
                    bool waves = (Mathf.Abs(u) > 0.58f && Mathf.Abs(u) < 0.68f && Mathf.Abs(v) < 0.45f) || (Mathf.Abs(u) > 0.8f && Mathf.Abs(u) < 0.9f && Mathf.Abs(v) < 0.3f);
                    on = frame || dot || waves;
                    break;
                }
                case UIIcon.Crown:
                    on = InPoly(u, v, -0.88f, -0.55f, -0.88f, 0.45f, -0.4f, -0.05f, 0f, 0.65f, 0.4f, -0.05f, 0.88f, 0.45f, 0.88f, -0.55f);
                    break;
                case UIIcon.Star:
                {
                    float[] p = new float[20];
                    for (int k = 0; k < 10; k++) { float ang = Mathf.PI * 0.5f + k * Mathf.PI * 0.2f, rad = (k % 2 == 0) ? 0.95f : 0.4f; p[k * 2] = Mathf.Cos(ang) * rad; p[k * 2 + 1] = Mathf.Sin(ang) * rad; }
                    on = InPoly(u, v, p);
                    break;
                }
                case UIIcon.Chevron:
                    on = DistToSegment(u, v, -0.3f, 0.72f, 0.42f, 0f) < 0.15f || DistToSegment(u, v, -0.3f, -0.72f, 0.42f, 0f) < 0.15f;
                    break;
                case UIIcon.ArrowLeft:
                    on = DistToSegment(u, v, -0.62f, 0f, 0.7f, 0f) < 0.12f || DistToSegment(u, v, -0.62f, 0f, -0.05f, 0.58f) < 0.12f || DistToSegment(u, v, -0.62f, 0f, -0.05f, -0.58f) < 0.12f;
                    break;
                case UIIcon.Refresh:
                {
                    float ang = Mathf.Atan2(v, u);
                    bool ring = r > 0.5f && r < 0.72f && !(ang > 0.15f && ang < 1.1f);
                    bool head = InPoly(u, v, 0.12f, 0.95f, 0.12f, 0.2f, 0.78f, 0.52f);
                    on = ring || head;
                    break;
                }
                case UIIcon.Plus:
                    on = (Mathf.Abs(u) < 0.14f && Mathf.Abs(v) < 0.75f) || (Mathf.Abs(v) < 0.14f && Mathf.Abs(u) < 0.75f);
                    break;
                case UIIcon.Shield:
                    on = InPoly(u, v, -0.78f, 0.9f, 0.78f, 0.9f, 0.78f, 0.08f, 0.5f, -0.5f, 0f, -0.96f, -0.5f, -0.5f, -0.78f, 0.08f);
                    break;
                case UIIcon.Join:
                {
                    bool frame = (u > 0.18f && u < 0.86f && v > -0.8f && v < 0.8f) && !(u > 0.3f && u < 0.74f && v > -0.68f && v < 0.68f) || (u > 0.18f && u < 0.86f && Mathf.Abs(v) < 0.0f);
                    bool arrow = DistToSegment(u, v, -0.85f, 0f, 0.35f, 0f) < 0.1f || DistToSegment(u, v, 0.35f, 0f, -0.05f, 0.4f) < 0.1f || DistToSegment(u, v, 0.35f, 0f, -0.05f, -0.4f) < 0.1f;
                    on = frame || arrow;
                    break;
                }
                case UIIcon.Whistle:
                    on = (u + 0.1f) * (u + 0.1f) + v * v < 0.6f * 0.6f && !((u + 0.1f) * (u + 0.1f) + v * v < 0.18f * 0.18f) || (u > 0.3f && u < 0.9f && v > -0.1f && v < 0.35f);
                    break;
            }
            return Solid(on);
        }

        /// <summary>Point-in-polygon for a flat list of x,y pairs (even-odd rule).</summary>
        static bool InPoly(float u, float v, params float[] pts)
        {
            bool inside = false;
            int n = pts.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = pts[i * 2], yi = pts[i * 2 + 1], xj = pts[j * 2], yj = pts[j * 2 + 1];
                if (((yi > v) != (yj > v)) && (u < (xj - xi) * (v - yi) / (yj - yi + 1e-6f) + xi)) inside = !inside;
            }
            return inside;
        }
    }
}
