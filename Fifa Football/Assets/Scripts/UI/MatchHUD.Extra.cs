using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FootballSim.Core;
using FootballSim.FootballAI;

namespace FootballSim.UI
{
    /// <summary>
    /// The additional screens of the redesign, all built from the same kit as the rest: a confirmation dialog, the pause menu, match
    /// statistics, the tournament bracket, the lineup / formation editor, the player profile, the matchmaking and room lobby overlay and the
    /// loading screen. Each reads the real game state; none is a mock-up.
    /// </summary>
    public partial class MatchHUD
    {
        static readonly Color HomeBlue = new Color(0.25f, 0.6f, 1f, 1f), AwayRed = new Color(0.95f, 0.36f, 0.3f, 1f);

        // ------------------------------------------------------------------ confirmation ----
        GameObject confirmPanel; TextMeshProUGUI confirmBody, confirmYesLabel; UnityAction confirmAction;

        public bool ConfirmOpen => confirmPanel != null && confirmPanel.activeSelf;

        void BuildConfirm(RectTransform root)
        {
            var p = UIKit.Screen(root, "ConfirmPanel", new Vector2(900f, 440f), "Are you sure?", null, CancelConfirm, null, out confirmPanel, 54f, true);
            confirmBody = UIKit.TextAt(p, "Body", "", 34f, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(780f, 140f), TextAlignmentOptions.Top, false);
            confirmBody.textWrappingMode = TextWrappingModes.Normal;
            UIKit.ActionButton(p, "Cancel", null, false, new Vector2(0.5f, 0f), new Vector2(-220f, 34f), new Vector2(360f, 92f), CancelConfirm, 42f);
            var yes = UIKit.SmallButton(p, "Confirm", UITheme.Red, new Vector2(0.5f, 0f), new Vector2(220f, 34f), new Vector2(360f, 92f), AcceptConfirm, 42f);
            confirmYesLabel = yes.GetComponentInChildren<TextMeshProUGUI>();
            confirmPanel.SetActive(false);
        }

        /// <summary>Asks before something that cannot be undone. Nothing happens unless the player presses the red button.</summary>
        public void Confirm(string message, string yesLabel, UnityAction onYes)
        {
            confirmBody.text = message; confirmYesLabel.text = yesLabel; confirmAction = onYes;
            confirmPanel.transform.SetAsLastSibling();
            confirmPanel.SetActive(true);
        }

        void CancelConfirm() { confirmAction = null; confirmPanel.SetActive(false); }
        void AcceptConfirm() { var a = confirmAction; CancelConfirm(); a?.Invoke(); }

        // ------------------------------------------------------------------ pause ----
        TextMeshProUGUI pauseHeader;

        void BuildPauseScreen(RectTransform root)
        {
            var p = UIKit.Screen(root, "PausePanel", new Vector2(920f, 780f), "Paused", UIIcon.Whistle, TogglePause, AskLeaveMatch, out pausePanel);
            pauseHeader = UIKit.TextAt(p, "Score", "", 40f, UITheme.Gold, new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(800f, 56f), TextAlignmentOptions.Center, true);
            UIKit.MenuButton(p, "Resume", UIIcon.Play, UITheme.Gold, UIKit.Center, new Vector2(0f, 120f), new Vector2(740f, 116f), TogglePause, true, 52f);
            UIKit.MenuButton(p, "Match Stats", UIIcon.Bars, UITheme.Blue, UIKit.Center, new Vector2(-185f, -20f), new Vector2(360f, 104f), OpenStats, false, 34f);
            UIKit.MenuButton(p, "Settings", UIIcon.Gear, UITheme.Blue, UIKit.Center, new Vector2(185f, -20f), new Vector2(360f, 104f), PauseSettings, false, 34f);
            UIKit.MenuButton(p, "Restart", UIIcon.Refresh, UITheme.Slate, UIKit.Center, new Vector2(-185f, -144f), new Vector2(360f, 104f), AskRestart, false, 34f);
            UIKit.MenuButton(p, "Main Menu", UIIcon.Home, UITheme.Slate, UIKit.Center, new Vector2(185f, -144f), new Vector2(360f, 104f), AskLeaveMatch, false, 34f);
            UIKit.TextAt(p, "Hint", "Restarting or leaving ends this match without a result.", 24f, UITheme.Muted, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(800f, 36f), TextAlignmentOptions.Center, false);
            pausePanel.SetActive(false);
        }

        void AskRestart() { Confirm("Restart this match from kick-off? The current score is lost.", "Restart", Rematch); }
        void AskLeaveMatch() { Confirm("Leave the match and go to the main menu? The current score is lost.", "Leave", GoToMainMenu); }

        // ------------------------------------------------------------------ match statistics ----
        GameObject statsPanel; TextMeshProUGUI statsHeader;
        static readonly string[] StatNames = { "Possession", "Shots", "Shots on Target", "Passes", "Corners", "Fouls" };
        readonly TextMeshProUGUI[] statHome = new TextMeshProUGUI[6], statAway = new TextMeshProUGUI[6];
        readonly RectTransform[] statHomeFill = new RectTransform[6], statAwayFill = new RectTransform[6];
        bool statsFromPause;

        public bool StatsOpen => statsPanel != null && statsPanel.activeSelf;

        void BuildStats(RectTransform root)
        {
            var p = UIKit.Screen(root, "StatsPanel", new Vector2(1200f, 820f), "Match Statistics", UIIcon.Bars, CloseStats, CloseStats, out statsPanel);
            statsHeader = UIKit.TextAt(p, "Header", "", 44f, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(1100f, 60f), TextAlignmentOptions.Center, true);
            statsHeader.enableAutoSizing = true; statsHeader.fontSizeMin = 24f; statsHeader.fontSizeMax = 44f;
            for (int i = 0; i < StatNames.Length; i++)
            {
                float y = -200f - i * 100f;
                UIKit.TextAt(p, "Label" + i, StatNames[i], 32f, UITheme.Muted, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(400f, 40f), TextAlignmentOptions.Center, true);
                statHome[i] = UIKit.TextAt(p, "Home" + i, "0", 40f, Color.white, new Vector2(0.5f, 1f), new Vector2(-480f, y), new Vector2(200f, 50f), TextAlignmentOptions.Left, true);
                statAway[i] = UIKit.TextAt(p, "Away" + i, "0", 40f, Color.white, new Vector2(0.5f, 1f), new Vector2(480f, y), new Vector2(200f, 50f), TextAlignmentOptions.Right, true);
                var track = UIKit.Image(p, "Track" + i, UITheme.Rounded, new Color(0f, 0f, 0f, 0.5f), true, false);
                track.rectTransform.anchorMin = track.rectTransform.anchorMax = track.rectTransform.pivot = new Vector2(0.5f, 1f);
                track.rectTransform.anchoredPosition = new Vector2(0f, y - 52f); track.rectTransform.sizeDelta = new Vector2(1040f, 18f);
                statHomeFill[i] = HalfBar(track.transform, HomeBlue, true);
                statAwayFill[i] = HalfBar(track.transform, AwayRed, false);
            }
            statsPanel.SetActive(false);
        }

        static RectTransform HalfBar(Transform track, Color color, bool left)
        {
            var f = UIKit.Image(track, left ? "HomeFill" : "AwayFill", UITheme.Rounded, color, true, false);
            f.rectTransform.anchorMin = new Vector2(left ? 0f : 0.5f, 0f); f.rectTransform.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
            f.rectTransform.offsetMin = f.rectTransform.offsetMax = Vector2.zero;
            return f.rectTransform;
        }

        /// <summary>Each team's share runs out from the middle: the bigger the share, the longer its half of the bar.</summary>
        static void SetShare(RectTransform home, RectTransform away, float h, float a)
        {
            float total = h + a, s = total <= 0f ? 0.5f : h / total;
            home.anchorMin = new Vector2(Mathf.Lerp(0.5f, 0f, s * 2f - 0f > 1f ? 1f : s * 2f), 0f);
            home.anchorMax = new Vector2(0.5f, 1f);
            away.anchorMin = new Vector2(0.5f, 0f);
            away.anchorMax = new Vector2(Mathf.Lerp(0.5f, 1f, (1f - s) * 2f > 1f ? 1f : (1f - s) * 2f), 1f);
        }

        void RefreshStats()
        {
            if (match == null) return;
            var s = match.Stats;
            statsHeader.text = $"{ClubName(true)}   {match.HomeScore} - {match.AwayScore}   {ClubName(false)}";
            float[] h = { s.PossessionPercent(0), s.shots[0], s.shotsOnTarget[0], s.passes[0], s.corners[0], match.HomeFouls };
            float[] a = { s.PossessionPercent(1), s.shots[1], s.shotsOnTarget[1], s.passes[1], s.corners[1], match.AwayFouls };
            for (int i = 0; i < 6; i++)
            {
                statHome[i].text = i == 0 ? $"{h[i]:0}%" : $"{h[i]:0}"; statAway[i].text = i == 0 ? $"{a[i]:0}%" : $"{a[i]:0}";
                SetShare(statHomeFill[i], statAwayFill[i], h[i], a[i]);
            }
        }

        public void OpenStats()
        {
            statsFromPause = IsPaused;
            RefreshStats();
            if (statsFromPause) pausePanel.SetActive(false);
            statsPanel.transform.SetAsLastSibling(); statsPanel.SetActive(true);
        }

        public void CloseStats()
        {
            if (statsPanel == null || !statsPanel.activeSelf) return;
            statsPanel.SetActive(false);
            if (statsFromPause) { statsFromPause = false; pausePanel.SetActive(true); }
        }

        /// <summary>The figures on the statistics screen, one line per statistic (test hook).</summary>
        public string StatsShown
        {
            get
            {
                string text = statsHeader != null ? statsHeader.text : "";
                for (int i = 0; i < 6; i++) text += $"\n{StatNames[i]}: {statHome[i].text} - {statAway[i].text}";
                return text;
            }
        }

        // ------------------------------------------------------------------ player profile ----
        TextMeshProUGUI profName, profLevel, profXp, profCoins, profGems, profFoot;
        RectTransform profXpFill;
        readonly TextMeshProUGUI[] profTiles = new TextMeshProUGUI[6];

        void BuildProfile(RectTransform root)
        {
            var p = UIKit.Screen(root, "Career", new Vector2(1200f, 800f), "Player Profile", UIIcon.Person, CloseCareer, CloseCareer, out careerPanel);
            var art = UITheme.Art("avatar");
            var ring = UIKit.Image(p, "Avatar", UITheme.Circle, new Color(0.3f, 0.62f, 1f, 1f), false, false);
            ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = ring.rectTransform.pivot = UIKit.TopLeft;
            ring.rectTransform.anchoredPosition = new Vector2(60f, -130f); ring.rectTransform.sizeDelta = new Vector2(190f, 190f);
            var face = UIKit.Image(ring.transform, "Face", art ?? UITheme.Icon(UIIcon.Person), art != null ? Color.white : new Color(0.85f, 0.93f, 1f), false, false);
            face.rectTransform.anchorMin = Vector2.zero; face.rectTransform.anchorMax = Vector2.one; face.rectTransform.offsetMin = new Vector2(6f, 6f); face.rectTransform.offsetMax = new Vector2(-6f, -6f);
            profName = UIKit.TextAt(p, "Name", "", 58f, Color.white, UIKit.TopLeft, new Vector2(290f, -128f), new Vector2(560f, 72f), TextAlignmentOptions.Left, true);
            profName.enableAutoSizing = true; profName.fontSizeMin = 30f; profName.fontSizeMax = 58f;
            profLevel = UIKit.TextAt(p, "Level", "", 36f, UITheme.Gold, UIKit.TopLeft, new Vector2(290f, -204f), new Vector2(220f, 48f), TextAlignmentOptions.Left, true);
            profXpFill = UIKit.Bar(p, "XpBar", UITheme.Gold, UIKit.TopLeft, new Vector2(520f, -220f), new Vector2(360f, 20f));
            profXp = UIKit.TextAt(p, "Xp", "", 26f, UITheme.Muted, UIKit.TopLeft, new Vector2(290f, -254f), new Vector2(590f, 36f), TextAlignmentOptions.Left, false);
            profCoins = UIKit.Pill(p, "Coins", UIIcon.Coin, UITheme.Gold, "0", UIKit.TopRight, new Vector2(-60f, -134f), new Vector2(240f, 62f));
            profGems = UIKit.Pill(p, "Gems", UIIcon.Gem, new Color(0.35f, 0.85f, 1f), "0", UIKit.TopRight, new Vector2(-60f, -208f), new Vector2(240f, 62f));
            string[] titles = { "Played", "Won", "Drawn", "Lost", "Win Rate", "Goal Diff" };
            for (int i = 0; i < 6; i++)
            {
                var tile = UIKit.Card(p, "Tile" + i, UIKit.TopLeft, new Vector2(60f + (i % 3) * 370f, -350f - (i / 3) * 150f), new Vector2(340f, 128f));
                UIKit.TextAt(tile, "Title", titles[i], 28f, UITheme.Muted, UIKit.TopLeft, new Vector2(0f, -12f), new Vector2(340f, 36f), TextAlignmentOptions.Center, true);
                profTiles[i] = UIKit.TextAt(tile, "Value", "0", 56f, Color.white, UIKit.TopLeft, new Vector2(0f, -50f), new Vector2(340f, 70f), TextAlignmentOptions.Center, true);
            }
            profFoot = UIKit.TextAt(p, "Foot", "", 26f, UITheme.Muted, UIKit.BottomLeft, new Vector2(60f, 46f), new Vector2(700f, 36f), TextAlignmentOptions.Left, false);
            UIKit.ActionButton(p, "Change Name", null, false, UIKit.BottomRight, new Vector2(-60f, 34f), new Vector2(360f, 80f), OpenName, 36f);
            // the plain-text record, kept for the tests and anything that reads it as one string
            careerText = UIKit.TextAt(p, "Stats", "", 20f, Color.white, UIKit.Center, Vector2.zero, new Vector2(10f, 10f), TextAlignmentOptions.Center, false);
            careerText.enabled = false;
            careerPanel.SetActive(false);
        }

        void RefreshProfile()
        {
            if (profName == null) return;
            profName.text = PlayerProfile.Name;
            profLevel.text = $"Lv. {PlayerProfile.Level}";
            UIKit.SetBar(profXpFill, PlayerProfile.XpPerLevel <= 0 ? 0f : (float)PlayerProfile.XpInLevel / PlayerProfile.XpPerLevel);
            profXp.text = $"{PlayerProfile.XpInLevel} / {PlayerProfile.XpPerLevel} XP to the next level";
            profCoins.text = PlayerProfile.Coins.ToString("N0"); profGems.text = PlayerProfile.Gems.ToString("N0");
            int played = CareerRecord.Played;
            profTiles[0].text = played.ToString(); profTiles[1].text = CareerRecord.Won.ToString(); profTiles[2].text = CareerRecord.Drawn.ToString(); profTiles[3].text = CareerRecord.Lost.ToString();
            profTiles[4].text = played == 0 ? "-" : $"{100f * CareerRecord.Won / played:0}%";
            profTiles[5].text = (CareerRecord.GoalsFor - CareerRecord.GoalsAgainst).ToString("+0;-0;0");
            profFoot.text = $"Goals for {CareerRecord.GoalsFor}, against {CareerRecord.GoalsAgainst}. Saved on this phone.";
        }

        // ------------------------------------------------------------------ tournament bracket ----
        GameObject bracketPanel, bracketButton; GameObject bracketNewButton; TextMeshProUGUI bracketResults, bracketPlayLabel;
        static readonly string[] BracketStages = { "Quarter-final", "Semi-final", "Final" };
        readonly Image[] bracketCards = new Image[3];
        readonly TextMeshProUGUI[] bracketStatus = new TextMeshProUGUI[3], bracketOpp = new TextMeshProUGUI[3];

        public bool BracketOpen => bracketPanel != null && bracketPanel.activeSelf;

        static string DifficultyName(Difficulty d) { return System.Text.RegularExpressions.Regex.Replace(d.ToString(), "(?<=[a-z])(?=[A-Z])", " "); }

        void BuildBracket(RectTransform root)
        {
            var p = UIKit.Screen(root, "BracketPanel", new Vector2(SettingsW, SettingsH), "Tournament", UIIcon.Trophy, CloseBracket, CloseBracket, out bracketPanel);
            Difficulty[] opp = { Difficulty.Amateur, Difficulty.Professional, Difficulty.WorldClass };
            for (int i = 0; i < 3; i++)
            {
                var card = UIKit.Card(p, "Round" + i, UIKit.TopLeft, new Vector2(50f + i * 485f, -130f), new Vector2(430f, 270f));
                bracketCards[i] = card.GetComponent<Image>();
                UIKit.TextAt(card, "Stage", BracketStages[i], 42f, Color.white, UIKit.TopLeft, new Vector2(0f, -24f), new Vector2(430f, 56f), TextAlignmentOptions.Center, true);
                bracketOpp[i] = UIKit.TextAt(card, "Opponent", "vs " + DifficultyName(opp[i]), 32f, UITheme.Muted, UIKit.TopLeft, new Vector2(0f, -96f), new Vector2(430f, 44f), TextAlignmentOptions.Center, false);
                bracketStatus[i] = UIKit.TextAt(card, "Status", "", 40f, Color.white, UIKit.TopLeft, new Vector2(0f, -170f), new Vector2(430f, 56f), TextAlignmentOptions.Center, true);
                if (i < 2)
                {
                    var link = UIKit.Image(p, "Link" + i, UITheme.Rounded, new Color(0.3f, 0.62f, 1f, 0.8f), true, false);
                    link.rectTransform.anchorMin = link.rectTransform.anchorMax = link.rectTransform.pivot = UIKit.TopLeft;
                    link.rectTransform.anchoredPosition = new Vector2(480f + i * 485f, -262f); link.rectTransform.sizeDelta = new Vector2(55f, 8f);
                }
            }
            var box = UIKit.Card(p, "Results", UIKit.TopLeft, new Vector2(50f, -424f), new Vector2(1400f, 190f));
            UIKit.TextAt(box, "Title", "Results", 30f, UITheme.Gold, UIKit.TopLeft, new Vector2(24f, -12f), new Vector2(400f, 40f), TextAlignmentOptions.Left, true);
            bracketResults = UIKit.TextAt(box, "Lines", "", 28f, Color.white, UIKit.TopLeft, new Vector2(24f, -54f), new Vector2(1350f, 130f), TextAlignmentOptions.TopLeft, false);
            bracketResults.textWrappingMode = TextWrappingModes.Normal;
            var play = UIKit.ActionButton(p, "Play Next Match", UIIcon.Play, true, new Vector2(0.5f, 0f), new Vector2(120f, 30f), new Vector2(560f, 92f), BracketPlay, 44f);
            bracketPlayLabel = play.GetComponentInChildren<TextMeshProUGUI>();
            UIKit.SmallButton(p, "Abandon Cup", UITheme.Red, UIKit.BottomLeft, new Vector2(50f, 30f), new Vector2(400f, 92f), () => Confirm("Abandon this tournament? Your progress in it is lost.", "Abandon", AbandonCup), 38f);
            bracketPanel.SetActive(false);
        }

        void RefreshBracket()
        {
            int round = Tournament.Round; bool over = Tournament.Over, out_ = Tournament.Eliminated;
            for (int i = 0; i < 3; i++)
            {
                string status; Color tint;
                if (i < round) { status = "Won"; tint = new Color(0.12f, 0.45f, 0.25f, 0.95f); }
                else if (i == round && out_) { status = "Knocked out"; tint = new Color(0.5f, 0.15f, 0.15f, 0.95f); }
                else if (i == round && !over) { status = "Up next"; tint = new Color(0.55f, 0.42f, 0.05f, 0.95f); }
                else { status = "Locked"; tint = UIKit.Row; }
                bracketStatus[i].text = status; bracketCards[i].color = tint;
            }
            var lines = Tournament.Results;
            string text = "";
            for (int i = Mathf.Max(0, lines.Count - 4); i < lines.Count; i++) text += (text.Length > 0 ? "\n" : "") + lines[i];
            bracketResults.text = lines.Count == 0 ? "No matches played yet. Win three in a row to take the cup." : text;
            bracketPlayLabel.text = Tournament.Won ? "New Tournament" : over ? "New Tournament" : "Play Next Match";
        }

        public void OpenBracket()
        {
            if (!Tournament.Active) return;
            RefreshBracket();
            bracketPanel.transform.SetAsLastSibling(); bracketPanel.SetActive(true);
        }

        public void CloseBracket() { if (bracketPanel != null) bracketPanel.SetActive(false); }

        void BracketPlay()
        {
            if (Tournament.Over) { Tournament.Start(); RefreshBracket(); RefreshMode(); return; }
            CloseBracket();
        }

        void AbandonCup()
        {
            Tournament.Abandon(); CloseBracket();
            if (CurrentMode == PlayMode.Tournament) SelectMode(PlayMode.Single);
            RefreshMode();
        }

        // ------------------------------------------------------------------ lineup (player selection and formation editor) ----
        GameObject lineupPanel; RectTransform lineupPitch;
        TextMeshProUGUI lineupFormation;
        readonly Image[] lineupDots = new Image[11], lineupRows = new Image[11];
        readonly RectTransform[] lineupRings = new RectTransform[11];
        readonly TextMeshProUGUI[] lineupNames = new TextMeshProUGUI[11], lineupRoles = new TextMeshProUGUI[11];
        int lineupSelected;

        public bool LineupOpen => lineupPanel != null && lineupPanel.activeSelf;

        static string RoleOf(int slot, Vector2[] slots)
        {
            if (slot == 0) return "GK";
            float y = slots[slot].y;
            return y < 0.36f ? "DEF" : y < 0.76f ? "MID" : "FWD";
        }

        string LineupName(int slot)
        {
            int club = TeamSelection.Home;
            int member = FootballSim.Players.TeamKits.HomeOrder[Mathf.Clamp(slot, 0, 10)];
            if (club >= 0 && club < TeamCatalog.Count && member < TeamCatalog.Clubs[club].players.Length) return TeamCatalog.Clubs[club].players[member];
            return "Player " + (member + 1);
        }

        void BuildLineup(RectTransform root)
        {
            var p = UIKit.Screen(root, "LineupPanel", new Vector2(SettingsW, SettingsH), "Lineup & Formation", UIIcon.Shirt, CloseLineup, CloseLineup, out lineupPanel);
            var card = UIKit.Card(p, "PitchCard", UIKit.TopLeft, new Vector2(36f, -118f), new Vector2(600f, 490f), new Color(0.05f, 0.1f, 0.2f, 0.9f));
            lineupPitch = UIKit.Rect(card, "Pitch", UIKit.TopLeft, new Vector2(20f, -20f), new Vector2(560f, 450f));
            var grass = UIKit.Image(lineupPitch, "Grass", UITheme.Rounded, new Color(0.18f, 0.55f, 0.2f, 1f), true, false);
            grass.rectTransform.anchorMin = Vector2.zero; grass.rectTransform.anchorMax = Vector2.one; grass.rectTransform.offsetMin = grass.rectTransform.offsetMax = Vector2.zero;
            Color line = new Color(1f, 1f, 1f, 0.8f);
            PitchLine(lineupPitch, UIKit.Center, new Vector2(0f, 0f), new Vector2(520f, 3f), line);
            PitchLine(lineupPitch, UIKit.Center, new Vector2(0f, 205f), new Vector2(520f, 3f), line); PitchLine(lineupPitch, UIKit.Center, new Vector2(0f, -205f), new Vector2(520f, 3f), line);
            PitchLine(lineupPitch, UIKit.Center, new Vector2(-260f, 0f), new Vector2(3f, 410f), line); PitchLine(lineupPitch, UIKit.Center, new Vector2(260f, 0f), new Vector2(3f, 410f), line);
            PitchLine(lineupPitch, UIKit.Center, new Vector2(0f, 150f), new Vector2(220f, 3f), line); PitchLine(lineupPitch, UIKit.Center, new Vector2(0f, -150f), new Vector2(220f, 3f), line);
            PitchLine(lineupPitch, UIKit.Center, new Vector2(-110f, 178f), new Vector2(3f, 56f), line); PitchLine(lineupPitch, UIKit.Center, new Vector2(110f, 178f), new Vector2(3f, 56f), line);
            PitchLine(lineupPitch, UIKit.Center, new Vector2(-110f, -178f), new Vector2(3f, 56f), line); PitchLine(lineupPitch, UIKit.Center, new Vector2(110f, -178f), new Vector2(3f, 56f), line);
            for (int i = 0; i < 11; i++)
            {
                int slot = i;
                var ring = UIKit.Image(lineupPitch, "Player" + i, UITheme.Circle, Color.white, false, true);
                ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = ring.rectTransform.pivot = Vector2.zero; ring.rectTransform.sizeDelta = new Vector2(50f, 50f);
                var dot = UIKit.Image(ring.transform, "Dot", UITheme.Circle, Color.white, false, false);
                dot.rectTransform.anchorMin = Vector2.zero; dot.rectTransform.anchorMax = Vector2.one; dot.rectTransform.offsetMin = new Vector2(4f, 4f); dot.rectTransform.offsetMax = new Vector2(-4f, -4f);
                lineupDotNo[i] = UIKit.Text(dot.transform, "Number", (i + 1).ToString(), 24f, Color.white, TextAlignmentOptions.Center, true);
                var b = ring.gameObject.AddComponent<Button>(); b.targetGraphic = ring; b.onClick.AddListener(() => SelectLineupSlot(slot));
                lineupDots[i] = dot; lineupRings[i] = ring.rectTransform;
            }
            lineupFormation = UIKit.Selector(p, "Formation", UIKit.TopLeft, new Vector2(336f, -666f), 520f, 76f, () => { CycleFormation(-1); RefreshLineup(); }, () => { CycleFormation(1); RefreshLineup(); }, 48f);

            var list = UIKit.Card(p, "List", UIKit.TopLeft, new Vector2(676f, -118f), new Vector2(788f, 562f));
            for (int i = 0; i < 11; i++)
            {
                int slot = i;
                var row = UIKit.Image(list, "Row" + i, UITheme.Rounded, new Color(0.04f, 0.1f, 0.24f, 0.9f), true, true);
                row.rectTransform.anchorMin = row.rectTransform.anchorMax = row.rectTransform.pivot = UIKit.TopLeft;
                row.rectTransform.anchoredPosition = new Vector2(12f, -12f - i * 49.5f); row.rectTransform.sizeDelta = new Vector2(764f, 44f);
                var b = row.gameObject.AddComponent<Button>(); b.targetGraphic = row; b.onClick.AddListener(() => SelectLineupSlot(slot));
                lineupRowNo[i] = UIKit.TextAt(row.transform, "No", (i + 1).ToString(), 26f, UITheme.Gold, UIKit.TopLeft, new Vector2(14f, 0f), new Vector2(60f, 44f), TextAlignmentOptions.Left, true);
                lineupNames[i] = UIKit.TextAt(row.transform, "Name", "", 28f, Color.white, UIKit.TopLeft, new Vector2(80f, 0f), new Vector2(520f, 44f), TextAlignmentOptions.Left, true);
                lineupRoles[i] = UIKit.TextAt(row.transform, "Role", "", 26f, UITheme.Muted, UIKit.TopRight, new Vector2(-16f, 0f), new Vector2(140f, 44f), TextAlignmentOptions.Right, true);
                lineupRows[i] = row;
            }
            lineupSwapLabel = UIKit.SmallButton(p, "Swap", UITheme.Blue, UIKit.TopLeft, new Vector2(676f, -688f), new Vector2(190f, 84f), ToggleSwap, 34f).GetComponentInChildren<TextMeshProUGUI>();
            UIKit.SmallButton(p, "Reset", UITheme.Slate, UIKit.TopLeft, new Vector2(876f, -688f), new Vector2(180f, 84f), ResetLineup, 34f);
            lineupHint = UIKit.TextAt(p, "Hint", "", 26f, UITheme.Muted, UIKit.TopLeft, new Vector2(60f, -756f), new Vector2(980f, 34f), TextAlignmentOptions.Left, false);
            UIKit.ActionButton(p, "Done", UIIcon.Check, true, UIKit.TopLeft, new Vector2(1070f, -688f), new Vector2(394f, 84f), CloseLineup, 44f);
            lineupPanel.SetActive(false);
        }

        readonly TextMeshProUGUI[] lineupDotNo = new TextMeshProUGUI[11], lineupRowNo = new TextMeshProUGUI[11];
        TextMeshProUGUI lineupSwapLabel, lineupHint;
        bool lineupSwapArmed;

        void ToggleSwap()
        {
            lineupSwapArmed = !lineupSwapArmed;
            if (lineupSwapArmed && lineupSelected == 0) lineupSelected = 1; // the keeper stays in goal
            RefreshLineup();
        }

        void ResetLineup()
        {
            FootballSim.Players.TeamKits.ResetOrder(); lineupSwapArmed = false; ApplyLineupToTeam(); RefreshLineup();
        }

        /// <summary>Puts the saved lineup on the players now, so the new numbers and names show without a restart.</summary>
        void ApplyLineupToTeam()
        {
            if (homeTeam != null && awayTeam != null) FootballSim.Players.TeamKits.Apply(homeTeam, awayTeam, selectedKit);
        }

        void SelectLineupSlot(int slot)
        {
            slot = Mathf.Clamp(slot, 0, 10);
            if (lineupSwapArmed && slot != lineupSelected)
            {
                if (FootballSim.Players.TeamKits.SwapSlots(lineupSelected, slot)) { ApplyLineupToTeam(); lineupSelected = slot; }
                lineupSwapArmed = false;
            }
            else lineupSelected = slot;
            RefreshLineup();
        }

        void RefreshLineup()
        {
            lineupFormation.text = FormationData.PresetNames[selectedFormation];
            var slots = FormationData.Slots(selectedFormation);
            FootballSim.Players.TeamKits.Resolve(selectedKit, out var hk, out _, out _, out _);
            Vector2 size = lineupPitch.sizeDelta;
            for (int i = 0; i < 11 && i < slots.Length; i++)
            {
                lineupRings[i].anchoredPosition = new Vector2(size.x * 0.5f + slots[i].x * size.x * 0.4f - 25f, 44f + slots[i].y * (size.y - 100f) - 25f);
                bool sel = i == lineupSelected;
                lineupRings[i].GetComponent<Image>().color = sel ? UITheme.Gold : Color.white;
                lineupDots[i].color = i == 0 ? new Color(0.98f, 0.82f, 0.2f) : Readable(hk.shirt);
                lineupRows[i].color = sel ? new Color(0.5f, 0.38f, 0.05f, 0.95f) : new Color(0.04f, 0.1f, 0.24f, 0.9f);
                lineupNames[i].text = LineupName(i); lineupRoles[i].text = RoleOf(i, slots);
                string no = FootballSim.Players.TeamKits.NumberInSlot(i + 1).ToString();
                lineupDotNo[i].text = no; lineupRowNo[i].text = no;
            }
            lineupSwapLabel.text = lineupSwapArmed ? "Pick..." : "Swap";
            lineupHint.text = lineupSwapArmed ? $"Tap the player to swap with {lineupNames[lineupSelected].text}." : lineupSelected == 0 ? "The goalkeeper always stays in goal." : $"{lineupNames[lineupSelected].text} selected. Press Swap, then tap another outfield player.";
        }

        public void OpenLineup()
        {
            lineupSwapArmed = false; lineupSelected = 0;
            RefreshLineup();
            lineupPanel.transform.SetAsLastSibling(); lineupPanel.SetActive(true);
        }

        public void CloseLineup() { if (lineupPanel != null) { lineupPanel.SetActive(false); if (teamsPanel != null && teamsPanel.activeSelf) RefreshTeams(); } }

        /// <summary>Names and roles on the lineup screen, one line per player (test hook).</summary>
        public string LineupShown { get { string t = ""; for (int i = 0; i < 11; i++) t += lineupNames[i].text + " " + lineupRoles[i].text + "\n"; return t; } }

        // ------------------------------------------------------------------ matchmaking and room lobby ----
        GameObject netPanel; TextMeshProUGUI netStatus, netTimer, netCode, netYou, netFriend; RectTransform netSpinner; Button netStart;
        int netMode; float netSince; // 0 hidden, 1 busy, 2 lobby

        public bool NetOverlayOpen => netPanel != null && netPanel.activeSelf;
        public string NetOverlayText => netPanel == null ? "" : netStatus.text + " " + netCode.text;

        void BuildNetOverlay(RectTransform root)
        {
            var p = UIKit.Screen(root, "NetPanel", new Vector2(980f, 560f), "Matchmaking", UIIcon.Globe, null, null, out netPanel, 50f, true);
            var spin = UIKit.Image(p, "Spinner", UITheme.Icon(UIIcon.Refresh), UITheme.Gold, false, false);
            spin.rectTransform.anchorMin = spin.rectTransform.anchorMax = spin.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            spin.rectTransform.anchoredPosition = new Vector2(0f, 90f); spin.rectTransform.sizeDelta = new Vector2(100f, 100f);
            spin.gameObject.AddComponent<Spinner>(); netSpinner = spin.rectTransform;
            netCode = UIKit.TextAt(p, "Code", "", 76f, UITheme.Gold, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(900f, 96f), TextAlignmentOptions.Center, true);
            netStatus = UIKit.TextAt(p, "Status", "", 36f, Color.white, UIKit.Center, new Vector2(0f, -10f), new Vector2(880f, 56f), TextAlignmentOptions.Center, true);
            netTimer = UIKit.TextAt(p, "Timer", "", 28f, UITheme.Muted, UIKit.Center, new Vector2(0f, -70f), new Vector2(600f, 40f), TextAlignmentOptions.Center, false);
            netYou = UIKit.TextAt(p, "You", "", 30f, Color.white, UIKit.Center, new Vector2(0f, -66f), new Vector2(880f, 42f), TextAlignmentOptions.Center, true);
            netFriend = UIKit.TextAt(p, "Friend", "", 30f, UITheme.Muted, UIKit.Center, new Vector2(0f, -110f), new Vector2(880f, 42f), TextAlignmentOptions.Center, true);
            UIKit.SmallButton(p, "Cancel", UITheme.Red, new Vector2(0.5f, 0f), new Vector2(-220f, 34f), new Vector2(380f, 90f), CancelNet, 40f);
            netStart = UIKit.ActionButton(p, "Start Match", UIIcon.Play, true, new Vector2(0.5f, 0f), new Vector2(220f, 34f), new Vector2(380f, 90f), StartRoomMatch, 38f);
            netPanel.SetActive(false);
        }

        void ShowNetBusy(string title, string message)
        {
            if (netPanel == null) return;
            netMode = 1; netSince = Time.unscaledTime;
            SetNetTitle(title);
            netCode.text = ""; netYou.text = ""; netFriend.text = ""; netStatus.text = message; netStart.gameObject.SetActive(false); netSpinner.gameObject.SetActive(true);
            netPanel.transform.SetAsLastSibling(); netPanel.SetActive(true);
        }

        void ShowLobby(string code)
        {
            if (netPanel == null) return;
            netMode = 2; SetNetTitle("Room Lobby");
            netCode.text = code; netStatus.text = "Share this code with your friend"; netTimer.text = "";
            netYou.text = "You (host)   Ready"; netFriend.text = "Friend   joins once you press Start Match";
            netStart.gameObject.SetActive(true); netSpinner.gameObject.SetActive(false);
            netPanel.transform.SetAsLastSibling(); netPanel.SetActive(true);
        }

        void SetNetTitle(string title) { var t = netPanel.transform.Find("Rim/Card/Title"); if (t != null) t.GetComponent<TextMeshProUGUI>().text = title; }

        void HideNet() { netMode = 0; if (netPanel != null) netPanel.SetActive(false); }

        void CancelNet()
        {
            onlineRequest++; // an answer still on its way is ignored
            if (pendingRoom != null && !OnlineHosting) DiscardPendingRoom();
            HideNet();
            if (onlineStatus != null) onlineStatus.text = "Cancelled.";
        }

        // ------------------------------------------------------------------ loading ----
        GameObject loadingPanel; RectTransform loadingFill; TextMeshProUGUI loadingTip;
        static readonly string[] LoadingTips =
        {
            "Tap the radar to look at any player.",
            "Settings > Controls lets you move and resize the buttons.",
            "Pick a formation in Teams to match how you like to play.",
            "A drawn cup match is settled on penalties.",
            "Change the camera view in Settings > Camera."
        };

        void BuildLoading(RectTransform root)
        {
            var holder = UIKit.Stretch(root, "LoadingPanel"); loadingPanel = holder.gameObject;
            var bg = UIKit.Image(holder, "Bg", UITheme.Gradient, new Color(0.02f, 0.06f, 0.16f, 1f), false, true);
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one; bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;
            var logoArt = UITheme.Art("logo");
            if (logoArt != null)
            {
                var logo = UIKit.Image(holder, "Logo", logoArt, Color.white, false, false); logo.preserveAspect = true;
                logo.rectTransform.anchorMin = logo.rectTransform.anchorMax = logo.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                logo.rectTransform.anchoredPosition = new Vector2(0f, 150f); logo.rectTransform.sizeDelta = new Vector2(1000f, 300f);
            }
            var spin = UIKit.Image(holder, "Spinner", UITheme.Icon(UIIcon.Refresh), UITheme.Gold, false, false);
            spin.rectTransform.anchorMin = spin.rectTransform.anchorMax = spin.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            spin.rectTransform.anchoredPosition = new Vector2(0f, -70f); spin.rectTransform.sizeDelta = new Vector2(84f, 84f);
            spin.gameObject.AddComponent<Spinner>();
            loadingFill = UIKit.Bar(holder, "Progress", UITheme.Gold, UIKit.Center, new Vector2(0f, -170f), new Vector2(700f, 20f));
            UIKit.TextAt(holder, "Loading", "Loading...", 34f, Color.white, UIKit.Center, new Vector2(0f, -220f), new Vector2(600f, 46f), TextAlignmentOptions.Center, true);
            loadingTip = UIKit.TextAt(holder, "Tip", "", 28f, UITheme.Muted, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1200f, 40f), TextAlignmentOptions.Center, false);
            loadingPanel.SetActive(false);
        }

        /// <summary>Reloads the match scene behind a loading screen with a real progress bar.</summary>
        void LoadWithScreen()
        {
            Time.timeScale = 1f;
            int index = SceneManager.GetActiveScene().buildIndex;
            if (loadingPanel == null || !isActiveAndEnabled) { SceneManager.LoadScene(index); return; }
            loadingTip.text = LoadingTips[Random.Range(0, LoadingTips.Length)];
            UIKit.SetBar(loadingFill, 0f);
            loadingPanel.transform.SetAsLastSibling(); loadingPanel.SetActive(true);
            StartCoroutine(LoadRoutine(index));
        }

        IEnumerator LoadRoutine(int index)
        {
            yield return null; // let the loading screen draw once
            var op = SceneManager.LoadSceneAsync(index);
            while (op != null && !op.isDone) { UIKit.SetBar(loadingFill, Mathf.Clamp01(op.progress / 0.9f)); yield return null; }
        }

        // ------------------------------------------------------------------ plumbing ----
        void BuildExtras(RectTransform root)
        {
            BuildStats(root); BuildBracket(root); BuildLineup(root); BuildNetOverlay(root); BuildConfirm(root); BuildLoading(root);
        }

        /// <summary>Button motion for every button and a fade for every screen. Run once the canvas is built.</summary>
        void WireFx(RectTransform root)
        {
            UIFx.WireButtons(root);
            foreach (var panel in new[] { mainMenu, startPanel, fullTimePanel, pausePanel, settingsPanel, controlsPanel, careerPanel, onlinePanel, teamsPanel, statsPanel, bracketPanel, lineupPanel, netPanel, confirmPanel })
                UIFx.FadeIn(panel);
        }

        /// <summary>Back for the extra screens, innermost first. True when one was closed.</summary>
        bool ExtraBack()
        {
            if (ConfirmOpen) { CancelConfirm(); return true; }
            if (NetOverlayOpen) { CancelNet(); return true; }
            if (LineupOpen) { CloseLineup(); return true; }
            if (BracketOpen) { CloseBracket(); return true; }
            if (StatsOpen) { CloseStats(); return true; }
            return false;
        }

        float lastNetShown = -1f;

        void UpdateExtras()
        {
            if (pauseHeader != null && pausePanel.activeSelf && match != null)
                pauseHeader.text = $"{ClubName(true)}  {match.HomeScore} - {match.AwayScore}  {ClubName(false)}";
            if (bracketButton != null)
            {
                bool show = Tournament.Active && startPanel.activeSelf;
                if (bracketButton.activeSelf != show) bracketButton.SetActive(show);
            }
            if (netMode == 1 && netPanel.activeSelf)
            {
                int secs = Mathf.FloorToInt(Time.unscaledTime - netSince);
                if (secs != (int)lastNetShown) { lastNetShown = secs; netTimer.text = $"Waiting {secs} s"; }
            }
        }
    }
}
