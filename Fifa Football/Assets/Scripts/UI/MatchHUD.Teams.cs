using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FootballSim.Core;
using FootballSim.FootballAI;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.UI
{
    /// <summary>
    /// The "Select Your Team" screen: your club with its crest and strengths, the formation on a small pitch, the shirt colour and the opponent.
    /// The strengths are worked out from what is really chosen (how many defenders, midfielders and attackers the formation has, plus the level the
    /// team has earned), so they move when the formation changes.
    /// </summary>
    public partial class MatchHUD
    {
        GameObject teamsPanel;
        TextMeshProUGUI teamsFormationLabel, teamsKitLabel, teamsInfoLabel, teamsHomeClubLabel, teamsAwayClubLabel;
        RectTransform teamsHomeCrest, teamsAwayCrest, teamsPitch;
        RectTransform[] teamsBars = new RectTransform[4];
        TextMeshProUGUI[] teamsBarValues = new TextMeshProUGUI[4];
        Image[] teamsKitFrames, teamsDots;
        RectTransform[] teamsRings;

        public bool TeamsOpen => teamsPanel != null && teamsPanel.activeSelf;

        public void OpenTeams()
        {
            if (teamsPanel == null) BuildTeams();
            RefreshTeams();
            teamsPanel.transform.SetAsLastSibling();
            teamsPanel.SetActive(true);
        }

        public void CloseTeams() { if (teamsPanel != null) teamsPanel.SetActive(false); }

        void CycleClub(bool homeSide, int dir)
        {
            TeamSelection.Cycle(homeSide, dir);
            ApplyTeams();
            RefreshTeams();
        }

        static Color Readable(Color c) { return (c.r + c.g + c.b) / 3f < 0.35f ? Color.Lerp(c, Color.white, 0.6f) : c; }

        /// <summary>Attack, Midfield, Defense and Overall (0-99) for a formation at a team level.</summary>
        public static void Strengths(int formation, int level, out int attack, out int midfield, out int defense, out int overall)
        {
            var slots = FormationData.Slots(formation);
            int d = 0, m = 0, f = 0;
            for (int i = 1; i < slots.Length; i++) { float y = slots[i].y; if (y < 0.36f) d++; else if (y < 0.76f) m++; else f++; }
            int basis = 62 + level * 4;
            attack = Mathf.Clamp(basis + (f - 2) * 6, 1, 99);
            midfield = Mathf.Clamp(basis + (m - 4) * 4, 1, 99);
            defense = Mathf.Clamp(basis + (d - 4) * 6, 1, 99);
            overall = Mathf.Clamp(Mathf.RoundToInt((attack + midfield + defense) / 3f), 1, 99);
        }

        void RefreshTeams()
        {
            teamsFormationLabel.text = FormationData.PresetNames[selectedFormation];
            FootballSim.Players.TeamKits.Resolve(selectedKit, out var hk, out var ak, out _, out _);
            teamsHomeClubLabel.text = TeamSelection.NameOf(TeamSelection.Home, "Plain colours");
            teamsAwayClubLabel.text = TeamSelection.NameOf(TeamSelection.Away, "Plain colours");
            teamsKitLabel.text = TeamSelection.Home >= 0 ? "Strip set by the club" : KitPalette.Names[selectedKit];
            RecolourCrest(teamsHomeCrest, hk.shirt, hk.secondary, TeamSelection.Home);
            RecolourCrest(teamsAwayCrest, ak.shirt, ak.secondary, TeamSelection.Away);
            int level = FootballSim.Players.TeamGrowth.CurrentLevel;
            Strengths(selectedFormation, level, out int att, out int mid, out int def, out int all);
            int[] values = { att, mid, def, all };
            for (int i = 0; i < 4; i++) { UIKit.SetBar(teamsBars[i], values[i] / 100f); teamsBarValues[i].text = values[i].ToString(); }
            teamsInfoLabel.text = $"Team level {level} of {FootballSim.Players.TeamGrowth.MaxLevel}\nWin matches to level up: every {FootballSim.Players.TeamGrowth.PointsPerLevel} points (win = 2, draw = 1) adds +1 to your players.";
            for (int i = 0; i < teamsKitFrames.Length; i++) teamsKitFrames[i].color = i == selectedKit && TeamSelection.Home < 0 ? UITheme.Gold : new Color(0.02f, 0.06f, 0.16f, 0.9f);
            PlaceFormationDots(hk.shirt);
        }

        void PlaceFormationDots(Color shirt)
        {
            var slots = FormationData.Slots(selectedFormation);
            Vector2 size = teamsPitch.sizeDelta;
            for (int i = 0; i < teamsDots.Length && i < slots.Length; i++)
            {
                var rt = teamsRings[i];
                rt.anchoredPosition = new Vector2(size.x * 0.5f + slots[i].x * size.x * 0.4f - 23f, 44f + slots[i].y * (size.y - 100f) - 23f);
                teamsDots[i].color = i == 0 ? new Color(1f, 0.85f, 0.2f) : shirt;
            }
        }

        void BuildTeams()
        {
            var root = startPanel.transform.parent as RectTransform;
            var p = UIKit.Screen(root, "TeamsPanel", new Vector2(SettingsW, SettingsH), "Select Your Team", null, CloseTeams, CloseTeams, out teamsPanel);

            // left: your club
            teamsHomeCrest = UIKit.Crest(p, "ClubCrest", Color.white, Color.white, UIKit.TopLeft, new Vector2(120f, -132f), 210f);
            UIKit.Arrow(p, "ClubPrev", UIKit.TopLeft, new Vector2(40f, -372f), 60f, true, () => CycleClub(true, -1));
            UIKit.Arrow(p, "ClubNext", UIKit.TopLeft, new Vector2(340f, -372f), 60f, false, () => CycleClub(true, 1));
            teamsHomeClubLabel = UIKit.TextAt(p, "HomeClub", "", 34f, Color.white, UIKit.TopLeft, new Vector2(108f, -372f), new Vector2(224f, 60f), TextAlignmentOptions.Center, true);
            teamsHomeClubLabel.enableAutoSizing = true; teamsHomeClubLabel.fontSizeMin = 18f; teamsHomeClubLabel.fontSizeMax = 34f;
            string[] names = { "Attack", "Midfield", "Defense", "Overall" };
            for (int i = 0; i < 4; i++)
            {
                float y = -452f - i * 58f;
                UIKit.TextAt(p, names[i] + "Name", names[i], 28f, Color.white, UIKit.TopLeft, new Vector2(40f, y), new Vector2(150f, 40f), TextAlignmentOptions.Left, true);
                teamsBars[i] = UIKit.Bar(p, names[i] + "Bar", new Color(0.25f, 0.85f, 0.3f, 1f), UIKit.TopLeft, new Vector2(196f, y - 12f), new Vector2(150f, 16f));
                teamsBarValues[i] = UIKit.TextAt(p, names[i] + "Value", "0", 28f, Color.white, UIKit.TopLeft, new Vector2(354f, y), new Vector2(60f, 40f), TextAlignmentOptions.Right, true);
            }
            teamsInfoLabel = UIKit.TextAt(p, "Info", "", 20f, UITheme.Muted, UIKit.TopLeft, new Vector2(40f, -690f), new Vector2(380f, 80f), TextAlignmentOptions.TopLeft, false);
            teamsInfoLabel.textWrappingMode = TextWrappingModes.Normal;

            // centre: the formation on a small pitch
            var pitchCard = UIKit.Card(p, "PitchCard", UIKit.TopLeft, new Vector2(446f, -118f), new Vector2(600f, 490f), new Color(0.05f, 0.1f, 0.2f, 0.9f));
            teamsPitch = UIKit.Rect(pitchCard, "Pitch", UIKit.TopLeft, new Vector2(20f, -20f), new Vector2(560f, 450f));
            var grass = UIKit.Image(teamsPitch, "Grass", UITheme.Rounded, new Color(0.18f, 0.55f, 0.2f, 1f), true, false);
            grass.rectTransform.anchorMin = Vector2.zero; grass.rectTransform.anchorMax = Vector2.one; grass.rectTransform.offsetMin = grass.rectTransform.offsetMax = Vector2.zero;
            for (int s = 0; s < 6; s++) // the mown stripes
            {
                var stripe = UIKit.Image(teamsPitch, "Stripe" + s, UITheme.Rounded, new Color(1f, 1f, 1f, s % 2 == 0 ? 0.05f : 0f), false, false);
                stripe.rectTransform.anchorMin = new Vector2(0f, s / 6f); stripe.rectTransform.anchorMax = new Vector2(1f, (s + 1) / 6f); stripe.rectTransform.offsetMin = new Vector2(8f, 0f); stripe.rectTransform.offsetMax = new Vector2(-8f, 0f);
            }
            Color line = new Color(1f, 1f, 1f, 0.8f);
            PitchLine(teamsPitch, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(520f, 3f), line);         // halfway line
            PitchLine(teamsPitch, new Vector2(0.5f, 0.5f), new Vector2(0f, 205f), new Vector2(520f, 3f), line);       // top line
            PitchLine(teamsPitch, new Vector2(0.5f, 0.5f), new Vector2(0f, -205f), new Vector2(520f, 3f), line);      // bottom line
            PitchLine(teamsPitch, new Vector2(0.5f, 0.5f), new Vector2(-260f, 0f), new Vector2(3f, 410f), line);
            PitchLine(teamsPitch, new Vector2(0.5f, 0.5f), new Vector2(260f, 0f), new Vector2(3f, 410f), line);
            PitchLine(teamsPitch, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(220f, 3f), line);       // penalty boxes
            PitchLine(teamsPitch, new Vector2(0.5f, 0.5f), new Vector2(-110f, 178f), new Vector2(3f, 56f), line);
            PitchLine(teamsPitch, new Vector2(0.5f, 0.5f), new Vector2(110f, 178f), new Vector2(3f, 56f), line);
            PitchLine(teamsPitch, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(220f, 3f), line);
            PitchLine(teamsPitch, new Vector2(0.5f, 0.5f), new Vector2(-110f, -178f), new Vector2(3f, 56f), line);
            PitchLine(teamsPitch, new Vector2(0.5f, 0.5f), new Vector2(110f, -178f), new Vector2(3f, 56f), line);
            var circle = UIKit.Image(teamsPitch, "CentreCircle", UITheme.Circle, line, false, false);
            circle.rectTransform.anchorMin = circle.rectTransform.anchorMax = circle.rectTransform.pivot = new Vector2(0.5f, 0.5f); circle.rectTransform.sizeDelta = new Vector2(110f, 110f);
            var circleIn = UIKit.Image(circle.transform, "In", UITheme.Circle, new Color(0.18f, 0.55f, 0.2f, 1f), false, false);
            circleIn.rectTransform.anchorMin = Vector2.zero; circleIn.rectTransform.anchorMax = Vector2.one; circleIn.rectTransform.offsetMin = new Vector2(3f, 3f); circleIn.rectTransform.offsetMax = new Vector2(-3f, -3f);
            teamsDots = new Image[11]; teamsRings = new RectTransform[11];
            for (int i = 0; i < 11; i++)
            {
                var ring = UIKit.Image(teamsPitch, "Player" + i, UITheme.Circle, Color.white, false, false);
                ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = ring.rectTransform.pivot = Vector2.zero; ring.rectTransform.sizeDelta = new Vector2(46f, 46f);
                var dot = UIKit.Image(ring.transform, "Dot", UITheme.Circle, Color.white, false, false);
                dot.rectTransform.anchorMin = Vector2.zero; dot.rectTransform.anchorMax = Vector2.one; dot.rectTransform.offsetMin = new Vector2(4f, 4f); dot.rectTransform.offsetMax = new Vector2(-4f, -4f);
                teamsDots[i] = dot; teamsRings[i] = ring.rectTransform;
                // PlaceFormationDots moves the ring, which is the dot's parent
            }
            UIKit.Arrow(p, "FormationPrev", UIKit.TopLeft, new Vector2(520f, -634f), 66f, true, () => { CycleFormation(-1); RefreshTeams(); });
            UIKit.Arrow(p, "FormationNext", UIKit.TopLeft, new Vector2(914f, -634f), 66f, false, () => { CycleFormation(1); RefreshTeams(); });
            teamsFormationLabel = UIKit.TextAt(p, "Formation", "", 50f, Color.white, UIKit.TopLeft, new Vector2(596f, -630f), new Vector2(310f, 74f), TextAlignmentOptions.Center, true);

            // right: kit colour and opponent
            var kitCard = UIKit.Card(p, "KitCard", UIKit.TopLeft, new Vector2(1076f, -118f), new Vector2(394f, 262f));
            UIKit.TextAt(kitCard, "KitTitle", "Kit Color", 30f, Color.white, UIKit.TopLeft, new Vector2(0f, -10f), new Vector2(394f, 40f), TextAlignmentOptions.Center, true);
            teamsKitFrames = new Image[KitPalette.Count];
            for (int k = 0; k < KitPalette.Count; k++)
            {
                int kit = k;
                var frame = UIKit.Image(kitCard, "Kit" + k, UITheme.Rounded, Color.white, true, true);
                frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = frame.rectTransform.pivot = UIKit.TopLeft;
                frame.rectTransform.anchoredPosition = new Vector2(24f + (k % 3) * 124f, -52f - (k / 3) * 92f); frame.rectTransform.sizeDelta = new Vector2(108f, 84f);
                teamsKitFrames[k] = frame;
                var b = frame.gameObject.AddComponent<Button>(); b.targetGraphic = frame;
                b.onClick.AddListener(() => { if (TeamSelection.Home >= 0) TeamSelection.Set(-1, TeamSelection.Away); SetHomeKit(kit); RefreshTeams(); });
                var back = UIKit.Image(frame.transform, "Back", UITheme.Rounded, new Color(0.12f, 0.2f, 0.38f, 1f), true, false);
                back.rectTransform.anchorMin = Vector2.zero; back.rectTransform.anchorMax = Vector2.one; back.rectTransform.offsetMin = new Vector2(4f, 4f); back.rectTransform.offsetMax = new Vector2(-4f, -4f);
                var shirt = UIKit.Image(back.transform, "Shirt", UITheme.Icon(UIIcon.Shirt), KitPalette.Colors[k], false, false);
                shirt.rectTransform.anchorMin = new Vector2(0.14f, 0.1f); shirt.rectTransform.anchorMax = new Vector2(0.86f, 0.9f); shirt.rectTransform.offsetMin = shirt.rectTransform.offsetMax = Vector2.zero;
            }
            teamsKitLabel = UIKit.TextAt(kitCard, "KitName", "", 22f, UITheme.Muted, UIKit.TopLeft, new Vector2(0f, -232f), new Vector2(394f, 28f), TextAlignmentOptions.Center, false);

            var oppCard = UIKit.Card(p, "OpponentCard", UIKit.TopLeft, new Vector2(1076f, -396f), new Vector2(394f, 232f));
            UIKit.TextAt(oppCard, "OppTitle", "Opponent", 30f, Color.white, UIKit.TopLeft, new Vector2(0f, -10f), new Vector2(394f, 40f), TextAlignmentOptions.Center, true);
            teamsAwayCrest = UIKit.Crest(oppCard, "OppCrest", Color.white, Color.white, UIKit.TopLeft, new Vector2(142f, -66f), 110f);
            UIKit.Arrow(oppCard, "OppPrev", UIKit.TopLeft, new Vector2(20f, -160f), 58f, true, () => CycleClub(false, -1));
            UIKit.Arrow(oppCard, "OppNext", UIKit.TopLeft, new Vector2(316f, -160f), 58f, false, () => CycleClub(false, 1));
            teamsAwayClubLabel = UIKit.TextAt(oppCard, "AwayClub", "", 32f, Color.white, UIKit.TopLeft, new Vector2(84f, -160f), new Vector2(226f, 58f), TextAlignmentOptions.Center, true);
            teamsAwayClubLabel.enableAutoSizing = true; teamsAwayClubLabel.fontSizeMin = 18f; teamsAwayClubLabel.fontSizeMax = 32f;

            UIKit.ActionButton(p, "Lineup", UIIcon.Shirt, false, UIKit.TopLeft, new Vector2(1076f, -650f), new Vector2(190f, 92f), OpenLineup, 30f);
            UIKit.ActionButton(p, "Confirm", UIIcon.Check, true, UIKit.TopLeft, new Vector2(1280f, -650f), new Vector2(190f, 92f), CloseTeams, 30f);
            teamsPanel.SetActive(false);
        }

        static void PitchLine(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var img = UIKit.Image(parent, "Line", UITheme.Rounded, color, false, false);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = img.rectTransform.pivot = anchor;
            img.rectTransform.anchoredPosition = pos; img.rectTransform.sizeDelta = size;
        }
    }
}
