using UnityEngine;
using UnityEngine.UI;

namespace FootballSim.UI
{
    /// <summary>
    /// A small real 3D scene for the main menu: a glowing pedestal with a golden trophy and a spinning panelled ball, with gold sparkles
    /// drifting around them. A second camera draws it into a transparent RenderTexture that the menu shows as a picture, so it sits
    /// between the stadium and the buttons. Everything is built from primitives; nothing is downloaded. The scene is far from the pitch
    /// (and lit by short-range lamps) so it never touches the match.
    /// </summary>
    public sealed class MenuStage : MonoBehaviour
    {
        static readonly Vector3 Origin = new Vector3(0f, 4000f, 0f);
        Camera cam;
        RenderTexture rt;
        Transform trophy, ball, ring;
        Transform[] sparkles;
        float[] sparkPhase;
        Material goldMat;

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

        Transform Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            var o = GameObject.CreatePrimitive(t);
            o.name = name; Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localScale = scale;
            o.GetComponent<Renderer>().sharedMaterial = m;
            return o.transform;
        }

        static Texture2D BallTexture()
        {
            const int w = 512, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, name = "MenuBall" };
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(246, 246, 250, 255);
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
                    float best = 999f; foreach (var d in dirs) best = Mathf.Min(best, Vector3.Angle(p, d));
                    if (best < 19f) px[y * w + x] = new Color32(24, 28, 44, 255);
                    else if (best < 21f) px[y * w + x] = new Color32(120, 130, 160, 255);
                }
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }

        void Build()
        {
            transform.position = Origin;
            rt = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "MenuStageRT" };
            var camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0.1f, 1.3f, -13.5f);
            camGo.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 32f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 40f;
            cam.targetTexture = rt; cam.allowHDR = false; cam.enabled = false;
            var data = camGo.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() ?? camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;

            // lamps: a warm key from the upper left, a cool rim from behind and a blue glow under the pedestal
            AddLamp(new Vector3(-4f, 5f, -5f), new Color(1f, 0.93f, 0.8f), 70f, 18f);
            AddLamp(new Vector3(5f, 3f, 3f), new Color(0.4f, 0.65f, 1f), 55f, 16f);
            AddLamp(new Vector3(0f, -0.4f, -1f), new Color(0.2f, 0.5f, 1f), 30f, 8f);

            goldMat = Mat(new Color(1f, 0.78f, 0.22f), 1f, 0.88f, new Color(0.18f, 0.1f, 0f));
            var steel = Mat(new Color(0.05f, 0.09f, 0.22f), 0.6f, 0.9f, Color.black);
            var glow = Mat(new Color(0.2f, 0.55f, 1f), 0f, 0.5f, new Color(0.3f, 1.1f, 2.2f));

            // pedestal: two dark discs and a glowing ring
            Prim(PrimitiveType.Cylinder, "PedestalLow", transform, new Vector3(0f, -0.55f, 0f), new Vector3(4.6f, 0.12f, 4.6f), steel);
            Prim(PrimitiveType.Cylinder, "PedestalTop", transform, new Vector3(0f, -0.38f, 0f), new Vector3(3.6f, 0.1f, 3.6f), steel);
            ring = Prim(PrimitiveType.Cylinder, "Ring", transform, new Vector3(0f, -0.3f, 0f), new Vector3(3.9f, 0.02f, 3.9f), glow);

            // trophy: base, stem, cup, rim and two handles
            var t = new GameObject("Trophy").transform; t.SetParent(transform, false); t.localPosition = new Vector3(-1.5f, -0.28f, 0.6f); trophy = t;
            Prim(PrimitiveType.Cylinder, "Foot", t, new Vector3(0f, 0.12f, 0f), new Vector3(1.5f, 0.12f, 1.5f), goldMat);
            Prim(PrimitiveType.Cylinder, "Plinth", t, new Vector3(0f, 0.38f, 0f), new Vector3(1.0f, 0.15f, 1.0f), steel);
            Prim(PrimitiveType.Cylinder, "Stem", t, new Vector3(0f, 0.95f, 0f), new Vector3(0.28f, 0.45f, 0.28f), goldMat);
            Prim(PrimitiveType.Sphere, "Node", t, new Vector3(0f, 1.15f, 0f), new Vector3(0.6f, 0.34f, 0.6f), goldMat);
            Prim(PrimitiveType.Sphere, "Cup", t, new Vector3(0f, 2.15f, 0f), new Vector3(1.55f, 1.7f, 1.55f), goldMat);
            Prim(PrimitiveType.Cylinder, "Lip", t, new Vector3(0f, 2.95f, 0f), new Vector3(1.4f, 0.05f, 1.4f), goldMat);
            for (int s = -1; s <= 1; s += 2)
            {
                var h = Prim(PrimitiveType.Sphere, "Handle", t, new Vector3(s * 0.95f, 2.3f, 0f), new Vector3(0.7f, 0.9f, 0.14f), goldMat);
                h.localRotation = Quaternion.Euler(0f, 0f, s * -20f);
            }
            Prim(PrimitiveType.Sphere, "Star", t, new Vector3(0f, 2.2f, -0.78f), new Vector3(0.35f, 0.35f, 0.08f), Mat(new Color(1f, 0.95f, 0.7f), 0.2f, 0.7f, new Color(0.8f, 0.6f, 0.2f)));

            // the ball, hovering on the right
            var tex = BallTexture();
            var ballMat = Mat(Color.white, 0f, 0.65f, Color.black); ballMat.SetTexture("_BaseMap", tex); ballMat.mainTexture = tex;
            ball = Prim(PrimitiveType.Sphere, "Ball", transform, new Vector3(1.55f, 1.0f, -0.4f), Vector3.one * 2.1f, ballMat);

            // sparkles
            sparkles = new Transform[16]; sparkPhase = new float[16];
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
            if (trophy != null) trophy.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * 0.5f) * 28f - 10f, 0f);
            if (ball != null)
            {
                ball.localRotation = Quaternion.Euler(18f, time * 55f, 8f);
                ball.localPosition = new Vector3(1.55f, 1.0f + Mathf.Sin(time * 1.4f) * 0.12f, -0.4f);
            }
            if (ring != null) ring.localScale = new Vector3(3.9f + Mathf.Sin(time * 2f) * 0.07f, 0.02f, 3.9f + Mathf.Sin(time * 2f) * 0.07f);
            for (int i = 0; i < sparkles.Length; i++)
            {
                float a = sparkPhase[i] + time * (0.25f + (i % 4) * 0.08f);
                float r = 2.0f + (i % 5) * 0.35f;
                sparkles[i].localPosition = new Vector3(Mathf.Cos(a) * r, 0.4f + (i * 0.23f) % 3.2f + Mathf.Sin(time * 1.3f + i) * 0.15f, Mathf.Sin(a) * r * 0.5f - 0.3f);
                sparkles[i].localRotation = Quaternion.Euler(time * 90f + i * 20f, time * 130f, 45f);
            }
        }

        void Update() { Place(Time.unscaledTime); }

        void OnDestroy() { if (rt != null) { rt.Release(); Destroy(rt); } }
    }
}
