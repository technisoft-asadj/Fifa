using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FootballSim.Ball;
using FootballSim.CameraSystem;
using FootballSim.Players;

namespace FootballSim.UI
{
    /// <summary>
    /// A small top-down map of the pitch showing all 22 players and the ball, so the player can see who stands where on every side, not
    /// just what the camera frames. It turns with the camera when the teams change ends. Tapping a dot makes the camera look at that
    /// player for a few seconds (like an operator switching to a player); tapping empty pitch goes back to following play.
    /// </summary>
    public class MiniRadar : MonoBehaviour, IPointerClickHandler
    {
        public static readonly Vector2 PitchSize = new Vector2(60f, 90f);
        public const float FocusSeconds = 5f;
        const float TapRadius = 22f;

        RectTransform area;
        Image[] dots;
        Image ballDot;
        PlayerController[] players = new PlayerController[0];
        BallController ball;
        BroadcastCamera cam;
        float nextFind;

        public Vector2 Size => area != null ? area.rect.size : Vector2.zero;
        public Image[] Dots => dots;

        /// <summary>Where a world position lands on the radar, relative to its centre; the map is turned when the camera looks the other way.</summary>
        public static Vector2 Map(Vector3 world, Vector2 radarSize, float viewSign)
        {
            return new Vector2(world.x * viewSign / PitchSize.x * radarSize.x, world.z * viewSign / PitchSize.y * radarSize.y);
        }

        public static MiniRadar Create(Transform parent, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject("Radar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
            var bg = go.GetComponent<Image>();
            bg.sprite = UITheme.Rounded; bg.type = Image.Type.Sliced; bg.color = new Color(0.05f, 0.22f, 0.10f, 0.78f);
            var rim = go.AddComponent<Outline>(); rim.effectColor = new Color(UITheme.Blue.r, UITheme.Blue.g, UITheme.Blue.b, 0.8f); rim.effectDistance = new Vector2(2f, -2f);
            var r = go.AddComponent<MiniRadar>();
            r.area = rt;
            // halfway line and centre circle so the map reads as a pitch
            Line(rt, "Halfway", new Vector2(size.x, 2f), Vector2.zero);
            var circle = Dot(rt, "Centre", Mathf.Min(size.x, size.y) * 0.30f, new Color(1f, 1f, 1f, 0.18f)); circle.rectTransform.anchoredPosition = Vector2.zero;
            r.dots = new Image[22];
            for (int i = 0; i < r.dots.Length; i++) r.dots[i] = Dot(rt, "P" + i, 11f, Color.white);
            r.ballDot = Dot(rt, "Ball", 8f, Color.white);
            return r;
        }

        static void Line(RectTransform parent, string name, Vector2 size, Vector2 pos)
        {
            var d = Dot(parent, name, 1f, new Color(1f, 1f, 1f, 0.35f));
            d.rectTransform.sizeDelta = size; d.rectTransform.anchoredPosition = pos;
        }

        static Image Dot(RectTransform parent, string name, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = UITheme.Rounded; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false;
            return img;
        }

        void Refresh()
        {
            if (Time.unscaledTime < nextFind && players.Length > 0) return;
            nextFind = Time.unscaledTime + 1f;
            players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            if (ball == null) ball = FindFirstObjectByType<BallController>();
            if (cam == null) cam = FindFirstObjectByType<BroadcastCamera>();
        }

        void LateUpdate()
        {
            Refresh();
            float sign = cam != null ? cam.ViewSign : 1f;
            Vector2 size = Size;
            Transform following = cam != null ? cam.Player : null;
            Transform focused = cam != null ? cam.FocusTarget : null;
            int used = 0;
            foreach (var p in players)
            {
                if (p == null || !p.isActiveAndEnabled || used >= dots.Length) continue;
                var d = dots[used++];
                if (!d.gameObject.activeSelf) d.gameObject.SetActive(true);
                bool mine = following != null && p.transform == following;
                bool looked = focused != null && p.transform == focused;
                d.rectTransform.anchoredPosition = Map(p.transform.position, size, sign);
                d.rectTransform.sizeDelta = Vector2.one * (mine || looked ? 17f : 11f);
                d.color = looked ? Color.white : mine ? UITheme.Gold : p.TeamId == 0 ? new Color(0.95f, 0.3f, 0.3f) : new Color(0.35f, 0.58f, 1f);
            }
            for (; used < dots.Length; used++) if (dots[used].gameObject.activeSelf) dots[used].gameObject.SetActive(false);
            if (ball != null) ballDot.rectTransform.anchoredPosition = Map(ball.Position, size, sign);
        }

        /// <summary>The player whose dot is nearest the given point on the radar (relative to its centre) within the tap radius, or null.</summary>
        public PlayerController PlayerAt(Vector2 local)
        {
            float sign = cam != null ? cam.ViewSign : 1f;
            PlayerController best = null; float bestD = TapRadius;
            foreach (var p in players)
            {
                if (p == null || !p.isActiveAndEnabled) continue;
                float d = Vector2.Distance(local, Map(p.transform.position, Size, sign));
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        /// <summary>A tap at a radar position: look at the player there, or go back to following play when nobody is there.</summary>
        public PlayerController Tap(Vector2 local)
        {
            Refresh();
            var p = PlayerAt(local);
            if (cam != null)
            {
                if (p != null) cam.FocusOn(p.transform, FocusSeconds); else cam.ClearFocus();
            }
            return p;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, null, out var local))
                Tap(local - area.rect.center); // the dots are placed from the centre, the pointer position from the pivot (top-left)
        }
    }
}
