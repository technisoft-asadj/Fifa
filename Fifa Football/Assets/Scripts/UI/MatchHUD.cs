using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FootballSim.Match;

namespace FootballSim.UI
{
    /// <summary>
    /// Code-built uGUI for the match: scoreboard, event banner, start screen (match length) and full-time screen.
    /// The simulation is paused (timeScale 0) while a menu is showing.
    /// </summary>
    public class MatchHUD : MonoBehaviour
    {
        [SerializeField] MatchManager match;

        Font font;
        Text scoreText, clockText, bannerText, resultText, durationLabel;
        GameObject startPanel, fullTimePanel;
        float bannerTimer;
        float selectedMinutes = 5f;
        static readonly Color Panel = new Color(0.05f, 0.08f, 0.12f, 0.88f);
        static readonly Color Accent = new Color(0.1f, 0.65f, 0.3f, 1f);

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildCanvas();
        }

        void OnEnable()
        {
            if (match == null) return;
            match.GoalScored += OnGoal;
            match.FullTime += OnFullTime;
            match.RestartAwarded += OnRestart;
        }

        void OnDisable()
        {
            if (match == null) return;
            match.GoalScored -= OnGoal;
            match.FullTime -= OnFullTime;
            match.RestartAwarded -= OnRestart;
        }

        void Start() { ShowStart(); }
        void OnDestroy() { Time.timeScale = 1f; }

        void Update()
        {
            if (match == null) return;
            scoreText.text = $"HOME  {match.HomeScore}  -  {match.AwayScore}  AWAY";
            clockText.text = $"{Mathf.FloorToInt(match.GameMinute):00}:00";
            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.unscaledDeltaTime;
                if (bannerTimer <= 0f) bannerText.text = "";
            }
        }

        // ---- match events ----
        void OnGoal(int team) { Banner(team == 0 ? "GOAL!  HOME SCORES" : "GOAL!  AWAY SCORES", 3f); }

        void OnRestart(RestartType type, int team)
        {
            string name = type == RestartType.ThrowIn ? "THROW-IN" : type == RestartType.Corner ? "CORNER" : "GOAL KICK";
            Banner($"{name}  -  {(team == 0 ? "HOME" : "AWAY")}", 1.4f);
        }

        void OnFullTime()
        {
            resultText.text = match.HomeScore > match.AwayScore ? $"HOME WINS  {match.HomeScore} - {match.AwayScore}"
                : match.AwayScore > match.HomeScore ? $"AWAY WINS  {match.AwayScore} - {match.HomeScore}"
                : $"DRAW  {match.HomeScore} - {match.AwayScore}";
            fullTimePanel.SetActive(true);
            Time.timeScale = 0f;
        }

        void Banner(string text, float seconds) { bannerText.text = text; bannerTimer = seconds; }

        // ---- menus ----
        void ShowStart()
        {
            Time.timeScale = 0f;
            startPanel.SetActive(true);
            fullTimePanel.SetActive(false);
        }

        void BeginMatch()
        {
            match.SetDurationMinutes(selectedMinutes);
            startPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        void Rematch()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void SetMinutes(float m)
        {
            selectedMinutes = m;
            durationLabel.text = $"Match length: {m:0} min";
        }

        // ---- UI construction ----
        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        void BuildCanvas()
        {
            var canvasGo = new GameObject("MatchCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasGo.transform;

            // Scoreboard (top centre)
            var board = Rect("Scoreboard", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(760, 110), Panel);
            scoreText = Label("Score", board, 44, TextAnchor.MiddleCenter, new Vector2(0, 14), new Vector2(740, 60));
            clockText = Label("Clock", board, 30, TextAnchor.MiddleCenter, new Vector2(0, -32), new Vector2(740, 40));
            clockText.color = new Color(1f, 0.9f, 0.4f);

            // Event banner
            bannerText = Label("Banner", root, 64, TextAnchor.MiddleCenter, new Vector2(0, 120), new Vector2(1600, 120));
            var brt = bannerText.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            bannerText.fontStyle = FontStyle.Bold;
            bannerText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3, -3);

            // Controls hint (bottom)
            var hint = Label("Hint", root, 22, TextAnchor.LowerCenter, new Vector2(0, 16), new Vector2(1700, 40));
            var hrt = hint.rectTransform; hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0f);
            hint.text = "WASD move  |  Shift sprint  |  Space shoot (hold)  |  K pass  |  J tackle  |  Tab switch player";
            hint.color = new Color(1, 1, 1, 0.75f);

            // Start panel
            startPanel = Rect("StartPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 620), Panel).gameObject;
            var st = startPanel.transform;
            var title = Label("Title", st, 72, TextAnchor.MiddleCenter, new Vector2(0, 220), new Vector2(860, 100)); title.text = "FOOTBALL SIMULATOR"; title.fontStyle = FontStyle.Bold;
            var sub = Label("Sub", st, 30, TextAnchor.MiddleCenter, new Vector2(0, 140), new Vector2(860, 50)); sub.text = "Kick-Off  -  Home (red) vs Away (blue)";
            durationLabel = Label("Duration", st, 36, TextAnchor.MiddleCenter, new Vector2(0, 50), new Vector2(860, 60));
            Button(st, "3 min", new Vector2(-260, -40), new Vector2(220, 80), () => SetMinutes(3f), new Color(0.2f, 0.3f, 0.45f));
            Button(st, "5 min", new Vector2(0, -40), new Vector2(220, 80), () => SetMinutes(5f), new Color(0.2f, 0.3f, 0.45f));
            Button(st, "10 min", new Vector2(260, -40), new Vector2(220, 80), () => SetMinutes(10f), new Color(0.2f, 0.3f, 0.45f));
            Button(st, "PLAY", new Vector2(0, -190), new Vector2(420, 110), BeginMatch, Accent);
            SetMinutes(selectedMinutes);

            // Full-time panel
            fullTimePanel = Rect("FullTimePanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 460), Panel).gameObject;
            var ft = fullTimePanel.transform;
            var ftTitle = Label("FT", ft, 72, TextAnchor.MiddleCenter, new Vector2(0, 120), new Vector2(860, 100)); ftTitle.text = "FULL TIME"; ftTitle.fontStyle = FontStyle.Bold;
            resultText = Label("Result", ft, 48, TextAnchor.MiddleCenter, new Vector2(0, 20), new Vector2(860, 80));
            Button(ft, "REMATCH", new Vector2(0, -130), new Vector2(420, 110), Rematch, Accent);
            fullTimePanel.SetActive(false);
        }

        RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = new Vector2(0.5f, anchorMax.y);
            if (anchorMax.y < 1f) rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        Text Label(string name, Transform parent, int size, TextAnchor anchor, Vector2 pos, Vector2 boxSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = boxSize;
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = anchor; t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.raycastTarget = false;
            return t;
        }

        void Button(Transform parent, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, Color color)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.color = color;
            var btn = go.GetComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            var colors = btn.colors; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f); colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            var t = Label("Text", go.transform, 40, TextAnchor.MiddleCenter, Vector2.zero, size);
            t.text = label; t.fontStyle = FontStyle.Bold;
        }
    }
}
