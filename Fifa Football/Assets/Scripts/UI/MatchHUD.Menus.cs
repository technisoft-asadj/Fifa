using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FootballSim.Core;

namespace FootballSim.UI
{
    public enum PlayMode { Single, League, Tournament, Season, Training }

    /// <summary>
    /// The redesigned front screens: Main Menu (profile bar, big action buttons, daily reward) and Settings. They sit on top of the existing
    /// match-setup panel, which stays the place where match length, formation, difficulty, kit and mode are chosen, so no working flow
    /// was replaced. Back (or Escape) goes one screen up: settings or online -> the screen below -> match setup -> main menu.
    /// </summary>
    public partial class MatchHUD
    {
        /// <summary>Set before a scene reload (rematch, restart, next match) so the new scene opens match setup instead of the main menu.</summary>
        public static bool OpenSetupDirectly;

        GameObject mainMenu, settingsPanel, controlsPanel, careerPanel;
        TextMeshProUGUI settingsCamera, careerText;
        FootballSim.CameraSystem.BroadcastCamera cam;
        TextMeshProUGUI ctlStyle, ctlHand, ctlSize, ctlOpacity;
        TextMeshProUGUI menuName, menuLevel,menuXp, menuCoins, dailySub, settingsSound, settingsSize, settingsOpacity, settingsQuality, settingsFps, settingsReset;
        RectTransform xpFill;
        Button dailyButton, adButton;
        TextMeshProUGUI dailyButtonLabel;
        const string FpsKey = "fps_cap", QualityKey = "quality_level";

        public bool MainMenuOpen => mainMenu != null && mainMenu.activeSelf;
        public bool SettingsOpen => settingsPanel != null && settingsPanel.activeSelf;
        public bool CareerOpen => careerPanel != null && careerPanel.activeSelf;
        public bool ControlsOpen => controlsPanel != null && controlsPanel.activeSelf;
        public bool OnlineOpen => onlinePanel != null && onlinePanel.activeSelf;
        public bool SetupOpen => startPanel != null && startPanel.activeSelf;
        bool MenuOpen => startPanel.activeSelf || fullTimePanel.activeSelf || MainMenuOpen || SplashOpen || NameOpen;

        public PlayMode CurrentMode => TrainingMode.Active ? PlayMode.Training : Season.Active ? PlayMode.Season : Tournament.Active ? PlayMode.Tournament : MiniLeague.Active ? PlayMode.League : PlayMode.Single;

        /// <summary>Switches the start-screen mode to <paramref name="mode"/> by stepping through the same cycle the arrows use.</summary>
        public void SelectMode(PlayMode mode)
        {
            for (int i = 0; i < 5 && CurrentMode != mode; i++) ToggleLeagueMode();
        }

        // ---- navigation ----
        public void ShowMainMenu()
        {
            Time.timeScale = 0f;
            startPanel.SetActive(false); fullTimePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            CloseControls(); CloseCareer();
            if (onlinePanel != null) onlinePanel.SetActive(false);
            RefreshMainMenu();
            mainMenu.SetActive(true);
        }

        void HideAllMenus()
        {
            startPanel.SetActive(false);
            if (mainMenu != null) mainMenu.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            CloseControls(); CloseCareer();
            if (onlinePanel != null) onlinePanel.SetActive(false);
        }

        public void MenuPlay() { mainMenu.SetActive(false); ShowStart(); }
        public void MenuTournaments() { SelectMode(PlayMode.Tournament); MenuPlay(); }
        public void MenuOnline() { OpenOnline(); }
        public void MenuTeams() { MenuPlay(); } // kit and formation are chosen on the match-setup panel
        public void MenuSettings() { RefreshSettings(); settingsPanel.SetActive(true); }
        public void CloseSettings() { settingsPanel.SetActive(false); }

        /// <summary>One step back for Escape / the Android Back button. Returns true if it did something.</summary>
        bool NavigateBack()
        {
            if (SplashOpen) { FinishSplash(); return true; }
            if (NameOpen) { if (PlayerProfile.HasName) CloseName(); else ConfirmName(""); return true; }
            if (CareerOpen) { CloseCareer(); return true; }
            if (ControlsOpen) { CloseControls(); return true; }
            if (SettingsOpen) { CloseSettings(); return true; }
            if (OnlineOpen) { CloseOnline(); return true; }
            if (SetupOpen && !OnlineGuestActive) { ShowMainMenu(); return true; }
            if (MainMenuOpen)
            {
                if (Application.platform == RuntimePlatform.Android) Application.Quit();
                return true;
            }
            return false;
        }

        /// <summary>One step back, as Escape or the Android Back button does. Returns true if it handled the press.</summary>
        public bool GoBack() { return NavigateBack(); }

        /// <summary>Claims today's reward doubled, but only after an ad was watched to the end. Nothing is paid if the ad is unavailable, skipped or fails.</summary>
        public void WatchDailyAd()
        {
            if (!PlayerProfile.DailyAvailable) return;
            Ads.ShowRewarded(watched =>
            {
                if (!watched || !PlayerProfile.DailyAvailable) return;
                if (PlayerProfile.ClaimDaily() > 0) PlayerProfile.AddBonusCoins(PlayerProfile.DailyRewardCoins);
                RefreshMainMenu();
            });
        }

        public bool AdButtonShown => adButton != null && adButton.gameObject.activeSelf;

        public void ClaimDailyReward()
        {
            PlayerProfile.ClaimDaily();
            RefreshMainMenu();
        }

        void RefreshMainMenu()
        {
            if (menuLevel == null) return;
            menuName.text = PlayerProfile.Name;
            menuLevel.text = $"Level {PlayerProfile.Level}";
            menuXp.text = $"{PlayerProfile.XpInLevel} / {PlayerProfile.XpPerLevel} XP";
            UIKit.SetBar(xpFill, PlayerProfile.XpInLevel / (float)PlayerProfile.XpPerLevel);
            menuCoins.text = PlayerProfile.Coins.ToString("N0");
            bool ready = PlayerProfile.DailyAvailable;
            dailySub.text = ready ? $"+{PlayerProfile.DailyRewardCoins} coins" : "Back tomorrow";
            dailyButton.interactable = ready;
            dailyButtonLabel.text = ready ? "CLAIM" : "CLAIMED";
            if (adButton != null) adButton.gameObject.SetActive(ready && Ads.RewardedReady); // ads are optional and only ever behind this button
        }

        // ---- construction ----
        void BuildMainMenu(RectTransform root, Color btnCol)
        {
            var menu = UIKit.Stretch(root, "MainMenu");
            mainMenu = menu.gameObject;
            var bg = UIKit.Image(menu, "Background", UITheme.Gradient, Color.white, false, true);
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one; bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;

            // top-left: profile
            var avatar = UIKit.Image(menu, "Avatar", UITheme.Circle, UITheme.BlueDark, false, false);
            avatar.rectTransform.anchorMin = avatar.rectTransform.anchorMax = UIKit.TopLeft; avatar.rectTransform.pivot = UIKit.TopLeft;
            avatar.rectTransform.anchoredPosition = new Vector2(40f, -30f); avatar.rectTransform.sizeDelta = new Vector2(104f, 104f);
            var face = UIKit.Image(avatar.transform, "Face", UITheme.Icon(UIIcon.Person), new Color(1f, 1f, 1f, 0.9f), false, false);
            face.rectTransform.anchorMin = new Vector2(0.18f, 0.18f); face.rectTransform.anchorMax = new Vector2(0.82f, 0.82f); face.rectTransform.offsetMin = face.rectTransform.offsetMax = Vector2.zero;
            var profileBtn = avatar.gameObject.AddComponent<Button>(); profileBtn.targetGraphic = avatar; profileBtn.onClick.AddListener(OpenCareer); avatar.raycastTarget = true;
            menuName = UIKit.TextAt(menu, "Name", "Player", 38f, Color.white, UIKit.TopLeft, new Vector2(166f, -30f), new Vector2(360f, 46f), TextAlignmentOptions.Left, true);
            menuLevel = UIKit.TextAt(menu, "Level", "Level 1", 26f, UITheme.Gold, UIKit.TopLeft, new Vector2(166f, -76f), new Vector2(200f, 34f), TextAlignmentOptions.Left, true);
            xpFill = UIKit.Bar(menu, "XpBar", UITheme.Gold, UIKit.TopLeft, new Vector2(166f, -118f), new Vector2(300f, 16f));
            menuXp = UIKit.TextAt(menu, "Xp", "0 / 100 XP", 22f, UITheme.Muted, UIKit.TopLeft, new Vector2(480f, -112f), new Vector2(200f, 28f), TextAlignmentOptions.Left);

            // top-right: coins and settings
            var gear = UIKit.SmallButton(menu, "", UITheme.Slate, UIKit.TopRight, new Vector2(-40f, -34f), new Vector2(84f, 84f), MenuSettings);
            var gearIcon = UIKit.Image(gear.transform, "Gear", UITheme.Icon(UIIcon.Gear), Color.white, false, false);
            gearIcon.rectTransform.anchorMin = new Vector2(0.2f, 0.2f); gearIcon.rectTransform.anchorMax = new Vector2(0.8f, 0.8f); gearIcon.rectTransform.offsetMin = gearIcon.rectTransform.offsetMax = Vector2.zero;
            menuCoins = UIKit.Pill(menu, "Coins", UIIcon.Coin, UITheme.Gold, "0", UIKit.TopRight, new Vector2(-144f, -42f), new Vector2(250f, 68f));

            // title block, left
            UIKit.TextAt(menu, "TitleTop", "FOOTBALL", 150f, UITheme.Gold, UIKit.Center, new Vector2(-480f, 190f), new Vector2(800f, 170f), TextAlignmentOptions.Center, true);
            UIKit.TextAt(menu, "TitleBottom", "SIMULATOR", 96f, Color.white, UIKit.Center, new Vector2(-480f, 60f), new Vector2(800f, 110f), TextAlignmentOptions.Center, true);
            var ball = UIKit.Image(menu, "Ball", UITheme.Icon(UIIcon.Ball), Color.white, false, false);
            ball.rectTransform.anchorMin = ball.rectTransform.anchorMax = UIKit.Center; ball.rectTransform.pivot = UIKit.Center;
            ball.rectTransform.anchoredPosition = new Vector2(-480f, -150f); ball.rectTransform.sizeDelta = new Vector2(200f, 200f);
            UIKit.TextAt(menu, "Tagline", "Play offline, or host a match on your Wi-Fi", 28f, UITheme.Muted, UIKit.Center, new Vector2(-480f, -5f), new Vector2(800f, 40f));

            // right: the five big buttons
            var x = new Vector2(500f, 0f);
            UIKit.MenuButton(menu, "Play", UIIcon.Ball, UITheme.Gold, UIKit.Center, x + new Vector2(0f, 300f), new Vector2(700f, 124f), MenuPlay, true, 56f);
            UIKit.MenuButton(menu, "Tournaments", UIIcon.Trophy, UITheme.Blue, UIKit.Center, x + new Vector2(0f, 150f), new Vector2(700f, 124f), MenuTournaments, false, 52f);
            UIKit.MenuButton(menu, "Online", UIIcon.People, UITheme.Purple, UIKit.Center, x + new Vector2(0f, 0f), new Vector2(700f, 124f), MenuOnline, false, 52f);
            UIKit.MenuButton(menu, "Teams", UIIcon.Shirt, UITheme.Green, UIKit.Center, x + new Vector2(0f, -150f), new Vector2(700f, 124f), MenuTeams, false, 52f);
            UIKit.MenuButton(menu, "Settings", UIIcon.Gear, UITheme.Slate, UIKit.Center, x + new Vector2(0f, -300f), new Vector2(700f, 124f), MenuSettings, false, 52f);

            // bottom-left: daily reward
            var card = UIKit.GlassPanel(menu, "DailyCard", UIKit.BottomLeft, new Vector2(50f, 50f), new Vector2(640f, 150f), false);
            var gift = UIKit.Image(card, "Gift", UITheme.Icon(UIIcon.Gift), UITheme.Gold, false, false);
            gift.rectTransform.anchorMin = gift.rectTransform.anchorMax = new Vector2(0f, 0.5f); gift.rectTransform.pivot = new Vector2(0f, 0.5f);
            gift.rectTransform.anchoredPosition = new Vector2(28f, 0f); gift.rectTransform.sizeDelta = new Vector2(96f, 96f);
            UIKit.TextAt(card, "DailyTitle", "Daily Reward", 38f, Color.white, UIKit.TopLeft, new Vector2(150f, -28f), new Vector2(280f, 46f), TextAlignmentOptions.Left, true);
            dailySub = UIKit.TextAt(card, "DailySub", "+250 coins", 30f, UITheme.Gold, UIKit.TopLeft, new Vector2(150f, -82f), new Vector2(280f, 40f), TextAlignmentOptions.Left, true);
            dailyButton = UIKit.SmallButton(card, "CLAIM", UITheme.Gold, UIKit.Center, new Vector2(215f, 0f), new Vector2(200f, 86f), ClaimDailyReward, 34f);
            dailyButtonLabel = dailyButton.GetComponentInChildren<TextMeshProUGUI>();
            dailyButtonLabel.color = UITheme.GoldText;
            // optional: with an ad service, a button beside the card claims the reward doubled (hidden while there is none, which is always for now)
            adButton = UIKit.SmallButton(menu, "Watch ad: 2x", UITheme.Purple, UIKit.BottomLeft, new Vector2(710f, 64f), new Vector2(330f, 86f), WatchDailyAd, 30f);
            adButton.gameObject.SetActive(false);

            UIKit.TextAt(menu, "Version", "v" + Application.version, 22f, new Color(1f, 1f, 1f, 0.45f), UIKit.BottomRight, new Vector2(-30f, 20f), new Vector2(200f, 30f), TextAlignmentOptions.Right);
            BuildSettingsPanel(root);
            BuildCareerPanel(root);
        }

        // ---- career: the player's own lifetime record (local; a real online leaderboard would need a server) ----
        void BuildCareerPanel(RectTransform root)
        {
            var p = UIKit.GlassPanel(root, "Career", UIKit.Center, Vector2.zero, new Vector2(1000f, 800f), true, 1f);
            careerPanel = p.parent.gameObject;
            UIKit.TextAt(p, "Title", "CAREER", 64f, Color.white, UIKit.Center, new Vector2(0f, 320f), new Vector2(700f, 90f), TextAlignmentOptions.Center, true);
            UIKit.SmallButton(p, "< Back", UITheme.Slate, UIKit.TopLeft, new Vector2(30f, -30f), new Vector2(190f, 72f), CloseCareer, 34f);
            careerText = UIKit.TextAt(p, "Stats", "", 40f, Color.white, UIKit.Center, new Vector2(0f, 10f), new Vector2(880f, 540f), TextAlignmentOptions.Center, false);
            careerText.textWrappingMode = TextWrappingModes.Normal; careerText.lineSpacing = 18f;
            UIKit.SmallButton(p, "Change name", UITheme.Blue, UIKit.Center, new Vector2(0f, -330f), new Vector2(460f, 80f), OpenName, 34f);
            careerPanel.SetActive(false);
        }

        /// <summary>The lifetime record as shown on the Career screen.</summary>
        public string CareerStats()
        {
            int played = CareerRecord.Played;
            string rate = played == 0 ? "-" : $"{100f * CareerRecord.Won / played:0}%";
            string gd = (CareerRecord.GoalsFor - CareerRecord.GoalsAgainst).ToString("+0;-0;0");
            return $"<b>{PlayerProfile.Name}</b>\n<color=#FFC71F>Level {PlayerProfile.Level}</color>{PlayerProfile.XpInLevel} / {PlayerProfile.XpPerLevel} XP   <color=#FFC71F>{PlayerProfile.Coins:N0} coins</color>\n\n"
                 + $"Matches played: {played}\nWon {CareerRecord.Won}   Drawn {CareerRecord.Drawn}   Lost {CareerRecord.Lost}\nWin rate: {rate}\n"
                 + $"Goals for {CareerRecord.GoalsFor}   against {CareerRecord.GoalsAgainst}   difference {gd}\n\n"
                 + "<size=28><color=#B0B8C8>Saved on this phone only. Training matches are not counted.</color></size>";
        }

        public string CareerShown => careerText != null ? careerText.text : "";
        public void OpenCareer() { careerText.text = CareerStats(); careerPanel.SetActive(true); }
        public void CloseCareer() { if (careerPanel != null) careerPanel.SetActive(false); }

        void BuildSettingsPanel(RectTransform root)
        {
            var rimGo = UIKit.GlassPanel(root, "Settings", UIKit.Center, Vector2.zero, new Vector2(1100f, 880f), true, 1f);
            settingsPanel = rimGo.parent.gameObject; // the rim object, so hiding it hides the whole card
            var p = rimGo;
            UIKit.TextAt(p, "Title", "SETTINGS", 64f, Color.white, UIKit.Center, new Vector2(0f, 350f), new Vector2(700f, 90f), TextAlignmentOptions.Center, true);
            UIKit.SmallButton(p, "< Back", UITheme.Slate, UIKit.TopLeft, new Vector2(30f, -30f), new Vector2(190f, 72f), CloseSettings, 34f);

            var blue = UITheme.BlueDark;
            Row(p, "Sound", 250f, out settingsSound, () => { ToggleSound(); RefreshSettings(); }, "TOGGLE");
            Row(p, "Camera", 150f, out settingsCamera, CycleCamera, "CHANGE");
            RowPlusMinus(p, "Touch button size", 50f, out settingsSize, () => AdjustTouch(-0.1f, 0f), () => AdjustTouch(0.1f, 0f), blue);
            RowPlusMinus(p, "Touch opacity", -50f, out settingsOpacity, () => AdjustTouch(0f, -0.1f), () => AdjustTouch(0f, 0.1f), blue);
            Row(p, "Graphics", -150f, out settingsQuality, CycleQuality, "CHANGE");
            Row(p, "Frame rate", -250f, out settingsFps, ToggleFps, "CHANGE");
            UIKit.SmallButton(p, "Controls", UITheme.Blue, UIKit.Center, new Vector2(-280f, -365f), new Vector2(480f, 80f), OpenControls, 34f);
            var reset = UIKit.SmallButton(p, "Reset progress", UITheme.Red, UIKit.Center, new Vector2(280f, -365f), new Vector2(480f, 80f), TapSettingsReset, 34f);
            settingsReset = reset.GetComponentInChildren<TextMeshProUGUI>();
            settingsPanel.SetActive(false);
            BuildControlsPanel(root);
        }

        // ---- controls customisation: stick style, handedness, button size and opacity; the real controls show live while it is open ----
        void BuildControlsPanel(RectTransform root)
        {
            var p = UIKit.GlassPanel(root, "Controls", UIKit.Center, Vector2.zero, new Vector2(1100f, 880f), true, 1f);
            controlsPanel = p.parent.gameObject;
            UIKit.TextAt(p, "Title", "CONTROLS", 64f, Color.white, UIKit.Center, new Vector2(0f, 350f), new Vector2(700f, 90f), TextAlignmentOptions.Center, true);
            UIKit.SmallButton(p, "< Back", UITheme.Slate, UIKit.TopLeft, new Vector2(30f, -30f), new Vector2(190f, 72f), CloseControls, 34f);
            Row(p, "Stick", 210f, out ctlStyle, ToggleStick, "CHANGE");
            Row(p, "Hand", 95f, out ctlHand, ToggleHand, "CHANGE");
            RowPlusMinus(p, "Button size", -20f, out ctlSize, () => AdjustTouch(-0.1f, 0f), () => AdjustTouch(0.1f, 0f), UITheme.BlueDark);
            RowPlusMinus(p, "Opacity", -135f, out ctlOpacity, () => AdjustTouch(0f, -0.1f), () => AdjustTouch(0f, 0.1f), UITheme.BlueDark);
            UIKit.TextAt(p, "Note", "The real buttons show at the sides of the screen while you change them.", 28f, UITheme.Muted, UIKit.Center, new Vector2(0f, -240f), new Vector2(1000f, 40f));
            UIKit.SmallButton(p, "Reset controls", UITheme.Red, UIKit.Center, new Vector2(0f, -365f), new Vector2(520f, 80f), ResetControls, 34f);
            controlsPanel.SetActive(false);
        }

        public void OpenControls() { RefreshControls(); controlsPanel.SetActive(true); var tc = Touch(); if (tc != null) tc.SetPreview(true); }

        public void CloseControls()
        {
            if (controlsPanel == null || !controlsPanel.activeSelf) return;
            controlsPanel.SetActive(false);
            var tc = Touch(); if (tc != null) tc.SetPreview(false);
        }

        public void ToggleStick() { var tc = Touch(); if (tc == null) return; tc.SetLayout(tc.leftHanded, !tc.fixedStick); RefreshControls(); }
        public void ToggleHand() { var tc = Touch(); if (tc == null) return; tc.SetLayout(!tc.leftHanded, tc.fixedStick); RefreshControls(); }
        void ResetControls() { var tc = Touch(); if (tc == null) return; tc.ResetToDefaults(); RefreshControls(); }

        void RefreshControls()
        {
            if (ctlStyle == null) return;
            var tc = Touch();
            ctlStyle.text = tc == null ? "-" : tc.fixedStick ? "Fixed" : "Floating";
            ctlHand.text = tc == null ? "-" : tc.leftHanded ? "Left-handed" : "Right-handed";
            ctlSize.text = tc == null ? "-" : $"{tc.controlScale * 100f:0}%";
            ctlOpacity.text = tc == null ? "-" : $"{tc.controlOpacity * 100f:0}%";
        }

        void Row(RectTransform parent, string label, float y, out TextMeshProUGUI value, UnityEngine.Events.UnityAction onClick, string button)
        {
            UIKit.TextAt(parent, label + "Label", label, 38f, Color.white, UIKit.Center, new Vector2(-250f, y), new Vector2(480f, 50f), TextAlignmentOptions.Left, true);
            var b = UIKit.SmallButton(parent, "", UITheme.BlueDark, UIKit.Center, new Vector2(250f, y), new Vector2(420f, 80f), onClick, 34f);
            value = b.GetComponentInChildren<TextMeshProUGUI>();
        }

        void RowPlusMinus(RectTransform parent, string label, float y, out TextMeshProUGUI value, UnityEngine.Events.UnityAction minus, UnityEngine.Events.UnityAction plus, Color c)
        {
            UIKit.TextAt(parent, label + "Label", label, 38f, Color.white, UIKit.Center, new Vector2(-250f, y), new Vector2(480f, 50f), TextAlignmentOptions.Left, true);
            UIKit.SmallButton(parent, "-", c, UIKit.Center, new Vector2(100f, y), new Vector2(90f, 80f), minus, 46f);
            value = UIKit.TextAt(parent, label + "Value", "", 36f, Color.white, UIKit.Center, new Vector2(250f, y), new Vector2(200f, 50f), TextAlignmentOptions.Center, true);
            UIKit.SmallButton(parent, "+", c, UIKit.Center, new Vector2(400f, y), new Vector2(90f, 80f), plus, 46f);
        }

        void RefreshSettings()
        {
            settingsSound.text = FootballSim.Audio.MatchAudio.SoundOn ? "Sound: ON" : "Sound: OFF";
            var tc = Touch();
            settingsSize.text = tc == null ? "-" : $"{tc.controlScale * 100f:0}%";
            settingsOpacity.text = tc == null ? "-" : $"{tc.controlOpacity * 100f:0}%";
            var names = QualitySettings.names;
            settingsQuality.text = names.Length == 0 ? "-" : names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, names.Length - 1)];
            settingsFps.text = Application.targetFrameRate > 0 ? Application.targetFrameRate + " FPS" : "Max";
            var bc = Cam(); settingsCamera.text = bc == null ? "-" : FootballSim.CameraSystem.BroadcastCamera.PresetNames[bc.Preset];
            if (!ResetArmed) settingsReset.text = "Reset progress";
        }

        // the base AdjustTouch refreshes the pause labels; keep the settings labels in step as well
        void OnTouchAdjusted() { if (settingsPanel != null && settingsPanel.activeSelf) RefreshSettings(); RefreshControls(); }

        FootballSim.CameraSystem.BroadcastCamera Cam()
        {
            if (cam == null) cam = FindFirstObjectByType<FootballSim.CameraSystem.BroadcastCamera>();
            return cam;
        }

        /// <summary>Next camera view: Broadcast, Close, Wide, High.</summary>
        public void CycleCamera() { var c = Cam(); if (c == null) return; c.SetPreset(c.Preset + 1); RefreshSettings(); }

        void CycleQuality()
        {
            int n = QualitySettings.names.Length;
            if (n == 0) return;
            int next = (QualitySettings.GetQualityLevel() + 1) % n;
            QualitySettings.SetQualityLevel(next, true);
            try { PlayerPrefs.SetInt(QualityKey, next); PlayerPrefs.Save(); } catch { }
            RefreshSettings();
        }

        void ToggleFps()
        {
            int next = Application.targetFrameRate == 30 ? 60 : 30;
            Application.targetFrameRate = next;
            try { PlayerPrefs.SetInt(FpsKey, next); PlayerPrefs.Save(); } catch { }
            RefreshSettings();
        }

        /// <summary>Applies the graphics choices saved from the Settings screen. Run once at start.</summary>
        void ApplySavedGraphics()
        {
            try
            {
                if (PlayerPrefs.HasKey(FpsKey)) Application.targetFrameRate = Mathf.Clamp(PlayerPrefs.GetInt(FpsKey), 30, 120);
                if (PlayerPrefs.HasKey(QualityKey) && QualitySettings.names.Length > 0)
                    QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(QualityKey), 0, QualitySettings.names.Length - 1), true);
            }
            catch { }
        }

        void TapSettingsReset()
        {
            TapResetProgress();
            settingsReset.text = ResetArmed ? "Tap again to erase" : "Progress erased";
            if (!ResetArmed) RefreshSettings();
            CancelInvoke(nameof(RestoreResetLabel));
            Invoke(nameof(RestoreResetLabel), ResetConfirmSeconds + 0.2f);
        }

        void RestoreResetLabel() { if (settingsReset != null && !ResetArmed) settingsReset.text = "Reset progress"; }
    }
}
