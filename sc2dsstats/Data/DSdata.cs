using System;
using System.Collections.Generic;

namespace sc2dsstats.Data
{
    public static class DSdata
    {
        public static string[] s_races { get; } = new string[]
        {
                "Abathur",
                 "Alarak",
                 "Artanis",
                 "Dehaka",
                 "Fenix",
                 "Horner",
                 "Karax",
                 "Kerrigan",
                 "Mengsk",
                 "Nova",
                 "Raynor",
                 "Stetmann",
                 "Stukov",
                 "Swann",
                 "Tychus",
                 "Vorazun",
                 "Zagara",
                 "Protoss",
                 "Terran",
                 "Zerg"
        };

        public static string[] s_races_cmdr { get; } = new string[]
        {
                "Abathur",
                 "Alarak",
                 "Artanis",
                 "Dehaka",
                 "Fenix",
                 "Horner",
                 "Karax",
                 "Kerrigan",
                 "Mengsk",
                 "Nova",
                 "Raynor",
                 "Stetmann",
                 "Stukov",
                 "Swann",
                 "Tychus",
                 "Vorazun",
                 "Zagara"
        };

        public static string[] s_gamemodes { get; } = new string[]
        {
            "GameModeBrawlCommanders",
            "GameModeBrawlStandard",
            "GameModeCommanders",
            "GameModeCommandersHeroic",
            "GameModeGear",
            "GameModeSabotage",
            "GameModeStandard",
            "GameModeSwitch"
        };

        public static string[] s_breakpoints { get; } = new string[]
        {
                 "MIN5",
                 "MIN10",
                 "MIN15",
                 "ALL",
        };

        public static string[] s_builds { get; } = new string[]
        {
            "PAX",
            "Feralan",
            "Panzerfaust"
        };

        public static string[] s_players { get; } = new string[]
        {
            "player",
            "player1",
            "player2",
            "player3",
            "player4",
            "player5",
            "player6"
        };

        public static Dictionary<string, string> INFO { get; } = new Dictionary<string, string>() {
            { "Winrate", "Winrate: Shows the winrate for each commander. When selecting a commander on the left it shows the winrate of the selected commander when matched vs the other commanders." },
            { "MVP", "MVP: Shows the % for the most ingame damage for each commander based on mineral value killed. When selecting a commander on the left it shows the mvp of the selected commander when matched vs the other commanders." },
            { "DPS", "DPS: Shows the damage delt for each commander based on mineral value killed / game duration (or army value, or minerals collected). When selecting a commander on the left it shows the damage of the selected commander when matched vs the other commanders." },
            { "Synergy", "Synergy: Shows the winrate for the selected commander when played together with the other commanders"},
            { "AntiSynergy", "Antisynergy: Shows the winrate for the selected commander when played vs the other commanders (at any position)"},
            { "Builds", "Builds: Shows the average unit count for the selected commander at the selected game duration. When selecting a vs commander it shows the average unit count of the selected commander when matched vs the other commanders."},
            { "Timeline", "Timeline: Shows the winrate development for the selected commander over the given time period."},
        };

        public static string color_max1 = "Crimson";
        public static string color_max2 = "OrangeRed";
        public static string color_max3 = "Chocolate";
        public static string color_def = "#FFCC00";
        public static string color_info = "#46a2c9";
        public static string color_diff1 = "Crimson";
        public static string color_diff2 = color_info;
        public static string color_null = color_diff2;
        public static string color_bg = "#0e0e24";
        public static string color_plbg_def = "#D8D8D8";
        public static string color_plbg_player = "#2E9AFE";
        public static string color_plbg_mvp = "#FFBF00";


        public static Dictionary<string, string> CMDRcolor { get; } = new Dictionary<string, string>()
        {
            {     "global", "#0000ff"  },
            {     "Abathur", "#266a1b" },
            {     "Alarak", "#ab0f0f" },
            {     "Artanis", "#edae0c" },
            {     "Dehaka", "#d52a38" },
            {     "Fenix", "#fcf32c" },
            {     "Horner", "#ba0d97" },
            {     "Karax", "#1565c7" },
            {     "Kerrigan", "#b021a1" },
            {     "Mengsk", "#a46532" },
            {     "Nova", "#f6f673" },
            {     "Raynor", "#dd7336" },
            {     "Stetmann", "#ebeae8" },
            {     "Stukov", "#663b35" },
            {     "Swann", "#ab4f21" },
            {     "Tychus", "#150d9f" },
            {     "Vorazun", "#07c543" },
            {     "Zagara", "#b01c48" },
            {     "Protoss", "#fcc828"   },
            {     "Terran", "#242331"   },
            {     "Zerg", "#440e5f"   }
        };

        public static string Enddate { get; set; } = DateTime.Today.AddDays(1).ToString("yyyyMMdd");

        public static string GetIcon(string race)
        {
            string r = race.ToLower();
            //r = "~/images/btn-unit-hero-" + r + ".png";
            r = "images/btn-unit-hero-" + r + ".png";
            return r;
        }

    }

    public class DSbuild
    {
        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>> UNITS = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>();
        public Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>> WR = new Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>();

        public DSbuild()
        {
            foreach (string cmdr in DSdata.s_races)
            {
                UNITS.Add(cmdr, new Dictionary<string, Dictionary<string, Dictionary<string, double>>>());
                WR.Add(cmdr, new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>());

                foreach (string vs in DSdata.s_races)
                {
                    UNITS[cmdr].Add(vs, new Dictionary<string, Dictionary<string, double>>());
                    WR[cmdr].Add(vs, new Dictionary<string, KeyValuePair<double, int>>());

                    foreach (string bp in DSdata.s_breakpoints)
                    {
                        UNITS[cmdr][vs].Add(bp, new Dictionary<string, double>());
                        WR[cmdr][vs].Add(bp, new KeyValuePair<double, int>(0, 0));
                    }
                }
                UNITS[cmdr].Add("ALL", new Dictionary<string, Dictionary<string, double>>());
                WR[cmdr].Add("ALL", new Dictionary<string, KeyValuePair<double, int>>());

                foreach (string bp in DSdata.s_breakpoints)
                {
                    UNITS[cmdr]["ALL"].Add(bp, new Dictionary<string, double>());
                    WR[cmdr]["ALL"].Add(bp, new KeyValuePair<double, int>(0, 0));
                }

            }
        }
    }
}



