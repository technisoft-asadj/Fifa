using TMPro;
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
        Text scoreText, clockText, bannerText, hintText;
        TextMeshProUGUI resultText, durationLabel, formationLabel, difficultyLabel;
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
            UpdateExtras();
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
            scoreText.text = $"{SideShort(0)}  {match.HomeScore}  -  {match.AwayScore}  {SideShort(1)}";
            clockText.text = $"{Mathf.FloorToInt(match.GameMinute):00}:00";
            var me = homeTeam != null ? homeTeam.Controlled : null;
            if (me != null)
            {
                staminaFill.anchorMax = new Vector2(Mathf.Clamp01(me.Stamina), 1f);
                staminaImage.color = me.Exhausted ? new Color(0.9f, 0.25f, 0.2f) : Color.Lerp(new Color(0.95f, 0.75f, 0.2f), new Color(0.25f, 0.85f, 0.35f), me.Stamina);
            }
        }

        // ---- match events ----
        void OnHalfTime() { Banner($"HALF TIME  {match.HomeScore} - {match.AwayScore}\n<size=60%>Teams change ends</size>", MatchManager.HalfTimeSeconds - 0.5f, true); }

        /// <summary>"HOME" / "AWAY", or the club's name in capitals when a club was picked for that side.</summary>
        string Side(int team) { return team == 0 ? TeamSelection.NameOf(TeamSelection.Home, "HOME").ToUpperInvariant() : TeamSelection.NameOf(TeamSelection.Away, "AWAY").ToUpperInvariant(); }
        /// <summary>The short form for the scoreboard: "HOME" / "AWAY" or the club's three letters.</summary>
        string SideShort(int team) { return team == 0 ? TeamSelection.ShortOf(TeamSelection.Home, "HOME") : TeamSelection.ShortOf(TeamSelection.Away, "AWAY"); }

        void OnGoal(int team)
        {
            bool og = match.Goals.Count > 0 && match.Goals[match.Goals.Count - 1].ownGoal;
            Banner($"GOAL!  {Side(team)} SCORES" + (og ? "  (OWN GOAL)" : ""), 3f, true);
        }

        void OnRestart(RestartType type, int team)
        {
            string name = type == RestartType.ThrowIn ? "THROW-IN" : type == RestartType.Corner ? "CORNER" : type == RestartType.FreeKick ? "FOUL - FREE KICK" : type == RestartType.Penalty ? "PENALTY" : type == RestartType.Offside ? "OFFSIDE - FREE KICK" : "GOAL KICK";
            Banner($"{name}  -  {Side(team)}", 1.4f);
        }

        void OnYellow(int team, int total) { Banner($"YELLOW CARD  -  {Side(team)}", 2f); }
        void OnRed(int team, int total) { Banner($"RED CARD  -  {Side(team)} DOWN TO {(team == 0 ? homeTeam : awayTeam)?.ActiveCount ?? 0}", 3f); }

        void OnFullTime()
        {
            resultText.text = match.HomeScore > match.AwayScore ? $"{Side(0)} WINS"
                : match.AwayScore > match.HomeScore ? $"{Side(1)} WINS"
                : "DRAW";
            FillFullTime();
            if (!TrainingMode.Active)
            {
                CareerRecord.Record(match.HomeScore, match.AwayScore);
                int levelBefore = PlayerProfile.Level;
                PlayerProfile.AwardMatch(match.HomeScore, match.AwayScore);
                PlayerProfile.Reward(match.HomeScore, match.AwayScore, out int earnedCoins, out int earnedXp);
                SetEarned(earnedCoins, earnedXp, PlayerProfile.GemsFor(match.HomeScore, match.AwayScore), PlayerProfile.Level > levelBefore);
            }
            else SetNoReward();
            if (MiniLeague.Active && !MiniLeague.Finished) MiniLeague.RecordResult(match.HomeScore, match.AwayScore); // the human is home
            if (Tournament.Active && !Tournament.Over)
            {
                ShootOut.Result penalties = null;
                if (match.HomeScore == match.AwayScore)
                {
                    ShootOut.Chances(Tournament.CurrentOpponent, out float homeChance, out float awayChance);
                    penalties = ShootOut.Simulate(new RandomStream(System.Environment.TickCount), homeChance, awayChance);
                    // on the same line, smaller: the result box is one line high and a second line was cut off
                    resultText.text = $"DRAW - {(penalties.homeWins ? "HOME" : "AWAY")} WIN {(penalties.homeWins ? penalties.homeGoals : penalties.awayGoals)} - {(penalties.homeWins ? penalties.awayGoals : penalties.homeGoals)} ON PENALTIES";
                }
                Tournament.RecordResult(match.HomeScore, match.AwayScore, penalties);
            }
            if (Season.Active && !Season.Over) Season.RecordResult(match.HomeScore, match.AwayScore);
            leagueText.text = Tournament.Active ? Tournament.Summary() : Season.Active ? Season.Summary() : MiniLeague.Summary();
            rematchLabel.text = Season.Active ? (Season.Over ? "New Season" : "Next Match")
                : Tournament.Active ? (Tournament.Over ? "New Tournament" : "Next Match")
                : !MiniLeague.Active ? "Rematch" : MiniLeague.Finished ? "New League" : "Next Match";
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

        /// <summary>Shows an event banner. With Commentary off in Settings only the important ones (goals, half time) are shown.</summary>
        void Banner(string text, float seconds, bool important = false)
        {
            if (!important && !FootballSim.Audio.MatchAudio.CommentaryOn) return;
            bannerText.text = text; bannerTimer = seconds;
        }

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
            LoadWithScreen();
        }

        void Rematch()
        {
            if (MiniLeague.Finished) MiniLeague.Start(); // "NEW LEAGUE"
            if (Tournament.Over) Tournament.Start(); // "NEW TOURNAMENT"
            if (Season.Over) Season.Start(); // "NEW SEASON"
            OpenSetupDirectly = true;
            LoadWithScreen();
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
            formationLabel.text = FormationData.PresetNames[selectedFormation];
            if (save) SaveSettings();
        }

        int selectedKit;
        TextMeshProUGUI kitLabel, subtitleLabel, recordLabel;

        /// <summary>Choose the home shirt colour (index into KitPalette); the away team gets a colour that does not clash.</summary>
        public void SetHomeKit(int index, bool save = true)
        {
            selectedKit = ((index % KitPalette.Count) + KitPalette.Count) % KitPalette.Count;
            if (save) KitPalette.Save(selectedKit);
            ApplyTeams();
        }

        /// <summary>Puts the chosen strips, goalkeeper colours, shirt numbers and player names on both teams and updates the labels that name them.</summary>
        public void ApplyTeams()
        {
            int away = KitPalette.AwayFor(selectedKit);
            FootballSim.Players.TeamKits.Apply(homeTeam, awayTeam, selectedKit);
            bool club = TeamSelection.Home >= 0 || TeamSelection.Away >= 0;
            if (kitLabel != null) kitLabel.text = club ? "Kit set by the club (see Teams)" : $"{KitPalette.Names[selectedKit]} kit";
            if (kitRings != null) for (int i = 0; i < kitRings.Length; i++) kitRings[i].color = i == selectedKit && TeamSelection.Home < 0 ? UITheme.Gold : new Color(0.02f, 0.06f, 0.16f, 0.9f);
            if (subtitleLabel != null)
                subtitleLabel.text = club ? $"Kick-Off  -  {TeamSelection.NameOf(TeamSelection.Home, "Home")} vs {TeamSelection.NameOf(TeamSelection.Away, "Away")}"
                    : $"Kick-Off  -  Home ({KitPalette.Names[selectedKit].ToLower()}) vs Away ({KitPalette.Names[away].ToLower()})";
            FootballSim.Venue.Stadium.RefreshKits(); // the benches' jackets follow the kits
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
            difficultyLabel.text = AiTuning.CurrentDifficulty.ToString();
            if (save) SaveSettings();
        }

        TextMeshProUGUI modeLabel, leagueText;
        Image[] kitRings, modeCards, durationCards;
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
        TextMeshProUGUI resetLabel;
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
                modeLabel.text = TrainingMode.Active ? "Training"
                    : Season.Active ? (Season.Over ? "Season over" : $"Season {Season.Round + 1}/{Season.Rounds}")
                    : Tournament.Active ? (Tournament.Won ? "Cup won" : Tournament.Eliminated ? "Cup over" : $"Cup - {Tournament.CurrentStage}")
                    : !MiniLeague.Active ? "Friendly"
                    : MiniLeague.Finished ? "League complete" : $"League {MiniLeague.Round + 1}/{MiniLeague.Matches}";
            if (recordLabel != null)
                recordLabel.text = Tournament.Active ? "Cup path\n" + Tournament.Bracket()
                    : CareerRecord.Summary() + (TeamGrowth.CurrentLevel > 0 ? $"   Squad lv {TeamGrowth.CurrentLevel}" : "");
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
            durationLabel.text = $"{m:0} min";
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

            // Match setup, as designed: match length, formation and difficulty on the left, competition type and kit selection on the right
            var setup = UIKit.Screen(root, "StartPanel", new Vector2(SettingsW, SettingsH), "Match Setup", UIIcon.Trophy, ShowMainMenu, ShowMainMenu, out startPanel);
            var btnCol = new Color(0.2f, 0.3f, 0.45f); // the colour the older builders (splash, name) still ask for
            var left = UIKit.Card(setup, "Options", UIKit.TopLeft, new Vector2(36f, -118f), new Vector2(700f, 540f));
            durationLabel = SetupBlock(left, 0, "Match Length", UIIcon.Clock, CycleDuration);
            formationLabel = SetupBlock(left, 1, "Formation", UIIcon.Clock, () => CycleFormation(1));
            difficultyLabel = SetupBlock(left, 2, "Difficulty", UIIcon.Bars, () => CycleDifficulty(1));
            var comp = UIKit.Card(setup, "Competition", UIKit.TopLeft, new Vector2(776f, -118f), new Vector2(688f, 190f));
            modeLabel = SetupBlock(comp, 0, "Competition Type", UIIcon.Trophy, ToggleLeagueMode, 190f);
            var kits = UIKit.Card(setup, "Kits", UIKit.TopLeft, new Vector2(776f, -328f), new Vector2(688f, 250f));
            UIKit.TextAt(kits, "KitTitle", "Kit Selection", 36f, Color.white, UIKit.TopLeft, new Vector2(0f, -14f), new Vector2(688f, 46f), TextAlignmentOptions.Center, true);
            kitRings = new Image[KitPalette.Count];
            float kitGap = 108f, kitX0 = 344f - kitGap * (KitPalette.Count - 1) * 0.5f;
            for (int k = 0; k < KitPalette.Count; k++)
            {
                int kit = k;
                var frame = UIKit.Image(kits, "KitFrame" + k, UITheme.Rounded, Color.white, true, true);
                frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = frame.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                frame.rectTransform.anchoredPosition = new Vector2(kitX0 + k * kitGap - 344f, -10f); frame.rectTransform.sizeDelta = new Vector2(98f, 112f);
                kitRings[k] = frame;
                var kb = frame.gameObject.AddComponent<Button>(); kb.targetGraphic = frame; kb.onClick.AddListener(() => { if (TeamSelection.Home >= 0) TeamSelection.Set(-1, TeamSelection.Away); SetHomeKit(kit); });
                var inner = UIKit.Image(frame.transform, "Back", UITheme.Rounded, new Color(0.12f, 0.2f, 0.38f, 1f), true, false);
                inner.rectTransform.anchorMin = Vector2.zero; inner.rectTransform.anchorMax = Vector2.one; inner.rectTransform.offsetMin = new Vector2(4f, 4f); inner.rectTransform.offsetMax = new Vector2(-4f, -4f);
                var shirt = UIKit.Image(inner.transform, "Shirt", UITheme.Icon(UIIcon.Shirt), KitPalette.Colors[k], false, false);
                shirt.rectTransform.anchorMin = new Vector2(0.1f, 0.12f); shirt.rectTransform.anchorMax = new Vector2(0.9f, 0.88f); shirt.rectTransform.offsetMin = shirt.rectTransform.offsetMax = Vector2.zero;
            }
            kitLabel = UIKit.TextAt(kits, "KitName", "", 28f, UITheme.Muted, UIKit.TopLeft, new Vector2(0f, -196f), new Vector2(688f, 40f), TextAlignmentOptions.Center, false);
            recordLabel = UIKit.TextAt(setup, "Record", "", 28f, new Color(1f, 0.9f, 0.4f), UIKit.TopLeft, new Vector2(776f, -590f), new Vector2(688f, 80f), TextAlignmentOptions.Center, true);
            recordLabel.enableAutoSizing = true; recordLabel.fontSizeMin = 18f; recordLabel.fontSizeMax = 28f;
            recordLabel.text = CareerRecord.Summary() + (TeamGrowth.CurrentLevel > 0 ? $"   Squad lv {TeamGrowth.CurrentLevel}" : "");
            UIKit.ActionButton(setup, "Start Match", UIIcon.Play, true, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(560f, 92f), BeginMatch, 50f);
            var saved = MatchSettingsStore.Load(selectedMinutes, FormationData.PresetNames.Length, (int)AiTuning.CurrentDifficulty, System.Enum.GetValues(typeof(Difficulty)).Length);
            selectedFormation = saved.formation;
            AiTuning.CurrentDifficulty = (Difficulty)saved.difficulty;
            savedDifficulty = AiTuning.CurrentDifficulty;
            SetMinutes(saved.minutes, false);
            CycleFormation(0, false);
            RefreshMode(); // also sets the difficulty label (the league's opponent while a league is running)
            SetHomeKit(KitPalette.Load(), false);

            // the main menu and settings sit above the setup screen, the online panel above those
            bracketButton = UIKit.SmallButton(setup, "Bracket", UITheme.Blue, UIKit.TopRight, new Vector2(-120f, -22f), new Vector2(200f, 74f), OpenBracket, 32f).gameObject;
            bracketButton.SetActive(false);
            BuildMainMenu(root, btnCol);
            BuildOnlinePanel(root, btnCol);
            BuildSplashAndName(root);
            BuildFpsReadout(root);

            BuildFullTime(root);

            // Pause button (top-right) and pause panel with touch-control settings
            var pb = Button(root, "II", new Vector2(-150, -70), new Vector2(100, 90), TogglePause, new Color(0.05f, 0.08f, 0.12f, 0.7f));
            pb.anchorMin = pb.anchorMax = new Vector2(1f, 1f);
            pauseButton = pb.gameObject;
            // the pause menu of the reference design: four big icon buttons (button size, opacity and sound live in Settings)
            BuildPauseScreen(root);
            BuildExtras(root);
            WireFx(root);
        }

        /// <summary>One block of the match-setup card: a centred title, an icon on the left, the value in the middle and a "next" arrow on the right.</summary>
        TextMeshProUGUI SetupBlock(RectTransform card, int index, string title, UIIcon icon, UnityEngine.Events.UnityAction onNext, float blockHeight = 172f)
        {
            float top = -index * blockHeight;
            UIKit.TextAt(card, title + "Title", title, 36f, Color.white, UIKit.TopLeft, new Vector2(0f, top - 14f), new Vector2(card.sizeDelta.x, 46f), TextAlignmentOptions.Center, true);
            var ic = UIKit.Image(card, title + "Icon", UITheme.Icon(icon), new Color(0.82f, 0.9f, 1f, 1f), false, false);
            ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = ic.rectTransform.pivot = UIKit.TopLeft;
            ic.rectTransform.anchoredPosition = new Vector2(34f, top - 84f); ic.rectTransform.sizeDelta = new Vector2(58f, 58f);
            var value = UIKit.TextAt(card, title + "Value", "", 42f, Color.white, UIKit.TopLeft, new Vector2(110f, top - 74f), new Vector2(card.sizeDelta.x - 230f, 78f), TextAlignmentOptions.Center, true);
            value.enableAutoSizing = true; value.fontSizeMin = 22f; value.fontSizeMax = 42f;
            UIKit.Arrow(card, title + "Next", UIKit.TopLeft, new Vector2(card.sizeDelta.x - 104f, top - 74f), 78f, false, onNext);
            return value;
        }

        /// <summary>3 min, 5 min, 10 min, and round again.</summary>
        void CycleDuration()
        {
            float[] lengths = { 3f, 5f, 10f };
            int at = 0;
            for (int i = 0; i < lengths.Length; i++) if (Mathf.Approximately(lengths[i], selectedMinutes)) at = i;
            SetMinutes(lengths[(at + 1) % lengths.Length]);
        }

        /// <summary>Shrinks a big panel so it fits inside the canvas (a wide phone shows only about 960 canvas units of height); never enlarges it.</summary>
        static void FitToCanvas(GameObject panel, float designWidth, float designHeight)
        {
            if (panel == null) return;
            var canvasRect = panel.transform.parent as RectTransform;
            if (canvasRect == null) return;
            float scale = Mathf.Clamp(Mathf.Min(canvasRect.rect.height * 0.96f / designHeight, canvasRect.rect.width * 0.98f / designWidth), 0.4f, 1f);
            if (!Mathf.Approximately(panel.transform.localScale.x, scale)) panel.transform.localScale = new Vector3(scale, scale, 1f);
        }

        void FitSetupPanel()
        {
            FitToCanvas(startPanel, SettingsW + 24f, SettingsH + 24f);
            FitToCanvas(onlinePanel, SettingsW + 24f, SettingsH + 24f);
            FitToCanvas(teamsPanel, SettingsW + 24f, SettingsH + 24f);
            FitToCanvas(fullTimePanel, SettingsW + 24f, SettingsH + 24f);
            FitToCanvas(settingsPanel, SettingsW + 24f, SettingsH + 24f);
            FitToCanvas(bracketPanel, SettingsW + 24f, SettingsH + 24f);
            FitToCanvas(lineupPanel, SettingsW + 24f, SettingsH + 24f);
            FitToCanvas(statsPanel, 1224f, 844f);
            FitToCanvas(careerPanel, 1224f, 824f);
            FitToCanvas(pausePanel, 944f, 804f);
            FitToCanvas(netPanel, 1004f, 584f);
            FitToCanvas(confirmPanel, 924f, 464f);
            FitToCanvas(controlsPanel, 1224f, 904f);
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
            UIKit.Raise(img, img.color);
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
