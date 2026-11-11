using UnityEngine;

namespace FootballSim.Players
{
    /// <summary>
    /// Procedural humanoid built from primitives with code-driven run, kick and tackle animation.
    /// Replaces the placeholder capsule's mesh (shirt colour is taken from its material). Gameplay never
    /// depends on this; a rigged model with an Animator can replace it later by implementing the same
    /// three calls (speed from PlayerController.Velocity, PlayKick, PlayTackle).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerVisual : MonoBehaviour
    {
        static readonly Color[] SkinTones =
        {
            new Color(0.96f, 0.80f, 0.69f), new Color(0.87f, 0.68f, 0.52f), new Color(0.72f, 0.53f, 0.38f),
            new Color(0.55f, 0.38f, 0.26f), new Color(0.36f, 0.24f, 0.17f)
        };
        static readonly Color[] HairColors =
        {
            new Color(0.08f, 0.06f, 0.05f), new Color(0.25f, 0.16f, 0.09f), new Color(0.55f, 0.38f, 0.16f), new Color(0.75f, 0.62f, 0.3f)
        };

        PlayerController pc;
        Transform body, hipL, hipR, kneeL, kneeR, shoulderL, shoulderR;
        float phase, amp, kickT = -1f, tackleT = -1f;
        bool kickRight = true;

        public Transform Body => body;
        public bool IsBuilt => body != null;

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            Build();
        }

        void Build()
        {
            var shirt = Color.red;
            var srcRenderer = GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (srcRenderer != null)
            {
                if (srcRenderer.sharedMaterial != null)
                {
                    shirt = srcRenderer.sharedMaterial.HasProperty("_BaseColor") ? srcRenderer.sharedMaterial.GetColor("_BaseColor") : srcRenderer.sharedMaterial.color;
                    if (srcRenderer.sharedMaterial.shader != null) shader = srcRenderer.sharedMaterial.shader;
                }
                srcRenderer.enabled = false;
            }
            var mf = GetComponent<MeshFilter>();
            if (mf != null) { /* mesh stays but is not rendered */ }

            // Deterministic variety per player from the object's name hash
            int h = Mathf.Abs(name.GetHashCode());
            var skin = SkinTones[h % SkinTones.Length];
            var hair = HairColors[(h / 7) % HairColors.Length];
            var shorts = Color.Lerp(shirt, Color.white, 0.75f);
            var boot = new Color(0.08f, 0.08f, 0.08f);

            Material M(Color c)
            {
                var m = new Material(shader);
                m.color = c;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                return m;
            }
            var mShirt = M(shirt); var mShorts = M(shorts); var mSkin = M(skin); var mHair = M(hair); var mBoot = M(boot);
            var mSock = M(Color.Lerp(shirt, Color.black, 0.15f));

            var visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            visual.localPosition = new Vector3(0f, 0.09f, 0f); // put boots on the ground (capsule feet are at -0.9)
            body = new GameObject("Body").transform;
            body.SetParent(visual, false);

            Part(PrimitiveType.Capsule, "Torso", body, new Vector3(0f, 0.3f, 0f), new Vector3(0.38f, 0.29f, 0.22f), mShirt);
            Part(PrimitiveType.Sphere, "Head", body, new Vector3(0f, 0.74f, 0f), Vector3.one * 0.22f, mSkin);
            Part(PrimitiveType.Sphere, "Hair", body, new Vector3(0f, 0.8f, -0.012f), new Vector3(0.235f, 0.13f, 0.235f), mHair);
            Part(PrimitiveType.Cube, "Shorts", body, new Vector3(0f, -0.07f, 0f), new Vector3(0.36f, 0.24f, 0.22f), mShorts);

            hipL = Leg("L", -0.12f, body, mSkin, mSock, mBoot, out kneeL);
            hipR = Leg("R", 0.12f, body, mSkin, mSock, mBoot, out kneeR);
            shoulderL = Arm("L", -0.27f, body, mSkin, mShirt);
            shoulderR = Arm("R", 0.27f, body, mSkin, mShirt);
        }

        static GameObject Part(PrimitiveType type, string n, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = n;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Transform Leg(string side, float x, Transform parent, Material skin, Material sock, Material boot, out Transform knee)
        {
            var hip = new GameObject("Hip" + side).transform;
            hip.SetParent(parent, false); hip.localPosition = new Vector3(x, -0.12f, 0f);
            Part(PrimitiveType.Capsule, "Thigh", hip, new Vector3(0f, -0.2f, 0f), new Vector3(0.17f, 0.21f, 0.17f), skin);
            knee = new GameObject("Knee" + side).transform;
            knee.SetParent(hip, false); knee.localPosition = new Vector3(0f, -0.41f, 0f);
            Part(PrimitiveType.Capsule, "Shin", knee, new Vector3(0f, -0.2f, 0f), new Vector3(0.14f, 0.2f, 0.14f), sock);
            Part(PrimitiveType.Cube, "Boot", knee, new Vector3(0f, -0.42f, 0.06f), new Vector3(0.14f, 0.09f, 0.27f), boot);
            return hip;
        }

        static Transform Arm(string side, float x, Transform parent, Material skin, Material sleeve)
        {
            var sh = new GameObject("Shoulder" + side).transform;
            sh.SetParent(parent, false); sh.localPosition = new Vector3(x, 0.5f, 0f);
            Part(PrimitiveType.Capsule, "Arm", sh, new Vector3(0f, -0.22f, 0f), new Vector3(0.095f, 0.22f, 0.095f), skin);
            Part(PrimitiveType.Capsule, "Sleeve", sh, new Vector3(0f, -0.07f, 0f), new Vector3(0.115f, 0.08f, 0.115f), sleeve);
            return sh;
        }

        public void PlayKick(bool rightFoot = true) { kickT = 0f; kickRight = rightFoot; }
        public void PlayTackle() { tackleT = 0f; kickRight = true; }

        void Update()
        {
            if (body == null || pc == null) return;
            float speed = pc.Velocity.magnitude;
            float targetAmp = speed > 0.25f ? Mathf.Clamp01(speed / 8f) * 50f + 8f : 0f;
            amp = Mathf.Lerp(amp, targetAmp, 1f - Mathf.Exp(-10f * Time.deltaTime));
            phase += speed * Time.deltaTime * 1.5f;

            float swing = Mathf.Sin(phase) * amp;
            float bendL = Mathf.Max(0f, Mathf.Sin(phase - 1.1f)) * amp * 1.3f;
            float bendR = Mathf.Max(0f, Mathf.Sin(phase + Mathf.PI - 1.1f)) * amp * 1.3f;

            float lean = Mathf.Clamp01(speed / 9f) * 12f;
            float dip = 0f;
            float kickHip = 0f, kickKnee = 0f;
            bool kicking = false, tackling = false;

            if (kickT >= 0f)
            {
                kickT += Time.deltaTime / 0.3f;
                if (kickT >= 1f) kickT = -1f;
                else
                {
                    kicking = true;
                    kickHip = kickT < 0.35f ? Mathf.Lerp(0f, 35f, kickT / 0.35f) : Mathf.Lerp(35f, -80f, (kickT - 0.35f) / 0.65f);
                    kickKnee = kickT < 0.35f ? 50f : Mathf.Lerp(50f, 8f, (kickT - 0.35f) / 0.65f);
                }
            }
            if (tackleT >= 0f)
            {
                tackleT += Time.deltaTime / 0.45f;
                if (tackleT >= 1f) tackleT = -1f;
                else
                {
                    tackling = true;
                    float s = Mathf.Sin(tackleT * Mathf.PI);
                    lean += 35f * s; dip = -0.28f * s;
                    kickHip = -85f * s; kickKnee = 5f;
                }
            }

            body.localRotation = Quaternion.Euler(lean, 0f, 0f);
            body.localPosition = new Vector3(0f, dip, 0f);

            ApplyLeg(hipL, kneeL, -swing, bendL, (kicking || tackling) && !kickRight, kickHip, kickKnee);
            ApplyLeg(hipR, kneeR, swing, bendR, (kicking || tackling) && kickRight, kickHip, kickKnee);
            shoulderL.localRotation = Quaternion.Euler(swing * 0.8f, 0f, 6f);
            shoulderR.localRotation = Quaternion.Euler(-swing * 0.8f, 0f, -6f);
        }

        static void ApplyLeg(Transform hip, Transform knee, float swing, float bend, bool override_, float kHip, float kKnee)
        {
            hip.localRotation = Quaternion.Euler(override_ ? kHip : swing, 0f, 0f);
            knee.localRotation = Quaternion.Euler(override_ ? kKnee : bend, 0f, 0f);
        }
    }
}
