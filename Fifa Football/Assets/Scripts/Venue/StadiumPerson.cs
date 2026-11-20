using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Venue
{
    /// <summary>
    /// A person of the venue (substitute, coach, physio, linesman) built from primitives and posed by code: a driver writes the
    /// <see cref="Target"/> angles each frame and the person eases toward them. Angles are degrees; the person faces local +Z.
    /// Shoulder X: negative raises the arm forward; Z: positive swings it outward. Thigh X: negative lifts the knee. Knee X: positive bends it back.
    /// Head pitch: positive nods down, negative tilts back.
    /// </summary>
    public sealed class StadiumPerson : MonoBehaviour
    {
        public const int Stand = 0, Spine = 1, HeadYaw = 2, HeadPitch = 3, ShR = 4, ShRz = 5, ElR = 6, ShL = 7, ShLz = 8, ElL = 9,
            ThighL = 10, ThighR = 11, KneeL = 12, KneeR = 13, Bob = 14, Count = 15;

        public const float SeatedHeight = 0.54f, StandingHeight = 0.93f;

        public readonly float[] Target = new float[Count];
        readonly float[] cur = new float[Count];
        /// <summary>How quickly the body follows its targets (per second); a jogger needs it fast.</summary>
        public float Rate = 9f;
        public float Facing; // world yaw of the body, degrees

        Transform pelvis, spine, neck, shR, elR, shL, elL, hipL, hipR, knL, knR;
        GameObject bottle, flag;

        public bool BottleVisible { set { if (bottle != null && bottle.activeSelf != value) bottle.SetActive(value); } }
        public bool FlagVisible { set { if (flag != null && flag.activeSelf != value) flag.SetActive(value); } }

        static readonly Color[] Skins =
        {
            new Color(0.96f, 0.80f, 0.69f), new Color(0.87f, 0.68f, 0.52f), new Color(0.72f, 0.53f, 0.38f), new Color(0.55f, 0.38f, 0.26f), new Color(0.36f, 0.24f, 0.17f)
        };
        static readonly Color[] Hairs = { new Color(0.08f, 0.06f, 0.05f), new Color(0.25f, 0.16f, 0.09f), new Color(0.55f, 0.38f, 0.16f), new Color(0.75f, 0.62f, 0.3f) };

        static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();
        static Shader shader;

        /// <summary>A shared material of this colour (people reuse them); <see cref="ClearMaterials"/> frees them with the venue.</summary>
        public static Material MaterialFor(Color c)
        {
            Color32 k = c; int key = k.r | (k.g << 8) | (k.b << 16);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = NewMaterial(c); cache[key] = m;
            return m;
        }

        public static Material NewMaterial(Color c)
        {
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
            return m;
        }

        public static void ClearMaterials()
        {
            foreach (var m in cache.Values) if (m != null) Destroy(m);
            cache.Clear();
        }

        static GameObject Part(PrimitiveType t, string n, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(t);
            go.name = n;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return go;
        }

        static Transform Pivot(string n, Transform parent, Vector3 pos)
        {
            var t = new GameObject(n).transform;
            t.SetParent(parent, false); t.localPosition = pos;
            return t;
        }

        /// <param name="shirt">top and sleeves; <paramref name="legs">trousers or shorts; <paramref name="socks">null for long trousers.</paramref></param>
        public static StadiumPerson Create(Transform parent, string name, Material shirt, Material legs, Material socks, int look, bool hat = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<StadiumPerson>();
            var skin = MaterialFor(Skins[Mathf.Abs(look) % Skins.Length]);
            var hair = MaterialFor(hat ? new Color(0.12f, 0.12f, 0.14f) : Hairs[Mathf.Abs(look / 5) % Hairs.Length]);
            var boot = MaterialFor(new Color(0.07f, 0.07f, 0.07f));
            if (socks == null) socks = legs;

            p.pelvis = Pivot("Pelvis", go.transform, new Vector3(0f, StandingHeight, 0f));
            Part(PrimitiveType.Cube, "Hips", p.pelvis, Vector3.zero, new Vector3(0.34f, 0.2f, 0.2f), legs);
            p.spine = Pivot("Spine", p.pelvis, new Vector3(0f, 0.08f, 0f));
            Part(PrimitiveType.Cube, "Torso", p.spine, new Vector3(0f, 0.26f, 0f), new Vector3(0.4f, 0.5f, 0.23f), shirt);
            p.neck = Pivot("Neck", p.spine, new Vector3(0f, 0.55f, 0f));
            Part(PrimitiveType.Sphere, "Head", p.neck, new Vector3(0f, 0.12f, 0f), new Vector3(0.22f, 0.25f, 0.23f), skin);
            Part(PrimitiveType.Sphere, "Hair", p.neck, new Vector3(0f, 0.19f, -0.01f), new Vector3(0.235f, 0.14f, 0.24f), hair);
            p.shR = Arm("R", 0.26f, p.spine, shirt, skin, out p.elR, out var handR);
            p.shL = Arm("L", -0.26f, p.spine, shirt, skin, out p.elL, out _);
            p.hipL = Leg(-0.1f, p.pelvis, legs, socks, skin, boot, out p.knL);
            p.hipR = Leg(0.1f, p.pelvis, legs, socks, skin, boot, out p.knR);

            p.bottle = Part(PrimitiveType.Cylinder, "Bottle", handR, new Vector3(0f, -0.03f, 0.04f), new Vector3(0.06f, 0.11f, 0.06f), MaterialFor(new Color(0.3f, 0.7f, 0.95f)));
            p.bottle.SetActive(false);
            p.flag = Part(PrimitiveType.Cube, "Flag", handR, new Vector3(0f, -0.04f, 0.22f), new Vector3(0.02f, 0.25f, 0.32f), MaterialFor(new Color(1f, 0.85f, 0.1f)));
            p.flag.SetActive(false);
            for (int i = 0; i < Count; i++) p.Target[i] = p.cur[i] = 0f;
            p.Target[Stand] = p.cur[Stand] = 1f;
            p.Apply();
            return p;
        }

        static Transform Arm(string side, float x, Transform parent, Material sleeve, Material skin, out Transform elbow, out Transform hand)
        {
            var sh = Pivot("Shoulder" + side, parent, new Vector3(x, 0.47f, 0f));
            Part(PrimitiveType.Capsule, "Upper", sh, new Vector3(0f, -0.14f, 0f), new Vector3(0.1f, 0.15f, 0.1f), sleeve);
            elbow = Pivot("Elbow" + side, sh, new Vector3(0f, -0.29f, 0f));
            Part(PrimitiveType.Capsule, "Fore", elbow, new Vector3(0f, -0.13f, 0f), new Vector3(0.085f, 0.14f, 0.085f), skin);
            hand = Pivot("Hand" + side, elbow, new Vector3(0f, -0.28f, 0f));
            Part(PrimitiveType.Sphere, "Fist", hand, Vector3.zero, Vector3.one * 0.09f, skin);
            return sh;
        }

        static Transform Leg(float x, Transform parent, Material legs, Material socks, Material skin, Material boot, out Transform knee)
        {
            var hip = Pivot("Hip", parent, new Vector3(x, -0.08f, 0f));
            Part(PrimitiveType.Capsule, "Thigh", hip, new Vector3(0f, -0.21f, 0f), new Vector3(0.16f, 0.23f, 0.16f), legs);
            knee = Pivot("Knee", hip, new Vector3(0f, -0.43f, 0f));
            Part(PrimitiveType.Capsule, "Shin", knee, new Vector3(0f, -0.2f, 0f), new Vector3(0.13f, 0.21f, 0.13f), socks);
            Part(PrimitiveType.Cube, "Boot", knee, new Vector3(0f, -0.41f, 0.05f), new Vector3(0.13f, 0.08f, 0.26f), boot);
            return hip;
        }

        /// <summary>Sets every target to a relaxed pose: seated (<paramref name="standing"/> false) or standing, arms down, looking ahead.</summary>
        public void Relax(bool standing)
        {
            for (int i = 0; i < Count; i++) Target[i] = 0f;
            Target[Stand] = standing ? 1f : 0f;
            Target[ShRz] = Target[ShLz] = 6f;
            Target[ElR] = Target[ElL] = -8f;
            if (!standing) { Target[ShR] = Target[ShL] = 12f; Target[ElR] = Target[ElL] = -25f; } // hands resting on the knees
        }

        /// <summary>Turns the head toward a world point (the ball).</summary>
        public void Watch(Vector3 world, float weight = 1f)
        {
            Vector3 d = transform.InverseTransformPoint(world);
            float yaw = Mathf.Atan2(d.x, Mathf.Max(0.01f, d.z)) * Mathf.Rad2Deg;
            if (d.z < 0f) yaw = Mathf.Sign(d.x == 0f ? 1f : d.x) * 70f;
            Target[HeadYaw] = Mathf.Clamp(yaw, -70f, 70f) * weight;
        }

        void LateUpdate() { Apply(); }

        /// <summary>Eases the body toward the targets and poses the joints.</summary>
        public void Apply()
        {
            float k = 1f - Mathf.Exp(-Rate * Time.deltaTime);
            if (Time.deltaTime <= 0f) k = 1f;
            for (int i = 0; i < Count; i++) cur[i] = Mathf.Lerp(cur[i], Target[i], k);
            transform.rotation = Quaternion.Euler(0f, Facing, 0f);
            float stand = Mathf.SmoothStep(0f, 1f, cur[Stand]);
            var pp = pelvis.localPosition; pp.y = Mathf.Lerp(SeatedHeight, StandingHeight, stand) + cur[Bob]; pelvis.localPosition = pp;
            spine.localRotation = Quaternion.Euler(cur[Spine], 0f, 0f);
            neck.localRotation = Quaternion.Euler(cur[HeadPitch], cur[HeadYaw], 0f);
            shR.localRotation = Quaternion.Euler(cur[ShR], 0f, cur[ShRz]);
            shL.localRotation = Quaternion.Euler(cur[ShL], 0f, -cur[ShLz]);
            elR.localRotation = Quaternion.Euler(cur[ElR], 0f, 0f);
            elL.localRotation = Quaternion.Euler(cur[ElL], 0f, 0f);
            float seat = 1f - stand;
            hipL.localRotation = Quaternion.Euler(cur[ThighL] - 90f * seat, 0f, 0f);
            hipR.localRotation = Quaternion.Euler(cur[ThighR] - 90f * seat, 0f, 0f);
            knL.localRotation = Quaternion.Euler(cur[KneeL] + 90f * seat, 0f, 0f);
            knR.localRotation = Quaternion.Euler(cur[KneeR] + 90f * seat, 0f, 0f);
        }
    }
}
