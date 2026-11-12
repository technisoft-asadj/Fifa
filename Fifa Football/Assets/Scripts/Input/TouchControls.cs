using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FootballSim.Inputs
{
    /// <summary>
    /// On-screen mobile controls: floating joystick (left half of the screen) plus Shoot, Pass, Tackle,
    /// Switch and Sprint buttons. Implements IPlayerInput so it can be merged with keyboard/gamepad.
    /// Only shown on touch devices (or when forceShow is set) and only while a match is running.
    /// </summary>
    public class TouchControls : MonoBehaviour, IPlayerInput
    {
        public bool forceShow;
        [Range(0.5f, 1.6f)] public float controlScale = 1f;
        [Range(0.2f, 1f)] public float controlOpacity = 0.6f;

        const string PrefScale = "touch.scale", PrefOpacity = "touch.opacity";

        Canvas canvas;
        Joystick joystick;
        HoldButton shoot, pass, tackle, switchBtn, sprint;
        GameObject root;

        public bool IsVisible => root != null && root.activeSelf;

        // ---- IPlayerInput ----
        public Vector2 Move => IsVisible ? joystick.Value : Vector2.zero;
        public bool MoveIsWorldSpace => false;
        public bool Sprint => IsVisible && sprint.Held;
        public bool PassPressed => pass.ConsumePress();
        public bool ShootHeld => shoot.Held;
        public bool ShootReleased => shoot.ConsumeRelease();
        public bool TacklePressed => tackle.ConsumePress();
        public bool SwitchPressed => switchBtn.ConsumePress();
        public Vector3 AimWorld => Vector3.zero;
        public float ActionPower => 0f;

        // ---- test hooks ----
        public Joystick StickForTest => joystick;
        public HoldButton PassForTest => pass;
        public HoldButton ShootForTest => shoot;

        void Awake()
        {
            try
            {
                controlScale = PlayerPrefs.GetFloat(PrefScale, controlScale);
                controlOpacity = PlayerPrefs.GetFloat(PrefOpacity, controlOpacity);
            }
            catch { /* prefs unavailable: use defaults */ }
            EnsureEventSystem();
            Build();
        }

        void Update()
        {
            bool want = forceShow || Application.isMobilePlatform || Touchscreen.current != null;
            want &= Time.timeScale > 0f; // hide during menus / pause
            if (root.activeSelf != want) root.SetActive(want);
        }

        /// <summary>Persist and apply a new scale/opacity (used by a future settings screen).</summary>
        public void SetAppearance(float scale, float opacity)
        {
            controlScale = Mathf.Clamp(scale, 0.5f, 1.6f);
            controlOpacity = Mathf.Clamp(opacity, 0.2f, 1f);
            try { PlayerPrefs.SetFloat(PrefScale, controlScale); PlayerPrefs.SetFloat(PrefOpacity, controlOpacity); PlayerPrefs.Save(); } catch { }
            Rebuild();
        }

        void Rebuild()
        {
            if (canvas != null) Destroy(canvas.gameObject);
            Build();
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        // ---- construction ----
        static Sprite circle;
        static Sprite Circle()
        {
            if (circle != null) return circle;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float a = Mathf.Clamp01((r - 1f - d) / 1.5f + 0.5f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return circle;
        }

        void Build()
        {
            var canvasGo = new GameObject("TouchCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5; // above the match HUD so buttons receive touches
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            root = canvasGo;
            // Everything sits inside the device safe area so notches / punch-holes never cover a button.
            var safeGo = new GameObject("SafeArea", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)safeGo.transform;
            ApplySafeArea(rt, Screen.safeArea, Screen.width, Screen.height);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Joystick touch area: left 45% of the screen, lower 65%
            var area = new GameObject("JoystickArea", typeof(RectTransform), typeof(Image));
            area.transform.SetParent(rt, false);
            var art = (RectTransform)area.transform;
            art.anchorMin = new Vector2(0f, 0f); art.anchorMax = new Vector2(0.45f, 0.65f);
            art.offsetMin = art.offsetMax = Vector2.zero;
            area.GetComponent<Image>().color = new Color(0, 0, 0, 0); // invisible but raycastable
            joystick = area.AddComponent<Joystick>();
            joystick.Init(rt, controlScale, controlOpacity, Circle());

            float s = controlScale;
            shoot = Button(rt, font, "SHOOT", new Vector2(-210, 210), 190 * s, new Color(0.9f, 0.25f, 0.2f));
            pass = Button(rt, font, "PASS", new Vector2(-460, 140), 150 * s, new Color(0.2f, 0.6f, 0.9f));
            tackle = Button(rt, font, "TACKLE", new Vector2(-120, 450), 150 * s, new Color(0.95f, 0.7f, 0.1f));
            switchBtn = Button(rt, font, "SWITCH", new Vector2(-390, 380), 130 * s, new Color(0.5f, 0.5f, 0.55f));
            sprint = Button(rt, font, "SPRINT", new Vector2(-640, 100), 130 * s, new Color(0.2f, 0.75f, 0.4f), anchorLeft: false);
        }

        /// <summary>Stretch a rect to the safe area, given in screen pixels.</summary>
        public static void ApplySafeArea(RectTransform rt, Rect safe, float screenW, float screenH)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            if (screenW <= 0f || screenH <= 0f || safe.width <= 0f || safe.height <= 0f)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            }
            else
            {
                rt.anchorMin = new Vector2(Mathf.Clamp01(safe.xMin / screenW), Mathf.Clamp01(safe.yMin / screenH));
                rt.anchorMax = new Vector2(Mathf.Clamp01(safe.xMax / screenW), Mathf.Clamp01(safe.yMax / screenH));
            }
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        HoldButton Button(RectTransform parent, Font font, string label, Vector2 pos, float size, Color color, bool anchorLeft = false)
        {
            var go = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(HoldButton));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f); // bottom-right
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(pos.x, pos.y);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = Circle();
            img.color = new Color(color.r, color.g, color.b, controlOpacity);

            var tgo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)tgo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            var t = tgo.GetComponent<Text>();
            t.font = font; t.text = label; t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = Mathf.RoundToInt(size * 0.2f); t.fontStyle = FontStyle.Bold;
            t.color = new Color(1, 1, 1, Mathf.Clamp01(controlOpacity + 0.3f)); t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return go.GetComponent<HoldButton>();
        }
    }

    /// <summary>Floating virtual joystick: the stick appears where the thumb lands.</summary>
    public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform canvasRect, baseRect, knobRect;
        float radius = 110f;

        public Vector2 Value { get; private set; }

        public void Init(RectTransform canvas, float scale, float opacity, Sprite circle)
        {
            canvasRect = canvas;
            radius = 110f * scale;
            baseRect = MakeDisc("Base", circle, radius * 2f, new Color(1, 1, 1, opacity * 0.5f));
            knobRect = MakeDisc("Knob", circle, radius * 0.9f, new Color(1, 1, 1, Mathf.Clamp01(opacity + 0.2f)));
            baseRect.gameObject.SetActive(false);
            knobRect.gameObject.SetActive(false);
        }

        RectTransform MakeDisc(string n, Sprite sprite, float size, Color c)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasRect, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = c; img.raycastTarget = false;
            return rt;
        }

        Vector2 ToCanvas(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, e.position, null, out var p);
            // canvas rect pivot is centre; convert to bottom-left origin
            return p + canvasRect.rect.size * 0.5f;
        }

        public void OnPointerDown(PointerEventData e)
        {
            var p = ToCanvas(e);
            baseRect.anchoredPosition = p;
            knobRect.anchoredPosition = p;
            baseRect.gameObject.SetActive(true);
            knobRect.gameObject.SetActive(true);
            Value = Vector2.zero;
        }

        public void OnDrag(PointerEventData e)
        {
            Vector2 delta = ToCanvas(e) - baseRect.anchoredPosition;
            Vector2 clamped = Vector2.ClampMagnitude(delta, radius);
            knobRect.anchoredPosition = baseRect.anchoredPosition + clamped;
            Vector2 v = clamped / radius;
            Value = v.magnitude < 0.12f ? Vector2.zero : v; // dead zone
        }

        public void OnPointerUp(PointerEventData e)
        {
            Value = Vector2.zero;
            HideStick();
        }

        void HideStick()
        {
            if (baseRect != null) baseRect.gameObject.SetActive(false);
            if (knobRect != null) knobRect.gameObject.SetActive(false);
        }

        void OnDisable() { Value = Vector2.zero; HideStick(); }

        public void SetValueForTest(Vector2 v) { Value = v; }
    }

    /// <summary>Press/hold/release button with latched one-shot events (consumed when read).</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        bool pressLatch, releaseLatch;
        public bool Held { get; private set; }

        public void OnPointerDown(PointerEventData e) { Press(); }
        public void OnPointerUp(PointerEventData e) { Release(); }
        public void OnPointerExit(PointerEventData e) { if (Held) Release(); }
        void OnDisable() { Held = false; }

        public void Press() { Held = true; pressLatch = true; }
        public void Release() { if (!Held) return; Held = false; releaseLatch = true; }

        public bool ConsumePress() { var v = pressLatch; pressLatch = false; return v; }
        public bool ConsumeRelease() { var v = releaseLatch; releaseLatch = false; return v; }
    }
}
