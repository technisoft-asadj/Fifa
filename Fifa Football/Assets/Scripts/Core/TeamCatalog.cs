using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Core
{
    public enum KitPattern { Solid, Stripes, Hoops, Sleeves }

    /// <summary>One strip: shirt, the second colour (stripes, hoops, sleeves, shirt number), shorts, socks.</summary>
    public struct ClubKit
    {
        public Color shirt, secondary, shorts, socks;
        public KitPattern pattern;

        public static ClubKit Plain(Color shirt)
        {
            return new ClubKit { shirt = shirt, secondary = Color.Lerp(shirt, Color.white, 0.8f), shorts = Color.Lerp(shirt, Color.white, 0.75f), socks = Color.Lerp(shirt, Color.black, 0.15f), pattern = KitPattern.Solid };
        }
    }

    /// <summary>A side: its names, its home strip and its eleven starters by shirt number (the first is the goalkeeper).</summary>
    public sealed class Club
    {
        public string name, shortName;
        public ClubKit home;
        public string[] players;
        /// <summary>The second strip, worn when the home strip would clash with the opponent's: white (or dark when the home shirt is light) with the club colour as trim.</summary>
        public ClubKit Away
        {
            get
            {
                bool light = (home.shirt.r + home.shirt.g + home.shirt.b) / 3f > 0.6f;
                Color baseCol = light ? new Color(0.1f, 0.12f, 0.2f) : new Color(0.96f, 0.96f, 0.96f);
                return new ClubKit { shirt = baseCol, secondary = home.shirt, shorts = baseCol, socks = baseCol, pattern = KitPattern.Sleeves };
            }
        }
    }

    /// <summary>
    /// The sides the player can pick. Names, colours and starting elevens are plain data in one place, so they are easy to change or replace.
    /// NOTE: real club and player names (and crests, which are not used here) belong to their owners; shipping a game with them in a store needs licences.
    /// </summary>
    public static class TeamCatalog
    {
        static Color C(int hex) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f); }

        static Club Make(string name, string shortName, int shirt, int second, int shorts, int socks, KitPattern pat, string players)
        {
            return new Club
            {
                name = name, shortName = shortName,
                home = new ClubKit { shirt = C(shirt), secondary = C(second), shorts = C(shorts), socks = C(socks), pattern = pat },
                players = players.Split(',')
            };
        }

        public static readonly Club[] Clubs =
        {
            Make("Real Madrid", "RMA", 0xF4F4F4, 0xE0B84A, 0xF4F4F4, 0xF4F4F4, KitPattern.Solid, "Courtois,Carvajal,Militao,Alaba,Mendy,Tchouameni,Valverde,Bellingham,Rodrygo,Mbappe,Vinicius"),
            Make("FC Barcelona", "BAR", 0x1B3F94, 0xA50044, 0x1B3F94, 0x1B3F94, KitPattern.Stripes, "Ter Stegen,Kounde,Cubarsi,Araujo,Balde,De Jong,Pedri,Gavi,Yamal,Lewandowski,Raphinha"),
            Make("Manchester City", "MCI", 0x6CABDD, 0xFFFFFF, 0xFFFFFF, 0x6CABDD, KitPattern.Solid, "Ederson,Walker,Dias,Gvardiol,Ake,Rodri,De Bruyne,Bernardo Silva,Foden,Haaland,Doku"),
            Make("Liverpool", "LIV", 0xC8102E, 0xF6EB61, 0xC8102E, 0xC8102E, KitPattern.Solid, "Alisson,Alexander-Arnold,Konate,Van Dijk,Robertson,Gravenberch,Mac Allister,Szoboszlai,Salah,Gakpo,Diaz"),
            Make("Arsenal", "ARS", 0xEF0107, 0xFFFFFF, 0xFFFFFF, 0xEF0107, KitPattern.Sleeves, "Raya,White,Saliba,Gabriel,Timber,Rice,Odegaard,Merino,Saka,Havertz,Martinelli"),
            Make("Chelsea", "CHE", 0x034694, 0xFFFFFF, 0x034694, 0x034694, KitPattern.Solid, "Sanchez,Gusto,Fofana,Colwill,Cucurella,Caicedo,Fernandez,Palmer,Madueke,Jackson,Neto"),
            Make("Bayern Munich", "BAY", 0xDC052D, 0xFFFFFF, 0xFFFFFF, 0xDC052D, KitPattern.Solid, "Neuer,Laimer,Upamecano,Kim,Davies,Kimmich,Goretzka,Olise,Musiala,Kane,Diaz"),
            Make("Juventus", "JUV", 0xF2F2F2, 0x101010, 0x101010, 0xF2F2F2, KitPattern.Stripes, "Di Gregorio,Cambiaso,Bremer,Kalulu,Cabal,Locatelli,Thuram,Koopmeiners,Conceicao,Vlahovic,Yildiz"),
            Make("AC Milan", "MIL", 0xD0121B, 0x101010, 0x101010, 0xD0121B, KitPattern.Stripes, "Maignan,Walker,Tomori,Pavlovic,Hernandez,Fofana,Reijnders,Pulisic,Leao,Gimenez,Loftus-Cheek"),
            Make("Inter Milan", "INT", 0x0A5BA8, 0x101010, 0x101010, 0x0A5BA8, KitPattern.Stripes, "Sommer,Pavard,Acerbi,Bastoni,Dumfries,Barella,Calhanoglu,Mkhitaryan,Dimarco,Thuram,Martinez"),
            Make("Paris Saint-Germain", "PSG", 0x0A1F44, 0xDA291C, 0x0A1F44, 0x0A1F44, KitPattern.Sleeves, "Donnarumma,Hakimi,Marquinhos,Pacho,Mendes,Vitinha,Ruiz,Neves,Dembele,Barcola,Doue"),
            Make("Borussia Dortmund", "BVB", 0xFDE100, 0x101010, 0x101010, 0xFDE100, KitPattern.Solid, "Kobel,Ryerson,Anton,Schlotterbeck,Bensebaini,Can,Nmecha,Brandt,Adeyemi,Guirassy,Beier"),
            Make("Atletico Madrid", "ATM", 0xCB3524, 0xFFFFFF, 0x1B3F94, 0xCB3524, KitPattern.Stripes, "Oblak,Molina,Le Normand,Gimenez,Ruggeri,De Paul,Koke,Barrios,Griezmann,Alvarez,Sorloth"),
            Make("Argentina", "ARG", 0x75AADB, 0xFFFFFF, 0x101010, 0xFFFFFF, KitPattern.Stripes, "Martinez,Molina,Romero,Otamendi,Tagliafico,De Paul,Fernandez,Mac Allister,Messi,Alvarez,Di Maria"),
            Make("Brazil", "BRA", 0xFFDC02, 0x009C3B, 0x1B3F94, 0xFFFFFF, KitPattern.Sleeves, "Alisson,Danilo,Marquinhos,Militao,Wendell,Casemiro,Paqueta,Raphinha,Rodrygo,Vinicius,Endrick"),
            Make("France", "FRA", 0x14275A, 0xFFFFFF, 0xFFFFFF, 0xDA291C, KitPattern.Solid, "Maignan,Kounde,Upamecano,Saliba,Theo Hernandez,Tchouameni,Kante,Dembele,Griezmann,Mbappe,Thuram"),
            Make("England", "ENG", 0xF4F4F4, 0x1B2A5C, 0x1B2A5C, 0xF4F4F4, KitPattern.Solid, "Pickford,Walker,Stones,Guehi,Shaw,Rice,Bellingham,Foden,Saka,Kane,Palmer"),
        };

        public static int Count => Clubs.Length;

        /// <summary>Chooses goalkeeper shirts: two colours picked from a fixed list so each differs from every outfield shirt on the pitch and from the other keeper.</summary>
        public static void GoalkeeperColors(Color homeShirt, Color awayShirt, out Color homeKeeper, out Color awayKeeper)
        {
            Color[] pool =
            {
                new Color(0.1f, 0.72f, 0.28f), new Color(0.98f, 0.55f, 0.08f), new Color(0.96f, 0.4f, 0.75f), new Color(0.2f, 0.85f, 0.85f),
                new Color(0.98f, 0.9f, 0.15f), new Color(0.55f, 0.2f, 0.7f), new Color(0.1f, 0.1f, 0.12f)
            };
            int bestH = 0; float bestHScore = -1f;
            for (int i = 0; i < pool.Length; i++)
            {
                float s = Mathf.Min(Distance(pool[i], homeShirt), Distance(pool[i], awayShirt));
                if (s > bestHScore) { bestHScore = s; bestH = i; }
            }
            int bestA = -1; float bestAScore = -1f;
            for (int i = 0; i < pool.Length; i++)
            {
                if (i == bestH) continue;
                float s = Mathf.Min(Distance(pool[i], homeShirt), Distance(pool[i], awayShirt), Distance(pool[i], pool[bestH]));
                if (s > bestAScore) { bestAScore = s; bestA = i; }
            }
            homeKeeper = pool[bestH]; awayKeeper = pool[bestA];
        }

        /// <summary>A rough colour distance (0 identical, about 1.7 for black against white).</summary>
        public static float Distance(Color a, Color b)
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return Mathf.Sqrt(dr * dr * 0.3f + dg * dg * 0.59f + db * db * 0.11f) * 3f;
        }

        /// <summary>True when two shirts are too alike to tell apart at a glance.</summary>
        public static bool Clash(Color a, Color b) { return Distance(a, b) < 0.55f; }
    }

    /// <summary>Which sides play: -1 is the plain coloured team (the shirt colour picked on the start screen), otherwise an index into <see cref="TeamCatalog.Clubs"/>. Saved.</summary>
    public static class TeamSelection
    {
        public const string HomeKey = "set_club_home", AwayKey = "set_club_away";
        static int home = -2, away = -2;

        public static int Home { get { Load(); return home; } }
        public static int Away { get { Load(); return away; } }

        static void Load()
        {
            if (home != -2) return;
            try { home = Mathf.Clamp(PlayerPrefs.GetInt(HomeKey, -1), -1, TeamCatalog.Count - 1); away = Mathf.Clamp(PlayerPrefs.GetInt(AwayKey, -1), -1, TeamCatalog.Count - 1); }
            catch { home = away = -1; }
            if (home >= 0 && home == away) away = -1;
        }

        public static void Set(int homeIndex, int awayIndex)
        {
            home = Mathf.Clamp(homeIndex, -1, TeamCatalog.Count - 1); away = Mathf.Clamp(awayIndex, -1, TeamCatalog.Count - 1);
            if (home >= 0 && home == away) away = -1;
            try { PlayerPrefs.SetInt(HomeKey, home); PlayerPrefs.SetInt(AwayKey, away); PlayerPrefs.Save(); } catch { }
        }

        /// <summary>Steps one side to the previous or next choice (wrapping, never the same club as the other side).</summary>
        public static void Cycle(bool homeSide, int dir)
        {
            Load();
            int n = TeamCatalog.Count + 1; // the plain team plus every club
            int cur = (homeSide ? home : away) + 1, other = (homeSide ? away : home);
            for (int i = 0; i < n; i++)
            {
                cur = ((cur + dir) % n + n) % n;
                if (cur - 1 < 0 || cur - 1 != other) break;
            }
            if (homeSide) Set(cur - 1, away); else Set(home, cur - 1);
        }

        public static void ForgetInMemory() { home = away = -2; }

        public static string NameOf(int index, string plain) { return index >= 0 && index < TeamCatalog.Count ? TeamCatalog.Clubs[index].name : plain; }
        public static string ShortOf(int index, string plain) { return index >= 0 && index < TeamCatalog.Count ? TeamCatalog.Clubs[index].shortName : plain; }
    }
}
