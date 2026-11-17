using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FootballSim.Core;

namespace FootballSim.UI
{
    /// <summary>
    /// The first screens after launch: a short splash, then (only the very first time) a name screen with a "Play as guest" choice. There
    /// is no account, sign-in or server behind it: the name is stored on the phone and shown on the menu. A scene reload (rematch,
    /// restart) does not show the splash again.
    /// </summary>
    public partial class MatchHUD
    {
        /// <summary>True once the splash has been shown in this app run. Tests set it so the scene opens straight on the main menu.</summary>
        public static bool SplashShown;
        public static float SplashSeconds = 1.8f; // static only so tests can hold the splash open on a slow machine

        GameObject splashPanel, namePanel;
        RectTransform splashFill;
        InputField nameField;
        TextMeshProUGUI nameHint;
        float splashStartedAt;

        public bool SplashOpen => splashPanel != null && splashPanel.activeSelf;
        public bool NameOpen => namePanel != null && namePanel.activeSelf;

        void BuildSplashAndName(RectTransform root)
        {
            // splash: full-screen gradient, title, loading bar; a tap skips it
            var splash = UIKit.Stretch(root, "Splash");
            splashPanel = splash.gameObject;
            var bg = UIKit.Image(splash, "Background", UITheme.Gradient, Color.white, false, true);
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one; bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;
            var skip = bg.gameObject.AddComponent<Button>(); skip.targetGraphic = bg; skip.transition = Selectable.Transition.None; skip.onClick.AddListener(FinishSplash);
            UIKit.TextAt(splash, "Top", "FOOTBALL", 190f, UITheme.Gold, UIKit.Center, new Vector2(0f, 150f), new Vector2(1400f, 210f), TextAlignmentOptions.Center, true);
            UIKit.TextAt(splash, "Bottom", "SIMULATOR", 120f, Color.white, UIKit.Center, new Vector2(0f, -20f), new Vector2(1400f, 140f), TextAlignmentOptions.Center, true);
            var ball = UIKit.Image(splash, "Ball", UITheme.Icon(UIIcon.Ball), Color.white, false, false);
            ball.rectTransform.anchorMin = ball.rectTransform.anchorMax = UIKit.Center; ball.rectTransform.pivot = UIKit.Center;
            ball.rectTransform.anchoredPosition = new Vector2(0f, -190f); ball.rectTransform.sizeDelta = new Vector2(150f, 150f);
            splashFill = UIKit.Bar(splash, "Loading", UITheme.Gold, UIKit.Center, new Vector2(-300f, -340f), new Vector2(600f, 18f));
            UIKit.TextAt(splash, "Loading text", "Loading...", 28f, UITheme.Muted, UIKit.Center, new Vector2(0f, -385f), new Vector2(600f, 40f));
            splashPanel.SetActive(false);

            // name: full-screen, one glass card
            var np = UIKit.Stretch(root, "NameScreen");
            namePanel = np.gameObject;
            var nbg = UIKit.Image(np, "Background", UITheme.Gradient, Color.white, false, true);
            nbg.rectTransform.anchorMin = Vector2.zero; nbg.rectTransform.anchorMax = Vector2.one; nbg.rectTransform.offsetMin = nbg.rectTransform.offsetMax = Vector2.zero;
            var card = UIKit.GlassPanel(np, "NameCard", UIKit.Center, Vector2.zero, new Vector2(1000f, 700f), true, 1f);
            UIKit.TextAt(card, "Title", "WELCOME", 72f, UITheme.Gold, UIKit.Center, new Vector2(0f, 260f), new Vector2(800f, 96f), TextAlignmentOptions.Center, true);
            UIKit.TextAt(card, "Sub", "Pick a name, or just play as a guest.", 34f, UITheme.Muted, UIKit.Center, new Vector2(0f, 180f), new Vector2(900f, 50f));
            nameField = BuildNameField(card, new Vector2(0f, 60f), new Vector2(640f, 96f));
            nameHint = UIKit.TextAt(card, "Hint", "Letters and numbers, up to 16. Saved on this phone only.", 26f, new Color(1f, 1f, 1f, 0.55f), UIKit.Center, new Vector2(0f, -20f), new Vector2(900f, 40f));
            UIKit.SmallButton(card, "CONTINUE", UITheme.Gold, UIKit.Center, new Vector2(0f, -130f), new Vector2(520f, 96f), () => ConfirmName(nameField.text), 44f)
                .GetComponentInChildren<TextMeshProUGUI>().color = UITheme.GoldText;
            UIKit.SmallButton(card, "Play as guest", UITheme.BlueDark, UIKit.Center, new Vector2(0f, -250f), new Vector2(520f, 84f), () => ConfirmName(""), 38f);
            namePanel.SetActive(false);
        }

        InputField BuildNameField(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("NameField", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.sprite = UITheme.Rounded; img.type = Image.Type.Sliced; img.color = new Color(0.14f, 0.19f, 0.30f);
            var inner = size - new Vector2(40, 10);
            var text = Label("Text", go.transform, 46, TextAnchor.MiddleLeft, Vector2.zero, inner); text.supportRichText = false;
            var placeholder = Label("Placeholder", go.transform, 46, TextAnchor.MiddleLeft, Vector2.zero, inner);
            placeholder.text = "Your name"; placeholder.fontStyle = FontStyle.Italic; placeholder.color = new Color(1, 1, 1, 0.35f);
            var field = go.GetComponent<InputField>();
            field.targetGraphic = img; field.textComponent = text; field.placeholder = placeholder;
            field.lineType = InputField.LineType.SingleLine;
            field.characterLimit = PlayerProfile.MaxNameLength;
            field.keyboardType = TouchScreenKeyboardType.Default;
            return field;
        }

        void ShowSplash()
        {
            SplashShown = true;
            Time.timeScale = 0f;
            mainMenu.SetActive(false); startPanel.SetActive(false);
            splashStartedAt = Time.unscaledTime;
            UIKit.SetBar(splashFill, 0f);
            splashPanel.SetActive(true);
        }

        void UpdateSplash()
        {
            if (!SplashOpen) return;
            float t = (Time.unscaledTime - splashStartedAt) / SplashSeconds;
            UIKit.SetBar(splashFill, t);
            if (t >= 1f) FinishSplash();
        }

        /// <summary>End the splash (it also ends on a tap or Back): the name screen the first time, otherwise the main menu.</summary>
        public void FinishSplash()
        {
            if (!SplashOpen) return;
            splashPanel.SetActive(false);
            if (PlayerProfile.HasName) ShowMainMenu(); else OpenName();
        }

        public void OpenName()
        {
            if (!CareerOpen) mainMenu.SetActive(false); // from the Career screen the menu stays underneath
            nameField.text = PlayerProfile.HasName ? PlayerProfile.Name : "";
            namePanel.SetActive(true);
        }

        /// <summary>Closes the name screen without changing anything; back to the Career screen or the main menu.</summary>
        public void CloseName()
        {
            namePanel.SetActive(false);
            if (!CareerOpen) ShowMainMenu();
        }

        /// <summary>Saves the name (empty means Guest); back to the Career screen it was opened from, or on to the main menu.</summary>
        public string ConfirmName(string raw)
        {
            string saved = PlayerProfile.SetName(raw);
            namePanel.SetActive(false);
            if (CareerOpen) { careerText.text = CareerStats(); RefreshMainMenu(); }
            else ShowMainMenu();
            return saved;
        }
    }
}
