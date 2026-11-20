using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FootballSim.Core;
using FootballSim.FootballAI;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.UI
{
    /// <summary>
    /// Code-built uGUI for the match: scoreboard, event banner, start screen (match length) and full-time screen.
    /// The simulation is paused (timeScale 0) while a menu is showing.
    /// </summary>
    public partial class MatchHUD : MonoBehaviour
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
            ApplySavedGraphics();
            if (OpenSetupDirectly) { OpenSetupDirectly = false; if (mainMenu != null) mainMenu.SetActive(false); ShowStart(); } // rematch / restart / next match: straight to match setup
            else if (!SplashShown) ShowSplash();
            else ShowMainMenu();
        }
        void OnDestroy() { Time.timeScale = 1f; }

        void Update()
        {
            UpdateSplash();
            FitSetupPanel();
            UpdatePerf();
            // Keyboard hint is noise on a phone; show it only when there is no touchscreen.
            bool touchDevice = Application.isMobilePlatform || UnityEngine.InputSystem.Touchscreen.current != null;
            bool frontScreen = SplashOpen || NameOpen; // the splash and name screens cover the match entirely
            bool showHint = !touchDevice && !MainMenuOpen && !frontScreen;
            if (hintText.gameObject.activeSelf != showHint) hintText.gameObject.SetActive(showHint);
            // the match scoreboard and stamina bar do not belong behind the main menu
            bool showHud = !MainMenuOpen && !startPanel.activeSelf && !frontScreen;
            if (scoreText.transform.parent.gameObject.activeSelf != showHud) { scoreText.transform.parent.gameObject.SetActive(showHud); staminaFill.parent.gameObject.SetActive(showHud); }
            if (radarRoot != null)
            {
                bool showRadar = showHud && !OnlineGuestActive; // a guest's players are drawn from snapshots, not the hidden local ones
                if (radarRoot.activeSelf != showRadar) radarRoot.SetActive(showRadar);
                if (showRadar) RefreshZoomLabel();
            }
            ExpireResetLabel();
            UpdateOnlineLabels();
            // Pause: on-screen button, Esc, or the Android back button (reported as Escape).
            bool inMenu = MenuOpen;
            if (pauseButton.activeSelf == inMenu) pauseButton.SetActive(!inMenu);
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool escape = kb != null && kb.escapeKey.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
            // The Input System never receives the Android Back key as Escape (KB-015); Unity's legacy Input maps Back to Escape.
            // Android only, and only when the Input System did not already see Escape, so one press cannot toggle twice.
            if (!escape && Application.platform == RuntimePlatform.Android) escape = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (escape && !NavigateBack()) TogglePause();
            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.unscaledDeltaTime;
                if (bannerTimer <= 0f) bannerText.text = "";
            }
            if (match == null || OnlineGuestActive) return; // a guest's scoreboard is driven by the host's snapshots (see UpdateOnlineLabels)
            scoreText.text = $"HOME  {match.HomeScore}  -  {match.AwayScore}  AWAY";
            clockText.text = $"{Mathf.FloorToInt(match.GameMinute):00}:00";
            var me = homeTeam != null ? homeTeam.Controlled : null;
            if (me != null)
            {
                staminaFill.anchorMax = new Vector2(Mathf.Clamp01(me.Stamina), 1f);
                staminaImage.color = me.Exhausted ? new Color(0.9f, 0.25f, 0.2f) : Color.Lerp(new Color(0.95f, 0.75f, 0.2f), new Color(0.25f, 0.85f, 0.35f), me.Stamina);
            }
        }

        // ---- match events ----
        void OnHalfTime() { Banner($"HALF TIME  {match.HomeScore} - {match.AwayScore}\n<size=60%>Teams change ends</size>", MatchManager.HalfTimeSeconds - 0.5f); }

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
            if (!TrainingMode.Active)
            {
                CareerRecord.Record(match.HomeScore, match.AwayScore);
                int levelBefore = PlayerProfile.Level;
                PlayerProfile.AwardMatch(match.HomeScore, match.AwayScore);
                PlayerProfile.Reward(match.HomeScore, match.AwayScore, out int earnedCoins, out int earnedXp);
                earnedText.text = $"+{earnedCoins} coins   +{earnedXp} XP" + (PlayerProfile.Level > levelBefore ? $"   LEVEL UP!  Level {PlayerProfile.Level}" : "");
            }
            else earnedText.text = "Training: no rewards";
            if (MiniLeague.Active && !MiniLeague.Finished) MiniLeague.RecordResult(match.HomeScore, match.AwayScore); // the human is home
            if (Tournament.Active && !Tournament.Over)
            {
                ShootOut.Result penalties = null;
                if (match.HomeScore == match.AwayScore)
                {
                    ShootOut.Chances(Tournament.CurrentOpponent, out float homeChance, out float awayChance);
                    penalties = ShootOut.Simulate(new RandomStream(System.Environment.TickCount), homeChance, awayChance);
                    // on the same line, smaller: the result box is one line high and a second line was cut off
                    resultText.text += $"  <size=30>({(penalties.homeWins ? "HOME" : "AWAY")} WIN {(penalties.homeWins ? penalties.homeGoals : penalties.awayGoals)} - {(penalties.homeWins ? penalties.awayGoals : penalties.homeGoals)} ON PENALTIES)</size>";
                }
                Tournament.RecordResult(match.HomeScore, match.AwayScore, penalties);
            }
            if (Season.Active && !Season.Over) Season.RecordResult(match.HomeScore, match.AwayScore);
            leagueText.text = Tournament.Active ? Tournament.Summary() : Season.Active ? Season.Summary() : MiniLeague.Summary();
            rematchLabel.text = Season.Active ? (Season.Over ? "NEW SEASON" : "NEXT MATCH")
                : Tournament.Active ? (Tournament.Over ? "NEW TOURNAMENT" : "NEXT MATCH")
                : !MiniLeague.Active ? "REMATCH" : MiniLeague.Finished ? "NEW LEAGUE" : "NEXT MATCH";
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
            CycleDifficulty(0, false); // a running league or cup fixes the opponent; otherwise this leaves the chosen difficulty alone
            match.SetDurationMinutes(TrainingMode.Active ? TrainingMode.Minutes : selectedMinutes);
            ApplyTraining();
            ApplyGrowth();
            ApplyFormations();
            match.PrepareKickoff();
            HideAllMenus();
            Time.timeScale = 1f;
        }

        /// <summary>In training the away outfield players stand still; otherwise they play. Run at kick-off.</summary>
        void ApplyTraining()
        {
            if (awayTeam == null) return;
            foreach (var p in awayTeam.players)
            {
                var ai = p.GetComponent<FootballSim.FootballAI.PlayerAI>();
                if (ai != null) ai.passive = TrainingMode.Active;
            }
        }

        /// <summary>The home outfield play with the growth their results have earned (see <see cref="TeamGrowth"/>); the keeper and the away side do not change.</summary>
        void ApplyGrowth()
        {
            if (homeTeam == null) return;
            int level = TeamGrowth.CurrentLevel;
            foreach (var p in homeTeam.players) if (!p.isGoalkeeper) p.SetGrowth(level);
        }

        /// <summary>Leave the match for the main menu (from the pause panel or the full-time panel). Reloads the scene so the next match starts clean.</summary>
        void GoToMainMenu()
        {
            OpenSetupDirectly = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void Rematch()
        {
            if (MiniLeague.Finished) MiniLeague.Start(); // "NEW LEAGUE"
            if (Tournament.Over) Tournament.Start(); // "NEW TOURNAMENT"
            if (Season.Over) Season.Start(); // "NEW SEASON"
            OpenSetupDirectly = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void ApplyFormations()
        {
            if (homeTeam != null) { homeTeam.formation = FormationData.Create(selectedFormation); homeTeam.ResetToKickoff(); }
            if (awayTeam != null) { awayTeam.formation = FormationData.Create(SimRandom.Range(0, FormationData.PresetNames.Length)); awayTeam.ResetToKickoff(); }
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
        Text kitLabel, subtitleLabel, recordLabel;

        /// <summary>Choose the home shirt colour (index into KitPalette); the away team gets a colour that does not clash.</summary>
        public void SetHomeKit(int index, bool save = true)
        {
            selectedKit = ((index % KitPalette.Count) + KitPalette.Count) % KitPalette.Count;
            int away = KitPalette.AwayFor(selectedKit);
            Recolour(homeTeam, KitPalette.Colors[selectedKit]);
            Recolour(awayTeam, KitPalette.Colors[away]);
            if (kitLabel != null) kitLabel.text = $"Your kit: {KitPalette.Names[selectedKit]}";
            if (kitRings != null) for (int i = 0; i < kitRings.Length; i++) kitRings[i].color = i == selectedKit ? Color.white : new Color(1f, 1f, 1f, 0.12f);
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
            if (Season.Active && !Season.Over)
            {
                AiTuning.CurrentDifficulty = Season.CurrentOpponent; // the season picks the opponent too
                difficultyLabel.text = $"Opponent: {Season.CurrentOpponentName} ({AiTuning.CurrentDifficulty})";
                return;
            }
            if (Tournament.Active && !Tournament.Over)
            {
                AiTuning.CurrentDifficulty = Tournament.CurrentOpponent; // the cup picks the opponent too
                difficultyLabel.text = $"{Tournament.CurrentStage}: {AiTuning.CurrentDifficulty}";
                return;
            }
            int n = System.Enum.GetValues(typeof(Difficulty)).Length;
            AiTuning.CurrentDifficulty = (Difficulty)(((int)AiTuning.CurrentDifficulty + step + n) % n);
            difficultyLabel.text = $"Difficulty: {AiTuning.CurrentDifficulty}";
            if (save) SaveSettings();
        }

        Text modeLabel, leagueText, earnedText;
        Image[] kitRings, modeCards, durationCards;
        /// <summary>The "+coins +XP" line on the full-time panel (test hook).</summary>
        public string EarnedText => earnedText != null ? earnedText.text : "";
        TMPro.TMP_Text rematchLabel;
        /// <summary>The text on the full-time screen's main button (test hook).</summary>
        public string RematchButtonText => rematchLabel != null ? rematchLabel.text : "";

        /// <summary>Switch between a single match and the three-match mini-league (see MiniLeague).</summary>
        public void ToggleLeagueMode()
        {
            // single match -> league -> tournament -> season -> training -> single match
            if (TrainingMode.Active) TrainingMode.Active = false;
            else if (Season.Active) { Season.Abandon(); AiTuning.CurrentDifficulty = savedDifficulty; TrainingMode.Active = true; }
            else if (Tournament.Active) { Tournament.Abandon(); Season.Start(); }
            else if (MiniLeague.Active) { MiniLeague.Abandon(); Tournament.Start(); }
            else { savedDifficulty = AiTuning.CurrentDifficulty; MiniLeague.Start(); }
            RefreshMode();
        }

        Difficulty savedDifficulty = Difficulty.Professional;

        // ---- reset saved progress: two taps within a few seconds, so a stray touch cannot erase a league ----
        Text resetLabel;
        float resetArmedUntil = -1f;
        const float ResetConfirmSeconds = 4f;
        public bool ResetArmed => Time.unscaledTime < resetArmedUntil;

        /// <summary>First tap arms the button; a second tap within a few seconds ends any league, tournament, season or training and returns to a single match.</summary>
        public void TapResetProgress()
        {
            if (!ResetArmed)
            {
                resetArmedUntil = Time.unscaledTime + ResetConfirmSeconds;
                if (resetLabel != null) resetLabel.text = "Tap again to erase";
                return;
            }
            bool wasMode = MiniLeague.Active || Tournament.Active || Season.Active || TrainingMode.Active;
            SaveData.ResetProgress();
            resetArmedUntil = -1f;
            if (wasMode)
            {
                var s = MatchSettingsStore.Load(selectedMinutes, FormationData.PresetNames.Length, (int)savedDifficulty, System.Enum.GetValues(typeof(Difficulty)).Length);
                AiTuning.CurrentDifficulty = (Difficulty)s.difficulty;
            }
            if (resetLabel != null) resetLabel.text = "Progress erased";
            RefreshMode();
        }

        void ExpireResetLabel() { if (resetLabel != null && !ResetArmed && resetLabel.text == "Tap again to erase") resetLabel.text = "Reset progress"; }

        void RefreshMode()
        {
            if (modeLabel != null)
                modeLabel.text = TrainingMode.Active ? "Mode: Training"
                    : Season.Active ? (Season.Over ? "Mode: Season over" : $"Mode: Season {Season.Round + 1}/{Season.Rounds}")
                    : Tournament.Active ? (Tournament.Won ? "Mode: Tournament won" : Tournament.Eliminated ? "Mode: Tournament over" : $"Mode: Tournament - {Tournament.CurrentStage}")
                    : !MiniLeague.Active ? "Mode: Single match"
                    : MiniLeague.Finished ? "Mode: League finished" : $"Mode: League {MiniLeague.Round + 1}/{MiniLeague.Matches}";
            CycleDifficulty(0, false);
            RefreshModeCards();
        }

        /// <summary>Gold for the current mode card, blue for the others.</summary>
        void RefreshModeCards()
        {
            if (modeCards == null) return;
            var cur = CurrentMode; var modes = new[] { PlayMode.Single, PlayMode.League, PlayMode.Tournament, PlayMode.Season, PlayMode.Training };
            for (int i = 0; i < modeCards.Length; i++) modeCards[i].color = modes[i] == cur ? UITheme.Gold : UITheme.BlueDark;
        }

        void SetMinutes(float m, bool save = true)
        {
            selectedMinutes = m;
            durationLabel.text = $"Match length: {m:0} min";
            if (durationCards != null)
            {
                float[] lengths = { 3f, 5f, 10f };
                for (int i = 0; i < durationCards.Length; i++) durationCards[i].color = Mathf.Approximately(lengths[i], m) ? UITheme.Gold : UITheme.BlueDark;
            }
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
            { var bi = board.GetComponent<Image>(); bi.sprite = UITheme.Rounded; bi.type = Image.Type.Sliced; bi.color = new Color(UITheme.Navy.r, UITheme.Navy.g, UITheme.Navy.b, 0.85f); }
            var boardRim = board.gameObject.AddComponent<Outline>(); boardRim.effectColor = new Color(UITheme.Blue.r, UITheme.Blue.g, UITheme.Blue.b, 0.8f); boardRim.effectDistance = new Vector2(2f, -2f);
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
            { var sb = staminaBg.GetComponent<Image>(); sb.sprite = UITheme.Rounded; sb.type = Image.Type.Sliced; }
            staminaImage.sprite = UITheme.Rounded; staminaImage.type = Image.Type.Sliced;

            // Controls hint (bottom)
            var hint = Label("Hint", root, 22, TextAnchor.LowerCenter, new Vector2(0, 16), new Vector2(1700, 40));
            hintText = hint;
            var hrt = hint.rectTransform; hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0f);
            hint.text = "WASD move  |  Shift sprint  |  Space shoot (hold)  |  K pass  |  J tackle  |  Tab switch player";
            hint.color = new Color(1, 1, 1, 0.75f);

            BuildRadar(root);

            // Start panel
            startPanel = Glassify(Rect("StartPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 1000), Panel).gameObject);
            var st = startPanel.transform;
            var title = Label("Title", st, 72, TextAnchor.MiddleCenter, new Vector2(0, 320), new Vector2(860, 100)); title.text = "SOCCER FIGHT"; title.fontStyle = FontStyle.Bold; title.color = UITheme.Gold;
            var sub = Label("Sub", st, 30, TextAnchor.MiddleCenter, new Vector2(0, 245), new Vector2(860, 50)); sub.text = "Kick-Off  -  Home vs Away"; sub.color = UITheme.Muted;
            subtitleLabel = sub;
            var btnCol = new Color(0.2f, 0.3f, 0.45f);
            durationLabel = Label("Duration", st, 34, TextAnchor.MiddleCenter, new Vector2(0, 170), new Vector2(860, 50));
            durationCards = new Image[3];
            float[] lengths = { 3f, 5f, 10f };
            for (int d = 0; d < lengths.Length; d++)
            {
                float len = lengths[d];
                var dc = Button(st, $"{len:0} min", new Vector2(-260 + d * 260, 100), new Vector2(220, 70), () => SetMinutes(len), btnCol);
                durationCards[d] = dc.GetComponent<Image>();
            }
            formationLabel = Label("Formation", st, 34, TextAnchor.MiddleCenter, new Vector2(0, 20), new Vector2(560, 50));
            Button(st, "<", new Vector2(-370, 20), new Vector2(90, 70), () => CycleFormation(-1), btnCol);
            Button(st, ">", new Vector2(370, 20), new Vector2(90, 70), () => CycleFormation(1), btnCol);
            difficultyLabel = Label("Difficulty", st, 34, TextAnchor.MiddleCenter, new Vector2(0, -90), new Vector2(560, 50));
            Button(st, "<", new Vector2(-370, -90), new Vector2(90, 70), () => CycleDifficulty(-1), btnCol);
            Button(st, ">", new Vector2(370, -90), new Vector2(90, 70), () => CycleDifficulty(1), btnCol);
            // kit: one colour swatch per shirt colour (the chosen one is ringed); mode: one card per mode (the current one is gold)
            kitLabel = Label("Kit", st, 30, TextAnchor.MiddleCenter, new Vector2(0, -185), new Vector2(560, 44));
            kitRings = new Image[KitPalette.Count];
            float swatchGap = 76f, swatchX0 = -swatchGap * (KitPalette.Count - 1) * 0.5f;
            for (int k = 0; k < KitPalette.Count; k++)
            {
                int kit = k;
                var ringImg = UIKit.Image(st, "KitRing" + k, UITheme.Circle, Color.white, false, false);
                ringImg.rectTransform.anchorMin = ringImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ringImg.rectTransform.anchoredPosition = new Vector2(swatchX0 + k * swatchGap, -235f); ringImg.rectTransform.sizeDelta = new Vector2(64f, 64f);
                kitRings[k] = ringImg;
                var sw = UIKit.Image(ringImg.transform, "Swatch", UITheme.Circle, KitPalette.Colors[k], false, true);
                sw.rectTransform.anchorMin = Vector2.zero; sw.rectTransform.anchorMax = Vector2.one; sw.rectTransform.offsetMin = new Vector2(6f, 6f); sw.rectTransform.offsetMax = new Vector2(-6f, -6f);
                var swBtn = sw.gameObject.AddComponent<UnityEngine.UI.Button>(); swBtn.targetGraphic = sw; swBtn.onClick.AddListener(() => SetHomeKit(kit));
            }
            modeLabel = Label("Mode", st, 30, TextAnchor.MiddleCenter, new Vector2(0, -290), new Vector2(700, 44));
            string[] modeNames = { "Single", "League", "Cup", "Season", "Training" };
            PlayMode[] modes = { PlayMode.Single, PlayMode.League, PlayMode.Tournament, PlayMode.Season, PlayMode.Training };
            modeCards = new Image[modes.Length];
            for (int m = 0; m < modes.Length; m++)
            {
                var mode = modes[m];
                var card = Button(st, modeNames[m], new Vector2(-348 + m * 174, -350), new Vector2(162, 64), () => { SelectMode(mode); RefreshModeCards(); }, btnCol);
                card.GetComponentInChildren<Text>().fontSize = 26;
                modeCards[m] = card.GetComponent<Image>();
            }
            Button(st, "PLAY", new Vector2(0, -435), new Vector2(420, 100), BeginMatch, Accent);
            var resetButton = Button(st, "Reset progress", new Vector2(-345, -435), new Vector2(180, 60), TapResetProgress, btnCol);
            resetLabel = resetButton.GetComponentInChildren<Text>();
            resetLabel.fontSize = 22; // the default 40 is wider than this small button
            var onlineButton = Button(st, "Online", new Vector2(370, 245), new Vector2(150, 50), OpenOnline, btnCol);
            onlineButton.GetComponentInChildren<Text>().fontSize = 26;
            recordLabel = Label("Record", st, 20, TextAnchor.MiddleCenter, new Vector2(345, -435), new Vector2(180, 84));
            recordLabel.color = new Color(1f, 0.9f, 0.4f);
            recordLabel.text = CareerRecord.Summary() + (TeamGrowth.CurrentLevel > 0 ? $"\nSquad lv {TeamGrowth.CurrentLevel}" : "");
            var saved = MatchSettingsStore.Load(selectedMinutes, FormationData.PresetNames.Length, (int)AiTuning.CurrentDifficulty, System.Enum.GetValues(typeof(Difficulty)).Length);
            selectedFormation = saved.formation;
            AiTuning.CurrentDifficulty = (Difficulty)saved.difficulty;
            savedDifficulty = AiTuning.CurrentDifficulty;
            SetMinutes(saved.minutes, false);
            CycleFormation(0, false);
            RefreshMode(); // also sets the difficulty label (the league's opponent while a league is running)
            SetHomeKit(KitPalette.Load(), false);

            // the start panel stays the match-setup screen; the main menu and settings sit above it, the online panel above those
            var setupBack = Button(st, "< Menu", new Vector2(-370, 245), new Vector2(150, 50), ShowMainMenu, btnCol);
            setupBack.GetComponentInChildren<Text>().fontSize = 26;
            BuildMainMenu(root, btnCol);
            BuildOnlinePanel(root, btnCol);
            BuildSplashAndName(root);
            BuildFpsReadout(root);

            // Full-time panel
            var ftGlass = UIKit.GlassPanel(root, "FullTimePanel", UIKit.Center, Vector2.zero, new Vector2(900, 700), true, 0.95f);
            fullTimePanel = ftGlass.parent.gameObject; // the rim holds the glass, so hiding it hides both
            var ft = ftGlass;
            var ftTitle = Label("FT", ft, 72, TextAnchor.MiddleCenter, new Vector2(0, 270), new Vector2(860, 100)); ftTitle.text = "FULL TIME"; ftTitle.fontStyle = FontStyle.Bold; ftTitle.color = UITheme.Gold;
            // Vertical budget (panel is 700 high): title, result, goal lines (<=5 lines), five stat lines, button. Nothing overlaps.
            resultText = Label("Result", ft, 48, TextAnchor.MiddleCenter, new Vector2(0, 190), new Vector2(860, 70));
            goalsText = Label("Goals", ft, 22, TextAnchor.MiddleCenter, new Vector2(0, 105), new Vector2(860, 130));
            goalsText.verticalOverflow = VerticalWrapMode.Truncate; goalsText.lineSpacing = 1.05f;
            statsText = Label("Stats", ft, 26, TextAnchor.MiddleCenter, new Vector2(0, -60), new Vector2(860, 190));
            statsText.verticalOverflow = VerticalWrapMode.Truncate; statsText.lineSpacing = 1.1f;
            leagueText = Label("League", ft, 24, TextAnchor.MiddleCenter, new Vector2(0, -175), new Vector2(860, 40));
            leagueText.color = new Color(1f, 0.9f, 0.4f);
            earnedText = Label("Earned", ft, 28, TextAnchor.MiddleCenter, new Vector2(0, -322), new Vector2(860, 40));
            earnedText.color = UITheme.Gold; earnedText.fontStyle = FontStyle.Bold;
            var rematchButton = UIKit.SmallButton(ft, "REMATCH", UITheme.Blue, UIKit.Center, new Vector2(-215, -250), new Vector2(400, 110), Rematch, 46f);
            UIKit.SmallButton(ft, "MENU", UITheme.Slate, UIKit.Center, new Vector2(215, -250), new Vector2(400, 110), GoToMainMenu, 46f);
            rematchLabel = rematchButton.GetComponentInChildren<TMPro.TMP_Text>();
            fullTimePanel.SetActive(false);

            // Pause button (top-right) and pause panel with touch-control settings
            var pb = Button(root, "II", new Vector2(-150, -70), new Vector2(100, 90), TogglePause, new Color(0.05f, 0.08f, 0.12f, 0.7f));
            pb.anchorMin = pb.anchorMax = new Vector2(1f, 1f);
            pauseButton = pb.gameObject;
            // the pause menu of the reference design: four big icon buttons (button size, opacity and sound live in Settings)
            var pauseGlass = UIKit.GlassPanel(root, "PausePanel", UIKit.Center, Vector2.zero, new Vector2(720, 800), true, 0.95f);
            pausePanel = pauseGlass.parent.gameObject;
            var pp = pauseGlass;
            UIKit.TextAt(pp, "Paused", "Paused", 76f, Color.white, UIKit.Center, new Vector2(0f, 300f), new Vector2(600f, 100f), TMPro.TextAlignmentOptions.Center, true);
            UIKit.MenuButton(pp, "Resume", UIIcon.Play, UITheme.Blue, UIKit.Center, new Vector2(0f, 150f), new Vector2(580f, 120f), TogglePause, false, 50f);
            UIKit.MenuButton(pp, "Settings", UIIcon.Gear, UITheme.Blue, UIKit.Center, new Vector2(0f, 10f), new Vector2(580f, 120f), PauseSettings, false, 50f);
            UIKit.MenuButton(pp, "Main Menu", UIIcon.Home, UITheme.Blue, UIKit.Center, new Vector2(0f, -130f), new Vector2(580f, 120f), GoToMainMenu, false, 50f);
            UIKit.MenuButton(pp, "Restart", UIIcon.Close, UITheme.Red, UIKit.Center, new Vector2(0f, -270f), new Vector2(580f, 120f), Rematch, false, 50f);
            pausePanel.SetActive(false);
        }

        /// <summary>The setup panel is laid out 1000 high, but a wide phone shows less canvas height than that (about 960 on 2436x1080), which cut off the PLAY button. Shrinks it to fit; never enlarges it.</summary>
        void FitSetupPanel()
        {
            if (startPanel == null) return;
            var canvasRect = startPanel.transform.parent as RectTransform;
            if (canvasRect == null) return;
            float scale = Mathf.Clamp(canvasRect.rect.height * 0.97f / 1000f, 0.5f, 1f);
            if (!Mathf.Approximately(startPanel.transform.localScale.x, scale)) startPanel.transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>The scale the setup panel is currently shown at (test hook).</summary>
        public float SetupPanelScale => startPanel != null ? startPanel.transform.localScale.x : 1f;

        // ---- pause ----
        public bool IsPaused => pausePanel != null && pausePanel.activeSelf;

        /// <summary>Pause or resume. Ignored while the start or full-time screen is showing.</summary>
        public void TogglePause()
        {
            if (OnlineGuestActive) { LeaveOnline(); return; } // the pause button and Back both leave an online game
            if (MenuOpen) return;
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
        bool pausedOverSettings;
        /// <summary>Settings opened from the pause menu: the pause panel steps aside and comes back when Settings closes.</summary>
        void PauseSettings() { pausePanel.SetActive(false); pausedOverSettings = true; MenuSettings(); }
        /// <summary>Called when Settings closes: brings the pause menu back if that is where Settings was opened from.</summary>
        void RestorePauseAfterSettings() { if (!pausedOverSettings) return; pausedOverSettings = false; pausePanel.SetActive(true); }
        public void ToggleSound() { FootballSim.Audio.MatchAudio.SoundOn = !FootballSim.Audio.MatchAudio.SoundOn; RefreshSoundLabel(); }
        void RefreshSoundLabel() { if (soundLabel != null) soundLabel.text = FootballSim.Audio.MatchAudio.SoundOn ? "SOUND: ON" : "SOUND: OFF"; }

        void AdjustTouch(float dScale, float dOpacity)
        {
            var tc = Touch();
            if (tc == null) return;
            tc.SetAppearance(Mathf.Round((tc.controlScale + dScale) * 10f) / 10f, Mathf.Round((tc.controlOpacity + dOpacity) * 10f) / 10f);
            RefreshTouchLabels();
            OnTouchAdjusted();
        }

        void RefreshTouchLabels()
        {
            var tc = Touch();
            if (touchScaleLabel != null) touchScaleLabel.text = tc == null ? "Touch controls: none" : $"Button size: {tc.controlScale * 100f:0}%";
            if (touchOpacityLabel != null) touchOpacityLabel.text = tc == null ? "" : $"Button opacity: {tc.controlOpacity * 100f:0}%";
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
            img.sprite = UITheme.Rounded; img.type = Image.Type.Sliced;
            bool primary = color == Accent;
            if (primary) img.color = UITheme.Gold;
            else if (Mathf.Approximately(color.r, 0.2f) && Mathf.Approximately(color.g, 0.3f)) img.color = UITheme.BlueDark; // the standard setup button
            var btn = go.GetComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            var colors = btn.colors; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f); colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            var t = Label("Text", go.transform, 40, TextAnchor.MiddleCenter, Vector2.zero, size);
            t.text = label; t.fontStyle = FontStyle.Bold;
            if (primary) t.color = UITheme.GoldText;
            return rt;
        }

        /// <summary>Gives a big old-style panel the glass look: rounded dark-navy card with an electric-blue rim.</summary>
        static GameObject Glassify(GameObject panel)
        {
            var img = panel.GetComponent<Image>();
            img.sprite = UITheme.Rounded; img.type = Image.Type.Sliced; img.color = UITheme.Glass;
            var rim = panel.AddComponent<Outline>();
            rim.effectColor = new Color(UITheme.Blue.r, UITheme.Blue.g, UITheme.Blue.b, 0.9f); rim.effectDistance = new Vector2(3f, -3f);
            return panel;
        }
    }
}
