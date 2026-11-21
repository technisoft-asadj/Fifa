using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FootballSim.UI
{
    /// <summary>
    /// The widgets of the designed screens: the framed screen with its round back and home buttons, round icon buttons, sliders, ON/OFF
    /// switches, arrow selectors, crests and cards. Built once, in the same 1920x1080 reference space as the rest of <see cref="UIKit"/>.
    /// </summary>
    public static partial class UIKit
    {
        public static readonly Color PanelDark = new Color(0.03f, 0.08f, 0.2f, 0.96f);
        public static readonly Color Row = new Color(0.07f, 0.16f, 0.36f, 0.85f);
        public static readonly Color Field = new Color(0.02f, 0.05f, 0.12f, 0.9f);
        public static readonly Color ButtonBlue = new Color(0.12f, 0.36f, 0.86f, 1f);

        /// <summary>
        /// A framed screen: dark navy card with a glowing blue rim, a round back button at the top left, a centred title with an optional icon
        /// and (optionally) a round home button at the top right. Returns the inner rectangle; <paramref name="root"/> is the object to show or hide.
        /// </summary>
        public static RectTransform Screen(Transform parent, string name, Vector2 size, string title, UIIcon? icon, UnityAction onBack, UnityAction onHome, out GameObject root, float titleSize = 50f, bool opaque = false)
        {
            var holder = Rect(parent, name, Center, Vector2.zero, size + new Vector2(24f, 24f));
            root = holder.gameObject;
            // the screen behind is dimmed (and its buttons cannot be pressed), then a soft glow sits behind the rim
            var dim = Image(holder, "Dim", UITheme.Rounded, new Color(0f, 0.02f, 0.07f, 0.9f), false, true);
            Place(dim.rectTransform, Center, Vector2.zero, new Vector2(6000f, 4000f));
            var glow = Image(holder, "Glow", UITheme.Rounded, new Color(0.15f, 0.5f, 1f, 0.22f), true, false);
            Place(glow.rectTransform, Center, Vector2.zero, size + new Vector2(24f, 24f));
            var rim = Image(holder, "Rim", UITheme.Rounded, new Color(0.22f, 0.58f, 1f, 1f), true, true);
            Place(rim.rectTransform, Center, Vector2.zero, size);
            var inner = Image(rim.transform, "Card", UITheme.Rounded, opaque ? new Color(PanelDark.r, PanelDark.g, PanelDark.b, 1f) : PanelDark, true, false);
            inner.rectTransform.anchorMin = Vector2.zero; inner.rectTransform.anchorMax = Vector2.one;
            inner.rectTransform.offsetMin = new Vector2(3f, 3f); inner.rectTransform.offsetMax = new Vector2(-3f, -3f);
            var card = inner.rectTransform;

            if (onBack != null) RoundButton(card, "Back", UIIcon.ArrowLeft, TopLeft, new Vector2(26f, -22f), new Vector2(74f, 74f), onBack);
            if (onHome != null) RoundButton(card, "Home", UIIcon.Home, TopRight, new Vector2(-26f, -22f), new Vector2(74f, 74f), onHome);
            if (!string.IsNullOrEmpty(title))
            {
                float w = title.Length * titleSize * 0.56f;
                var t = TextAt(card, "Title", title, titleSize, Color.white, new Vector2(0.5f, 1f), new Vector2(icon.HasValue ? 36f : 0f, -22f), new Vector2(w + 40f, 74f), TextAlignmentOptions.Center, true);
                if (icon.HasValue)
                {
                    var ic = Image(card, "TitleIcon", UITheme.Icon(icon.Value), Gold, false, false);
                    Place(ic.rectTransform, new Vector2(0.5f, 1f), new Vector2(36f - w * 0.5f - 52f, -28f), new Vector2(62f, 62f));
                }
            }
            return card;
        }

        public static readonly Color Gold = new Color(1f, 0.78f, 0.12f, 1f);

        /// <summary>A round blue button holding an icon (back, home, settings).</summary>
        public static Button RoundButton(Transform parent, string name, UIIcon icon, Vector2 anchor, Vector2 pos, Vector2 size, UnityAction onClick, Color? tint = null)
        {
            var rim = Image(parent, name + "Button", UITheme.Circle, new Color(0.3f, 0.62f, 1f, 1f), false, true);
            Place(rim.rectTransform, anchor, pos, size);
            var body = Image(rim.transform, "Body", UITheme.Circle, tint ?? new Color(0.1f, 0.3f, 0.78f, 1f), false, false);
            body.rectTransform.anchorMin = Vector2.zero; body.rectTransform.anchorMax = Vector2.one; body.rectTransform.offsetMin = new Vector2(3f, 3f); body.rectTransform.offsetMax = new Vector2(-3f, -3f);
            var ic = Image(body.transform, "Icon", UITheme.Icon(icon), Color.white, false, false);
            ic.rectTransform.anchorMin = new Vector2(0.2f, 0.2f); ic.rectTransform.anchorMax = new Vector2(0.8f, 0.8f); ic.rectTransform.offsetMin = ic.rectTransform.offsetMax = Vector2.zero;
            Edge(rim, tint ?? new Color(0.1f, 0.3f, 0.78f, 1f));
            Gloss(body.transform, true);
            var btn = rim.gameObject.AddComponent<Button>(); btn.targetGraphic = rim;
            var c = btn.colors; c.highlightedColor = new Color(1.15f, 1.15f, 1.15f); c.pressedColor = new Color(0.75f, 0.75f, 0.75f); c.fadeDuration = 0.05f; btn.colors = c;
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        /// <summary>A rounded button with a label and (optionally) an icon beside it. Gold for the main action, blue otherwise.</summary>
        public static Button ActionButton(Transform parent, string label, UIIcon? icon, bool gold, Vector2 anchor, Vector2 pos, Vector2 size, UnityAction onClick, float fontSize = 40f)
        {
            var b = SmallButton(parent, label, gold ? Gold : ButtonBlue, anchor, pos, size, onClick, fontSize);
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (gold) t.color = UITheme.GoldText;
            if (icon.HasValue)
            {
                // the icon sits at the left end and the label gets the rest (and shrinks to fit), so a longer label such as "New Tournament" never runs into it
                float s = size.y * 0.5f;
                var ic = Image(b.transform, "Icon", UITheme.Icon(icon.Value), gold ? UITheme.GoldText : Color.white, false, false);
                Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(size.y * 0.32f, 0f), new Vector2(s, s));
                t.rectTransform.offsetMin = new Vector2(size.y * 0.32f + s + 8f, 0f); t.rectTransform.offsetMax = new Vector2(-12f, 0f);
            }
            return b;
        }

        /// <summary>A "<  value  >" selector: two small square arrow buttons around a value text. Returns the value text.</summary>
        public static TextMeshProUGUI Selector(Transform parent, string name, Vector2 anchor, Vector2 pos, float width, float height, UnityAction left, UnityAction right, float fontSize = 40f, bool bold = true)
        {
            // <paramref name="pos"/> is where the selector's CENTRE sits, measured from the anchor; boxes pivot on the anchor, so shift each by its own size
            Vector2 Edge(Vector2 centre, Vector2 size) { return centre + new Vector2((anchor.x - 0.5f) * size.x, (anchor.y - 0.5f) * size.y); }
            var l = Arrow(parent, name + "Prev", anchor, Edge(pos + new Vector2(-width * 0.5f + height * 0.5f, 0f), new Vector2(height, height)), height, true, left);
            var r = Arrow(parent, name + "Next", anchor, Edge(pos + new Vector2(width * 0.5f - height * 0.5f, 0f), new Vector2(height, height)), height, false, right);
            var box = new Vector2(width - height * 2f - 10f, height);
            return TextAt(parent, name, "", fontSize, Color.white, anchor, Edge(pos, box), box, TextAlignmentOptions.Center, bold);
        }

        /// <summary>One small blue square button with a chevron (pointing left or right).</summary>
        public static Button Arrow(Transform parent, string name, Vector2 anchor, Vector2 pos, float size, bool pointLeft, UnityAction onClick)
        {
            var img = Image(parent, name + "Button", UITheme.Rounded, ButtonBlue, true, true);
            Place(img.rectTransform, anchor, pos, new Vector2(size, size)); // pivot = anchor, so a centred anchor places the centre
            var btn = img.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
            var c = btn.colors; c.highlightedColor = new Color(1.15f, 1.15f, 1.15f); c.pressedColor = new Color(0.75f, 0.75f, 0.75f); c.fadeDuration = 0.05f; btn.colors = c;
            if (onClick != null) btn.onClick.AddListener(onClick);
            Raise(img, ButtonBlue);
            var ic = Image(img.transform, "Chevron", UITheme.Icon(UIIcon.Chevron), Color.white, false, false);
            ic.rectTransform.anchorMin = new Vector2(0.18f, 0.18f); ic.rectTransform.anchorMax = new Vector2(0.82f, 0.82f); ic.rectTransform.offsetMin = ic.rectTransform.offsetMax = Vector2.zero;
            if (pointLeft) ic.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            return btn;
        }

        /// <summary>A dark rounded row/card background.</summary>
        public static RectTransform Card(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color? color = null)
        {
            var img = Image(parent, name, UITheme.Rounded, color ?? Row, true, false);
            Place(img.rectTransform, anchor, pos, size);
            return img.rectTransform;
        }

        /// <summary>A real, draggable slider (0..1) with a blue fill, a round handle and the value in percent beside it.</summary>
        public static Slider SliderRow(Transform parent, string name, Vector2 anchor, Vector2 pos, float width, float value, UnityAction<float> onChange, out TextMeshProUGUI valueText)
        {
            var go = new GameObject(name + "Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; Place(rt, anchor, pos, new Vector2(width, 44f));
            var track = Image(rt, "Track", UITheme.Rounded, new Color(0.04f, 0.09f, 0.2f, 1f), true, true);
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f); track.rectTransform.anchorMax = new Vector2(1f, 0.5f); track.rectTransform.offsetMin = new Vector2(0f, -8f); track.rectTransform.offsetMax = new Vector2(0f, 8f);
            var fillArea = new GameObject("FillArea", typeof(RectTransform)); fillArea.transform.SetParent(rt, false);
            var fa = (RectTransform)fillArea.transform; fa.anchorMin = new Vector2(0f, 0.5f); fa.anchorMax = new Vector2(1f, 0.5f); fa.offsetMin = new Vector2(0f, -8f); fa.offsetMax = new Vector2(-22f, 8f);
            var fill = Image(fa, "Fill", UITheme.Rounded, new Color(0.2f, 0.58f, 1f, 1f), true, false);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one; fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = new Vector2(22f, 0f);
            var handleArea = new GameObject("HandleArea", typeof(RectTransform)); handleArea.transform.SetParent(rt, false);
            var ha = (RectTransform)handleArea.transform; ha.anchorMin = new Vector2(0f, 0.5f); ha.anchorMax = new Vector2(1f, 0.5f); ha.offsetMin = new Vector2(17f, -17f); ha.offsetMax = new Vector2(-17f, 17f);
            var handle = Image(ha, "Handle", UITheme.Circle, Color.white, false, true);
            handle.rectTransform.sizeDelta = new Vector2(34f, 0f); // the slider stretches the handle's vertical anchors over the 34-high area
            var s = go.GetComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle; s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f; s.maxValue = 1f; s.wholeNumbers = false;
            valueText = TextAt(parent, name + "Value", Mathf.RoundToInt(value * 100f) + "%", 34f, Color.white, anchor, pos + new Vector2(width + 30f, 0f), new Vector2(120f, 44f), TextAlignmentOptions.Left, true);
            var vt = valueText;
            s.SetValueWithoutNotify(value);
            s.onValueChanged.AddListener(v => { vt.text = Mathf.RoundToInt(v * 100f) + "%"; if (onChange != null) onChange(v); });
            return s;
        }

        /// <summary>An ON/OFF switch: a blue pill with a white knob; tapping flips it. <see cref="SetSwitch"/> changes it from code.</summary>
        public static Button Switch(Transform parent, string name, Vector2 anchor, Vector2 pos, bool on, UnityAction<bool> onChange)
        {
            var img = Image(parent, name + "Switch", UITheme.Rounded, Color.white, true, true);
            Place(img.rectTransform, anchor, pos, new Vector2(150f, 62f));
            var label = Text(img.transform, "Label", "ON", 30f, Color.white, TextAlignmentOptions.Center, true);
            var knob = Image(img.transform, "Knob", UITheme.Circle, Color.white, false, false);
            knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); knob.rectTransform.sizeDelta = new Vector2(50f, 50f);
            var btn = img.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
            var sw = img.gameObject.AddComponent<SwitchState>(); sw.Init(img, label, knob.rectTransform, on);
            btn.onClick.AddListener(() => { sw.Set(!sw.On); if (onChange != null) onChange(sw.On); });
            return btn;
        }

        public static void SetSwitch(Button b, bool on) { if (b != null) { var s = b.GetComponent<SwitchState>(); if (s != null) s.Set(on); } }
        public static bool SwitchOn(Button b) { var s = b != null ? b.GetComponent<SwitchState>() : null; return s != null && s.On; }

        /// <summary>The state of one <see cref="Switch"/>.</summary>
        public class SwitchState : MonoBehaviour
        {
            Image bg; TextMeshProUGUI label; RectTransform knob;
            public bool On { get; private set; }
            public void Init(Image background, TextMeshProUGUI text, RectTransform knobRect, bool on) { bg = background; label = text; knob = knobRect; Set(on); }
            public void Set(bool on)
            {
                On = on;
                bg.color = on ? new Color(0.16f, 0.5f, 1f, 1f) : new Color(0.22f, 0.28f, 0.4f, 1f);
                label.text = on ? "ON" : "OFF";
                label.rectTransform.offsetMin = new Vector2(on ? 0f : 52f, 0f); label.rectTransform.offsetMax = new Vector2(on ? -52f : 0f, 0f);
                knob.anchoredPosition = new Vector2(on ? 44f : -44f, 0f);
            }
        }

        /// <summary>A club crest: a shield in the club's colours with a star on it (there is no real badge art, so none is imitated).</summary>
        public static RectTransform Crest(Transform parent, string name, Color main, Color trim, Vector2 anchor, Vector2 pos, float size)
        {
            var holder = Rect(parent, name, anchor, pos, new Vector2(size, size));
            var outer = Image(holder, "Outer", UITheme.Icon(UIIcon.Shield), new Color(1f, 0.82f, 0.25f, 1f), false, false);
            outer.rectTransform.anchorMin = Vector2.zero; outer.rectTransform.anchorMax = Vector2.one; outer.rectTransform.offsetMin = outer.rectTransform.offsetMax = Vector2.zero;
            var body = Image(holder, "Body", UITheme.Icon(UIIcon.Shield), main, false, false);
            body.rectTransform.anchorMin = new Vector2(0.1f, 0.1f); body.rectTransform.anchorMax = new Vector2(0.9f, 0.9f); body.rectTransform.offsetMin = body.rectTransform.offsetMax = Vector2.zero;
            var emblem = Image(holder, "Emblem", UITheme.Icon(UIIcon.Star), trim, false, false);
            emblem.rectTransform.anchorMin = new Vector2(0.3f, 0.34f); emblem.rectTransform.anchorMax = new Vector2(0.7f, 0.74f); emblem.rectTransform.offsetMin = emblem.rectTransform.offsetMax = Vector2.zero;
            var crown = Image(holder, "Crown", UITheme.Icon(UIIcon.Crown), new Color(1f, 0.82f, 0.25f, 1f), false, false);
            crown.rectTransform.anchorMin = new Vector2(0.3f, 0.86f); crown.rectTransform.anchorMax = new Vector2(0.7f, 1.08f); crown.rectTransform.offsetMin = crown.rectTransform.offsetMax = Vector2.zero;
            var art = Image(holder, "Art", null, Color.white, false, false);
            art.rectTransform.anchorMin = new Vector2(-0.02f, -0.02f); art.rectTransform.anchorMax = new Vector2(1.02f, 1.02f); art.rectTransform.offsetMin = art.rectTransform.offsetMax = Vector2.zero;
            art.preserveAspect = true; art.gameObject.SetActive(false);
            return holder;
        }

        /// <summary>Shows the club's own crest picture; with none (the plain team) the coloured shield is shown instead.</summary>
        public static void SetCrest(RectTransform crest, int club, Color main, Color trim)
        {
            if (crest == null) return;
            var sprite = UITheme.ClubCrest(club);
            var art = crest.Find("Art");
            if (art != null) { art.gameObject.SetActive(sprite != null); var im = art.GetComponent<UnityEngine.UI.Image>(); im.sprite = sprite; }
            foreach (string n in new[] { "Outer", "Body", "Emblem", "Crown" }) { var t = crest.Find(n); if (t != null) t.gameObject.SetActive(sprite == null); }
        }
    }
}
