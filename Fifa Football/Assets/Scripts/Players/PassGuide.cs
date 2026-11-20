using UnityEngine;

namespace FootballSim.Players
{
    /// <summary>
    /// Shows the user where a pass would go right now: a blue ring under the team-mate the stick is pointing at and a line from the
    /// passer to him. What it shows is exactly the player the pass button would find, so the user picks the receiver with the stick.
    /// </summary>
    public class PassGuide : MonoBehaviour
    {
        static PassGuide instance;
        Transform ring;
        LineRenderer line;
        public static bool IsShowing => instance != null && instance.ring != null && instance.ring.gameObject.activeInHierarchy;
        /// <summary>Where the ring is now (for tests).</summary>
        public static Vector3 RingPosition => instance != null && instance.ring != null ? instance.ring.position : Vector3.zero;

        static PassGuide Get()
        {
            if (instance != null) return instance;
            var go = new GameObject("PassGuide");
            instance = go.AddComponent<PassGuide>();
            var mat = ControlRing.Glow(new Color(0.3f, 0.78f, 1f), "PassGuide");

            var rg = new GameObject("PassRing");
            rg.transform.SetParent(go.transform, false);
            rg.AddComponent<MeshFilter>().sharedMesh = ControlRing.PlainRing();
            var r = rg.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            instance.ring = rg.transform;

            var lg = new GameObject("PassLine");
            lg.transform.SetParent(go.transform, false);
            lg.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // the line lies flat on the grass
            var l = lg.AddComponent<LineRenderer>();
            l.sharedMaterial = mat; l.useWorldSpace = true; l.alignment = LineAlignment.TransformZ;
            l.positionCount = 2; l.startWidth = l.endWidth = 0.16f;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; l.receiveShadows = false;
            instance.line = l;
            go.SetActive(false);
            return instance;
        }

        /// <summary>Draw the guide from the passer's feet to the receiver's feet.</summary>
        public static void Show(Vector3 from, Vector3 to)
        {
            var g = Get();
            if (!g.gameObject.activeSelf) g.gameObject.SetActive(true);
            from.y = 0.06f; to.y = 0.06f;
            g.ring.position = to;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 7f) * 0.07f;
            g.ring.localScale = new Vector3(pulse, 1f, pulse);
            // stop the line at the ring so it points at the receiver instead of running through him
            Vector3 dir = to - from;
            float len = dir.magnitude;
            Vector3 end = len > 1.2f ? to - dir / len * 0.95f : to;
            g.line.SetPosition(0, from + (len > 1.2f ? dir / len * 0.5f : Vector3.zero)); g.line.SetPosition(1, end);
        }

        public static void Hide()
        {
            if (instance != null && instance.gameObject.activeSelf) instance.gameObject.SetActive(false);
        }
    }
}
