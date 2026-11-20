using System.Collections.Generic;
using UnityEngine;
using FootballSim.Core;
using FootballSim.FootballAI;

namespace FootballSim.Players
{
    /// <summary>Works out what each side wears and puts it on: strips, goalkeeper colours, shirt numbers and real player names.</summary>
    public static class TeamKits
    {
        static readonly Dictionary<PlayerController, string> baseNames = new Dictionary<PlayerController, string>();

        /// <summary>The strips for the two sides. A side is the plain coloured team (<paramref name="plainKit"/> picks the home colour) or a club.
        /// When the shirts would clash the away side changes: a club to its second strip, a plain team to the palette colour farthest from the home shirt.</summary>
        public static void Resolve(int plainKit, out ClubKit home, out ClubKit away, out ClubKit homeKeeper, out ClubKit awayKeeper)
        {
            int hc = TeamSelection.Home, ac = TeamSelection.Away;
            home = hc >= 0 ? TeamCatalog.Clubs[hc].home : ClubKit.Plain(KitPalette.Colors[Mathf.Clamp(plainKit, 0, KitPalette.Count - 1)]);
            away = ac >= 0 ? TeamCatalog.Clubs[ac].home : ClubKit.Plain(KitPalette.Colors[KitPalette.AwayFor(plainKit)]);
            if (TeamCatalog.Clash(home.shirt, away.shirt))
            {
                if (ac >= 0) away = TeamCatalog.Clubs[ac].Away;
                else
                {
                    int best = 0; float bestD = -1f;
                    for (int i = 0; i < KitPalette.Count; i++)
                    {
                        float d = TeamCatalog.Distance(KitPalette.Colors[i], home.shirt);
                        if (d > bestD) { bestD = d; best = i; }
                    }
                    away = ClubKit.Plain(KitPalette.Colors[best]);
                }
                if (TeamCatalog.Clash(home.shirt, away.shirt) && ac >= 0) { var alt = TeamCatalog.Clubs[ac].Away; if (!TeamCatalog.Clash(home.shirt, alt.shirt)) away = alt; }
            }
            TeamCatalog.GoalkeeperColors(home.shirt, away.shirt, out var gh, out var ga);
            homeKeeper = KeeperKit(gh); awayKeeper = KeeperKit(ga);
        }

        static ClubKit KeeperKit(Color shirt)
        {
            return new ClubKit { shirt = shirt, secondary = Color.Lerp(shirt, Color.white, 0.6f), shorts = Color.Lerp(shirt, Color.black, 0.75f), socks = Color.Lerp(shirt, Color.black, 0.55f), pattern = KitPattern.Solid };
        }

        public static void Apply(TeamController home, TeamController away, int plainKit)
        {
            Resolve(plainKit, out var hk, out var ak, out var hgk, out var agk);
            Dress(home, hk, hgk, TeamSelection.Home);
            Dress(away, ak, agk, TeamSelection.Away);
        }

        static void Dress(TeamController team, ClubKit outfield, ClubKit keeper, int clubIndex)
        {
            if (team == null) return;
            foreach (var p in team.players)
            {
                if (p == null) continue;
                if (!baseNames.TryGetValue(p, out var baseName)) { baseName = p.name; baseNames[p] = baseName; }
                int number = ShirtNumber(baseName);
                var v = p.GetComponent<PlayerVisual>();
                if (v != null) v.ApplyKit(p.isGoalkeeper ? keeper : outfield, p.isGoalkeeper, number > 0 ? number.ToString() : null);
                p.name = clubIndex >= 0 && number >= 1 && number <= TeamCatalog.Clubs[clubIndex].players.Length ? TeamCatalog.Clubs[clubIndex].players[number - 1] : baseName;
            }
        }

        /// <summary>The shirt number is the digits after the underscore in the scene's player name ("Home_9" is 9).</summary>
        public static int ShirtNumber(string baseName)
        {
            int u = baseName == null ? -1 : baseName.LastIndexOf('_');
            return u >= 0 && int.TryParse(baseName.Substring(u + 1), out int n) ? n : 0;
        }

        /// <summary>For tests: forget the remembered scene names.</summary>
        public static void ForgetNames() { baseNames.Clear(); }
    }
}
