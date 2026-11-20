using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FootballSim.Core;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.UI
{
    /// <summary>
    /// The Full Time screen of the design: both crests with the score between them, the scorers under each side, the match statistics in
    /// the middle, the Player of the Match and the rewards at the bottom, and the Rematch and Menu buttons.
    /// </summary>
    public partial class MatchHUD
    {
        RectTransform ftHomeCrest, ftAwayCrest;
        TextMeshProUGUI ftScore, ftHomeName, ftAwayName, ftHomeGoals, ftAwayGoals, ftMotmName, ftMotmDetail, ftCoins, ftGems, ftXp;
        TextMeshProUGUI[] ftStatHome, ftStatLabel, ftStatAway;
        string earnedSummary = "";

        /// <summary>The "+coins +XP" summary of the full-time screen (test hook).</summary>
        public string EarnedText => earnedSummary;
        /// <summary>The line under the score: who won, or how a cup tie was settled (test hook).</summary>
        public string FullTimeResultText => resultText != null ? resultText.text : "";
        /// <summary>The league, cup or season line (test hook).</summary>
        public string FullTimeInfoText => leagueText != null ? leagueText.text : "";
        public string FullTimeScoreText => ftScore != null ? ftScore.text : "";
        public string FullTimeGoalsText(bool home) { var t = home ? ftHomeGoals : ftAwayGoals; return t != null ? t.text : ""; }
        /// <summary>True when both scorer lists fit inside their boxes (test hook).</summary>
        public bool FullTimeScorersFit()
        {
            foreach (var t in new[] { ftHomeGoals, ftAwayGoals })
            {
                if (t == null) continue;
                t.ForceMeshUpdate();
                if (t.preferredHeight > t.rectTransform.rect.height + 0.5f) return false;
            }
            return true;
        }

        void BuildFullTime(RectTransform root)
        {
            var ft = UIKit.Screen(root, "FullTimePanel", new Vector2(SettingsW, SettingsH), "", null, GoToMainMenu, GoToMainMenu, out fullTimePanel);
            var title = UIKit.TextAt(ft, "FT", "FULL TIME", 74f, UITheme.Gold, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(800f, 90f), TextAlignmentOptions.Center, true);
            title.fontStyle = FontStyles.Bold | FontStyles.Italic;

            // crests with the names, the score between them
            ftHomeCrest = UIKit.Crest(ft, "HomeCrest", Color.white, Color.white, new Vector2(0.5f, 1f), new Vector2(-400f, -128f), 170f);
            ftAwayCrest = UIKit.Crest(ft, "AwayCrest", Color.white, Color.white, new Vector2(0.5f, 1f), new Vector2(400f, -128f), 170f);
            ftScore = UIKit.TextAt(ft, "Score", "0 - 0", 130f, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(560f, 160f), TextAlignmentOptions.Center, true);
            resultText = UIKit.TextAt(ft, "Result", "", 32f, UITheme.Muted, new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(900f, 44f), TextAlignmentOptions.Center, true);
            ftHomeName = UIKit.TextAt(ft, "HomeName", "Home", 38f, Color.white, new Vector2(0.5f, 1f), new Vector2(-400f, -304f), new Vector2(500f, 50f), TextAlignmentOptions.Center, true);
            ftAwayName = UIKit.TextAt(ft, "AwayName", "Away", 38f, Color.white, new Vector2(0.5f, 1f), new Vector2(400f, -304f), new Vector2(500f, 50f), TextAlignmentOptions.Center, true);
            ftHomeGoals = UIKit.TextAt(ft, "HomeGoals", "", 26f, UITheme.Muted, new Vector2(0.5f, 1f), new Vector2(-400f, -354f), new Vector2(270f, 176f), TextAlignmentOptions.Top, false);
            ftAwayGoals = UIKit.TextAt(ft, "AwayGoals", "", 26f, UITheme.Muted, new Vector2(0.5f, 1f), new Vector2(400f, -354f), new Vector2(270f, 176f), TextAlignmentOptions.Top, false);
            ftHomeGoals.overflowMode = ftAwayGoals.overflowMode = TextOverflowModes.Truncate;
            ftHomeGoals.lineSpacing = ftAwayGoals.lineSpacing = 4f;
            leagueText = UIKit.TextAt(ft, "League", "", 26f, new Color(1f, 0.9f, 0.4f), new Vector2(0.5f, 1f), new Vector2(0f, -352f), new Vector2(440f, 44f), TextAlignmentOptions.Center, true);
            leagueText.enableAutoSizing = true; leagueText.fontSizeMin = 16f; leagueText.fontSizeMax = 26f;

            // statistics, home value - label - away value
            string[] labels = { "Possession", "Shots", "Shots on Target", "Corners", "Fouls" };
            ftStatHome = new TextMeshProUGUI[5]; ftStatLabel = new TextMeshProUGUI[5]; ftStatAway = new TextMeshProUGUI[5];
            for (int i = 0; i < 5; i++)
            {
                float y = -396f - i * 52f;
                ftStatHome[i] = UIKit.TextAt(ft, "StatHome" + i, "0", 32f, Color.white, new Vector2(0.5f, 1f), new Vector2(-200f, y), new Vector2(110f, 44f), TextAlignmentOptions.Center, true);
                ftStatLabel[i] = UIKit.TextAt(ft, "StatName" + i, labels[i], 30f, UITheme.Muted, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(290f, 44f), TextAlignmentOptions.Center, false);
                ftStatAway[i] = UIKit.TextAt(ft, "StatAway" + i, "0", 32f, Color.white, new Vector2(0.5f, 1f), new Vector2(200f, y), new Vector2(110f, 44f), TextAlignmentOptions.Center, true);
            }

            // bottom left: Player of the Match
            var motm = UIKit.Card(ft, "Motm", UIKit.TopLeft, new Vector2(40f, -536f), new Vector2(440f, 138f), new Color(0.04f, 0.1f, 0.24f, 0.8f));
            var face = UIKit.Image(motm, "Portrait", UITheme.Circle, new Color(0.35f, 0.65f, 1f, 1f), false, false);
            face.rectTransform.anchorMin = face.rectTransform.anchorMax = new Vector2(0f, 0.5f); face.rectTransform.pivot = new Vector2(0f, 0.5f);
            face.rectTransform.anchoredPosition = new Vector2(20f, 0f); face.rectTransform.sizeDelta = new Vector2(110f, 110f);
            var faceIn = UIKit.Image(face.transform, "In", UITheme.Circle, new Color(0.07f, 0.2f, 0.55f, 1f), false, false);
            faceIn.rectTransform.anchorMin = Vector2.zero; faceIn.rectTransform.anchorMax = Vector2.one; faceIn.rectTransform.offsetMin = new Vector2(4f, 4f); faceIn.rectTransform.offsetMax = new Vector2(-4f, -4f);
            var portrait = UITheme.Art("avatar");
            var person = UIKit.Image(faceIn.transform, "Person", portrait ?? UITheme.Icon(UIIcon.Person), Color.white, false, false);
            if (portrait != null) { person.rectTransform.anchorMin = Vector2.zero; person.rectTransform.anchorMax = Vector2.one; }
            else { person.rectTransform.anchorMin = new Vector2(0.2f, 0.2f); person.rectTransform.anchorMax = new Vector2(0.8f, 0.8f); }
            person.rectTransform.offsetMin = person.rectTransform.offsetMax = Vector2.zero;
            UIKit.TextAt(motm, "MotmTitle", "Player of the Match", 26f, UITheme.Gold, UIKit.TopLeft, new Vector2(150f, -22f), new Vector2(280f, 34f), TextAlignmentOptions.Left, true);
            var star = UIKit.Image(motm, "Star", UITheme.Icon(UIIcon.Star), UITheme.Gold, false, false);
            star.rectTransform.anchorMin = star.rectTransform.anchorMax = star.rectTransform.pivot = UIKit.TopLeft;
            star.rectTransform.anchoredPosition = new Vector2(150f, -66f); star.rectTransform.sizeDelta = new Vector2(34f, 34f);
            ftMotmName = UIKit.TextAt(motm, "MotmName", "-", 34f, Color.white, UIKit.TopLeft, new Vector2(194f, -62f), new Vector2(236f, 44f), TextAlignmentOptions.Left, true);
            ftMotmName.enableAutoSizing = true; ftMotmName.fontSizeMin = 20f; ftMotmName.fontSizeMax = 34f;
            ftMotmDetail = UIKit.TextAt(motm, "MotmDetail", "", 24f, UITheme.Muted, UIKit.TopLeft, new Vector2(150f, -102f), new Vector2(280f, 30f), TextAlignmentOptions.Left, false);

            // bottom right: Rewards
            var rewards = UIKit.Card(ft, "Rewards", UIKit.TopRight, new Vector2(-40f, -536f), new Vector2(440f, 138f), new Color(0.04f, 0.1f, 0.24f, 0.8f));
            UIKit.TextAt(rewards, "RewardsTitle", "Rewards", 30f, Color.white, UIKit.TopLeft, new Vector2(26f, -12f), new Vector2(300f, 38f), TextAlignmentOptions.Left, true);
            var coin = UIKit.Image(rewards, "Coin", UITheme.Icon(UIIcon.Coin), UITheme.Gold, false, false);
            coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = coin.rectTransform.pivot = UIKit.TopLeft; coin.rectTransform.anchoredPosition = new Vector2(26f, -56f); coin.rectTransform.sizeDelta = new Vector2(40f, 40f);
            ftCoins = UIKit.TextAt(rewards, "Coins", "+0", 34f, UITheme.Gold, UIKit.TopLeft, new Vector2(78f, -54f), new Vector2(330f, 44f), TextAlignmentOptions.Left, true);
            var gem = UIKit.Image(rewards, "Gem", UITheme.Icon(UIIcon.Gem), new Color(0.35f, 0.85f, 1f, 1f), false, false);
            gem.rectTransform.anchorMin = gem.rectTransform.anchorMax = gem.rectTransform.pivot = UIKit.TopLeft; gem.rectTransform.anchoredPosition = new Vector2(26f, -96f); gem.rectTransform.sizeDelta = new Vector2(40f, 40f);
            ftGems = UIKit.TextAt(rewards, "Gems", "+0", 34f, new Color(0.45f, 0.9f, 1f), UIKit.TopLeft, new Vector2(78f, -94f), new Vector2(150f, 44f), TextAlignmentOptions.Left, true);
            ftXp = UIKit.TextAt(rewards, "Xp", "", 28f, Color.white, UIKit.TopLeft, new Vector2(232f, -98f), new Vector2(200f, 40f), TextAlignmentOptions.Left, true);
            ftXp.enableAutoSizing = true; ftXp.fontSizeMin = 16f; ftXp.fontSizeMax = 28f;

            // buttons
            var rematch = UIKit.ActionButton(ft, "Rematch", UIIcon.Refresh, false, new Vector2(0.5f, 0f), new Vector2(-190f, 28f), new Vector2(340f, 92f), Rematch, 42f);
            UIKit.ActionButton(ft, "Menu", UIIcon.Home, false, new Vector2(0.5f, 0f), new Vector2(190f, 28f), new Vector2(340f, 92f), GoToMainMenu, 42f);
            rematchLabel = rematch.GetComponentInChildren<TextMeshProUGUI>();
            fullTimePanel.SetActive(false);
        }

        static string ClubName(bool home)
        {
            return home ? TeamSelection.NameOf(TeamSelection.Home, "Home") : TeamSelection.NameOf(TeamSelection.Away, "Away");
        }

        /// <summary>The two sides' strip colours (shirt, trim) as the match is dressed, for the crests.</summary>
        void SideColours(out Color homeMain, out Color homeTrim, out Color awayMain, out Color awayTrim)
        {
            FootballSim.Players.TeamKits.Resolve(selectedKit, out var hk, out var ak, out _, out _);
            homeMain = hk.shirt; homeTrim = hk.secondary; awayMain = ak.shirt; awayTrim = ak.secondary;
        }

        static void RecolourCrest(RectTransform crest, Color main, Color trim, int club = -1)
        {
            if (crest == null) return;
            UIKit.SetCrest(crest, club, main, trim);
            var body = crest.Find("Body"); if (body != null) body.GetComponent<Image>().color = main;
            var emblem = crest.Find("Emblem"); if (emblem != null) emblem.GetComponent<Image>().color = (main.r + main.g + main.b) / 3f > 0.75f && (trim.r + trim.g + trim.b) / 3f > 0.75f ? new Color(0.15f, 0.2f, 0.4f) : trim;
        }

        static string ScorerLines(MatchManager m, int team)
        {
            const int shown = 4;
            int count = 0, total = 0; string text = "";
            foreach (var g in m.Goals)
            {
                if (g.team != team) continue;
                total++;
                if (count >= shown) continue;
                text += (count > 0 ? "\n" : "") + $"{g.minute}'  {g.scorer}" + (g.ownGoal ? " (OG)" : "");
                count++;
            }
            if (total > shown) text += $"\n+{total - shown} more";
            return text;
        }

        /// <summary>Fills the full-time screen from the finished match.</summary>
        void FillFullTime()
        {
            ftScore.text = $"{match.HomeScore} - {match.AwayScore}";
            ftHomeName.text = ClubName(true); ftAwayName.text = ClubName(false);
            SideColours(out var hm, out var ht, out var am, out var at);
            RecolourCrest(ftHomeCrest, hm, ht, TeamSelection.Home); RecolourCrest(ftAwayCrest, am, at, TeamSelection.Away);
            ftHomeGoals.text = ScorerLines(match, 0); ftAwayGoals.text = ScorerLines(match, 1);
            var s = match.Stats;
            string[] home = { s.PossessionPercent(0) + "%", s.shots[0].ToString(), s.shotsOnTarget[0].ToString(), s.corners[0].ToString(), match.HomeFouls.ToString() };
            string[] away = { s.PossessionPercent(1) + "%", s.shots[1].ToString(), s.shotsOnTarget[1].ToString(), s.corners[1].ToString(), match.AwayFouls.ToString() };
            for (int i = 0; i < 5; i++) { ftStatHome[i].text = home[i]; ftStatAway[i].text = away[i]; }

            // Player of the Match: whoever scored most (the user's side first); with no goals, nobody stands out
            string best = null; int bestGoals = 0;
            for (int pass = 0; pass < 2 && best == null; pass++)
            {
                var tally = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var g in match.Goals)
                {
                    if (g.ownGoal || (pass == 0 && g.team != 0) || string.IsNullOrEmpty(g.scorer)) continue;
                    tally.TryGetValue(g.scorer, out int n); tally[g.scorer] = n + 1;
                    if (tally[g.scorer] > bestGoals) { bestGoals = tally[g.scorer]; best = g.scorer; }
                }
            }
            ftMotmName.text = best ?? "No goals";
            ftMotmDetail.text = best == null ? "A tight match" : bestGoals == 1 ? "1 goal" : bestGoals + " goals";
        }

        /// <summary>Shows what the match paid on the Rewards card.</summary>
        void SetEarned(int coins, int xp, int gems, bool levelUp)
        {
            ftCoins.text = $"+{coins:N0}"; ftGems.text = $"+{gems}"; ftXp.text = levelUp ? $"+{xp} XP  LEVEL UP!" : $"+{xp} XP";
            earnedSummary = $"+{coins} coins   +{xp} XP" + (levelUp ? $"   LEVEL UP!  Level {PlayerProfile.Level}" : "");
        }

        void SetNoReward()
        {
            ftCoins.text = "+0"; ftGems.text = "+0"; ftXp.text = "Training";
            earnedSummary = "Training: no rewards";
        }
    }
}
