using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using dsweb_electron6.Models;
using Newtonsoft.Json;
using dsweb_electron6;

namespace dsweb_electron6.Data
{
    public class DSdata : Interfaces.IDSdata
    {
        public Dictionary<string, KeyValuePair<double, int>> WINRATE { get; private set; } = new Dictionary<string, KeyValuePair<double, int>>();
        public Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> WINRATEVS { get; private set; } = new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>();
        public Dictionary<string, KeyValuePair<double, int>> WINRATE_PLAYER { get; private set; } = new Dictionary<string, KeyValuePair<double, int>>();
        public Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> WINRATEVS_PLAYER { get; private set; } = new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>();

        public Dictionary<string, Dictionary<string, double>> SYNERGY { get; private set; } = new Dictionary<string, Dictionary<string, double>>();

        // mode, startdate, enddate, data

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

        public DSdata()
        {
            CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");


        }

        public static string GetIcon(string race)
        {
            string r = race.ToLower();
            //r = "~/images/btn-unit-hero-" + r + ".png";
            r = "images/btn-unit-hero-" + r + ".png";
            return r;
        }

    }

    class DSdata_cache : Interfaces.IDSdata_cache
    {
        public List<Models.dsreplay> REPLAYS { get; private set; } = new List<dsreplay>();
        public List<Models.dsreplay> FILTERED_REPLAYS { get; private set; } = new List<dsreplay>();
        public dsfilter FIL_INFO { get; private set; } = new dsfilter();
        public double FIL_WR { get; private set; }

        public Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winrate_CACHE { get; private set; } = new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>();
        public Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>> winratevs_CACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>();
        public Dictionary<string, dsfilter> filter_CACHE { get; private set; } = new Dictionary<string, dsfilter>();

        // build, cmdr, cmdr_vs, breakpoint, unit, count | wr, games
        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>> BUILDCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>>();
        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>> BUILDWRCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>>();
        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>> BUILDDURCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>();
        // build, cmdr, vs, replays
        public Dictionary<string, Dictionary<string, Dictionary<string, List<dsreplay>>>> BUILDREPLAYSCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, List<dsreplay>>>>();


        // mode, startdate, enddate, filter
        public Dictionary<string, Dictionary<string, Dictionary<string, dsfilter>>> FILTER { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, dsfilter>>>();

        public Dictionary<string, List<Models.dsreplay>> BUILD_REPLAYS { get; private set; } = new Dictionary<string, List<dsreplay>>();
        StartUp _startUp;

        HashSet<string> ALLUNITS = new HashSet<string>();
        static Regex rx_star = new Regex(@"(.*)Starlight(.*)", RegexOptions.Singleline);
        static Regex rx_light = new Regex(@"(.*)Lightweight(.*)", RegexOptions.Singleline);
        static Regex rx_hero = new Regex(@"Hero(.*)WaveUnit", RegexOptions.Singleline);
        static Regex rx_mp = new Regex(@"(.*)MP$", RegexOptions.Singleline);

        public DSdata_cache(StartUp startUp) {
            _startUp = startUp;
        }

        public void Init(List<dsreplay> replays)
        {
            lock (REPLAYS)
            {
                DSdata.Enddate = DateTime.Today.AddDays(1).ToString("yyyyMMdd");
                
                REPLAYS.Clear();
                REPLAYS = new List<dsreplay>(replays);
                winratevs_CACHE.Clear();
                winrate_CACHE.Clear();
                BUILDCACHE.Clear();
                BUILDREPLAYSCACHE.Clear();
                BUILDDURCACHE.Clear();
                BUILDWRCACHE.Clear();
                filter_CACHE.Clear();
                DSfilter fil = new DSfilter();
                FILTERED_REPLAYS = fil.Filter(REPLAYS);
                FIL_INFO = fil.FIL;
                FIL_WR = fil.FIL.WR;
                FILTER.Clear();
                FILTER.Add("Winrate", new Dictionary<string, Dictionary<string, dsfilter>>());
                FILTER["Winrate"].Add("0", new Dictionary<string, dsfilter>());
                FILTER["Winrate"]["0"].Add("0", fil.FIL);
                BUILD_REPLAYS.Clear();
                BUILD_REPLAYS.Add("ALL", REPLAYS);
                BUILD_REPLAYS.Add("player", REPLAYS);
                foreach (string player in BUILD_REPLAYS.Keys.ToArray())
                {
                    GenBuilds(player);
                }
                //File.WriteAllLines(@"C:/temp/bab/analyzes/units.txt", ALLUNITS.OrderBy(o => o));
            }
        }

        public void GenBuilds(string player, string startdate = "20190101", string enddate = "0") {
            if (enddate == "0") enddate = DSdata.Enddate;

            if (!BUILDREPLAYSCACHE.ContainsKey(player)) BUILDREPLAYSCACHE.Add(player, new Dictionary<string, Dictionary<string, List<dsreplay>>>());
            else BUILDREPLAYSCACHE[player] = new Dictionary<string, Dictionary<string, List<dsreplay>>>();

            List<dsreplay> myreplays = new List<dsreplay>();
            if (player == "ALL") myreplays = REPLAYS;
            else if (BUILD_REPLAYS.ContainsKey(player)) myreplays = BUILD_REPLAYS[player];
            List<dsreplay> replays;
            if (startdate != "0" && enddate != "0")
            {
                DSfilter fil = new DSfilter();
                replays = new List<dsreplay>(fil.Filter(myreplays, startdate, enddate));
                if (!FILTER.ContainsKey("Builds")) FILTER.Add("Builds", new Dictionary<string, Dictionary<string, dsfilter>>());
                if (!FILTER["Builds"].ContainsKey(startdate)) FILTER["Builds"].Add(startdate, new Dictionary<string, dsfilter>());
                if (!FILTER["Builds"][startdate].ContainsKey(enddate)) FILTER["Builds"][startdate].Add(enddate, fil.FIL);
            }
            else
            {
                replays = FILTERED_REPLAYS;
                if (!FILTER.ContainsKey("Builds")) FILTER.Add("Builds", new Dictionary<string, Dictionary<string, dsfilter>>());
                if (!FILTER["Builds"].ContainsKey(startdate)) FILTER["Builds"].Add(startdate, new Dictionary<string, dsfilter>());
                if (!FILTER["Builds"][startdate].ContainsKey(enddate)) FILTER["Builds"][startdate].Add(enddate, FILTER["Winrate"][startdate][enddate]);

            }
            int games = 0;
            int rgames = 0;
            int ygames = 0;
            int wins = 0;

            Dictionary<string, Dictionary<string, Dictionary<string, int>>> GAMES = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> WINS = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
            Dictionary<string, Dictionary<string, Dictionary<string, double>>> DURATION = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, int>>>> UNITS = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, int>>>>();
            //init
            foreach (string bp in DSdata.s_breakpoints)
            {
                GAMES.Add(bp, new Dictionary<string, Dictionary<string, int>>());
                WINS.Add(bp, new Dictionary<string, Dictionary<string, int>>());
                DURATION.Add(bp, new Dictionary<string, Dictionary<string, double>>());
                UNITS.Add(bp, new Dictionary<string, Dictionary<string, Dictionary<string, int>>>());

                foreach (string cmdr in DSdata.s_races)
                {
                    GAMES[bp].Add(cmdr, new Dictionary<string, int>());
                    WINS[bp].Add(cmdr, new Dictionary<string, int>());
                    DURATION[bp].Add(cmdr, new Dictionary<string, double>());
                    UNITS[bp].Add(cmdr, new Dictionary<string, Dictionary<string, int>>());
                    if (bp == "ALL") BUILDREPLAYSCACHE[player].Add(cmdr, new Dictionary<string, List<dsreplay>>());

                    foreach (string vs in DSdata.s_races)
                    {
                        GAMES[bp][cmdr].Add(vs, 0);
                        WINS[bp][cmdr].Add(vs, 0);
                        DURATION[bp][cmdr].Add(vs, 0);
                        UNITS[bp][cmdr].Add(vs, new Dictionary<string, int>());
                        if (bp == "ALL") BUILDREPLAYSCACHE[player][cmdr].Add(vs, new List<dsreplay>());
                    }
                    GAMES[bp][cmdr].Add("ALL", 0);
                    WINS[bp][cmdr].Add("ALL", 0);
                    DURATION[bp][cmdr].Add("ALL", 0);
                    UNITS[bp][cmdr].Add("ALL", new Dictionary<string, int>());
                    if (bp == "ALL") BUILDREPLAYSCACHE[player][cmdr].Add("ALL", new List<dsreplay>());
                }
            }


            foreach (dsreplay rep in replays)
            {
                //if (rep.PLAYERCOUNT != 6) continue;
                if (rep.ISBRAWL == true) continue;
                foreach (dsplayer pl in rep.PLAYERS)
                {
                    foreach (string bp in DSdata.s_breakpoints)
                    {
                        if (!pl.UNITS.ContainsKey(bp)) continue;
                        dsplayer opp = rep.GetOpp(pl.REALPOS);
                        if (opp == null || opp.RACE == null) continue;

                        if (player == "player" && !_startUp.Conf.Players.Contains(pl.NAME)) continue;
                        //if (pl.NAME == player)
                        {
                            if (bp == "ALL")
                            {
                                BUILDREPLAYSCACHE[player][pl.RACE]["ALL"].Add(rep);
                                BUILDREPLAYSCACHE[player][pl.RACE][opp.RACE].Add(rep);
                            }
                            if (pl.UNITS.ContainsKey(bp))
                            {
                                games++;
                                GAMES[bp][pl.RACE]["ALL"]++;
                                GAMES[bp][pl.RACE][opp.RACE]++;
                                if (pl.TEAM == rep.WINNER)
                                {
                                    wins++;
                                    WINS[bp][pl.RACE]["ALL"]++;
                                    WINS[bp][pl.RACE][opp.RACE]++;
                                }

                                foreach (string unit in pl.UNITS[bp].Keys.ToArray())
                                {
                                    if (unit.StartsWith("Decoration")) continue;

                                    bool isBrawl = false;
                                    if (unit.StartsWith("Hybrid")) isBrawl = true;
                                    else if (unit.StartsWith("MercCamp")) isBrawl = true;

                                    if (isBrawl) continue;

                                    if (unit == "TychusTychus")
                                    {
                                        if (pl.UNITS[bp][unit] > 1) pl.UNITS[bp][unit] = 1;
                                    }
                                    
                                    string fixunit = FixUnitName(unit);
                                    //ALLUNITS.Add(fixunit);

                                    if (fixunit == "") continue;

                                    if (UNITS[bp][pl.RACE]["ALL"].ContainsKey(fixunit)) UNITS[bp][pl.RACE]["ALL"][fixunit] += pl.UNITS[bp][unit];
                                    else UNITS[bp][pl.RACE]["ALL"].Add(fixunit, pl.UNITS[bp][unit]);

                                    if (UNITS[bp][pl.RACE][opp.RACE].ContainsKey(fixunit)) UNITS[bp][pl.RACE][opp.RACE][fixunit] += pl.UNITS[bp][unit];
                                    else UNITS[bp][pl.RACE][opp.RACE].Add(fixunit, pl.UNITS[bp][unit]);


                                }
                                DURATION[bp][pl.RACE]["ALL"] += rep.DURATION;
                                DURATION[bp][pl.RACE][opp.RACE] += rep.DURATION;
                            }
                        }
                    }
                }
            }

            Console.WriteLine(replays.Count + " " + player + " Games: " + games + "|" + rgames + "|" + ygames + " Wins: " + wins);

              // build, cmdr, cmdr_vs, breakpoint, unit, count | wr, games
            //public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>> BUILDCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>>();
            //public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>> BUILDWRCACHE { get; private set; } = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>>();

            foreach (string bp in DSdata.s_breakpoints)
            {
                foreach (string cmdr in DSdata.s_races)
                {
                    double gwr = 0;
                    if (WINS.ContainsKey(bp) && WINS[bp].ContainsKey(cmdr) && WINS[bp][cmdr].ContainsKey("ALL") &&
                        GAMES.ContainsKey(bp) && GAMES[bp].ContainsKey(cmdr) && GAMES[bp][cmdr].ContainsKey("ALL"))
                    {
                        gwr = GenWr(WINS[bp][cmdr]["ALL"], GAMES[bp][cmdr]["ALL"]);
                    }
                    double gdur = 0;
                    if (DURATION.ContainsKey(bp) && DURATION[bp].ContainsKey(cmdr) && DURATION[bp][cmdr].ContainsKey("ALL"))
                    {
                        if (GAMES[bp][cmdr]["ALL"] > 0) gdur = DURATION[bp][cmdr]["ALL"] / GAMES[bp][cmdr]["ALL"] / 22.4;
                    }

                    if (!BUILDDURCACHE.ContainsKey(player)) BUILDDURCACHE.Add(player, new Dictionary<string, Dictionary<string, Dictionary<string, double>>>());
                    if (!BUILDDURCACHE[player].ContainsKey(cmdr)) BUILDDURCACHE[player].Add(cmdr, new Dictionary<string, Dictionary<string, double>>());
                    if (!BUILDDURCACHE[player][cmdr].ContainsKey("ALL")) BUILDDURCACHE[player][cmdr].Add("ALL", new Dictionary<string, double>());
                    if (!BUILDDURCACHE[player][cmdr]["ALL"].ContainsKey(bp)) BUILDDURCACHE[player][cmdr]["ALL"].Add(bp, 0);
                    BUILDDURCACHE[player][cmdr]["ALL"][bp] = gdur;

                    if (!BUILDWRCACHE.ContainsKey(player)) BUILDWRCACHE.Add(player, new Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>());
                    if (!BUILDWRCACHE[player].ContainsKey(cmdr)) BUILDWRCACHE[player].Add(cmdr, new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>());
                    if (!BUILDWRCACHE[player][cmdr].ContainsKey("ALL")) BUILDWRCACHE[player][cmdr].Add("ALL", new Dictionary<string, KeyValuePair<double, int>>());
                    if (!BUILDWRCACHE[player][cmdr]["ALL"].ContainsKey(bp)) BUILDWRCACHE[player][cmdr]["ALL"].Add(bp, new KeyValuePair<double, int>());
                    BUILDWRCACHE[player][cmdr]["ALL"][bp] = new KeyValuePair<double, int>(gwr, GAMES[bp][cmdr]["ALL"]);

                    if (!BUILDCACHE.ContainsKey(player)) BUILDCACHE.Add(player, new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>());
                    if (!BUILDCACHE[player].ContainsKey(cmdr)) BUILDCACHE[player].Add(cmdr, new Dictionary<string, Dictionary<string, Dictionary<string, double>>>());
                    if (!BUILDCACHE[player][cmdr].ContainsKey("ALL")) BUILDCACHE[player][cmdr].Add("ALL", new Dictionary<string, Dictionary<string, double>>());
                    if (!BUILDCACHE[player][cmdr]["ALL"].ContainsKey(bp)) BUILDCACHE[player][cmdr]["ALL"].Add(bp, new Dictionary<string, double>());

                    if (UNITS.ContainsKey(bp) && UNITS[bp].ContainsKey(cmdr) && UNITS[bp][cmdr].ContainsKey("ALL"))
                    {
                        foreach (string unit in UNITS[bp][cmdr]["ALL"].Keys)
                        {
                            double ucount = 0;
                            ucount = (double)UNITS[bp][cmdr]["ALL"][unit] / (double)GAMES[bp][cmdr]["ALL"];

                            if (!BUILDCACHE[player][cmdr]["ALL"].ContainsKey(unit)) BUILDCACHE[player][cmdr]["ALL"][bp].Add(unit, ucount);
                            else BUILDCACHE[player][cmdr]["ALL"][bp][unit] = ucount;
                        }
                    }

                    foreach (string vs in DSdata.s_races)
                    {
                        double wr = 0;
                        if (WINS.ContainsKey(bp) && WINS[bp].ContainsKey(cmdr) && WINS[bp][cmdr].ContainsKey(vs) &&
                            GAMES.ContainsKey(bp) && GAMES[bp].ContainsKey(cmdr) && GAMES[bp][cmdr].ContainsKey(vs))
                        {
                            wr = GenWr(WINS[bp][cmdr][vs], GAMES[bp][cmdr][vs]);
                        }
                        double dur = 0;
                        if (DURATION.ContainsKey(bp) && DURATION[bp].ContainsKey(cmdr) && DURATION[bp][cmdr].ContainsKey(vs))
                        {
                            if (GAMES[bp][cmdr][vs] > 0) dur = DURATION[bp][cmdr][vs] / GAMES[bp][cmdr][vs] / 22.4;
                        }

                        if (!BUILDDURCACHE[player][cmdr].ContainsKey(vs)) BUILDDURCACHE[player][cmdr].Add(vs, new Dictionary<string, double>());
                        if (!BUILDDURCACHE[player][cmdr][vs].ContainsKey(bp)) BUILDDURCACHE[player][cmdr][vs].Add(bp, 0);
                        BUILDDURCACHE[player][cmdr][vs][bp] = dur;

                        if (!BUILDWRCACHE[player][cmdr].ContainsKey(vs)) BUILDWRCACHE[player][cmdr].Add(vs, new Dictionary<string, KeyValuePair<double, int>>());
                        if (!BUILDWRCACHE[player][cmdr][vs].ContainsKey(bp)) BUILDWRCACHE[player][cmdr][vs].Add(bp, new KeyValuePair<double, int>());
                        BUILDWRCACHE[player][cmdr][vs][bp] = new KeyValuePair<double, int>(wr, GAMES[bp][cmdr][vs]);

                        if (!BUILDCACHE[player][cmdr].ContainsKey(vs)) BUILDCACHE[player][cmdr].Add(vs, new Dictionary<string, Dictionary<string, double>>());
                        if (!BUILDCACHE[player][cmdr][vs].ContainsKey(bp)) BUILDCACHE[player][cmdr][vs].Add(bp, new Dictionary<string, double>());

                        if (UNITS.ContainsKey(bp) && UNITS[bp].ContainsKey(cmdr) && UNITS[bp][cmdr].ContainsKey(vs))
                        {
                            foreach (string unit in UNITS[bp][cmdr][vs].Keys)
                            {
                                double ucount = 0;
                                ucount = (double)UNITS[bp][cmdr][vs][unit] / (double)GAMES[bp][cmdr][vs];

                                if (!BUILDCACHE[player][cmdr][vs].ContainsKey(unit)) BUILDCACHE[player][cmdr][vs][bp].Add(unit, ucount);
                                else BUILDCACHE[player][cmdr][vs][bp][unit] = ucount;
                            }
                        }
                    }
                }
            }

        }

        public string FixUnitName(string unit)
        {
            if (unit == "TrophyRiftPremium") return "";
            // abathur unknown
            if (unit == "ParasiticBombRelayDummy") return "";
            // raynor viking
            if (unit == "VikingFighter" || unit == "VikingAssault") return "Viking";
            if (unit == "DuskWings") return "DuskWing";
            // stukov lib
            if (unit == "InfestedLiberatorViralSwarm") return "InfestedLiberator";
            // Tychus extra mins
            if (unit == "MineralIncome") return "";
            // Zagara
            if (unit == "InfestedAbomination") return "Aberration";
            // Horner viking
            if (unit == "HornerDeimosVikingFighter" || unit == "HornerDeimosVikingAssault") return "HornerDeimosViking";
            if (unit == "HornerAssaultGalleonUpgraded") return "HornerAssaultGalleon";
            // Terrran thor
            if (unit == "ThorAP") return "Thor";

            Match m = rx_star.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString() + m.Groups[2].ToString();

            m = rx_light.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString() + m.Groups[2].ToString();

            m = rx_hero.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString();

            m = rx_mp.Match(unit);
            if (m.Success)
                return m.Groups[1].ToString();

            return unit;
        }
 
        public double GenWr(int wins, int games)
        {
            double wr = 0;
            if (games > 0)
            {
                wr = (double)wins * 100 / (double)games;
                wr = Math.Round(wr, 2);
            }
            return wr;
        }

        public double GenWR(double wins, double games)
        {
            double wr = 0;
            if (games > 0)
            {
                wr = wins * 100 / games;
                wr = Math.Round(wr, 2);
            }
            return wr;
        }

        public void GetDynData(DSdyn_filteroptions fil, 
                                out Dictionary<string, KeyValuePair<double, int>> winrate,
                                out Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winratevs,
                                out string info
                                )
        {
            if (fil == null) fil = new DSdyn_filteroptions();
            string myhash = fil.GenHash();

            winrate = new Dictionary<string, KeyValuePair<double, int>>();
            winratevs = new Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>();
            info = "Computing.";
            if (fil.Interest == "")
            {
                if (winrate_CACHE.ContainsKey(myhash))
                {
                    winrate = winrate_CACHE[myhash];
                    fil.fil = filter_CACHE[myhash];
                    info = "Winrate from Cache. " + myhash;
                    return;
                }
            } else
            {
                if (winratevs_CACHE.ContainsKey(myhash))
                {
                    winratevs = winratevs_CACHE[myhash];
                    fil.fil = filter_CACHE[myhash];
                    info = "WinrateVs from Cache." + myhash;
                    return;
                }
            }

            List<dsreplay> replays = new List<dsreplay>();
            replays = DBfilter.Filter(BUILD_REPLAYS[fil.Build].ToList(), fil, _startUp);

            if (fil.Mode == "Winrate")
            {
                if (fil.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (fil.Player == true)
                        {
                            List<dsreplay> temp = new List<dsreplay>();
                            temp = replays.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.RACE == cmdr)).ToList();
                            games = temp.Count();
                            wins = temp.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.TEAM == x.WINNER)).ToArray().Count();
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == cmdr)
                                    {
                                        games++;
                                        if (pl.TEAM == rep.WINNER)
                                        {
                                            wins++;
                                        }
                                    }
                                }
                            }
                        }
                        wr = GenWR(wins, games);
                        winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
                else if (fil.Interest != "")
                {
                    winratevs.Add(fil.Interest, new Dictionary<string, KeyValuePair<double, int>>());
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (fil.Player == true)
                        {
                            List<dsreplay> temp = new List<dsreplay>();
                            temp = replays.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.RACE == fil.Interest && x.GetOpp(y.REALPOS).RACE == cmdr)).ToList();
                            games = temp.Count();
                            wins = temp.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.RACE == fil.Interest && x.GetOpp(y.REALPOS).RACE == cmdr && y.TEAM == x.WINNER)).ToArray().Count();
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == fil.Interest && rep.GetOpp(pl.REALPOS).RACE == cmdr)
                                    {
                                        games++;
                                        if (pl.TEAM == rep.WINNER)
                                        {
                                            wins++;
                                        }
                                    }
                                }
                            }
                        }

                        wr = GenWR(wins, games);
                        winratevs[fil.Interest].Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
            }

            if (fil.Mode == "MVP")
            {
                if (fil.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (fil.Player == true)
                        {
                            List<dsreplay> temp = new List<dsreplay>();
                            temp = replays.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.RACE == cmdr)).ToList();
                            games = temp.Count();
                            wins = temp.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.KILLSUM == x.MAXKILLSUM)).ToArray().Count();
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == cmdr)
                                    {
                                        games++;
                                        if (pl.KILLSUM == rep.MAXKILLSUM)
                                        {
                                            wins++;
                                        }
                                    }
                                }
                            }
                        }

                        wr = GenWR(wins, games);
                        winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
                else if (fil.Interest != "")
                {
                    winratevs.Add(fil.Interest, new Dictionary<string, KeyValuePair<double, int>>());
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (fil.Player == true)
                        {
                            List<dsreplay> temp = new List<dsreplay>();
                            temp = replays.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.RACE == fil.Interest && x.GetOpp(y.REALPOS).RACE == cmdr)).ToList();
                            games = temp.Count();
                            wins = temp.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.RACE == fil.Interest && x.GetOpp(y.REALPOS).RACE == cmdr && y.KILLSUM == x.MAXKILLSUM)).ToArray().Count();
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == fil.Interest && rep.GetOpp(pl.REALPOS).RACE == cmdr)
                                    {
                                        games++;
                                        if (pl.KILLSUM == rep.MAXKILLSUM)
                                        {
                                            wins++;
                                        }
                                    }
                                }
                            }
                        }

                        wr = GenWR(wins, games);
                        winratevs[fil.Interest].Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
            }

            else if (fil.Mode == "Synergy")
            {
                if (fil.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        winrate.Add(cmdr, new KeyValuePair<double, int>(0, 0));
                    }
                }
                else if (fil.Interest != "")
                {
                    winratevs.Add(fil.Interest, new Dictionary<string, KeyValuePair<double, int>>());
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (fil.Player == true)
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (_startUp.Conf.Players.Contains(pl.NAME) && pl.RACE == fil.Interest)
                                    {
                                        foreach (var ent in rep.GetTeammates(pl).Where(x => x.RACE == cmdr))
                                        {
                                            games++;
                                            if (pl.TEAM == rep.WINNER)
                                            {
                                                wins++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == fil.Interest)
                                    {
                                        foreach (var ent in rep.GetTeammates(pl).Where(x => x.RACE == cmdr))
                                        {
                                            games++;
                                            if (pl.TEAM == rep.WINNER)
                                            {
                                                wins++;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        wr = GenWR(wins, games);
                        winratevs[fil.Interest].Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
            }

            else if (fil.Mode == "AntiSynergy")
            {
                if (fil.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        winrate.Add(cmdr, new KeyValuePair<double, int>(0, 0));
                    }
                }
                else if (fil.Interest != "")
                {
                    winratevs.Add(fil.Interest, new Dictionary<string, KeyValuePair<double, int>>());
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (fil.Player == true)
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (_startUp.Conf.Players.Contains(pl.NAME) && pl.RACE == fil.Interest)
                                    {
                                        foreach (var ent in rep.GetOpponents(pl).Where(x => x.RACE == cmdr))
                                        {
                                            games++;
                                            if (pl.TEAM == rep.WINNER)
                                            {
                                                wins++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (dsreplay rep in replays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == fil.Interest)
                                    {
                                        foreach (var ent in rep.GetOpponents(pl).Where(x => x.RACE == cmdr))
                                        {
                                            games++;
                                            if (pl.TEAM == rep.WINNER)
                                            {
                                                wins++;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        wr = GenWR(wins, games);
                        winratevs[fil.Interest].Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
            }

            else if (fil.Mode == "DPS")
            {
                if (fil.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (fil.Player == true)
                        {
                            List<dsreplay> dpslist = new List<dsreplay>();
                            dpslist = replays.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.RACE == cmdr)).ToList();
                            games = dpslist.Count();
                            foreach (dsreplay rep in dpslist)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (_startUp.Conf.Players.Contains(pl.NAME)) wins += pl.GetDPV();
                                }
                            }
                        }
                        else
                        {
                            List<dsreplay> dpslist = new List<dsreplay>();
                            dpslist = replays.Where(x => x.PLAYERS.Exists(y => y.RACE == cmdr)).ToList();
                            games = dpslist.Count();
                            foreach (dsreplay rep in dpslist)
                            {
                                int i = -1;
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == cmdr)
                                    {
                                        wins += pl.GetDPV();
                                        i++;
                                    }
                                }
                                games += i;
                            }
                        }

                        wr = Math.Round(wins / games, 2);
                        winrate.Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
                else if (fil.Interest != "")
                {
                    winratevs.Add(fil.Interest, new Dictionary<string, KeyValuePair<double, int>>());
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (fil.Player == true)
                        {
                            List<dsreplay> dpslist = new List<dsreplay>();
                            dpslist = replays.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.RACE == fil.Interest && x.GetOpp(y.REALPOS).RACE == cmdr)).ToList();
                            games = dpslist.Count();
                            foreach (dsreplay rep in dpslist)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (_startUp.Conf.Players.Contains(pl.NAME)) wins += pl.GetDPV();
                                }
                            }
                        }
                        else
                        {
                            List<dsreplay> dpslist = new List<dsreplay>();
                            dpslist = replays.Where(x => x.PLAYERS.Exists(y => y.RACE == fil.Interest && x.GetOpp(y.REALPOS).RACE == cmdr)).ToList();
                            games = dpslist.Count();
                            foreach (dsreplay rep in dpslist)
                            {
                                int i = -1;
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == fil.Interest && rep.GetOpp(pl.REALPOS).RACE == cmdr)
                                    {
                                        wins += pl.GetDPV();
                                        i++;
                                    }
                                }
                                games += i;
                            }
                        }

                        wr = Math.Round(wins / games, 2);
                        winratevs[fil.Interest].Add(cmdr, new KeyValuePair<double, int>(wr, (int)games));
                    }
                }
            }
            else if (fil.Mode == "Timeline")
            {
                if (fil.Interest == "")
                {
                    foreach (var cmdr in DSdata.s_races_cmdr)
                    {
                        winrate.Add(cmdr, new KeyValuePair<double, int>(0, 0));
                    }
                } else
                {
                    DateTime startdate = DateTime.Parse(fil.Startdate);
                    DateTime enddate = DateTime.Parse(fil.Enddate);
                    DateTime breakpoint = startdate;
                    DSdyn_filteroptions tfil = new DSdyn_filteroptions();
                    tfil.PropertyChanged += null;
                    tfil.Startdate = fil.Startdate;
                    tfil.Enddate = fil.Enddate;
                    winratevs.Add(fil.Interest, new Dictionary<string, KeyValuePair<double, int>>());

                    while (DateTime.Compare(breakpoint, enddate) < 0)
                    {
                        breakpoint = breakpoint.AddDays(7);
                        tfil.Enddate = breakpoint.ToString("yyyy-MM-dd");
                        List<Models.dsreplay> treplays = new List<Models.dsreplay>();
                        treplays = Models.DBfilter.Filter(replays.ToList(), tfil, _startUp);
                        

                        double games = 0;
                        double wins = 0;
                        double wr = 0;

                        if (fil.Player == true)
                        {
                            List<dsreplay> temp = new List<dsreplay>();
                            temp = treplays.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.RACE == fil.Interest)).ToList();
                            games = temp.Count();
                            wins = temp.Where(x => x.PLAYERS.Exists(y => _startUp.Conf.Players.Contains(y.NAME) && y.TEAM == x.WINNER)).ToArray().Count();
                        }
                        else
                        {
                            foreach (dsreplay rep in treplays)
                            {
                                foreach (dsplayer pl in rep.PLAYERS)
                                {
                                    if (pl.RACE == fil.Interest)
                                    {
                                        games++;
                                        if (pl.TEAM == rep.WINNER)
                                        {
                                            wins++;
                                        }
                                    }
                                }
                            }
                        }
                        if (games >= 10)
                        {
                            wr = GenWR(wins, games);
                            winratevs[fil.Interest].Add(breakpoint.ToString("yyyy-MM-dd"), new KeyValuePair<double, int>(wr, (int)games));
                            startdate = breakpoint;
                            tfil.Startdate = startdate.ToString("yyyy-MM-dd");
                        }

                    }
                }
            }
            
            winrate_CACHE.Add(myhash, winrate);
            winratevs_CACHE.Add(myhash, winratevs);
            filter_CACHE.Add(myhash, fil.fil);
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



