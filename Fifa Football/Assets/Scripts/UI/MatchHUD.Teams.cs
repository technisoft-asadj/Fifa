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

        void RefreshTeams()
        {
            teamsFormationLabel.text = FormationData.PresetNames[selectedFormation];
            teamsKitLabel.text = KitPalette.Names[selectedKit];
            teamsKitLabel.color = KitPalette.Colors[selectedKit];
            int level = FootballSim.Players.TeamGrowth.CurrentLevel;
            teamsInfoLabel.text = $"Team level {level} of {FootballSim.Players.TeamGrowth.MaxLevel}\n" +
                                  $"Win matches to level up: every {FootballSim.Players.TeamGrowth.PointsPerLevel} points (win = 2, draw = 1) adds +1 to your players.";
        }

        void BuildTeams()
        {
            var root = startPanel.transform.parent as RectTransform;
            var rt = Rect("TeamsPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UITheme.Navy);
            rt.pivot = new Vector2(0.5f, 0.5f);
            teamsPanel = rt.gameObject;

            var title = Label("Title", rt, 70, TextAnchor.MiddleCenter, new Vector2(0f, 420f), new Vector2(1000f, 100f));
            title.text = "TEAMS"; title.fontStyle = FontStyle.Bold;

            var card = Rect("Card", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1500f, 760f), Color.white);
            Glassify(card.gameObject);

            var f = Label("FormationTitle", card, 36, TextAnchor.MiddleCenter, new Vector2(0f, 280f), new Vector2(900f, 60f));
            f.text = "Formation"; f.color = UITheme.Muted;
            Button(card, "<", new Vector2(-330f, 200f), new Vector2(110f, 90f), () => { CycleFormation(-1); RefreshTeams(); }, new Color(0.2f, 0.3f, 0.5f));
            Button(card, ">", new Vector2(330f, 200f), new Vector2(110f, 90f), () => { CycleFormation(1); RefreshTeams(); }, new Color(0.2f, 0.3f, 0.5f));
            teamsFormationLabel = Label("Formation", card, 64, TextAnchor.MiddleCenter, new Vector2(0f, 200f), new Vector2(500f, 100f));
            teamsFormationLabel.fontStyle = FontStyle.Bold;

            var k = Label("KitTitle", card, 36, TextAnchor.MiddleCenter, new Vector2(0f, 90f), new Vector2(900f, 60f));
            k.text = "Shirt colour"; k.color = UITheme.Muted;
            float gap = 100f, x0 = -gap * (KitPalette.Count - 1) * 0.5f;
            for (int i = 0; i < KitPalette.Count; i++)
            {
                int kit = i;
                var sw = Rect("Swatch" + i, card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x0 + gap * i, 10f), new Vector2(80f, 80f), KitPalette.Colors[i]);
                sw.GetComponent<Image>().sprite = UITheme.Circle;
                var b = sw.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = sw.GetComponent<Image>();
                b.onClick.AddListener(() => { SetHomeKit(kit); RefreshTeams(); });
            }
            teamsKitLabel = Label("KitName", card, 44, TextAnchor.MiddleCenter, new Vector2(0f, -80f), new Vector2(900f, 70f));
            teamsKitLabel.fontStyle = FontStyle.Bold;

            teamsInfoLabel = Label("Info", card, 34, TextAnchor.MiddleCenter, new Vector2(0f, -230f), new Vector2(1300f, 160f));
            teamsInfoLabel.horizontalOverflow = HorizontalWrapMode.Wrap; teamsInfoLabel.color = UITheme.Muted;

            Button(rt, "BACK", new Vector2(-300f, -440f), new Vector2(400f, 100f), CloseTeams, new Color(0.2f, 0.3f, 0.5f));
            Button(rt, "PLAY", new Vector2(300f, -440f), new Vector2(400f, 100f), () => { CloseTeams(); MenuPlay(); }, Accent);
        }
    }
}
