using UnityEngine;
using UnityEngine.UI;
using FootballSim.Core;
using FootballSim.FootballAI;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.UI
{
    /// <summary>The Teams screen: pick the formation and shirt colour for your side and see your team's level, without starting the match setup.</summary>
    public partial class MatchHUD
    {
        GameObject teamsPanel;
        Text teamsFormationLabel, teamsKitLabel, teamsInfoLabel;

        public bool TeamsOpen => teamsPanel != null && teamsPanel.activeSelf;

        public void OpenTeams()
        {
            if (teamsPanel == null) BuildTeams();
            RefreshTeams();
            teamsPanel.transform.SetAsLastSibling();
            teamsPanel.SetActive(true);
        }

        public void CloseTeams() { if (teamsPanel != null) teamsPanel.SetActive(false); }

        Text teamsHomeClubLabel, teamsAwayClubLabel;

        void CycleClub(bool homeSide, int dir)
        {
            TeamSelection.Cycle(homeSide, dir);
            ApplyTeams();
            RefreshTeams();
        }

        static Color Readable(Color c) { return (c.r + c.g + c.b) / 3f < 0.35f ? Color.Lerp(c, Color.white, 0.6f) : c; }

        void RefreshTeams()
        {
            teamsFormationLabel.text = FormationData.PresetNames[selectedFormation];
            teamsKitLabel.text = KitPalette.Names[selectedKit];
            teamsKitLabel.color = KitPalette.Colors[selectedKit];
            if (teamsHomeClubLabel != null)
            {
                FootballSim.Players.TeamKits.Resolve(selectedKit, out var hk, out var ak, out _, out _);
                teamsHomeClubLabel.text = TeamSelection.NameOf(TeamSelection.Home, "Plain colours"); teamsHomeClubLabel.color = Readable(hk.shirt);
                teamsAwayClubLabel.text = TeamSelection.NameOf(TeamSelection.Away, "Plain colours"); teamsAwayClubLabel.color = Readable(ak.shirt);
            }
            int level = FootballSim.Players.TeamGrowth.CurrentLevel;
            teamsInfoLabel.text = $"Team level {level} of {FootballSim.Players.TeamGrowth.MaxLevel}\n" +
                                  $"Win matches to level up: every {FootballSim.Players.TeamGrowth.PointsPerLevel} points (win = 2, draw = 1) adds +1 to your players.";
        }

        GameObject teamsCard; // the fitted container: title, card and buttons scale together to fit the screen

        void BuildTeams()
        {
            var root = startPanel.transform.parent as RectTransform;
            var rt = Rect("TeamsPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UITheme.Navy);
            rt.pivot = new Vector2(0.5f, 0.5f);
            teamsPanel = rt.gameObject;
            var holder = Rect("Content", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560f, 860f), new Color(0f, 0f, 0f, 0f));
            holder.GetComponent<Image>().raycastTarget = false;
            teamsCard = holder.gameObject;

            var title = Label("Title", holder, 66, TextAnchor.MiddleCenter, new Vector2(0f, 385f), new Vector2(1000f, 90f));
            title.text = "TEAMS"; title.fontStyle = FontStyle.Bold; title.color = UITheme.Gold;

            var card = Rect("Card", holder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(1560f, 620f), Color.white);
            Glassify(card.gameObject);
            var divider = UIKit.Image(card, "Divider", UITheme.Rounded, new Color(1f, 1f, 1f, 0.14f), false, false);
            divider.rectTransform.anchorMin = divider.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            divider.rectTransform.anchoredPosition = new Vector2(0f, 90f); divider.rectTransform.sizeDelta = new Vector2(3f, 360f);

            // left: formation
            var f = Label("FormationTitle", card, 38, TextAnchor.MiddleCenter, new Vector2(-390f, 215f), new Vector2(700f, 60f));
            f.text = "Formation"; f.color = UITheme.Muted;
            Button(card, "<", new Vector2(-640f, 110f), new Vector2(110f, 96f), () => { CycleFormation(-1); RefreshTeams(); }, new Color(0.2f, 0.3f, 0.5f));
            Button(card, ">", new Vector2(-140f, 110f), new Vector2(110f, 96f), () => { CycleFormation(1); RefreshTeams(); }, new Color(0.2f, 0.3f, 0.5f));
            teamsFormationLabel = Label("Formation", card, 70, TextAnchor.MiddleCenter, new Vector2(-390f, 110f), new Vector2(380f, 100f));
            teamsFormationLabel.fontStyle = FontStyle.Bold;

            // right: shirt colour
            var k = Label("KitTitle", card, 38, TextAnchor.MiddleCenter, new Vector2(390f, 215f), new Vector2(700f, 60f));
            k.text = "Shirt colour"; k.color = UITheme.Muted;
            float gap = 100f, x0 = 390f - gap * (KitPalette.Count - 1) * 0.5f;
            for (int i = 0; i < KitPalette.Count; i++)
            {
                int kit = i;
                var sw = Rect("Swatch" + i, card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x0 + gap * i, 110f), new Vector2(84f, 84f), KitPalette.Colors[i]);
                sw.GetComponent<Image>().sprite = UITheme.Circle;
                var b = sw.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = sw.GetComponent<Image>();
                b.onClick.AddListener(() => { SetHomeKit(kit); RefreshTeams(); });
            }
            teamsKitLabel = Label("KitName", card, 48, TextAnchor.MiddleCenter, new Vector2(390f, 10f), new Vector2(700f, 70f));
            teamsKitLabel.fontStyle = FontStyle.Bold;

            // clubs: real sides with their strips and player names, for you and for the opponent (or plain colours)
            var btnCol = new Color(0.2f, 0.3f, 0.5f);
            var cap1 = Label("HomeClubTitle", card, 32, TextAnchor.MiddleCenter, new Vector2(-390f, -45f), new Vector2(700f, 50f)); cap1.text = "Your club"; cap1.color = UITheme.Muted;
            var cap2 = Label("AwayClubTitle", card, 32, TextAnchor.MiddleCenter, new Vector2(390f, -45f), new Vector2(700f, 50f)); cap2.text = "Opponent"; cap2.color = UITheme.Muted;
            Button(card, "<", new Vector2(-640f, -115f), new Vector2(110f, 84f), () => CycleClub(true, -1), btnCol);
            Button(card, ">", new Vector2(-140f, -115f), new Vector2(110f, 84f), () => CycleClub(true, 1), btnCol);
            Button(card, "<", new Vector2(140f, -115f), new Vector2(110f, 84f), () => CycleClub(false, -1), btnCol);
            Button(card, ">", new Vector2(640f, -115f), new Vector2(110f, 84f), () => CycleClub(false, 1), btnCol);
            teamsHomeClubLabel = Label("HomeClub", card, 40, TextAnchor.MiddleCenter, new Vector2(-390f, -115f), new Vector2(380f, 80f)); teamsHomeClubLabel.fontStyle = FontStyle.Bold;
            teamsAwayClubLabel = Label("AwayClub", card, 40, TextAnchor.MiddleCenter, new Vector2(390f, -115f), new Vector2(380f, 80f)); teamsAwayClubLabel.fontStyle = FontStyle.Bold;
            teamsHomeClubLabel.horizontalOverflow = teamsAwayClubLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            // bottom of the card: how the team levels up
            teamsInfoLabel = Label("Info", card, 30, TextAnchor.MiddleCenter, new Vector2(0f, -235f), new Vector2(1400f, 110f));
            teamsInfoLabel.horizontalOverflow = HorizontalWrapMode.Wrap; teamsInfoLabel.color = UITheme.Muted;

            Button(holder, "BACK", new Vector2(-300f, -385f), new Vector2(400f, 90f), CloseTeams, new Color(0.2f, 0.3f, 0.5f));
            Button(holder, "PLAY", new Vector2(300f, -385f), new Vector2(400f, 90f), () => { CloseTeams(); MenuPlay(); }, Accent);
        }
    }
}
