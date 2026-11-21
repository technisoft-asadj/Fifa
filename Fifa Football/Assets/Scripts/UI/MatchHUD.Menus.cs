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
        TextMeshProUGUI ctlStyle, ctlHand, ctlSize, ctlOpacity, ctlSens, ctlBuzz, ctlAuto;
        GameObject moveBar;
        TextMeshProUGUI menuName, menuLevel,menuXp, menuCoins, menuGems, dailySub, settingsSound, settingsSize, settingsOpacity, settingsQuality, settingsFps, settingsReset;
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
            CloseControls(); CloseCareer(); CloseTeams();
            if (onlinePanel != null) onlinePanel.SetActive(false);
        }

        public void MenuPlay() { mainMenu.SetActive(false); ShowStart(); }
        public void MenuTournaments() { SelectMode(PlayMode.Tournament); MenuPlay(); }
        /// <summary>Online play is switched off for the first (offline) release. Set to false to bring the Online screen back.</summary>
        public const bool OnlineLocked = true;
        public void MenuOnline() { if (!OnlineLocked) OpenOnline(); }
        public void MenuTeams() { OpenTeams(); }
        public void MenuSettings() { RefreshSettings(); settingsPanel.SetActive(true); }
        public void CloseSettings() { settingsPanel.SetActive(false); RestorePauseAfterSettings(); }

        /// <summary>One step back for Escape / the Android Back button. Returns true if it did something.</summary>
        bool NavigateBack()
        {
            if (SplashOpen) { FinishSplash(); return true; }
            if (NameOpen) { if (PlayerProfile.HasName) CloseName(); else ConfirmName(""); return true; }
            if (ExtraBack()) return true;
            if (TeamsOpen) { CloseTeams(); return true; }
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

        /// <summary>The "+" beside the coins and gems: takes today's reward if it is waiting, otherwise does nothing.</summary>
        void ClaimDailyIfReady() { if (PlayerProfile.DailyAvailable) ClaimDailyReward(); }

        public void ClaimDailyReward()
        {
            PlayerProfile.ClaimDaily();
            RefreshMainMenu();
        }

        void RefreshMainMenu()
        {
            if (menuLevel == null) return;
            menuName.text = PlayerProfile.Name;
            menuLevel.text = $"Lv. {PlayerProfile.Level}";
            menuXp.text = $"{PlayerProfile.XpInLevel} / {PlayerProfile.XpPerLevel}";
            UIKit.SetBar(xpFill, PlayerProfile.XpInLevel / (float)PlayerProfile.XpPerLevel);
            menuCoins.text = PlayerProfile.Coins.ToString("N0");
            if (menuGems != null) menuGems.text = PlayerProfile.Gems.ToString("N0");
            bool ready = PlayerProfile.DailyAvailable;
            dailySub.text = ready ? "Claim Now!" : "Back tomorrow";
            dailyButton.interactable = ready;
            if (dailyButtonLabel != null) dailyButtonLabel.text = ready ? "CLAIM" : "CLAIMED";
            if (adButton != null) adButton.gameObject.SetActive(ready && Ads.RewardedReady); // ads are optional and only ever behind this button
        }

        // ---- construction ----
        MenuStage menuStage;

        void BuildMainMenu(RectTransform root, Color btnCol)
        {
            var menu = UIKit.Stretch(root, "MainMenu");
            mainMenu = menu.gameObject;
            var bg = UIKit.Image(menu, "Background", UITheme.Gradient, Color.white, false, true);
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one; bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;

            // the stadium glow behind the logo
            var glow = UIKit.Image(menu, "LogoGlow", UITheme.Circle, new Color(0.25f, 0.55f, 1f, 0.28f), false, false);
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = glow.rectTransform.pivot = new Vector2(0.5f, 1f);
            glow.rectTransform.anchoredPosition = new Vector2(0f, 60f); glow.rectTransform.sizeDelta = new Vector2(1300f, 420f);

            // top-left: profile (round portrait, name, level and the experience bar)
            var avatar = UIKit.Image(menu, "Avatar", UITheme.Circle, new Color(0.35f, 0.65f, 1f, 1f), false, true);
            avatar.rectTransform.anchorMin = avatar.rectTransform.anchorMax = UIKit.TopLeft; avatar.rectTransform.pivot = UIKit.TopLeft;
            avatar.rectTransform.anchoredPosition = new Vector2(34f, -26f); avatar.rectTransform.sizeDelta = new Vector2(96f, 96f);
            var avatarIn = UIKit.Image(avatar.transform, "In", UITheme.Circle, new Color(0.08f, 0.25f, 0.68f, 1f), false, false);
            avatarIn.rectTransform.anchorMin = Vector2.zero; avatarIn.rectTransform.anchorMax = Vector2.one; avatarIn.rectTransform.offsetMin = new Vector2(4f, 4f); avatarIn.rectTransform.offsetMax = new Vector2(-4f, -4f);
            var avatarArt = UITheme.Art("avatar");
            var face = UIKit.Image(avatarIn.transform, "Face", avatarArt ?? UITheme.Icon(UIIcon.Person), Color.white, false, false);
            if (avatarArt != null) { face.rectTransform.anchorMin = Vector2.zero; face.rectTransform.anchorMax = Vector2.one; }
            else { face.rectTransform.anchorMin = new Vector2(0.2f, 0.2f); face.rectTransform.anchorMax = new Vector2(0.8f, 0.8f); }
            face.rectTransform.offsetMin = face.rectTransform.offsetMax = Vector2.zero;
            var profileBtn = avatar.gameObject.AddComponent<Button>(); profileBtn.targetGraphic = avatar; profileBtn.onClick.AddListener(OpenCareer);
            menuName = UIKit.TextAt(menu, "Name", "Player", 34f, Color.white, UIKit.TopLeft, new Vector2(150f, -24f), new Vector2(360f, 42f), TextAlignmentOptions.Left, true);
            menuLevel = UIKit.TextAt(menu, "Level", "Lv. 1", 26f, UITheme.Gold, UIKit.TopLeft, new Vector2(150f, -66f), new Vector2(120f, 34f), TextAlignmentOptions.Left, true);
            xpFill = UIKit.Bar(menu, "XpBar", UITheme.Gold, UIKit.TopLeft, new Vector2(262f, -76f), new Vector2(240f, 16f));
            menuXp = UIKit.TextAt(menu, "Xp", "0 / 100", 20f, UITheme.Muted, UIKit.TopLeft, new Vector2(150f, -102f), new Vector2(240f, 26f), TextAlignmentOptions.Left);

            // top-right: coins and gems (a plus beside each), then the gear
            var gear = UIKit.SmallButton(menu, "", new Color(0.2f, 0.28f, 0.45f, 1f), UIKit.TopRight, new Vector2(-30f, -30f), new Vector2(88f, 88f), MenuSettings);
            var gearIcon = UIKit.Image(gear.transform, "Gear", UITheme.Icon(UIIcon.Gear), Color.white, false, false);
            gearIcon.rectTransform.anchorMin = new Vector2(0.2f, 0.2f); gearIcon.rectTransform.anchorMax = new Vector2(0.8f, 0.8f); gearIcon.rectTransform.offsetMin = gearIcon.rectTransform.offsetMax = Vector2.zero;
            menuGems = UIKit.Pill(menu, "Gems", UIIcon.Gem, new Color(0.35f, 0.85f, 1f, 1f), "0", UIKit.TopRight, new Vector2(-138f, -40f), new Vector2(230f, 68f));
            UIKit.SmallButton(menu, "+", UITheme.Blue, UIKit.TopRight, new Vector2(-150f, -52f), new Vector2(44f, 44f), ClaimDailyIfReady, 32f);
            menuCoins = UIKit.Pill(menu, "Coins", UIIcon.Coin, UITheme.Gold, "0", UIKit.TopRight, new Vector2(-400f, -40f), new Vector2(260f, 68f));
            UIKit.SmallButton(menu, "+", UITheme.Blue, UIKit.TopRight, new Vector2(-412f, -52f), new Vector2(44f, 44f), ClaimDailyIfReady, 32f);

            // logo, top centre: crown, SOCCER FIGHT and the tagline
            var logoArt = UITheme.Art("logo");
            if (logoArt != null)
            {
                var logo = UIKit.Image(menu, "Logo", logoArt, Color.white, false, false);
                logo.preserveAspect = true;
                logo.rectTransform.anchorMin = logo.rectTransform.anchorMax = logo.rectTransform.pivot = new Vector2(0.5f, 1f);
                logo.rectTransform.anchoredPosition = new Vector2(0f, -4f); logo.rectTransform.sizeDelta = new Vector2(1080f, 316f);
            }
            else
            {
            var crown = UIKit.Image(menu, "Crown", UITheme.Icon(UIIcon.Crown), UITheme.Gold, false, false);
            crown.rectTransform.anchorMin = crown.rectTransform.anchorMax = crown.rectTransform.pivot = new Vector2(0.5f, 1f);
            crown.rectTransform.anchoredPosition = new Vector2(150f, -14f); crown.rectTransform.sizeDelta = new Vector2(84f, 64f);
            var soccer = UIKit.TextAt(menu, "TitleTop", "SOCCER", 130f, new Color(0.9f, 0.93f, 1f, 1f), new Vector2(0.5f, 1f), new Vector2(-250f, -62f), new Vector2(620f, 150f), TextAlignmentOptions.Right, true);
            var fight = UIKit.TextAt(menu, "TitleBottom", "FIGHT", 130f, UITheme.Gold, new Vector2(0.5f, 1f), new Vector2(375f, -62f), new Vector2(560f, 150f), TextAlignmentOptions.Left, true);
            soccer.fontStyle = fight.fontStyle = FontStyles.Bold | FontStyles.Italic;
            var tag = UIKit.TextAt(menu, "Tagline", "PLAY  •  COMPETE  •  WIN", 28f, new Color(0.85f, 0.9f, 1f, 0.9f), new Vector2(0.5f, 1f), new Vector2(0f, -212f), new Vector2(800f, 40f), TextAlignmentOptions.Center, true);
            tag.characterSpacing = 14f;
            }

            // the match ball, top right of the logo
            // left: a real 3D scene (trophy, spinning ball, sparkles on a glowing pedestal), behind the Daily Reward card
            MenuStage.Create(menu, out menuStage).rectTransform.SetSiblingIndex(2);
            var stageRt = menu.Find("StageView") as RectTransform;
            stageRt.anchorMin = stageRt.anchorMax = stageRt.pivot = new Vector2(0f, 0f);
            stageRt.anchoredPosition = new Vector2(30f, 0f); stageRt.sizeDelta = new Vector2(800f, 800f);

            // right: PLAY (gold, with the double chevron) and the four blue buttons under it
            var x = new Vector2(470f, 0f);
            var play = UIKit.MenuButton(menu, "Play", UIIcon.Ball, UITheme.Gold, UIKit.Center, x + new Vector2(0f, 120f), new Vector2(660f, 140f), MenuPlay, true, 62f);
            for (int c = 0; c < 2; c++)
            {
                var chev = UIKit.Image(play.transform, "Chevron" + c, UITheme.Icon(UIIcon.Chevron), UITheme.GoldText, false, false);
                chev.rectTransform.anchorMin = chev.rectTransform.anchorMax = new Vector2(1f, 0.5f); chev.rectTransform.pivot = new Vector2(1f, 0.5f);
                chev.rectTransform.anchoredPosition = new Vector2(-34f - c * 40f, 0f); chev.rectTransform.sizeDelta = new Vector2(64f, 64f);
            }
            var rowColor = new Color(0.1f, 0.3f, 0.78f, 1f);
            UIKit.MenuButton(menu, "Tournaments", UIIcon.Trophy, rowColor, UIKit.Center, x + new Vector2(0f, -40f), new Vector2(560f, 100f), MenuTournaments, false, 46f);
            var online = UIKit.MenuButton(menu, OnlineLocked ? "Online (Soon)" : "Online", UIIcon.Globe, OnlineLocked ? new Color(0.28f, 0.32f, 0.4f, 1f) : rowColor, UIKit.Center, x + new Vector2(0f, -152f), new Vector2(560f, 100f), MenuOnline, false, 46f);
            if (OnlineLocked) online.GetComponentInChildren<TMP_Text>().color = new Color(1f, 1f, 1f, 0.7f);
            UIKit.MenuButton(menu, "Teams", UIIcon.Shirt, rowColor, UIKit.Center, x + new Vector2(0f, -264f), new Vector2(560f, 100f), MenuTeams, false, 46f);
            UIKit.MenuButton(menu, "Settings", UIIcon.Gear, rowColor, UIKit.Center, x + new Vector2(0f, -376f), new Vector2(560f, 100f), MenuSettings, false, 46f);

            // bottom-left: Daily Reward - the whole card is the button
            var card = UIKit.GlassPanel(menu, "DailyCard", UIKit.BottomLeft, new Vector2(40f, 40f), new Vector2(560f, 150f), false);
            var giftArt = UITheme.Art("gift");
            var gift = UIKit.Image(card, "Gift", giftArt ?? UITheme.Icon(UIIcon.Gift), giftArt != null ? Color.white : UITheme.Gold, false, false);
            gift.preserveAspect = true;
            gift.rectTransform.anchorMin = gift.rectTransform.anchorMax = new Vector2(0f, 0.5f); gift.rectTransform.pivot = new Vector2(0f, 0.5f);
            gift.rectTransform.anchoredPosition = new Vector2(26f, 0f); gift.rectTransform.sizeDelta = new Vector2(104f, 104f);
            UIKit.TextAt(card, "DailyTitle", "Daily Reward", 42f, Color.white, UIKit.TopLeft, new Vector2(160f, -30f), new Vector2(380f, 50f), TextAlignmentOptions.Left, true);
            dailySub = UIKit.TextAt(card, "DailySub", "Claim Now!", 34f, UITheme.Gold, UIKit.TopLeft, new Vector2(160f, -86f), new Vector2(380f, 44f), TextAlignmentOptions.Left, true);
            var cardHit = card.parent.GetComponent<Image>(); cardHit.raycastTarget = true;
            dailyButton = card.parent.gameObject.AddComponent<Button>(); dailyButton.targetGraphic = cardHit; dailyButton.onClick.AddListener(ClaimDailyReward);
            dailyButtonLabel = null;
            // optional: with an ad service, a button beside the card claims the reward doubled (hidden while there is none, which is always for now)
            adButton = UIKit.SmallButton(menu, "Watch ad: 2x", UITheme.Purple, UIKit.BottomLeft, new Vector2(630f, 64f), new Vector2(330f, 86f), WatchDailyAd, 30f);
            adButton.gameObject.SetActive(false);

            UIKit.TextAt(menu, "Version", "v" + Application.version, 22f, new Color(1f, 1f, 1f, 0.45f), UIKit.BottomRight, new Vector2(-30f, 20f), new Vector2(200f, 30f), TextAlignmentOptions.Right);
            BuildSettingsPanel(root);
            BuildCareerPanel(root);
        }

        // ---- career: the player's own lifetime record (local; a real online leaderboard would need a server) ----
        void BuildCareerPanel(RectTransform root)
        {
            BuildProfile(root);
        }

        /// <summary>The lifetime record as shown on the Career screen.</summary>
        public string CareerStats()
        {
            int played = CareerRecord.Played;
            string rate = played == 0 ? "-" : $"{100f * CareerRecord.Won / played:0}%";
            string gd = (CareerRecord.GoalsFor - CareerRecord.GoalsAgainst).ToString("+0;-0;0");
            return $"<b>{PlayerProfile.Name}</b>\n<color=#FFC71F>Level {PlayerProfile.Level}</color>   {PlayerProfile.XpInLevel} / {PlayerProfile.XpPerLevel} XP   <color=#FFC71F>{PlayerProfile.Coins:N0} coins</color>\n\n"
                 + $"Matches played: {played}\nWon {CareerRecord.Won}   Drawn {CareerRecord.Drawn}   Lost {CareerRecord.Lost}\nWin rate: {rate}\n"
                 + $"Goals for {CareerRecord.GoalsFor}   against {CareerRecord.GoalsAgainst}   difference {gd}\n\n"
                 + "<size=28><color=#B0B8C8>Saved on this phone only. Training matches are not counted.</color></size>";
        }

        public string CareerShown => careerText != null ? careerText.text : "";
        public void OpenCareer() { careerText.text = CareerStats(); RefreshProfile(); careerPanel.transform.SetAsLastSibling(); careerPanel.SetActive(true); }
        public void CloseCareer() { if (careerPanel != null) careerPanel.SetActive(false); }

        // ---- settings: the designed screen with tabs (Audio, Graphics, Controls, Camera, About) ----
        const float SettingsW = 1500f, SettingsH = 800f;
        GameObject[] settingsTabPages;
        Image[] settingsTabBg, settingsTabIcon;
        TextMeshProUGUI[] settingsTabText;
        int settingsTab;
        Button settingsSoundSwitch, settingsCommentary, settingsCrowd, settingsVibration, settingsFpsSwitch, settingsRestore;
        Slider settingsMusic, settingsSfx;

        void BuildSettingsPanel(RectTransform root)
        {
            var p = UIKit.Screen(root, "Settings", new Vector2(SettingsW, SettingsH), "Settings", UIIcon.Gear, CloseSettings, SettingsHome, out settingsPanel);
            string[] names = { "Audio", "Graphics", "Controls", "Camera", "About" };
            UIIcon[] icons = { UIIcon.Speaker, UIIcon.Monitor, UIIcon.Gamepad, UIIcon.Camera, UIIcon.Info };
            settingsTabBg = new Image[5]; settingsTabIcon = new Image[5]; settingsTabText = new TextMeshProUGUI[5]; settingsTabPages = new GameObject[5];
            var column = UIKit.Card(p, "Tabs", UIKit.TopLeft, new Vector2(24f, -118f), new Vector2(320f, 560f), new Color(0.05f, 0.12f, 0.28f, 0.75f));
            for (int i = 0; i < 5; i++)
            {
                int tab = i;
                var bg = UIKit.Image(column, names[i] + "Tab", UITheme.Rounded, Color.white, true, true);
                bg.rectTransform.anchorMin = bg.rectTransform.anchorMax = bg.rectTransform.pivot = UIKit.TopLeft;
                bg.rectTransform.anchoredPosition = new Vector2(8f, -8f - i * 108f); bg.rectTransform.sizeDelta = new Vector2(304f, 98f);
                var btn = bg.gameObject.AddComponent<Button>(); btn.targetGraphic = bg; btn.onClick.AddListener(() => SelectSettingsTab(tab));
                UIKit.Raise(bg, UITheme.BlueDark);
                var ic = UIKit.Image(bg.transform, "Icon", UITheme.Icon(icons[i]), Color.white, false, false);
                ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0f, 0.5f); ic.rectTransform.pivot = new Vector2(0f, 0.5f);
                ic.rectTransform.anchoredPosition = new Vector2(22f, 0f); ic.rectTransform.sizeDelta = new Vector2(54f, 54f);
                var tx = UIKit.Text(bg.transform, "Label", names[i], 38f, Color.white, TextAlignmentOptions.Left, true);
                tx.rectTransform.offsetMin = new Vector2(96f, 0f); tx.rectTransform.offsetMax = Vector2.zero;
                settingsTabBg[i] = bg; settingsTabIcon[i] = ic; settingsTabText[i] = tx;
            }
            // the page area to the right of the tabs
            for (int i = 0; i < 5; i++)
            {
                var page = UIKit.Rect(p, names[i] + "Page", UIKit.TopLeft, new Vector2(372f, -118f), new Vector2(1104f, 560f));
                settingsTabPages[i] = page.gameObject;
            }
            BuildSettingsAudio(settingsTabPages[0].transform);
            BuildSettingsGraphics(settingsTabPages[1].transform);
            BuildSettingsControls(settingsTabPages[2].transform);
            BuildSettingsCamera(settingsTabPages[3].transform);
            BuildSettingsAbout(settingsTabPages[4].transform);

            settingsRestore = UIKit.ActionButton(p, "Restore Defaults", UIIcon.Refresh, false, UIKit.BottomLeft, new Vector2(372f, 24f), new Vector2(520f, 80f), RestoreSettingsDefaults, 36f);
            settingsRestore.GetComponent<Image>().color = new Color(0.08f, 0.16f, 0.34f, 1f);
            UIKit.ActionButton(p, "Save", UIIcon.Check, true, UIKit.BottomLeft, new Vector2(956f, 24f), new Vector2(520f, 80f), CloseSettings, 40f);
            settingsPanel.SetActive(false);
            SelectSettingsTab(0);
            BuildControlsPanel(root);
        }

        // one dark row: an icon and a label on the left; the control goes on the right
        RectTransform SettingsRow(Transform page, int index, UIIcon icon, string label)
        {
            var row = UIKit.Card(page, label + "Row", UIKit.TopLeft, new Vector2(0f, -index * 100f), new Vector2(1104f, 90f));
            var ic = UIKit.Image(row, "Icon", UITheme.Icon(icon), new Color(0.82f, 0.9f, 1f, 1f), false, false);
            ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0f, 0.5f); ic.rectTransform.pivot = new Vector2(0f, 0.5f);
            ic.rectTransform.anchoredPosition = new Vector2(26f, 0f); ic.rectTransform.sizeDelta = new Vector2(46f, 46f);
            UIKit.TextAt(row, "Label", label, 38f, Color.white, new Vector2(0f, 0.5f), new Vector2(100f, 0f), new Vector2(430f, 56f), TextAlignmentOptions.Left, true);
            return row;
        }

        void BuildSettingsAudio(Transform page)
        {
            var r0 = SettingsRow(page, 0, UIIcon.Music, "Music Volume");
            settingsMusic = UIKit.SliderRow(r0, "Music", new Vector2(0f, 0.5f), new Vector2(520f, 0f), 400f, FootballSim.Audio.MatchAudio.MusicVolume, v => FootballSim.Audio.MatchAudio.MusicVolume = v, out _);
            var r1 = SettingsRow(page, 1, UIIcon.Speaker, "SFX Volume");
            settingsSfx = UIKit.SliderRow(r1, "Sfx", new Vector2(0f, 0.5f), new Vector2(520f, 0f), 400f, FootballSim.Audio.MatchAudio.SfxVolume, v => FootballSim.Audio.MatchAudio.SfxVolume = v, out _);
            var r2 = SettingsRow(page, 2, UIIcon.Mic, "Commentary");
            settingsCommentary = UIKit.Switch(r2, "Commentary", new Vector2(1f, 0.5f), new Vector2(-100f, 0f), FootballSim.Audio.MatchAudio.CommentaryOn, on => FootballSim.Audio.MatchAudio.CommentaryOn = on);
            var r3 = SettingsRow(page, 3, UIIcon.People, "Crowd Sounds");
            settingsCrowd = UIKit.Switch(r3, "Crowd", new Vector2(1f, 0.5f), new Vector2(-100f, 0f), FootballSim.Audio.MatchAudio.CrowdOn, on => FootballSim.Audio.MatchAudio.CrowdOn = on);
            var r4 = SettingsRow(page, 4, UIIcon.Phone, "Vibration");
            settingsVibration = UIKit.Switch(r4, "Vibration", new Vector2(1f, 0.5f), new Vector2(-100f, 0f), true, on => { var tc = Touch(); if (tc != null) tc.SetTuning(tc.stickSensitivity, on); });
        }

        void BuildSettingsGraphics(Transform page)
        {
            var r0 = SettingsRow(page, 0, UIIcon.Monitor, "Graphics");
            settingsQuality = UIKit.Selector(r0, "Quality", new Vector2(1f, 0.5f), new Vector2(-300f, 0f), 520f, 70f, () => CycleQuality(-1), () => CycleQuality(1), 36f);
            var r1 = SettingsRow(page, 1, UIIcon.Clock, "Frame rate");
            settingsFps = UIKit.Selector(r1, "Fps", new Vector2(1f, 0.5f), new Vector2(-300f, 0f), 520f, 70f, ToggleFps, ToggleFps, 36f);
            var r2 = SettingsRow(page, 2, UIIcon.Bars, "Show FPS");
            settingsFpsSwitch = UIKit.Switch(r2, "ShowFps", new Vector2(1f, 0.5f), new Vector2(-100f, 0f), false, on => { if (on != showFps) ToggleShowFps(); });
            var r3 = SettingsRow(page, 3, UIIcon.Person, "Player models");
            settingsModels = UIKit.Selector(r3, "Models", new Vector2(1f, 0.5f), new Vector2(-300f, 0f), 520f, 70f, () => { CycleModels(); CycleModels(); }, CycleModels, 34f);
        }

        void BuildSettingsControls(Transform page)
        {
            var r0 = SettingsRow(page, 0, UIIcon.Gamepad, "Touch button size");
            settingsSize = UIKit.Selector(r0, "Size", new Vector2(1f, 0.5f), new Vector2(-300f, 0f), 520f, 70f, () => AdjustTouch(-0.1f, 0f), () => AdjustTouch(0.1f, 0f), 38f);
            var r1 = SettingsRow(page, 1, UIIcon.Gamepad, "Touch opacity");
            settingsOpacity = UIKit.Selector(r1, "Opacity", new Vector2(1f, 0.5f), new Vector2(-300f, 0f), 520f, 70f, () => AdjustTouch(0f, -0.1f), () => AdjustTouch(0f, 0.1f), 38f);
            var r2 = SettingsRow(page, 2, UIIcon.Gear, "Stick, hand, buttons");
            UIKit.ActionButton(r2, "Controls", null, false, new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(520f, 70f), OpenControls, 36f);
        }

        void BuildSettingsCamera(Transform page)
        {
            var r0 = SettingsRow(page, 0, UIIcon.Camera, "Camera");
            settingsCamera = UIKit.Selector(r0, "View", new Vector2(1f, 0.5f), new Vector2(-300f, 0f), 520f, 70f, () => StepCamera(-1), () => StepCamera(1), 36f);
            UIKit.TextAt(page, "Hint", "Broadcast: the side-on TV view.  Close: nearer the ball.  Wide: the whole pitch.  High: from above.", 28f, UITheme.Muted, UIKit.TopLeft, new Vector2(0f, -120f), new Vector2(1104f, 80f), TextAlignmentOptions.Left);
        }

        void BuildSettingsAbout(Transform page)
        {
            var r0 = SettingsRow(page, 0, UIIcon.Info, "Soccer Fight");
            UIKit.TextAt(r0, "Version", "v" + Application.version, 38f, UITheme.Gold, new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(360f, 56f), TextAlignmentOptions.Right, true);
            UIKit.TextAt(page, "Tagline", "Play offline, on Wi-Fi, or online worldwide. Your progress is saved on this phone only.", 28f, UITheme.Muted, UIKit.TopLeft, new Vector2(0f, -310f), new Vector2(1104f, 80f), TextAlignmentOptions.Left);
            var r1 = SettingsRow(page, 1, UIIcon.Speaker, "Master sound");
            settingsSoundSwitch = UIKit.Switch(r1, "Sound", new Vector2(1f, 0.5f), new Vector2(-100f, 0f), FootballSim.Audio.MatchAudio.SoundOn, on => { if (on != FootballSim.Audio.MatchAudio.SoundOn) ToggleSound(); });
            var r2 = SettingsRow(page, 2, UIIcon.Close, "Reset progress");
            var reset = UIKit.SmallButton(r2, "Reset progress", UITheme.Red, new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(520f, 70f), TapSettingsReset, 34f);
            settingsReset = reset.GetComponentInChildren<TextMeshProUGUI>();
        }

        void SelectSettingsTab(int tab)
        {
            settingsTab = Mathf.Clamp(tab, 0, 4);
            for (int i = 0; i < 5; i++)
            {
                bool on = i == settingsTab;
                settingsTabPages[i].SetActive(on);
                settingsTabBg[i].color = on ? UITheme.Gold : new Color(0.1f, 0.22f, 0.48f, 0.9f);
                settingsTabText[i].color = on ? UITheme.GoldText : Color.white;
                settingsTabIcon[i].color = on ? UITheme.GoldText : Color.white;
            }
            if (settingsRestore != null) settingsRestore.gameObject.SetActive(settingsTab != 4);
            RefreshSettings();
        }

        /// <summary>Puts the current tab's settings back to their defaults.</summary>
        public void RestoreSettingsDefaults()
        {
            switch (settingsTab)
            {
                case 0:
                {
                    FootballSim.Audio.MatchAudio.MusicVolume = 0.8f; FootballSim.Audio.MatchAudio.SfxVolume = 0.8f;
                    FootballSim.Audio.MatchAudio.CommentaryOn = true; FootballSim.Audio.MatchAudio.CrowdOn = true;
                    if (!FootballSim.Audio.MatchAudio.SoundOn) ToggleSound();
                    var tc = Touch(); if (tc != null) tc.SetTuning(tc.stickSensitivity, true);
                    break;
                }
                case 1:
                    Application.targetFrameRate = 60; try { PlayerPrefs.SetInt(FpsKey, 60); PlayerPrefs.Save(); } catch { }
                    if (showFps) ToggleShowFps();
                    FootballSim.Players.PlayerVisual.SetMode(FootballSim.Players.PlayerVisual.ModelMode.Off);
                    break;
                case 2: { var tc = Touch(); if (tc != null) tc.ResetToDefaults(); break; }
                case 3: { var c = Cam(); if (c != null) c.SetPreset(0); break; }
            }
            RefreshSettings();
        }

        /// <summary>Home from Settings: back to the main menu (from a paused match that leaves the match).</summary>
        void SettingsHome()
        {
            if (pausedOverSettings) { settingsPanel.SetActive(false); pausedOverSettings = false; GoToMainMenu(); return; }
            CloseSettings();
        }

        // ---- controls customisation: stick style, handedness, button size and opacity; the real controls show live while it is open ----
        void BuildControlsPanel(RectTransform root)
        {
            var p = UIKit.Screen(root, "Controls", new Vector2(1200f, 880f), "Controls", UIIcon.Gamepad, CloseControls, CloseControls, out controlsPanel);
            Row(p, "Stick", 250f, out ctlStyle, ToggleStick, "CHANGE");
            Row(p, "Hand", 170f, out ctlHand, ToggleHand, "CHANGE");
            RowPlusMinus(p, "Button size", 90f, out ctlSize, () => AdjustTouch(-0.1f, 0f), () => AdjustTouch(0.1f, 0f), UITheme.BlueDark);
            RowPlusMinus(p, "Opacity", 10f, out ctlOpacity, () => AdjustTouch(0f, -0.1f), () => AdjustTouch(0f, 0.1f), UITheme.BlueDark);
            RowPlusMinus(p, "Stick response", -70f, out ctlSens, () => AdjustSensitivity(-0.1f), () => AdjustSensitivity(0.1f), UITheme.BlueDark);
            Row(p, "Vibration", -150f, out ctlBuzz, ToggleBuzz, "CHANGE");
            Row(p, "Auto sprint", -230f, out ctlAuto, ToggleAuto, "CHANGE");
            UIKit.TextAt(p, "Note", "Auto sprint: push the stick all the way to run.", 28f, UITheme.Muted, UIKit.Center, new Vector2(0f, -292f), new Vector2(900f, 36f));
            UIKit.SmallButton(p, "Move buttons", UITheme.Blue, UIKit.Center, new Vector2(-280f, -365f), new Vector2(480f, 80f), StartMovingButtons, 34f);
            UIKit.SmallButton(p, "Reset controls", UITheme.Red, UIKit.Center, new Vector2(280f, -365f), new Vector2(480f, 80f), ResetControls, 34f);
            controlsPanel.SetActive(false);

            // while the buttons are being moved the big panel gets out of the way and only this bar stays
            var holder = UIKit.Rect(root, "MoveBar", UIKit.Center, new Vector2(0f, 400f), new Vector2(1000f, 150f));
            moveBar = holder.gameObject;
            var rim = UIKit.Image(holder, "Rim", UITheme.Rounded, new Color(0.22f, 0.58f, 1f, 1f), true, true);
            rim.rectTransform.anchorMin = Vector2.zero; rim.rectTransform.anchorMax = Vector2.one; rim.rectTransform.offsetMin = rim.rectTransform.offsetMax = Vector2.zero;
            var inner = UIKit.Image(rim.transform, "Card", UITheme.Rounded, new Color(0.04f, 0.09f, 0.22f, 0.96f), true, false);
            inner.rectTransform.anchorMin = Vector2.zero; inner.rectTransform.anchorMax = Vector2.one; inner.rectTransform.offsetMin = new Vector2(3f, 3f); inner.rectTransform.offsetMax = new Vector2(-3f, -3f);
            var bar = inner.rectTransform;
            UIKit.TextAt(bar, "Hint", "Drag a button to where your thumb likes it", 36f, Color.white, UIKit.Center, new Vector2(0f, 38f), new Vector2(900f, 50f), TextAlignmentOptions.Center, true);
            UIKit.SmallButton(bar, "Done", UITheme.Blue, UIKit.Center, new Vector2(-150f, -32f), new Vector2(260f, 64f), StopMovingButtons, 32f);
            UIKit.SmallButton(bar, "Put back", UITheme.Slate, UIKit.Center, new Vector2(150f, -32f), new Vector2(260f, 64f), PutButtonsBack, 32f);
            moveBar.SetActive(false);
        }

        // the live buttons only show behind this panel when it was opened from a paused match; from the main menu they would draw over the menus
        public void OpenControls() { RefreshControls(); controlsPanel.SetActive(true); var tc = Touch(); if (tc != null) tc.SetPreview(pausedOverSettings); }

        public void CloseControls()
        {
            StopMovingButtons();
            if (controlsPanel == null || !controlsPanel.activeSelf) return;
            controlsPanel.SetActive(false);
            var tc = Touch(); if (tc != null) tc.SetPreview(false);
        }

        public bool MovingButtons => moveBar != null && moveBar.activeSelf;

        public void StartMovingButtons()
        {
            var tc = Touch(); if (tc == null) return;
            controlsPanel.SetActive(false); moveBar.SetActive(true);
            tc.SetPreview(true); tc.SetEditing(true);
        }

        public void StopMovingButtons()
        {
            if (moveBar == null || !moveBar.activeSelf) return;
            moveBar.SetActive(false);
            var tc = Touch(); if (tc != null) tc.SetEditing(false);
            RefreshControls(); controlsPanel.SetActive(true);
        }

        void PutButtonsBack() { var tc = Touch(); if (tc != null) tc.ResetPositions(); }

        void AdjustSensitivity(float d)
        {
            var tc = Touch(); if (tc == null) return;
            tc.SetTuning(Mathf.Round((tc.stickSensitivity + d) * 10f) / 10f, tc.haptics);
            RefreshControls();
        }

        public void ToggleAuto() { var tc = Touch(); if (tc == null) return; tc.SetAutoSprint(!tc.autoSprint); RefreshControls(); }

        public void ToggleBuzz() { var tc = Touch(); if (tc == null) return; tc.SetTuning(tc.stickSensitivity, !tc.haptics); RefreshControls(); }

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
            ctlSens.text = tc == null ? "-" : $"{tc.stickSensitivity * 100f:0}%";
            ctlBuzz.text = tc == null ? "-" : tc.haptics ? "On" : "Off";
            ctlAuto.text = tc == null ? "-" : tc.autoSprint ? "On" : "Off";
        }

        /// <summary>A faint rounded bar behind a settings row, so the label and its button read as one line.</summary>
        static void RowStripe(RectTransform parent, float y)
        {
            var bar = UIKit.Image(parent, "RowStripe", UITheme.Rounded, new Color(0.35f, 0.5f, 0.85f, 0.10f), false, false); bar.type = Image.Type.Sliced;
            bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            bar.rectTransform.anchoredPosition = new Vector2(0f, y); bar.rectTransform.sizeDelta = new Vector2(1040f, 72f);
        }

        void Row(RectTransform parent, string label, float y, out TextMeshProUGUI value, UnityEngine.Events.UnityAction onClick, string button)
        {
            RowStripe(parent, y);
            UIKit.TextAt(parent, label + "Label", label, 38f, Color.white, UIKit.Center, new Vector2(-250f, y), new Vector2(480f, 50f), TextAlignmentOptions.Left, true);
            var b = UIKit.SmallButton(parent, "", UITheme.BlueDark, UIKit.Center, new Vector2(250f, y), new Vector2(420f, 70f), onClick, 34f);
            value = b.GetComponentInChildren<TextMeshProUGUI>();
        }

        void RowPlusMinus(RectTransform parent, string label, float y, out TextMeshProUGUI value, UnityEngine.Events.UnityAction minus, UnityEngine.Events.UnityAction plus, Color c)
        {
            RowStripe(parent, y);
            UIKit.TextAt(parent, label + "Label", label, 38f, Color.white, UIKit.Center, new Vector2(-250f, y), new Vector2(480f, 50f), TextAlignmentOptions.Left, true);
            UIKit.SmallButton(parent, "-", c, UIKit.Center, new Vector2(100f, y), new Vector2(90f, 70f), minus, 46f);
            value = UIKit.TextAt(parent, label + "Value", "", 36f, Color.white, UIKit.Center, new Vector2(250f, y), new Vector2(200f, 50f), TextAlignmentOptions.Center, true);
            UIKit.SmallButton(parent, "+", c, UIKit.Center, new Vector2(400f, y), new Vector2(90f, 70f), plus, 46f);
        }

        void RefreshSettings()
        {
            if (settingsSoundSwitch == null || settingsReset == null) return;
            UIKit.SetSwitch(settingsSoundSwitch, FootballSim.Audio.MatchAudio.SoundOn);
            UIKit.SetSwitch(settingsCommentary, FootballSim.Audio.MatchAudio.CommentaryOn);
            UIKit.SetSwitch(settingsCrowd, FootballSim.Audio.MatchAudio.CrowdOn);
            if (settingsMusic != null) settingsMusic.SetValueWithoutNotify(FootballSim.Audio.MatchAudio.MusicVolume);
            if (settingsSfx != null) settingsSfx.SetValueWithoutNotify(FootballSim.Audio.MatchAudio.SfxVolume);
            var tc = Touch();
            UIKit.SetSwitch(settingsVibration, tc == null || tc.haptics);
            UIKit.SetSwitch(settingsFpsSwitch, showFps);
            settingsSize.text = tc == null ? "-" : $"{tc.controlScale * 100f:0}%";
            settingsOpacity.text = tc == null ? "-" : $"{tc.controlOpacity * 100f:0}%";
            var names = QualitySettings.names;
            settingsQuality.text = names.Length == 0 ? "-" : names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, names.Length - 1)];
            settingsFps.text = Application.targetFrameRate > 0 ? Application.targetFrameRate + " FPS" : "Max";
            if (settingsModels != null) settingsModels.text = ModelsLabel(FootballSim.Players.PlayerVisual.Mode);
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
        public void CycleCamera() { StepCamera(1); }

        void StepCamera(int dir)
        {
            var c = Cam(); if (c == null) return;
            int n = FootballSim.CameraSystem.BroadcastCamera.PresetNames.Length;
            c.SetPreset((c.Preset + dir + n) % n); RefreshSettings();
        }

        void CycleQuality(int dir = 1)
        {
            int n = QualitySettings.names.Length;
            if (n == 0) return;
            int next = (QualitySettings.GetQualityLevel() + dir + n) % n;
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
