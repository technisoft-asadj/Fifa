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

        /// <summary>Mirror the layout: thumb stick on the right, buttons on the left.</summary>
        public bool leftHanded;
        /// <summary>A stick that is always shown in the same place instead of appearing where the thumb lands.</summary>
        public bool fixedStick;

        /// <summary>Stick response: above 1 a smaller push already gives full speed.</summary>
        public float stickSensitivity = 1f;
        /// <summary>A short buzz on the action buttons (phones only).</summary>
        public bool haptics = true;
        /// <summary>A nearly full push on the stick sprints (the sprint button still works); a lighter push jogs. On by default so a phone player is not stuck at jog speed.</summary>
        public bool autoSprint = true;
        bool autoSprinting;

        public const string PrefScale = "touch.scale", PrefOpacity = "touch.opacity", PrefLeft = "touch.left", PrefFixed = "touch.fixed";
        public const string PrefSens = "touch.sens", PrefHaptics = "touch.haptics", PrefPositions = "touch.pos", PrefAuto = "touch.auto";

        // buttons the player has moved (right-handed positions; the rest sit at their defaults)
        System.Collections.Generic.Dictionary<Core.TouchButtonId, Vector2> custom = new System.Collections.Generic.Dictionary<Core.TouchButtonId, Vector2>();
        readonly System.Collections.Generic.Dictionary<Core.TouchButtonId, HoldButton> buttons = new System.Collections.Generic.Dictionary<Core.TouchButtonId, HoldButton>();
        bool editing;

        Canvas canvas;
        Joystick joystick;
        HoldButton shoot, pass, tackle, switchBtn, sprint;
        GameObject root;
        CanvasGroup group;
        bool preview;

        public bool IsVisible => root != null && root.activeSelf;

        // ---- IPlayerInput ----
        public Vector2 Move => IsVisible ? joystick.Value : Vector2.zero;
        public bool MoveIsWorldSpace => false;
        public bool Sprint => IsVisible && (sprint.Held || autoSprinting);
        public bool AutoSprinting => autoSprinting;
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
        public HoldButton ButtonFor(Core.TouchButtonId id) { return buttons.TryGetValue(id, out var b) ? b : null; }

        void Awake()
        {
            try
            {
                controlScale = PlayerPrefs.GetFloat(PrefScale, controlScale);
                controlOpacity = PlayerPrefs.GetFloat(PrefOpacity, controlOpacity);
                leftHanded = PlayerPrefs.GetInt(PrefLeft, leftHanded ? 1 : 0) == 1;
                fixedStick = PlayerPrefs.GetInt(PrefFixed, fixedStick ? 1 : 0) == 1;
                stickSensitivity = Core.TouchTuning.ClampSensitivity(PlayerPrefs.GetFloat(PrefSens, stickSensitivity));
                haptics = PlayerPrefs.GetInt(PrefHaptics, haptics ? 1 : 0) == 1;
                autoSprint = PlayerPrefs.GetInt(PrefAuto, autoSprint ? 1 : 0) == 1;
                custom = Core.TouchLayout.Parse(PlayerPrefs.GetString(PrefPositions, ""));
            }
            catch { /* prefs unavailable: use defaults */ }
            EnsureEventSystem();
            Build();
        }

        void Update()
        {
            bool want = forceShow || Application.isMobilePlatform || Touchscreen.current != null;
            bool running = Time.timeScale > 0f;
            autoSprinting = autoSprint && root.activeSelf && joystick != null && Core.TouchTuning.AutoSprint(autoSprinting, joystick.Value.magnitude);
            want &= running || preview; // hide during menus, except while previewing from the pause settings
            if (root.activeSelf != want) root.SetActive(want);
            if (group != null) group.blocksRaycasts = running || editing; // a preview must not swallow menu touches, but moving buttons needs them
            if (running && Time.unscaledTime >= nextContextCheck) { nextContextCheck = Time.unscaledTime + 0.2f; UpdateContext(); }
        }

        // ---- context-sensitive look (attacking / defending / keeper) ----
        float nextContextCheck;
        Core.ControlContext context = Core.ControlContext.Loose;
        bool contextApplied;
        FootballSim.Ball.BallController ball;
        public Core.ControlContext Context => context;
        public string ShootLabel => LabelOf(shoot);
        public string PassLabel => LabelOf(pass);
        public string TackleLabel => LabelOf(tackle);
        public float TackleScale => tackle != null ? tackle.transform.localScale.x : 1f;
        static string LabelOf(HoldButton b) { return b != null ? b.GetComponentInChildren<Text>().text : ""; }

        void UpdateContext()
        {
            FootballSim.Players.PlayerController me = null;
            foreach (var t in FindObjectsByType<FootballSim.FootballAI.TeamController>(FindObjectsSortMode.None))
                if (t.humanControlled && t.Controlled != null) { me = t.Controlled; break; }
            if (me == null) return;
            if (ball == null) ball = FindFirstObjectByType<FootballSim.Ball.BallController>();
            if (ball == null) return;
            var next = Core.ControlContextRules.Decide(me.isGoalkeeper, me.TeamId, ball.LastTouchTeam, ball.Velocity.magnitude);
            if (contextApplied && next == context) return;
            SetContext(next);
        }

        /// <summary>Applies a context's labels, sizes and dimming (also used by tests).</summary>
        public void SetContext(Core.ControlContext c)
        {
            context = c; contextApplied = true;
            var look = Core.ControlContextRules.Look(c);
            Restyle(shoot, look.shoot); Restyle(pass, look.pass); Restyle(tackle, look.tackle);
        }

        void Restyle(HoldButton b, Core.ButtonLook l)
        {
            if (b == null) return;
            var txt = b.GetComponentInChildren<Text>();
            txt.text = l.label;
            b.transform.localScale = Vector3.one * l.size;
            var img = b.GetComponent<Image>();
            var c = img.color; c.a = controlOpacity * l.alpha; img.color = c;
            var tc = txt.color; tc.a = Mathf.Clamp01(controlOpacity + 0.3f) * l.alpha; txt.color = tc;
        }

        /// <summary>Show the controls while the game is paused so size/opacity changes can be seen.</summary>
        public void SetPreview(bool on) { preview = on; }

        /// <summary>Persist and apply a new scale/opacity (used by a future settings screen).</summary>
        public void SetAppearance(float scale, float opacity)
        {
            controlScale = Mathf.Clamp(scale, 0.5f, 1.6f);
            controlOpacity = Mathf.Clamp(opacity, 0.2f, 1f);
            try { PlayerPrefs.SetFloat(PrefScale, controlScale); PlayerPrefs.SetFloat(PrefOpacity, controlOpacity); PlayerPrefs.Save(); } catch { }
            Rebuild();
        }

        /// <summary>Persist and apply the stick style and handedness.</summary>
        public void SetLayout(bool left, bool fixedStyle)
        {
            leftHanded = left; fixedStick = fixedStyle;
            try { PlayerPrefs.SetInt(PrefLeft, left ? 1 : 0); PlayerPrefs.SetInt(PrefFixed, fixedStyle ? 1 : 0); PlayerPrefs.Save(); } catch { }
            Rebuild();
        }

        /// <summary>Back to the default look and layout (and forget the saved choices).</summary>
        public void ResetToDefaults()
        {
            controlScale = 1f; controlOpacity = 0.6f; leftHanded = false; fixedStick = false;
            stickSensitivity = 1f; haptics = true; autoSprint = true; custom.Clear();
            try
            {
                PlayerPrefs.DeleteKey(PrefScale); PlayerPrefs.DeleteKey(PrefOpacity); PlayerPrefs.DeleteKey(PrefLeft); PlayerPrefs.DeleteKey(PrefFixed);
                PlayerPrefs.DeleteKey(PrefSens); PlayerPrefs.DeleteKey(PrefHaptics); PlayerPrefs.DeleteKey(PrefAuto); PlayerPrefs.DeleteKey(PrefPositions); PlayerPrefs.Save();
            }
            catch { }
            Rebuild();
        }

        /// <summary>Persist and apply the stick sensitivity and the button buzz.</summary>
        public void SetTuning(float sensitivity, bool buzz)
        {
            stickSensitivity = Core.TouchTuning.ClampSensitivity(sensitivity); haptics = buzz;
            try { PlayerPrefs.SetFloat(PrefSens, stickSensitivity); PlayerPrefs.SetInt(PrefHaptics, buzz ? 1 : 0); PlayerPrefs.Save(); } catch { }
            if (joystick != null) joystick.Sensitivity = stickSensitivity;
        }

        /// <summary>Persist and apply auto-sprint on a full stick push.</summary>
        public void SetAutoSprint(bool on)
        {
            autoSprint = on; if (!on) autoSprinting = false;
            try { PlayerPrefs.SetInt(PrefAuto, on ? 1 : 0); PlayerPrefs.Save(); } catch { }
        }

        // ---- moving the buttons ----
        public bool IsEditing => editing;

        /// <summary>While editing, the buttons are dragged instead of pressed.</summary>
        public void SetEditing(bool on)
        {
            editing = on;
            foreach (var b in buttons.Values) if (b != null) b.Locked = on;
            if (group != null) group.blocksRaycasts = Time.timeScale > 0f || on;
        }

        /// <summary>Where a button sits now, in right-handed terms (offset from the bottom-right corner of the safe area).</summary>
        public Vector2 ButtonPosition(Core.TouchButtonId id)
        {
            return custom.TryGetValue(id, out var p) ? p : Core.TouchLayout.DefaultPosition(id);
        }

        public bool IsMoved(Core.TouchButtonId id) { return custom.ContainsKey(id); }
        public int MovedCount => custom.Count;

        /// <summary>Size of the usable area in canvas units.</summary>
        Vector2 AreaSize() { return safeRect != null ? safeRect.rect.size : new Vector2(1920f, 1080f); }

        /// <summary>
        /// Puts a button at a position (right-handed terms), kept inside the screen and apart from the other buttons. Returns where it ended up.
        /// <paramref name="save"/> writes the layout (done when a drag ends, not on every frame of it).
        /// </summary>
        public Vector2 MoveButton(Core.TouchButtonId id, Vector2 wanted, bool save = true)
        {
            float size = Core.TouchLayout.DefaultSize(id) * controlScale;
            var pos = Core.TouchLayout.Clamp(wanted, size, AreaSize());
            foreach (Core.TouchButtonId other in System.Enum.GetValues(typeof(Core.TouchButtonId)))
            {
                if (other == id) continue;
                if (Core.TouchLayout.Overlaps(pos, size, ButtonPosition(other), Core.TouchLayout.DefaultSize(other) * controlScale)) { pos = ButtonPosition(id); break; }
            }
            if (pos == Core.TouchLayout.DefaultPosition(id)) custom.Remove(id); else custom[id] = pos;
            ApplyPosition(id);
            if (save) SaveLayout();
            return pos;
        }

        /// <summary>Put every button back where it started (the rest of the settings stay).</summary>
        public void ResetPositions()
        {
            custom.Clear();
            SaveLayout();
            foreach (Core.TouchButtonId id in System.Enum.GetValues(typeof(Core.TouchButtonId))) ApplyPosition(id);
        }

        public void SaveLayout()
        {
            try
            {
                if (custom.Count == 0) PlayerPrefs.DeleteKey(PrefPositions);
                else PlayerPrefs.SetString(PrefPositions, Core.TouchLayout.Serialize(custom));
                PlayerPrefs.Save();
            }
            catch { }
        }

        void ApplyPosition(Core.TouchButtonId id)
        {
            if (!buttons.TryGetValue(id, out var b) || b == null) return;
            var p = ButtonPosition(id);
            ((RectTransform)b.transform).anchoredPosition = new Vector2(leftHanded ? -p.x : p.x, p.y);
        }

        /// <summary>Called by a button while it is dragged: move it by a screen-pixel delta.</summary>
        public void DragButton(Core.TouchButtonId id, Vector2 screenDelta)
        {
            if (!editing || !buttons.TryGetValue(id, out var b) || b == null) return;
            float k = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            var rt = (RectTransform)b.transform;
            Vector2 now = rt.anchoredPosition + screenDelta / k;
            var wanted = new Vector2(leftHanded ? -now.x : now.x, now.y);
            // follow the finger freely (inside the screen) and only settle apart from the others when it is let go
            float size = Core.TouchLayout.DefaultSize(id) * controlScale;
            var clamped = Core.TouchLayout.Clamp(wanted, size, AreaSize());
            custom[id] = clamped;
            ApplyPosition(id);
        }

        /// <summary>A drag ended: if the button was dropped on another one it goes back to where it was picked up.</summary>
        public void DropButton(Core.TouchButtonId id, Vector2 pickedUpAt)
        {
            if (!editing) return;
            float size = Core.TouchLayout.DefaultSize(id) * controlScale;
            foreach (Core.TouchButtonId other in System.Enum.GetValues(typeof(Core.TouchButtonId)))
            {
                if (other == id) continue;
                if (Core.TouchLayout.Overlaps(ButtonPosition(id), size, ButtonPosition(other), Core.TouchLayout.DefaultSize(other) * controlScale))
                { custom[id] = pickedUpAt; ApplyPosition(id); break; }
            }
            if (custom.TryGetValue(id, out var p) && p == Core.TouchLayout.DefaultPosition(id)) custom.Remove(id);
            SaveLayout();
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
            group = canvasGo.AddComponent<CanvasGroup>();
            root = canvasGo;
            // Everything sits inside the device safe area so notches / punch-holes never cover a button.
            var safeGo = new GameObject("SafeArea", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)safeGo.transform;
            safeRect = rt;
            ApplySafeArea(rt, Screen.safeArea, Screen.width, Screen.height);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Joystick touch area: left 45% of the screen, lower 65%
            var area = new GameObject("JoystickArea", typeof(RectTransform), typeof(Image));
            area.transform.SetParent(rt, false);
            var art = (RectTransform)area.transform;
            art.anchorMin = leftHanded ? new Vector2(0.55f, 0f) : new Vector2(0f, 0f); art.anchorMax = leftHanded ? new Vector2(1f, 0.65f) : new Vector2(0.45f, 0.65f);
            art.offsetMin = art.offsetMax = Vector2.zero;
            area.GetComponent<Image>().color = new Color(0, 0, 0, 0); // invisible but raycastable
            joystick = area.AddComponent<Joystick>();
            joystick.Init(rt, controlScale, controlOpacity, Circle(), fixedStick, leftHanded);
            joystick.Sensitivity = stickSensitivity;

            float s = controlScale;
            buttons.Clear();
            shoot = Button(rt, font, "SHOOT", Core.TouchButtonId.Shoot, s, new Color(0.9f, 0.25f, 0.2f));
            pass = Button(rt, font, "PASS", Core.TouchButtonId.Pass, s, new Color(0.2f, 0.6f, 0.9f));
            tackle = Button(rt, font, "TACKLE", Core.TouchButtonId.Tackle, s, new Color(0.95f, 0.7f, 0.1f));
            switchBtn = Button(rt, font, "SWITCH", Core.TouchButtonId.Switch, s, new Color(0.5f, 0.5f, 0.55f));
            sprint = Button(rt, font, "SPRINT", Core.TouchButtonId.Sprint, s, new Color(0.2f, 0.75f, 0.4f));
            // a button saved on a bigger screen may no longer fit: pull it back inside (the saved value is kept as it was)
            foreach (var id in new System.Collections.Generic.List<Core.TouchButtonId>(custom.Keys))
            {
                var fitted = Core.TouchLayout.Clamp(custom[id], Core.TouchLayout.DefaultSize(id) * s, AreaSize());
                ((RectTransform)buttons[id].transform).anchoredPosition = new Vector2(leftHanded ? -fitted.x : fitted.x, fitted.y);
            }
            if (contextApplied) SetContext(context); // a rebuild (new size or layout) keeps the current context look
        }

        RectTransform safeRect;

        /// <summary>Number of buzzes asked for (phones actually vibrate; the editor only counts, which tests read).</summary>
        public static int BuzzCount { get; private set; }

        void Buzz()
        {
            if (!haptics || editing) return;
            BuzzCount++;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            try { Handheld.Vibrate(); } catch { }
#endif
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

        HoldButton Button(RectTransform parent, Font font, string label, Core.TouchButtonId id, float scale, Color color)
        {
            float size = Core.TouchLayout.DefaultSize(id) * scale;
            Vector2 pos = ButtonPosition(id);
            var go = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(HoldButton), typeof(ButtonDragHandle));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(leftHanded ? 0f : 1f, 0f); // bottom-right, or bottom-left when mirrored
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(leftHanded ? -pos.x : pos.x, pos.y);
            var hb = go.GetComponent<HoldButton>();
            hb.Locked = editing;
            if (id != Core.TouchButtonId.Sprint) hb.OnPressed = Buzz; // sprint is held, so a buzz on it would only be noise
            go.GetComponent<ButtonDragHandle>().Init(this, id);
            buttons[id] = hb;
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
        bool fixedBase;
        float sensitivity = 1f;

        /// <summary>Scales the thumb travel (see TouchTuning.Shape).</summary>
        public float Sensitivity { get { return sensitivity; } set { sensitivity = Core.TouchTuning.ClampSensitivity(value); } }

        public Vector2 Value { get; private set; }

        void Set(Vector2 v)
        {
            Value = v.magnitude < 0.12f ? Vector2.zero : Core.TouchTuning.Shape(v, sensitivity); // dead zone first, then the response
        }

        public bool IsFixed => fixedBase;
        public RectTransform BaseForTest => baseRect;

        public void Init(RectTransform canvas, float scale, float opacity, Sprite circle, bool fixedStyle = false, bool leftHanded = false)
        {
            canvasRect = canvas;
            radius = 110f * scale;
            fixedBase = fixedStyle;
            baseRect = MakeDisc("Base", circle, radius * 2f, new Color(1, 1, 1, opacity * 0.5f));
            knobRect = MakeDisc("Knob", circle, radius * 0.9f, new Color(1, 1, 1, Mathf.Clamp01(opacity + 0.2f)));
            if (fixedBase)
            {
                // always visible, in the corner under the thumb
                float x = radius + 110f, y = radius + 110f;
                var anchor = new Vector2(leftHanded ? 1f : 0f, 0f);
                baseRect.anchorMin = baseRect.anchorMax = knobRect.anchorMin = knobRect.anchorMax = anchor;
                baseRect.anchoredPosition = knobRect.anchoredPosition = new Vector2(leftHanded ? -x : x, y);
            }
            else
            {
                baseRect.gameObject.SetActive(false);
                knobRect.gameObject.SetActive(false);
            }
        }

        void DriveFixed(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, e.position, null, out var d);
            Vector2 clamped = Vector2.ClampMagnitude(d, radius);
            knobRect.anchoredPosition = baseRect.anchoredPosition + clamped;
            Set(clamped / radius);
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
            if (fixedBase) { DriveFixed(e); return; }
            var p = ToCanvas(e);
            baseRect.anchoredPosition = p;
            knobRect.anchoredPosition = p;
            baseRect.gameObject.SetActive(true);
            knobRect.gameObject.SetActive(true);
            Value = Vector2.zero;
        }

        public void OnDrag(PointerEventData e)
        {
            if (fixedBase) { DriveFixed(e); return; }
            Vector2 delta = ToCanvas(e) - baseRect.anchoredPosition;
            Vector2 clamped = Vector2.ClampMagnitude(delta, radius);
            knobRect.anchoredPosition = baseRect.anchoredPosition + clamped;
            Set(clamped / radius); // dead zone, then sensitivity
        }

        public void OnPointerUp(PointerEventData e)
        {
            Value = Vector2.zero;
            HideStick();
        }

        void HideStick()
        {
            if (fixedBase) { if (knobRect != null && baseRect != null) knobRect.anchoredPosition = baseRect.anchoredPosition; return; }
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
        /// <summary>While the buttons are being moved they must not act.</summary>
        public bool Locked;
        /// <summary>Called on a real press (the buzz).</summary>
        public System.Action OnPressed;

        public void OnPointerDown(PointerEventData e) { Press(); }
        public void OnPointerUp(PointerEventData e) { Release(); }
        public void OnPointerExit(PointerEventData e) { if (Held) Release(); }
        void OnDisable() { Held = false; }

        public void Press()
        {
            if (Locked) return;
            Held = true; pressLatch = true;
            if (OnPressed != null) OnPressed();
        }
        public void Release() { if (!Held) return; Held = false; releaseLatch = true; }

        public bool ConsumePress() { var v = pressLatch; pressLatch = false; return v; }
        public bool ConsumeRelease() { var v = releaseLatch; releaseLatch = false; return v; }
    }

    /// <summary>Lets a button be dragged to a new spot while the controls are in edit mode; does nothing otherwise.</summary>
    public class ButtonDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        TouchControls owner;
        Core.TouchButtonId id;
        Vector2 pickedUpAt;

        public void Init(TouchControls o, Core.TouchButtonId buttonId) { owner = o; id = buttonId; }

        public void OnBeginDrag(PointerEventData e) { if (owner != null && owner.IsEditing) pickedUpAt = owner.ButtonPosition(id); }
        public void OnDrag(PointerEventData e) { if (owner != null) owner.DragButton(id, e.delta); }
        public void OnEndDrag(PointerEventData e) { if (owner != null) owner.DropButton(id, pickedUpAt); }
    }
}
