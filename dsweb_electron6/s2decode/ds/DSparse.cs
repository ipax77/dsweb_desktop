using dsweb_electron6.Models;
using IronPython.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using dsweb_electron6;

namespace s2decode
{
    class DSparse
    {

        static Regex rx_race2 = new Regex(@"Worker(.*)", RegexOptions.Singleline);
        static Regex rx_unit = new Regex(@"([^']+)Place([^']+)?", RegexOptions.Singleline);

        public static int MIN5 = 6720;
        public static int MIN10 = 13440;
        public static int MIN15 = 20160;
        public static List<KeyValuePair<string, int>> BREAKPOINTS { get; } = new List<KeyValuePair<string, int>>()
        {
            new KeyValuePair<string, int>("MIN5", MIN5),
            new KeyValuePair<string, int>("MIN10", MIN10),
            new KeyValuePair<string, int>("MIN15", MIN15),
            new KeyValuePair<string, int>("ALL", 0)
        };

        private static REParea AREA { get; set; } = new REParea();


        public static dsreplay GetDetails(string replay_file, dynamic details_dec)
        {
            string id = Path.GetFileNameWithoutExtension(replay_file);
            string reppath = Path.GetDirectoryName(replay_file);
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(reppath);
            MD5 md5 = new MD5CryptoServiceProvider();
            string reppath_md5 = System.BitConverter.ToString(md5.ComputeHash(plainTextBytes));
            string repid = reppath_md5 + "/" + id;


            dsreplay replay = new dsreplay();
            replay.REPLAY = repid;
            int failsafe_pos = 0;
            foreach (var player in details_dec["m_playerList"])
            {
                failsafe_pos++;
                string name = "";
                IronPython.Runtime.Bytes bab = null;
                try
                {
                    bab = player["m_name"];
                }
                catch { }

                if (bab != null) name = Encoding.UTF8.GetString(bab.ToByteArray());
                else name = player["m_name"].ToString();

                Match m2 = s2parse.rx_subname.Match(name);
                if (m2.Success) name = m2.Groups[1].Value;
                dsplayer pl = new dsplayer();

                pl.NAME = name;
                pl.RACE = player["m_race"].ToString();
                pl.RESULT = (int)player["m_result"];
                pl.TEAM = (int)player["m_teamId"];
                try
                {
                    pl.POS = (int)player["m_workingSetSlotId"] + 1;
                }
                catch
                {
                    pl.POS = failsafe_pos;
                }
                replay.PLAYERS.Add(pl);
            }

            long offset = (long)details_dec["m_timeLocalOffset"];
            long timeutc = (long)details_dec["m_timeUTC"];

            long georgian = timeutc + offset;
            DateTime gametime = DateTime.FromFileTime(georgian);
            replay.GAMETIME = double.Parse(gametime.ToString("yyyyMMddhhmmss"));

            return replay;
        }

        public static dsreplay GetTrackerevents(string replay_file, dynamic trackerevents_dec, dsreplay replay, StartUp _startUp)
        {
            string id = Path.GetFileNameWithoutExtension(replay_file);
            HashSet<string> bab = new HashSet<string>();

            //var tjson = JsonConvert.SerializeObject(trackerevents_dec, Formatting.Indented);
            //File.WriteAllText(@"C:\temp\bab\track_" + id + ".txt", tjson);

            REPtrackerevents track = new REPtrackerevents();
            Dictionary<int, REPvec> UNITPOS = new Dictionary<int, REPvec>();

            Dictionary<int, Dictionary<int, Dictionary<string, int>>> Spawns = new Dictionary<int, Dictionary<int, Dictionary<string, int>>>();
            Dictionary<int, Dictionary<int, bool>> Middle = new Dictionary<int, Dictionary<int, bool>>();


            foreach (dsplayer pl in replay.PLAYERS)
                track.PLAYERS.Add(pl.POS, pl);

            bool fix = false;
            if (replay.GAMETIME <= 20190121000000) fix = true;

            int races = 0;
            int units = 0;
            int spawns = 0;
            bool isBrawl_set = false;
            HashSet<string> Mutation = new HashSet<string>();

            foreach (PythonDictionary pydic in trackerevents_dec)
            {

                if (pydic.ContainsKey("m_unitTypeName"))
                {
                    if (pydic.ContainsKey("m_controlPlayerId"))
                    {
                        int playerid = (int)pydic["m_controlPlayerId"];
                        Match m = rx_race2.Match(pydic["m_unitTypeName"].ToString());
                        if (m.Success && m.Groups[1].Value.Length > 0)
                        {
                            races++;
                            string race = m.Groups[1].Value;

                            if (track.PLAYERS.ContainsKey(playerid))
                                track.PLAYERS[playerid].RACE = race;

                            if (fix == true)
                            {
                                if (race == "Nova") track.PLAYERS[playerid].ARMY += 250;
                                else if (race == "Zagara") track.PLAYERS[playerid].ARMY += 275;
                                else if (race == "Alarak") track.PLAYERS[playerid].ARMY += 300;
                                else if (race == "Kerrigan") track.PLAYERS[playerid].ARMY += 400;
                            }
                        }
                        else if (pydic.ContainsKey("m_creatorAbilityName") && pydic["m_creatorAbilityName"] == null)
                        {
                            if (pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitBornEvent")
                            {
                                int born_gameloop = (int)pydic["_gameloop"];
                                if (born_gameloop < 480) continue;
                                int born_playerid = (int)pydic["m_controlPlayerId"];
                                string born_unit = pydic["m_unitTypeName"].ToString();

                                if (born_unit == "TrophyRiftPremium") continue;
                                if (born_unit == "MineralIncome") continue;
                                if (born_unit == "ParasiticBombRelayDummy") continue;

                                dsplayer pl = replay.PLAYERS.Where(x => x.POS == born_playerid).FirstOrDefault();
                                if (pl != null)
                                {
                                    int fixloop = born_gameloop;

                                    if (Spawns.ContainsKey(born_playerid) && Spawns[born_playerid].Count > 0)
                                    {
                                        int maxloop = Spawns[born_playerid].OrderByDescending(x => x.Key).First().Key;
                                        if ((born_gameloop - maxloop) <= 720) {
                                            fixloop = maxloop;
                                        }
                                    }

                                    if (!Spawns.ContainsKey(born_playerid)) Spawns.Add(playerid, new Dictionary<int, Dictionary<string, int>>());
                                    if (!Spawns[born_playerid].ContainsKey(fixloop)) Spawns[born_playerid].Add(fixloop, new Dictionary<string, int>());
                                    if (!Spawns[born_playerid][fixloop].ContainsKey(born_unit)) Spawns[born_playerid][fixloop].Add(born_unit, 1);
                                    else Spawns[born_playerid][fixloop][born_unit]++;


                                    if (!track.UNITS.ContainsKey(born_playerid)) track.UNITS.Add(born_playerid, new Dictionary<string, Dictionary<string, int>>());

                                    if (fixloop >= 20640 && fixloop <= 22080)
                                        track.UNITS[born_playerid]["MIN15"] = Spawns[born_playerid][fixloop];
                                    else if (fixloop >= 13440 && fixloop <= 14880)
                                        track.UNITS[born_playerid]["MIN10"] = Spawns[born_playerid][fixloop];
                                    else if (fixloop >= 6240 && fixloop <= 7680)
                                        track.UNITS[born_playerid]["MIN5"] = Spawns[born_playerid][fixloop];

                                    if (track.UNITS[born_playerid].ContainsKey("ALL") && track.UNITS[born_playerid]["ALL"].ContainsKey("Gas"))
                                    {
                                        int gas = track.UNITS[born_playerid]["ALL"]["Gas"];
                                        int middle = track.UNITS[born_playerid]["ALL"]["Mid"];
                                        int upgrades = track.UNITS[born_playerid]["ALL"]["Upgrades"];
                                        track.UNITS[born_playerid]["ALL"] = Spawns[born_playerid][fixloop];
                                        track.UNITS[born_playerid]["ALL"]["Gas"] = gas;
                                        track.UNITS[born_playerid]["ALL"]["Mid"] = middle;
                                        track.UNITS[born_playerid]["ALL"]["Upgrades"] = upgrades;
                                    } else
                                        track.UNITS[born_playerid]["ALL"] = Spawns[born_playerid][fixloop];

                                    if (pl.REALPOS == null || pl.REALPOS == 0)
                                    {
                                        int pos = 0;
                                        if ((born_gameloop - 480) % 1440 == 0)
                                            pos = 1;
                                        else if ((born_gameloop - 481) % 1440 == 0)
                                            pos = 1;
                                        else if ((born_gameloop - 960) % 1440 == 0)
                                            pos = 2;
                                        else if ((born_gameloop - 961) % 1440 == 0)
                                            pos = 2;
                                        else if ((born_gameloop - 1440) % 1440 == 0)
                                            pos = 3;
                                        else if ((born_gameloop - 1441) % 1440 == 0)
                                            pos = 3;

                                        if (pos > 0)
                                        {
                                            int team = REParea.GetTeam((int)pydic["m_x"], (int)pydic["m_y"]);
                                            if (team == 1) pl.REALPOS = pos;
                                            else if (team == 2) pl.REALPOS = pos + 3;
                                            pl.TEAM = team - 1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (pydic.ContainsKey("m_upgradeTypeName") && pydic["m_upgradeTypeName"].ToString() == "MineralIncomeBonus") {
                    int playerid = (int)pydic["m_playerId"];
                    int gameloop = (int)pydic["_gameloop"];
                    int bonus = (int)pydic["m_count"];

                    dsplayer pl = replay.PLAYERS.Where(x => x.POS == playerid).FirstOrDefault();
                    if (pl != null)
                    {
                        if (pl.REALPOS > 0)
                        {
                            int lastmid = -1;
                            lastmid = track.Inc.Middle.LastOrDefault().Value;
                            
                            if (pl.TEAM == 0)
                            {
                                if (bonus > 0)
                                {
                                    track.Inc.MidT1 = true;
                                    track.Inc.MidT2 = false;
                                    track.Inc.Middle[gameloop] = 0;
                                }
                                else if (bonus < 0) track.Inc.MidT1 = false;
                                else if (lastmid >= 0) track.Inc.Middle[gameloop] = lastmid;
                            }
                            else if (pl.TEAM == 1)
                            {
                                if (bonus > 0)
                                {
                                    track.Inc.MidT2 = true;
                                    track.Inc.MidT1 = false;
                                    track.Inc.Middle[gameloop] = 1;
                                }
                                else if (bonus < 0) track.Inc.MidT2 = false;
                                else if (lastmid >= 0) track.Inc.Middle[gameloop] = lastmid;
                            }
                        }
                    }
                }
                else if (pydic.ContainsKey("m_unitTagIndex") && (int)pydic["m_unitTagIndex"] == 20 && pydic.ContainsKey("_event") && pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitOwnerChangeEvent")
                {
                    int gameloop = (int)pydic["_gameloop"];
                    int upkeepid = (int)pydic["m_upkeepPlayerId"];

                    if (upkeepid == 13)
                        track.Inc.Middle[gameloop] = 0;
                    else if (upkeepid == 14)
                        track.Inc.Middle[gameloop] = 1;

                }
                else if (pydic.ContainsKey("m_stats"))
                {
                    int playerid = (int)pydic["m_playerId"];
                    int gameloop = (int)pydic["_gameloop"];
                    int spawn = (gameloop - 480) % 1440;
                    int pos = 0;

                    if (isBrawl_set == false)
                    {
                        if (Mutation.Contains("MutationCovenant"))
                            replay.GAMEMODE = "GameModeSwitch";
                        else if (Mutation.Contains("MutationEquipment"))
                            replay.GAMEMODE = "GameModeGear";
                        else if (Mutation.Contains("MutationExile")
                                && Mutation.Contains("MutationRescue")
                                && Mutation.Contains("MutationShroud")
                                && Mutation.Contains("MutationSuperscan"))
                            replay.GAMEMODE = "GameModeSabotage";
                        else if (Mutation.Contains("MutationCommanders"))
                        {
                            replay.GAMEMODE = "GameModeCommanders"; // fail safe
                            if (Mutation.Count() == 3 && Mutation.Contains("MutationExpansion") && Mutation.Contains("MutationOvertime")) replay.GAMEMODE = "GameModeCommandersHeroic";
                            else if (Mutation.Count() == 2 && Mutation.Contains("MutationOvertime")) replay.GAMEMODE = "GameModeCommanders";
                            else if (Mutation.Count() >= 3) replay.GAMEMODE = "GameModeBrawlCommanders";
                        }
                        else
                        {
                            if (replay.GAMEMODE == "unknown" && Mutation.Count() == 0) replay.GAMEMODE = "GameModeStandard";
                            else if (replay.GAMEMODE == "unknown" && Mutation.Count() > 0) replay.GAMEMODE = "GameModeBrawlStandard";
                        }

                        replay.ISBRAWL = true;
                        if (replay.GAMEMODE == "GameModeCommanders" || replay.GAMEMODE == "GameModeCommandersHeroic" || replay.GAMEMODE == "GameModeStandard")
                            replay.ISBRAWL = false;

                        isBrawl_set = true;
                    }

                    if (track.PLAYERS.ContainsKey(playerid))
                    {
                        spawns++;
                        bool failsafe = false;
                        if (track.PLAYERS[playerid].REALPOS > 0) pos = track.PLAYERS[playerid].REALPOS;
                        else
                        {
                            pos = track.PLAYERS[playerid].POS;
                            failsafe = true;
                        }

                        PythonDictionary pystats = pydic["m_stats"] as PythonDictionary;
                        double income = Convert.ToDouble((int)pystats["m_scoreValueMineralsCollectionRate"]);
                        track.PLAYERS[playerid].KILLSUM = (int)pystats["m_scoreValueMineralsKilledArmy"];
                        track.PLAYERS[playerid].INCOME += income / 9.15;
                        bool playerspawn = false;
                        if (spawn == 0 && (pos == 1 || pos == 4)) playerspawn = true;
                        if (spawn == 480 && (pos == 2 || pos == 5)) playerspawn = true;
                        if (spawn == 960 && (pos == 3 || pos == 6)) playerspawn = true;
                        if (playerspawn == true) track.PLAYERS[playerid].ARMY += (int)pystats["m_scoreValueMineralsUsedActiveForces"];

                        if (failsafe) continue;
                        if (income < 400) continue;
                        /**
                        ticks modify  1gas income  possible1 possible2
                        160 2,8125  0,1875  450 base
                        160 3       0,1875  480 1gas
                        160 3,1875  0,1875  510 2gas mid
                        160 3,375   0,1875  540 3gas mid +1gas
                        160 3,5625  0,1875  570 4gas mid +2gas
                        160 3,75    0,1875  600     mid + 3gas
                        160 3,9375  0,1875  630     mid + 4gas
                        **/
                        int gas = 0;
                        int mid = 0;

                        int last_gameloop = 0;
                        int last_teammid = -1;

                        int solidmid_t1 = 0;
                        int solidmid_t2 = 0;
                        int solidmid = 0;

                        int i = 0;
                        foreach (int myloop in track.Inc.Middle.Keys)
                        {

                            int gmid = track.Inc.Middle[myloop];

                            if (last_gameloop > 0 && last_teammid >= 0)
                            {
                                if (!(gmid == track.PLAYERS[playerid].TEAM && last_teammid == track.PLAYERS[playerid].TEAM))
                                    mid += myloop - last_gameloop;
                            }

                            if (i > track.Inc.Middle.Count - 120)
                            {
                                if (gmid == 0) solidmid_t1++;
                                else if (gmid == 1) solidmid_t2++;
                            }

                            if (gmid == last_teammid) solidmid++;
                            else solidmid = 0;

                            last_gameloop = myloop;
                            last_teammid = gmid;
                            i++;
                        }
                        // unknown why (empiric research)
                        mid -= gameloop;
                        mid *= -1;
                        mid -= track.Inc.Middle.FirstOrDefault().Key;
                        if (mid < 0) mid = 0;

                        if (!Middle.ContainsKey(pos)) Middle.Add(pos, new Dictionary<int, bool>());

                        bool? lastmid = Middle[pos].LastOrDefault().Value;
                        bool middle = false;
                        if (pos <= 3)
                        {
                            Middle[pos][gameloop] = track.Inc.MidT1;
                            middle = track.Inc.MidT1;
                        }
                        else if (pos > 3)
                        {
                            Middle[pos][gameloop] = track.Inc.MidT2;
                            middle = track.Inc.MidT2;
                        }
                        

                        //string info = gameloop + "; " + pos + "; " + (int)income + "; " + track.Inc.MidT1 + "; " + track.Inc.MidT2 + "; " + solidmid + "; " + solidmid_t1 + "; " + solidmid_t2;
                        //File.AppendAllText(@"C:\temp\bab\analyzes\income.txt", info + Environment.NewLine);
                        /**
                        double midincome = 0.5;
                        if (pos <= 3) income -= midincome * (double)solidmid_t1;
                        else if (pos > 3) income -= midincome * (double)solidmid_t2;
                        **/

                        if (lastmid != null && lastmid == middle)
                        {

                            if (middle) income -= 60;

                            //if (pos <= 3 && solidmid_t1 >= 14) income -= 60;
                            //else if (pos > 3 && solidmid_t2 >= 14) income -= 60;

                            if (income < 470) gas = 0; // base income
                            else if (income < 500) gas = 1;
                            else if (income < 530 && gameloop > 2240) gas = 2;
                            else if (income < 560 && gameloop > 4480) gas = 3;
                            else if (income < 600 && gameloop > 13440) gas = 4;
                        }



                        replay.DURATION = (int)pydic["_gameloop"];
                        track.PLAYERS[playerid].PDURATION = replay.DURATION;

                        foreach (var bp in BREAKPOINTS)
                        {
                            if (bp.Value > 0 && gameloop > bp.Value) continue;
                            if (!track.UNITS.ContainsKey(playerid)) track.UNITS.Add(playerid, new Dictionary<string, Dictionary<string, int>>());
                            if (!track.UNITS[playerid].ContainsKey(bp.Key)) track.UNITS[playerid].Add(bp.Key, new Dictionary<string, int>());
                            if (!track.UNITS[playerid][bp.Key].ContainsKey("Upgrades"))
                                track.UNITS[playerid][bp.Key]["Upgrades"] = (int)pystats["m_scoreValueMineralsUsedCurrentTechnology"];
                            else
                                if ((int)pystats["m_scoreValueMineralsUsedCurrentTechnology"] > track.UNITS[playerid][bp.Key]["Upgrades"])
                                track.UNITS[playerid][bp.Key]["Upgrades"] = (int)pystats["m_scoreValueMineralsUsedCurrentTechnology"];

                            if (!track.UNITS[playerid][bp.Key].ContainsKey("Gas")) track.UNITS[playerid][bp.Key]["Gas"] = gas;
                            else if (gas > 0 && gas > track.UNITS[playerid][bp.Key]["Gas"]) track.UNITS[playerid][bp.Key]["Gas"] = gas;

                            track.UNITS[playerid][bp.Key]["Mid"] = mid;
                        }
                    }
                }
                else if (isBrawl_set == false && pydic.ContainsKey("_gameloop") && (int)pydic["_gameloop"] == 0 && pydic.ContainsKey("m_upgradeTypeName"))
                {
                    if (pydic["m_upgradeTypeName"].ToString().StartsWith("Mutation"))
                        Mutation.Add(pydic["m_upgradeTypeName"].ToString());
                }
            }

            replay.PLAYERCOUNT = replay.PLAYERS.Count;

            // fail safe
            FixPos(replay);
            FixWinner(replay, _startUp);

            int flast_teammid = 0;
            int fmid = 0;
            int flast_gameloop = 0;
            foreach (int myloop in track.Inc.Middle.Keys)
            {

                int gmid = track.Inc.Middle[myloop];

                if (flast_gameloop > 0 && flast_teammid >= 0)
                {
                    if (!(gmid == 1 && flast_teammid == 1))
                        fmid += myloop - flast_gameloop;
                }

                flast_gameloop = myloop;
                flast_teammid = gmid;
            }

            if (replay.WINNER == 0) {
                replay.MIDTEAMWINNER = fmid;
                replay.MIDTEAMSECOND = replay.DURATION - fmid;
            }
            else {
                replay.MIDTEAMSECOND = fmid;
                replay.MIDTEAMWINNER = replay.DURATION - fmid;
            }

            foreach (dsplayer pl in replay.PLAYERS)
            {
                if (track.UNITS.ContainsKey(pl.POS)) pl.UNITS = track.UNITS[pl.POS];
                pl.INCOME = Math.Round(pl.INCOME, 2);
            }

            return replay;
        }

        public static void FixWinner(dsreplay replay, StartUp _startUp)
        {
            bool player = false;
            foreach (dsplayer pl in replay.PLAYERS)
            {
                //if (MW.player_list.Contains(pl.NAME))
                if (_startUp.Conf.Players.Contains(pl.NAME))
                {
                    player = true;
                    int oppteam;
                    if (pl.TEAM == 0) oppteam = 1;
                    else oppteam = 0;

                    if (pl.RESULT == 1) replay.WINNER = pl.TEAM;
                    else replay.WINNER = oppteam;
                    break;
                }
            }

            if (player == false)
            {
                foreach (dsplayer pl in replay.PLAYERS)
                {
                    if (pl.RESULT == 1)
                    {
                        int oppteam;
                        if (pl.TEAM == 0) oppteam = 1;
                        else oppteam = 0;

                        replay.WINNER = pl.TEAM;
                        break;
                    }
                }
            }

            foreach (dsplayer pl in replay.PLAYERS)
            {
                if (pl.TEAM == replay.WINNER) pl.RESULT = 1;
                else pl.RESULT = 2;
            }

        }

        public static void FixPos(dsreplay replay)
        {
            foreach (dsplayer pl in replay.PLAYERS)
            {
                if (pl.REALPOS == 0)
                {
                    for (int j = 1; j <= 6; j++)
                    {
                        if (replay.PLAYERCOUNT == 2 && (j == 2 || j == 3 || j == 5 || j == 6)) continue;
                        if (replay.PLAYERCOUNT == 4 && (j == 3 || j == 6)) continue;

                        List<dsplayer> temp = new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == j).ToList());
                        if (temp.Count == 0)
                        {
                            pl.REALPOS = j;
                            Program.Log("Fixing missing playerid for " + pl.POS + "|" + pl.REALPOS + " => " + j);
                        }
                    }



                    if (new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == pl.POS).ToList()).Count == 0) pl.REALPOS = pl.POS;

                }

                if (new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == pl.REALPOS).ToList()).Count > 1)
                {
                    Console.WriteLine("Found double playerid for " + pl.POS + "|" + pl.REALPOS);

                    for (int j = 1; j <= 6; j++)
                    {
                        if (replay.PLAYERCOUNT == 2 && (j == 2 || j == 3 || j == 5 || j == 6)) continue;
                        if (replay.PLAYERCOUNT == 4 && (j == 3 || j == 6)) continue;
                        if (new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == j).ToList()).Count == 0)
                        {
                            pl.REALPOS = j;
                            Program.Log("Fixing double playerid for " + pl.POS + "|" + pl.REALPOS + " => " + j);
                            break;
                        }
                    }

                }

            }
        }

        public class REPvec
        {
            public int x { get; set; }
            public int y { get; set; }

            public REPvec(int X, int Y)
            {
                x = X;
                y = Y;
            }
        }

        private class REParea
        {
            public static Dictionary<int, Dictionary<string, REPvec>> POS { get; set; } = new Dictionary<int, Dictionary<string, REPvec>>()
            {
                // spawn area pl 1,2,3
                { 1, new Dictionary<string, REPvec>() {
                    { "A", new REPvec(107, 162) },
                    { "B", new REPvec(160, 106) },
                    { "C", new REPvec(218, 160) },
                    { "D", new REPvec(162, 216) }
                }
                },
                // spawn area pl 4,5,6
                { 2, new Dictionary<string, REPvec>()
                {
                    { "A", new REPvec(35, 88) },
                    { "B", new REPvec(92, 30) },
                    { "C", new REPvec(142, 99) },
                    { "D", new REPvec(100, 144) }
                }
                }
            };

            public static int GetTeam(int x, int y)
            {
                int team = 0;
                bool indahouse = false;
                foreach (int plpos in POS.Keys)
                {
                    indahouse = PointInTriangle(x, y, POS[plpos]["A"].x, POS[plpos]["A"].y, POS[plpos]["B"].x, POS[plpos]["B"].y, POS[plpos]["C"].x, POS[plpos]["C"].y);
                    if (indahouse == false) indahouse = PointInTriangle(x, y, POS[plpos]["A"].x, POS[plpos]["A"].y, POS[plpos]["D"].x, POS[plpos]["D"].y, POS[plpos]["C"].x, POS[plpos]["C"].y);

                    if (indahouse == true)
                    {
                        team = plpos;
                        break;
                    }
                }
                return team;
            }


            private static bool PointInTriangle(int Px, int Py, int Ax, int Ay, int Bx, int By, int Cx, int Cy)
            {
                bool indahouse = false;
                int b1 = 0;
                int b2 = 0;
                int b3 = 0;

                if (sign(Px, Py, Ax, Ay, Bx, By) < 0) b1 = 1;
                if (sign(Px, Py, Bx, By, Cx, Cy) < 0) b2 = 1;
                if (sign(Px, Py, Cx, Cy, Ax, Ay) < 0) b3 = 1;

                if ((b1 == b2) && (b2 == b3)) indahouse = true;
                return indahouse;
            }

            private static int sign(int Ax, int Ay, int Bx, int By, int Cx, int Cy)
            {
                int sig = (Ax - Cx) * (By - Cy) - (Bx - Cx) * (Ay - Cy);
                return sig;
            }
        }

        private class REPtrackerevents
        {
            public Dictionary<int, dsplayer> PLAYERS { get; set; } = new Dictionary<int, dsplayer>();
            public Dictionary<int, Dictionary<string, Dictionary<string, int>>> UNITS { get; set; } = new Dictionary<int, Dictionary<string, Dictionary<string, int>>>();
            public int SUMT1 { get; set; } = 0;
            public int SUMT2 { get; set; } = 0;
            public Teamincome Inc { get; set; } = new Teamincome();
        }

        private class Teamincome
        {
            public bool MidT1 { get; set; } = false;
            public bool MidT2 { get; set; } = false;
            public Dictionary<int, int> Middle { get; set; } = new Dictionary<int, int>();
        }
    }
}

