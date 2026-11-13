using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FootballSim.Core;
using FootballSim.FootballAI;
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
        [SerializeField] TeamController homeTeam;
        [SerializeField] TeamController awayTeam;

        Font font;
        Text scoreText, clockText, bannerText, resultText, durationLabel, formationLabel, difficultyLabel, statsText, goalsText, hintText;
        RectTransform staminaFill;
        Image staminaImage;
        int selectedFormation;
        GameObject startPanel, fullTimePanel, pausePanel, pauseButton;
        Text touchScaleLabel, touchOpacityLabel;
        FootballSim.Inputs.TouchControls touch;
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
            match.YellowCard += OnYellow;
            match.RedCard += OnRed;
            match.HalfTime += OnHalfTime;
        }

        void OnDisable()
        {
            if (match == null) return;
            match.GoalScored -= OnGoal;
            match.FullTime -= OnFullTime;
            match.RestartAwarded -= OnRestart;
            match.YellowCard -= OnYellow;
            match.RedCard -= OnRed;
            match.HalfTime -= OnHalfTime;
        }

        void Start()
        {
            if (match != null)
            {
                var audio = gameObject.GetComponent<FootballSim.Audio.MatchAudio>();
                if (audio == null) audio = gameObject.AddComponent<FootballSim.Audio.MatchAudio>();
                audio.Init(match);
            }
            SetHomeKit(selectedKit, false); // players are built by now (Awake order is undefined), so colour them
            ShowStart();
        }
        void OnDestroy() { Time.timeScale = 1f; }

        void Update()
        {
            // Keyboard hint is noise on a phone; show it only when there is no touchscreen.
            bool touchDevice = Application.isMobilePlatform || UnityEngine.InputSystem.Touchscreen.current != null;
            if (hintText.gameObject.activeSelf == touchDevice) hintText.gameObject.SetActive(!touchDevice);
            // Pause: on-screen button, Esc, or the Android back button (reported as Escape).
            bool inMenu = startPanel.activeSelf || fullTimePanel.activeSelf;
            if (pauseButton.activeSelf == inMenu) pauseButton.SetActive(!inMenu);
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) TogglePause();
            if (match == null) return;
            scoreText.text = $"HOME  {match.HomeScore}  -  {match.AwayScore}  AWAY";
            clockText.text = $"{Mathf.FloorToInt(match.GameMinute):00}:00";
            var me = homeTeam != null ? homeTeam.Controlled : null;
            if (me != null)
            {
                staminaFill.anchorMax = new Vector2(Mathf.Clamp01(me.Stamina), 1f);
                staminaImage.color = me.Exhausted ? new Color(0.9f, 0.25f, 0.2f) : Color.Lerp(new Color(0.95f, 0.75f, 0.2f), new Color(0.25f, 0.85f, 0.35f), me.Stamina);
            }
            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.unscaledDeltaTime;
                if (bannerTimer <= 0f) bannerText.text = "";
            }
        }

        // ---- match events ----
        void OnHalfTime() { Banner($"HALF TIME  {match.HomeScore} - {match.AwayScore}", MatchManager.HalfTimeSeconds - 0.5f); }

        void OnGoal(int team)
        {
            bool og = match.Goals.Count > 0 && match.Goals[match.Goals.Count - 1].ownGoal;
            Banner((team == 0 ? "GOAL!  HOME SCORES" : "GOAL!  AWAY SCORES") + (og ? "  (OWN GOAL)" : ""), 3f);
        }

        void OnRestart(RestartType type, int team)
        {
            string name = type == RestartType.ThrowIn ? "THROW-IN" : type == RestartType.Corner ? "CORNER" : type == RestartType.FreeKick ? "FOUL - FREE KICK" : type == RestartType.Penalty ? "PENALTY" : type == RestartType.Offside ? "OFFSIDE - FREE KICK" : "GOAL KICK";
            Banner($"{name}  -  {(team == 0 ? "HOME" : "AWAY")}", 1.4f);
        }

        void OnYellow(int team, int total) { Banner($"YELLOW CARD  -  {(team == 0 ? "HOME" : "AWAY")}", 2f); }
        void OnRed(int team, int total) { Banner($"RED CARD  -  {(team == 0 ? "HOME" : "AWAY")} DOWN TO {(team == 0 ? homeTeam : awayTeam)?.ActiveCount ?? 0}", 3f); }

        void OnFullTime()
        {
            resultText.text = match.HomeScore > match.AwayScore ? $"HOME WINS  {match.HomeScore} - {match.AwayScore}"
                : match.AwayScore > match.HomeScore ? $"AWAY WINS  {match.AwayScore} - {match.HomeScore}"
                : $"DRAW  {match.HomeScore} - {match.AwayScore}";
            goalsText.text = GoalsSummary(match);
            statsText.text = StatLines(match);
            if (MiniLeague.Active && !MiniLeague.Finished) MiniLeague.RecordResult(match.HomeScore, match.AwayScore); // the human is home
            leagueText.text = MiniLeague.Summary();
            rematchLabel.text = !MiniLeague.Active ? "REMATCH" : MiniLeague.Finished ? "NEW LEAGUE" : "NEXT MATCH";
            fullTimePanel.SetActive(true);
            Time.timeScale = 0f;
        }

        /// <summary>Full-time statistics, home value first on every line.</summary>
        public static string StatsSummary(MatchManager m)
        {
            string goals = GoalsSummary(m);
            return (goals.Length > 0 ? goals + "\n\n" : "") + StatLines(m);
        }

        /// <summary>Up to 4 goal lines (then "+N more"), one per line, no trailing newline.</summary>
        public static string GoalsSummary(MatchManager m)
        {
            const int shown = 4;
            string goals = "";
            for (int i = 0; i < m.Goals.Count && i < shown; i++) goals += (i > 0 ? "\n" : "") + m.Goals[i];
            if (m.Goals.Count > shown) goals += $"\n+{m.Goals.Count - shown} more";
            return goals;
        }

        static string StatLines(MatchManager m)
        {
            var s = m.Stats;
            return $"Possession   {s.PossessionPercent(0)}%  -  {s.PossessionPercent(1)}%\n"
                 + $"Shots (on target)   {s.shots[0]} ({s.shotsOnTarget[0]})  -  {s.shots[1]} ({s.shotsOnTarget[1]})\n"
                 + $"Passes   {s.passes[0]}  -  {s.passes[1]}      Saves   {s.saves[0]}  -  {s.saves[1]}\n"
                 + $"Corners   {s.corners[0]}  -  {s.corners[1]}      Offsides   {s.offsides[0]}  -  {s.offsides[1]}\n"
                 + $"Fouls   {m.HomeFouls}  -  {m.AwayFouls}      Yellow   {m.HomeYellows}  -  {m.AwayYellows}      Red   {m.HomeReds}  -  {m.AwayReds}";
        }

        void Banner(string text, float seconds) { bannerText.text = text; bannerTimer = seconds; }

        // ---- menus ----
        void ShowStart()
        {
            Time.timeScale = 0f;
            startPanel.SetActive(true);
            fullTimePanel.SetActive(false);
        }

        public void BeginMatch()
        {
            match.SetDurationMinutes(selectedMinutes);
            ApplyFormations();
            match.PrepareKickoff();
            startPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        void Rematch()
        {
            if (MiniLeague.Finished) MiniLeague.Start(); // "NEW LEAGUE"
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void ApplyFormations()
        {
            if (homeTeam != null) { homeTeam.formation = FormationData.Create(selectedFormation); homeTeam.ResetToKickoff(); }
            if (awayTeam != null) { awayTeam.formation = FormationData.Create(Random.Range(0, FormationData.PresetNames.Length)); awayTeam.ResetToKickoff(); }
        }

        void SaveSettings() { MatchSettingsStore.Save(selectedMinutes, selectedFormation, (int)AiTuning.CurrentDifficulty); }

        void CycleFormation(int step, bool save = true)
        {
            int n = FormationData.PresetNames.Length;
            selectedFormation = (selectedFormation + step + n) % n;
            formationLabel.text = $"Your formation: {FormationData.PresetNames[selectedFormation]}";
            if (save) SaveSettings();
        }

        int selectedKit;
        Text kitLabel, subtitleLabel;

        /// <summary>Choose the home shirt colour (index into KitPalette); the away team gets a colour that does not clash.</summary>
        public void SetHomeKit(int index, bool save = true)
        {
            selectedKit = ((index % KitPalette.Count) + KitPalette.Count) % KitPalette.Count;
            int away = KitPalette.AwayFor(selectedKit);
            Recolour(homeTeam, KitPalette.Colors[selectedKit]);
            Recolour(awayTeam, KitPalette.Colors[away]);
            if (kitLabel != null) kitLabel.text = $"Your kit: {KitPalette.Names[selectedKit]}";
            if (subtitleLabel != null) subtitleLabel.text = $"Kick-Off  -  Home ({KitPalette.Names[selectedKit].ToLower()}) vs Away ({KitPalette.Names[away].ToLower()})";
            if (save) KitPalette.Save(selectedKit);
        }

        static void Recolour(TeamController team, Color shirt)
        {
            if (team == null) return;
            foreach (var p in team.players)
            {
                var v = p.GetComponent<FootballSim.Players.PlayerVisual>();
                if (v != null) v.SetKit(shirt);
            }
        }

        void CycleDifficulty(int step, bool save = true)
        {
            if (MiniLeague.Active && !MiniLeague.Finished)
            {
                // The league picks the opponent: the difficulty is fixed and the saved single-match choice stays untouched
                AiTuning.CurrentDifficulty = MiniLeague.CurrentOpponent;
                difficultyLabel.text = $"Opponent: {AiTuning.CurrentDifficulty}";
                return;
            }
            int n = System.Enum.GetValues(typeof(Difficulty)).Length;
            AiTuning.CurrentDifficulty = (Difficulty)(((int)AiTuning.CurrentDifficulty + step + n) % n);
            difficultyLabel.text = $"Difficulty: {AiTuning.CurrentDifficulty}";
            if (save) SaveSettings();
        }

        Text modeLabel, leagueText;
        Text rematchLabel;

        /// <summary>Switch between a single match and the three-match mini-league (see MiniLeague).</summary>
        public void ToggleLeagueMode()
        {
            if (MiniLeague.Active) { MiniLeague.Abandon(); AiTuning.CurrentDifficulty = savedDifficulty; }
            else { savedDifficulty = AiTuning.CurrentDifficulty; MiniLeague.Start(); }
            RefreshMode();
        }

        Difficulty savedDifficulty = Difficulty.Professional;

        void RefreshMode()
        {
            if (modeLabel != null)
                modeLabel.text = !MiniLeague.Active ? "Mode: Single match"
                    : MiniLeague.Finished ? "Mode: League finished" : $"Mode: League {MiniLeague.Round + 1}/{MiniLeague.Matches}";
            CycleDifficulty(0, false);
        }

        void SetMinutes(float m, bool save = true)
        {
            selectedMinutes = m;
            durationLabel.text = $"Match length: {m:0} min";
            if (save) SaveSettings();
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

            // Stamina bar of the player you control (bottom centre)
            var staminaBg = Rect("StaminaBar", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 62), new Vector2(320, 16), new Color(0, 0, 0, 0.55f));
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            staminaFill = (RectTransform)fillGo.transform;
            staminaFill.SetParent(staminaBg, false);
            staminaFill.anchorMin = Vector2.zero; staminaFill.anchorMax = Vector2.one;
            staminaFill.offsetMin = new Vector2(2, 2); staminaFill.offsetMax = new Vector2(-2, -2);
            staminaImage = fillGo.GetComponent<Image>();
            staminaImage.raycastTarget = false;

            // Controls hint (bottom)
            var hint = Label("Hint", root, 22, TextAnchor.LowerCenter, new Vector2(0, 16), new Vector2(1700, 40));
            hintText = hint;
            var hrt = hint.rectTransform; hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0f);
            hint.text = "WASD move  |  Shift sprint  |  Space shoot (hold)  |  K pass  |  J tackle  |  Tab switch player";
            hint.color = new Color(1, 1, 1, 0.75f);

            // Start panel
            startPanel = Rect("StartPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 900), Panel).gameObject;
            var st = startPanel.transform;
            var title = Label("Title", st, 72, TextAnchor.MiddleCenter, new Vector2(0, 320), new Vector2(860, 100)); title.text = "FOOTBALL SIMULATOR"; title.fontStyle = FontStyle.Bold;
            var sub = Label("Sub", st, 30, TextAnchor.MiddleCenter, new Vector2(0, 245), new Vector2(860, 50)); sub.text = "Kick-Off  -  Home vs Away";
            subtitleLabel = sub;
            var btnCol = new Color(0.2f, 0.3f, 0.45f);
            durationLabel = Label("Duration", st, 34, TextAnchor.MiddleCenter, new Vector2(0, 170), new Vector2(860, 50));
            Button(st, "3 min", new Vector2(-260, 100), new Vector2(220, 70), () => SetMinutes(3f), btnCol);
            Button(st, "5 min", new Vector2(0, 100), new Vector2(220, 70), () => SetMinutes(5f), btnCol);
            Button(st, "10 min", new Vector2(260, 100), new Vector2(220, 70), () => SetMinutes(10f), btnCol);
            formationLabel = Label("Formation", st, 34, TextAnchor.MiddleCenter, new Vector2(0, 20), new Vector2(560, 50));
            Button(st, "<", new Vector2(-370, 20), new Vector2(90, 70), () => CycleFormation(-1), btnCol);
            Button(st, ">", new Vector2(370, 20), new Vector2(90, 70), () => CycleFormation(1), btnCol);
            difficultyLabel = Label("Difficulty", st, 34, TextAnchor.MiddleCenter, new Vector2(0, -90), new Vector2(560, 50));
            Button(st, "<", new Vector2(-370, -90), new Vector2(90, 70), () => CycleDifficulty(-1), btnCol);
            Button(st, ">", new Vector2(370, -90), new Vector2(90, 70), () => CycleDifficulty(1), btnCol);
            kitLabel = Label("Kit", st, 34, TextAnchor.MiddleCenter, new Vector2(0, -190), new Vector2(560, 50));
            Button(st, "<", new Vector2(-370, -190), new Vector2(90, 70), () => SetHomeKit(selectedKit - 1), btnCol);
            Button(st, ">", new Vector2(370, -190), new Vector2(90, 70), () => SetHomeKit(selectedKit + 1), btnCol);
            modeLabel = Label("Mode", st, 34, TextAnchor.MiddleCenter, new Vector2(0, -290), new Vector2(560, 50));
            Button(st, "<", new Vector2(-370, -290), new Vector2(90, 70), ToggleLeagueMode, btnCol);
            Button(st, ">", new Vector2(370, -290), new Vector2(90, 70), ToggleLeagueMode, btnCol);
            Button(st, "PLAY", new Vector2(0, -385), new Vector2(420, 110), BeginMatch, Accent);
            var saved = MatchSettingsStore.Load(selectedMinutes, FormationData.PresetNames.Length, (int)AiTuning.CurrentDifficulty, System.Enum.GetValues(typeof(Difficulty)).Length);
            selectedFormation = saved.formation;
            AiTuning.CurrentDifficulty = (Difficulty)saved.difficulty;
            savedDifficulty = AiTuning.CurrentDifficulty;
            SetMinutes(saved.minutes, false);
            CycleFormation(0, false);
            RefreshMode(); // also sets the difficulty label (the league's opponent while a league is running)
            SetHomeKit(KitPalette.Load(), false);

            // Full-time panel
            fullTimePanel = Rect("FullTimePanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 700), Panel).gameObject;
            var ft = fullTimePanel.transform;
            var ftTitle = Label("FT", ft, 72, TextAnchor.MiddleCenter, new Vector2(0, 270), new Vector2(860, 100)); ftTitle.text = "FULL TIME"; ftTitle.fontStyle = FontStyle.Bold;
            // Vertical budget (panel is 700 high): title, result, goal lines (<=5 lines), five stat lines, button. Nothing overlaps.
            resultText = Label("Result", ft, 48, TextAnchor.MiddleCenter, new Vector2(0, 190), new Vector2(860, 70));
            goalsText = Label("Goals", ft, 22, TextAnchor.MiddleCenter, new Vector2(0, 105), new Vector2(860, 130));
            goalsText.verticalOverflow = VerticalWrapMode.Truncate; goalsText.lineSpacing = 1.05f;
            statsText = Label("Stats", ft, 26, TextAnchor.MiddleCenter, new Vector2(0, -60), new Vector2(860, 190));
            statsText.verticalOverflow = VerticalWrapMode.Truncate; statsText.lineSpacing = 1.1f;
            leagueText = Label("League", ft, 24, TextAnchor.MiddleCenter, new Vector2(0, -175), new Vector2(860, 40));
            leagueText.color = new Color(1f, 0.9f, 0.4f);
            var rematchButton = Button(ft, "REMATCH", new Vector2(0, -250), new Vector2(420, 110), Rematch, Accent);
            rematchLabel = rematchButton.GetComponentInChildren<Text>();
            fullTimePanel.SetActive(false);

            // Pause button (top-right) and pause panel with touch-control settings
            var pb = Button(root, "II", new Vector2(-150, -70), new Vector2(100, 90), TogglePause, new Color(0.05f, 0.08f, 0.12f, 0.7f));
            pb.anchorMin = pb.anchorMax = new Vector2(1f, 1f);
            pauseButton = pb.gameObject;
            pausePanel = Rect("PausePanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(820, 600), Panel).gameObject;
            var pp = pausePanel.transform;
            var pTitle = Label("Paused", pp, 64, TextAnchor.MiddleCenter, new Vector2(0, 230), new Vector2(780, 90)); pTitle.text = "PAUSED"; pTitle.fontStyle = FontStyle.Bold;
            touchScaleLabel = Label("TouchScale", pp, 32, TextAnchor.MiddleCenter, new Vector2(0, 130), new Vector2(520, 50));
            Button(pp, "-", new Vector2(-330, 130), new Vector2(90, 70), () => AdjustTouch(-0.1f, 0f), btnCol);
            Button(pp, "+", new Vector2(330, 130), new Vector2(90, 70), () => AdjustTouch(0.1f, 0f), btnCol);
            touchOpacityLabel = Label("TouchOpacity", pp, 32, TextAnchor.MiddleCenter, new Vector2(0, 30), new Vector2(520, 50));
            Button(pp, "-", new Vector2(-330, 30), new Vector2(90, 70), () => AdjustTouch(0f, -0.1f), btnCol);
            Button(pp, "+", new Vector2(330, 30), new Vector2(90, 70), () => AdjustTouch(0f, 0.1f), btnCol);
            Button(pp, "RESUME", new Vector2(-215, -120), new Vector2(390, 90), TogglePause, Accent);
            var soundBtn = Button(pp, "", new Vector2(215, -120), new Vector2(390, 90), ToggleSound, btnCol);
            soundLabel = soundBtn.GetComponentInChildren<Text>();
            RefreshSoundLabel();
            Button(pp, "RESTART", new Vector2(0, -230), new Vector2(420, 80), Rematch, btnCol);
            pausePanel.SetActive(false);
        }

        // ---- pause ----
        public bool IsPaused => pausePanel != null && pausePanel.activeSelf;

        /// <summary>Pause or resume. Ignored while the start or full-time screen is showing.</summary>
        public void TogglePause()
        {
            if (startPanel.activeSelf || fullTimePanel.activeSelf) return;
            bool pause = !IsPaused;
            pausePanel.SetActive(pause);
            Time.timeScale = pause ? 0f : 1f;
            var tc = Touch();
            if (tc != null) tc.SetPreview(pause);
            if (pause) RefreshTouchLabels();
        }

        FootballSim.Inputs.TouchControls Touch()
        {
            if (touch == null) touch = FindFirstObjectByType<FootballSim.Inputs.TouchControls>();
            return touch;
        }

        Text soundLabel;
        public void ToggleSound() { FootballSim.Audio.MatchAudio.SoundOn = !FootballSim.Audio.MatchAudio.SoundOn; RefreshSoundLabel(); }
        void RefreshSoundLabel() { if (soundLabel != null) soundLabel.text = FootballSim.Audio.MatchAudio.SoundOn ? "SOUND: ON" : "SOUND: OFF"; }

        void AdjustTouch(float dScale, float dOpacity)
        {
            var tc = Touch();
            if (tc == null) return;
            tc.SetAppearance(Mathf.Round((tc.controlScale + dScale) * 10f) / 10f, Mathf.Round((tc.controlOpacity + dOpacity) * 10f) / 10f);
            RefreshTouchLabels();
        }

        void RefreshTouchLabels()
        {
            var tc = Touch();
            touchScaleLabel.text = tc == null ? "Touch controls: none" : $"Button size: {tc.controlScale * 100f:0}%";
            touchOpacityLabel.text = tc == null ? "" : $"Button opacity: {tc.controlOpacity * 100f:0}%";
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

        RectTransform Button(Transform parent, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, Color color)
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
            return rt;
        }
    }
}
