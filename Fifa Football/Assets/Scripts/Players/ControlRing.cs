using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Players
{
    /// <summary>
    /// The gold ring on the grass under the player the user controls, with a chevron on the side he is facing, so it is always clear which
    /// player is "me" and which way he will run or pass. It lies flat on the pitch and turns with the player.
    /// </summary>
    public class ControlRing : MonoBehaviour
    {
        const float Inner = 0.78f, Outer = 0.98f, Lift = 0.05f;
        static Mesh mesh;
        static Material material;

        /// <summary>Adds (or returns) the ring on a player.</summary>
        public static ControlRing On(Transform owner)
        {
            var existing = owner.Find("ControlRing");
            if (existing != null) return existing.GetComponent<ControlRing>();
            var go = new GameObject("ControlRing");
            go.transform.SetParent(owner, false);
            go.AddComponent<MeshFilter>().sharedMesh = SharedMesh();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = SharedMaterial();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return go.AddComponent<ControlRing>();
        }

        void LateUpdate()
        {
            // flat on the grass whatever the height of the player's pivot, gently pulsing so it catches the eye
            var p = transform.parent.position;
            transform.position = new Vector3(p.x, Lift, p.z);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.05f;
            transform.localScale = new Vector3(pulse, 1f, pulse);
        }

        static Material SharedMaterial()
        {
            if (material != null) return material;
            material = Glow(new Color(1f, 0.82f, 0.12f), "ControlRing");
            return material;
        }

        /// <summary>A flat, self-lit material in one colour (the rings and lines drawn on the grass).</summary>
        public static Material Glow(Color color, string name)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(sh) { name = name };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.color = color;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * 1.4f);
            return m;
        }

        static Mesh plainRing;
        /// <summary>The ring without the chevron (used under a pass target).</summary>
        public static Mesh PlainRing()
        {
            if (plainRing != null) return plainRing;
            plainRing = Build(false);
            return plainRing;
        }

        static Mesh SharedMesh()
        {
            if (mesh != null) return mesh;
            mesh = Build(true);
            return mesh;
        }

        static Mesh Build(bool chevron)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            const int seg = 40;
            for (int i = 0; i < seg; i++)
            {
                // leave a gap in front where the chevron sits
                float a0 = i / (float)seg * Mathf.PI * 2f, a1 = (i + 1) / (float)seg * Mathf.PI * 2f;
                float mid = (a0 + a1) * 0.5f;
                if (chevron && Mathf.Abs(Mathf.DeltaAngle(mid * Mathf.Rad2Deg, 90f)) < 14f) continue; // 90 degrees is +Z, the way he faces
                Quad(v, n, t, Pt(a0, Inner), Pt(a0, Outer), Pt(a1, Outer), Pt(a1, Inner));
            }
            // chevron pointing forward (+Z), sitting just outside the ring
            if (chevron) Tri(v, n, t, new Vector3(-0.42f, 0f, Outer + 0.02f), new Vector3(0f, 0f, Outer + 0.62f), new Vector3(0.42f, 0f, Outer + 0.02f));
            var m = new Mesh { name = chevron ? "ControlRing" : "PlainRing" };
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        static Vector3 Pt(float angle, float r) { return new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r); }

        // both faces are added so the ring shows whichever way the winding is read
        static void Quad(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(v, n, t, a, b, c); Tri(v, n, t, a, c, d);
        }

        static void Tri(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            for (int k = 0; k < 3; k++) n.Add(Vector3.up);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            // the reverse side
            int j = v.Count;
            v.Add(a); v.Add(c); v.Add(b);
            for (int k = 0; k < 3; k++) n.Add(Vector3.up);
            t.Add(j); t.Add(j + 1); t.Add(j + 2);
        }
    }
}
