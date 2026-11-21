using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballSim.UI
{
    /// <summary>
    /// Builders for the reusable pieces of the redesigned screens (glass panel, menu button, pill, text, bar). All positions are in the
    /// 1920x1080 reference space of the canvas, measured from the chosen anchor. Nothing here has an Update: a screen is built once and
    /// only changes when a value changes.
    /// </summary>
    public static partial class UIKit
    {
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f), TopLeft = new Vector2(0f, 1f), TopRight = new Vector2(1f, 1f), BottomLeft = new Vector2(0f, 0f), BottomRight = new Vector2(1f, 0f);

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool sliced = true, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = raycast;
            img.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            return img;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        /// <summary>Dark glass card with an electric-blue rim. Returns the inner rectangle to parent the contents to.</summary>
        public static RectTransform GlassPanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, bool blocksTouches = true, float alpha = 0.92f)
        {
            var rim = Image(parent, name + "Rim", UITheme.Rounded, new Color(UITheme.Blue.r, UITheme.Blue.g, UITheme.Blue.b, 0.85f), true, blocksTouches);
            Place(rim.rectTransform, anchor, pos, size);
            var inner = Image(rim.transform, name, UITheme.Rounded, new Color(UITheme.Glass.r, UITheme.Glass.g, UITheme.Glass.b, alpha), true, false);
            inner.rectTransform.anchorMin = Vector2.zero; inner.rectTransform.anchorMax = Vector2.one;
            inner.rectTransform.offsetMin = new Vector2(3f, 3f); inner.rectTransform.offsetMax = new Vector2(-3f, -3f);
            return inner.rectTransform;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Text placed at an anchor with its own box (not stretched).</summary>
        public static TextMeshProUGUI TextAt(Transform parent, string name, string text, float size, Color color, Vector2 anchor, Vector2 pos, Vector2 box, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            var t = Text(parent, name, text, size, color, align, bold);
            Place(t.rectTransform, anchor, pos, box);
            return t;
        }

        /// <summary>
        /// Gives a button face a raised, glossy look: a darker "thickness" edge under it (a hard drop copy of the shape, no extra object) and a
        /// bright glass highlight over its top half. <paramref name="circle"/> uses the round sprite for the highlight. Safe to call once per face.
        /// </summary>
        public static void Raise(Image face, Color tint, bool circle = false)
        {
            Edge(face, tint);
            Gloss(face.transform, circle);
        }

        /// <summary>The darker thickness under a button face.</summary>
        public static void Edge(Image face, Color tint)
        {
            var rt = face.rectTransform;
            float h = Mathf.Max(24f, rt.rect.height > 1f ? rt.rect.height : rt.sizeDelta.y);
            float lip = Mathf.Clamp(h * 0.085f, 4f, 9f);
            var edge = face.gameObject.AddComponent<Shadow>();
            edge.effectDistance = new Vector2(0f, -lip); edge.useGraphicAlpha = false;
            edge.effectColor = new Color(tint.r * 0.30f, tint.g * 0.32f, tint.b * 0.42f, 1f);
        }

        /// <summary>The glass highlight over the top half of a button face.</summary>
        public static void Gloss(Transform face, bool circle)
        {
            var gloss = Image(face, "Gloss", circle ? UITheme.Circle : UITheme.Rounded, new Color(1f, 1f, 1f, circle ? 0.26f : 0.20f), !circle, false);
            gloss.rectTransform.anchorMin = new Vector2(circle ? 0.2f : 0.025f, 0.52f); gloss.rectTransform.anchorMax = new Vector2(circle ? 0.8f : 0.975f, circle ? 0.93f : 0.95f);
            gloss.rectTransform.offsetMin = new Vector2(circle ? 0f : 4f, 0f); gloss.rectTransform.offsetMax = new Vector2(circle ? 0f : -4f, circle ? 0f : -3f);
            gloss.transform.SetAsFirstSibling(); // under the icon and label
        }

        /// <summary>
        /// Menu button: a gradient-tinted rounded rectangle, a round icon badge on the left and a bold label. <paramref name="dark"/> text is
        /// for the gold button. Returns the button root so callers can read its label (<c>GetComponentInChildren&lt;TMP_Text&gt;</c>).
        /// </summary>
        public static Button MenuButton(Transform parent, string label, UIIcon icon, Color color, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, bool dark = false, float fontSize = 46f)
        {
            var img = Image(parent, label + "Button", UITheme.Rounded, color, true, true);
            Place(img.rectTransform, anchor, pos, size);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f); colors.pressedColor = new Color(0.78f, 0.78f, 0.78f); colors.fadeDuration = 0.05f;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            Raise(img, color);

            float badge = size.y * 0.66f;
            var badgeImg = Image(img.transform, "Badge", UITheme.Circle, dark ? new Color(0.35f, 0.2f, 0f, 0.9f) : new Color(0f, 0f, 0f, 0.28f), false, false);
            Place(badgeImg.rectTransform, new Vector2(0f, 0.5f), new Vector2(size.y * 0.2f, 0f), new Vector2(badge, badge));
            var ic = Image(badgeImg.transform, "Icon", UITheme.Icon(icon), Color.white, false, false);
            ic.rectTransform.anchorMin = Vector2.zero; ic.rectTransform.anchorMax = Vector2.one; ic.rectTransform.offsetMin = new Vector2(badge * 0.16f, badge * 0.16f); ic.rectTransform.offsetMax = new Vector2(-badge * 0.16f, -badge * 0.16f);

            var t = Text(img.transform, "Label", label, fontSize, dark ? UITheme.GoldText : Color.white, TextAlignmentOptions.Center, true);
            t.rectTransform.offsetMin = new Vector2(size.y * 1.0f, 0f); t.rectTransform.offsetMax = new Vector2(-size.y * 0.3f, 0f);
            return btn;
        }

        /// <summary>A plain rounded button without icon (for + - and Back).</summary>
        public static Button SmallButton(Transform parent, string label, Color color, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, float fontSize = 40f)
        {
            var img = Image(parent, label + "Button", UITheme.Rounded, color, true, true);
            Place(img.rectTransform, anchor, pos, size);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f); colors.pressedColor = new Color(0.78f, 0.78f, 0.78f); colors.fadeDuration = 0.05f;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            Raise(img, color);
            var t = Text(img.transform, "Label", label, fontSize, Color.white, TextAlignmentOptions.Center, true);
            t.enableAutoSizing = true; t.fontSizeMin = Mathf.Max(14f, fontSize * 0.55f); t.fontSizeMax = fontSize; // long labels shrink instead of spilling out
            t.rectTransform.offsetMin = new Vector2(10f, 0f); t.rectTransform.offsetMax = new Vector2(-10f, 0f);
            return btn;
        }

        /// <summary>A small dark pill (rounded bar) with a tinted icon on the left and a value; returns the value text.</summary>
        public static TextMeshProUGUI Pill(Transform parent, string name, UIIcon icon, Color iconColor, string value, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var bg = Image(parent, name, UITheme.Rounded, new Color(0f, 0.02f, 0.08f, 0.75f), true, false);
            Place(bg.rectTransform, anchor, pos, size);
            var ic = Image(bg.transform, "Icon", UITheme.Icon(icon), iconColor, false, false);
            Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(size.y * 0.15f, 0f), new Vector2(size.y * 0.7f, size.y * 0.7f));
            var t = Text(bg.transform, "Value", value, size.y * 0.5f, Color.white, TextAlignmentOptions.Center, true);
            t.rectTransform.offsetMin = new Vector2(size.y * 0.9f, 0f); t.rectTransform.offsetMax = new Vector2(-size.y * 0.2f, 0f);
            return t;
        }

        /// <summary>A thin progress bar; <see cref="SetBar"/> changes the fill.</summary>
        public static RectTransform Bar(Transform parent, string name, Color fill, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var bg = Image(parent, name, UITheme.Rounded, new Color(0f, 0f, 0f, 0.55f), true, false);
            Place(bg.rectTransform, anchor, pos, size);
            var f = Image(bg.transform, "Fill", UITheme.Rounded, fill, true, false);
            f.rectTransform.anchorMin = Vector2.zero; f.rectTransform.anchorMax = new Vector2(0f, 1f);
            f.rectTransform.offsetMin = Vector2.zero; f.rectTransform.offsetMax = Vector2.zero;
            return f.rectTransform;
        }

        public static void SetBar(RectTransform fill, float fraction)
        {
            if (fill != null) fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }
    }
}
