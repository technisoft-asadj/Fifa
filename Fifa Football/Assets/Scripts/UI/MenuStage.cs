using UnityEngine;
using UnityEngine.UI;

namespace FootballSim.UI
{
    /// <summary>
    /// A small real 3D scene for the main menu: one large panelled football hovering and slowly spinning in a real spotlight, with a
    /// visible light beam, a glowing pool of light on a dark plinth, a soft contact shadow and a few gold sparkles drifting around it.
    /// A second camera draws it into a transparent RenderTexture that the menu shows as a picture, so it sits between the stadium and
    /// the buttons. Everything is built from primitives and generated textures; nothing is downloaded. The scene is far from the pitch
    /// (and lit by short-range lights) so it never touches the match.
    /// </summary>
    public sealed class MenuStage : MonoBehaviour
    {
        static readonly Vector3 Origin = new Vector3(0f, 4000f, 0f);
        const float BallY = 1.65f, BallSize = 3.3f, FloorY = -0.3f, SpotIntensity = 105f;
        Camera cam;
        RenderTexture rt;
        Transform ball, ring, shadow, pool, beam;
        Transform[] sparkles;
        float[] sparkPhase;
        Light spot;

        public RenderTexture Texture => rt;

        /// <summary>Builds the stage and returns the RawImage showing it (under <paramref name="parent"/>).</summary>
        public static RawImage Create(Transform parent, out MenuStage stage)
        {
            var go = new GameObject("MenuStage");
            stage = go.AddComponent<MenuStage>();
            stage.Build();
            var view = new GameObject("StageView", typeof(RectTransform), typeof(RawImage), typeof(StageSwitch));
            view.transform.SetParent(parent, false);
            var raw = view.GetComponent<RawImage>();
            raw.texture = stage.rt; raw.raycastTarget = false;
            view.GetComponent<StageSwitch>().stage = stage;
            stage.SetShown(view.activeInHierarchy);
            return raw;
        }

        /// <summary>Turns the stage's camera on and off with the menu (it costs nothing while hidden).</summary>
        sealed class StageSwitch : MonoBehaviour
        {
            public MenuStage stage;
            void OnEnable() { if (stage != null) stage.SetShown(true); }
            void OnDisable() { if (stage != null) stage.SetShown(false); }
        }

        public void SetShown(bool on) { if (cam != null) cam.enabled = on; enabled = on; }

        static Material Mat(Color c, float metallic, float smooth, Color emission)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c); m.color = c;
            m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smooth);
            if (emission.maxColorComponent > 0.01f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission); }
            return m;
        }

        /// <summary>A flat, alpha-blended, vertex- and texture-coloured material for the light beam, the pool of light and the shadow.</summary>
        static Material Glow(Texture tex, Color tint)
        {
            var m = new Material(Shader.Find("Sprites/Default")) { color = tint };
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        Transform Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            var o = GameObject.CreatePrimitive(t);
            o.name = name; Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localScale = scale;
            o.GetComponent<Renderer>().sharedMaterial = m;
            return o.transform;
        }

        /// <summary>A panelled ball: soft black pentagons and fine grey seams, drawn from the twelve corner directions of an icosahedron.</summary>
        const float PrintYaw = 90f; // ball yaw at which the print faces the camera

        static Texture2D BallTexture()
        {
            const int w = 1024, h = 512;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4, name = "MenuBall" };
            var px = new Color32[w * h];
            float phi = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var dirs = new System.Collections.Generic.List<Vector3>();
            foreach (float a in new[] { -1f, 1f })
                foreach (float b in new[] { -phi, phi })
                { dirs.Add(new Vector3(0f, a, b).normalized); dirs.Add(new Vector3(a, b, 0f).normalized); dirs.Add(new Vector3(b, 0f, a).normalized); }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float lo = ((float)x / w - 0.5f) * 2f * Mathf.PI, la = ((float)y / h - 0.5f) * Mathf.PI;
                    var p = new Vector3(Mathf.Cos(la) * Mathf.Cos(lo), Mathf.Sin(la), Mathf.Cos(la) * Mathf.Sin(lo));
                    float d1 = -2f, d2 = -2f;
                    for (int i = 0; i < dirs.Count; i++) { float d = Vector3.Dot(p, dirs[i]); if (d > d1) { d2 = d1; d1 = d; } else if (d > d2) d2 = d; }
                    float a1 = Mathf.Acos(Mathf.Clamp(d1, -1f, 1f)) * Mathf.Rad2Deg, a2 = Mathf.Acos(Mathf.Clamp(d2, -1f, 1f)) * Mathf.Rad2Deg;
                    Color c = new Color(0.96f, 0.96f, 0.98f);
                    // slightly shaded white panels: darker towards the seams
                    c *= Mathf.Lerp(0.9f, 1f, Smooth(0f, 14f, a2 - a1));
                    float seam = 1f - Smooth(0f, 2.2f, a2 - a1);                        // thin grey line between neighbouring panels
                    c = Color.Lerp(c, new Color(0.45f, 0.5f, 0.6f), seam * 0.8f);
                    float black = Smooth(37f, 40.5f, a2 - a1);                          // soft-edged black pentagon (the contour of the panel's own pentagon shape)
                    c = Color.Lerp(c, new Color(0.05f, 0.06f, 0.1f), black);
                    px[y * w + x] = c;
                }
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }

        /// <summary>0 below <paramref name="a"/>, 1 above <paramref name="b"/>, smooth in between (like GLSL smoothstep).</summary>
        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        /// <summary>A white disc that fades out smoothly towards its edge (alpha only); tinted by the material.</summary>
        static Texture2D RadialTexture(float power)
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "MenuRadial" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float r = Mathf.Sqrt(Mathf.Pow((x + 0.5f) / n - 0.5f, 2f) + Mathf.Pow((y + 0.5f) / n - 0.5f, 2f)) * 2f;
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - r), power));
                }
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }

        /// <summary>The visible cone of light: wide and faint at the floor, narrow and brighter at the lamp, double-sided, alpha in the vertex colours.</summary>
        static Mesh BeamMesh(float topY, float topR, float bottomY, float bottomR)
        {
            const int seg = 48;
            var v = new Vector3[(seg + 1) * 2]; var col = new Color[v.Length]; var tri = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f, c = Mathf.Cos(a), s = Mathf.Sin(a);
                v[i * 2] = new Vector3(c * topR, topY, s * topR); col[i * 2] = new Color(1f, 0.95f, 0.8f, 0.15f);
                v[i * 2 + 1] = new Vector3(c * bottomR, bottomY, s * bottomR); col[i * 2 + 1] = new Color(1f, 0.95f, 0.8f, 0.02f);
            }
            for (int i = 0; i < seg; i++)
            {
                int k = i * 2, t = i * 6;
                tri[t] = k; tri[t + 1] = k + 2; tri[t + 2] = k + 1; tri[t + 3] = k + 1; tri[t + 4] = k + 2; tri[t + 5] = k + 3;
            }
            var m = new Mesh { name = "MenuBeam", vertices = v, colors = col, triangles = tri };
            m.RecalculateBounds(); return m;
        }

        void Build()
        {
            transform.position = Origin;
            rt = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "MenuStageRT" };
            var camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.75f, -13.5f);
            camGo.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 32f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 40f;
            cam.targetTexture = rt; cam.allowHDR = false; cam.enabled = false;
            var data = camGo.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() ?? camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;

            // lights: the spotlight from above (the star of the scene), a cool rim from behind-right and a faint warm fill from the left
            var sp = new GameObject("Spotlight").AddComponent<Light>();
            sp.transform.SetParent(transform, false); sp.transform.localPosition = new Vector3(0.4f, 8.5f, -2.2f);
            sp.transform.LookAt(transform.position + new Vector3(0f, BallY, 0f));
            sp.type = LightType.Spot; sp.spotAngle = 80f; sp.innerSpotAngle = 45f; sp.range = 18f;   // wide enough that its edge never cuts across the ball
            sp.color = new Color(1f, 0.95f, 0.82f); sp.intensity = SpotIntensity; sp.shadows = LightShadows.None; spot = sp;
            AddLamp(new Vector3(5f, 3f, 3f), new Color(0.35f, 0.62f, 1f), 38f, 16f);
            AddLamp(new Vector3(-5f, 1.5f, -4f), new Color(1f, 0.8f, 0.55f), 14f, 14f);

            var steel = Mat(new Color(0.04f, 0.07f, 0.18f), 0.7f, 0.92f, Color.black);
            var glowMat = Mat(new Color(0.2f, 0.55f, 1f), 0f, 0.5f, new Color(0.3f, 1.1f, 2.2f));

            // plinth: two dark discs and a glowing blue ring
            Prim(PrimitiveType.Cylinder, "PlinthLow", transform, new Vector3(0f, FloorY - 0.26f, 0f), new Vector3(5.4f, 0.12f, 5.4f), steel);
            Prim(PrimitiveType.Cylinder, "PlinthTop", transform, new Vector3(0f, FloorY - 0.1f, 0f), new Vector3(4.4f, 0.1f, 4.4f), steel);
            ring = Prim(PrimitiveType.Cylinder, "Ring", transform, new Vector3(0f, FloorY - 0.04f, 0f), new Vector3(4.6f, 0.02f, 4.6f), glowMat);

            // the pool of light the spotlight makes on the plinth, and the ball's soft shadow in it
            var poolTex = RadialTexture(1.6f); var shadowTex = RadialTexture(1.1f);
            pool = Prim(PrimitiveType.Quad, "Pool", transform, new Vector3(0f, FloorY + 0.012f, 0f), new Vector3(5.2f, 5.2f, 1f), Glow(poolTex, new Color(1f, 0.93f, 0.72f, 0.45f)));
            pool.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow = Prim(PrimitiveType.Quad, "Shadow", transform, new Vector3(0f, FloorY + 0.02f, 0f), new Vector3(2.6f, 2.6f, 1f), Glow(shadowTex, new Color(0f, 0f, 0.04f, 0.7f)));
            shadow.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // the beam of the spotlight, from the lamp down to the pool
            var bm = new GameObject("Beam", typeof(MeshFilter), typeof(MeshRenderer)); bm.transform.SetParent(transform, false);
            bm.GetComponent<MeshFilter>().sharedMesh = BeamMesh(9.5f, 0.45f, FloorY + 0.02f, 2.7f);
            bm.GetComponent<MeshRenderer>().sharedMaterial = Glow(null, Color.white);
            beam = bm.transform;
            bm.transform.localPosition = new Vector3(0.1f, 0f, -0.3f);

            // the ball
            var tex = Resources.Load<Texture2D>("UI/ball");   // panelled ball with the SOCCER FIGHT print (generated, see ThirdParty/ASSETS.md)
            if (tex == null) tex = BallTexture();
            else { tex.wrapMode = TextureWrapMode.Repeat; tex.anisoLevel = 4; }
            var ballMat = Mat(Color.white, 0f, 0.78f, new Color(0.03f, 0.03f, 0.035f)); ballMat.SetTexture("_BaseMap", tex); ballMat.mainTexture = tex;
            ball = Prim(PrimitiveType.Sphere, "Ball", transform, new Vector3(0f, BallY, 0f), Vector3.one * BallSize, ballMat);

            // sparkles drifting around the ball
            sparkles = new Transform[14]; sparkPhase = new float[14];
            var spark = Mat(new Color(1f, 0.9f, 0.5f), 0f, 0.5f, new Color(2.2f, 1.6f, 0.5f));
            for (int i = 0; i < sparkles.Length; i++)
            {
                sparkles[i] = Prim(PrimitiveType.Cube, "Sparkle" + i, transform, Vector3.zero, Vector3.one * Random.Range(0.04f, 0.1f), spark);
                sparkPhase[i] = Random.value * 6.28f;
            }
            Place(0f);
        }

        void AddLamp(Vector3 pos, Color c, float intensity, float range)
        {
            var l = new GameObject("Lamp").AddComponent<Light>();
            l.transform.SetParent(transform, false); l.transform.localPosition = pos;
            l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
        }

        void Place(float time)
        {
            float bob = Mathf.Sin(time * 1.3f) * 0.14f;
            if (ball != null)
            {
                // hold with the SOCCER FIGHT print facing the camera (gentle sway), then one slow full turn
                const float hold = 6.5f, spin = 3.2f;
                float c = time % (hold + spin), yaw = PrintYaw;
                if (c < hold) yaw += Mathf.Sin(time * 0.9f) * 12f * Smooth(0f, 1f, Mathf.Min(c, hold - c));
                else { float s = Mathf.Clamp01((c - hold) / spin); yaw += 360f * (s * s * (3f - 2f * s)); }
                ball.localRotation = Quaternion.Euler(8f, yaw, 0f);
                ball.localPosition = new Vector3(0f, BallY + bob, 0f);
            }
            if (shadow != null)
            {
                float k = 1f - bob * 0.45f; // the shadow shrinks a little as the ball rises
                shadow.localScale = new Vector3(2.6f * k, 2.6f * k, 1f);
            }
            if (ring != null) ring.localScale = new Vector3(4.6f + Mathf.Sin(time * 2f) * 0.08f, 0.02f, 4.6f + Mathf.Sin(time * 2f) * 0.08f);
            if (spot != null) spot.intensity = SpotIntensity + Mathf.Sin(time * 2.3f) * 4f;   // a very gentle shimmer
            for (int i = 0; i < sparkles.Length; i++)
            {
                float a = sparkPhase[i] + time * (0.3f + (i % 4) * 0.09f);
                float r = 2.3f + (i % 5) * 0.3f;
                sparkles[i].localPosition = new Vector3(Mathf.Cos(a) * r, 0.2f + (i * 0.29f) % 3.6f + Mathf.Sin(time * 1.3f + i) * 0.15f, Mathf.Sin(a) * r * 0.5f);
                sparkles[i].localRotation = Quaternion.Euler(time * 90f + i * 20f, time * 130f, 45f);
            }
        }

        void Update() { Place(Time.unscaledTime); }

        void OnDestroy() { if (rt != null) { rt.Release(); Destroy(rt); } }
    }
}
