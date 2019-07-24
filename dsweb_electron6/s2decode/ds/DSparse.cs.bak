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
                pl.RESULT = int.Parse(player["m_result"].ToString());
                pl.TEAM = int.Parse(player["m_teamId"].ToString());
                try
                {
                    pl.POS = int.Parse(player["m_workingSetSlotId"].ToString()) + 1;
                }
                catch
                {
                    pl.POS = failsafe_pos;
                }
                replay.PLAYERS.Add(pl);
            }

            long offset = long.Parse(details_dec["m_timeLocalOffset"].ToString());
            long timeutc = long.Parse(details_dec["m_timeUTC"].ToString());

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
            REParea area = AREA;
            Dictionary<int, REPvec> UNITPOS = new Dictionary<int, REPvec>();

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
                        int playerid = int.Parse(pydic["m_controlPlayerId"].ToString());
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
                        else if (pydic.ContainsKey("m_creatorAbilityName") && pydic["m_creatorAbilityName"] != null)
                        {
                            m = rx_unit.Match(pydic["m_creatorAbilityName"].ToString());
                            if (m.Success)
                            {
                                units++;
                                int gameloop = int.Parse(pydic["_gameloop"].ToString());
                                string unit = m.Groups[1].Value;
                                if (m.Groups[2].Value.Length > 0) unit += m.Groups[2].Value;

                                // failsafe double tychus
                                if (pydic.ContainsKey("m_unitTypeName"))
                                    if (pydic["m_unitTypeName"].ToString() == "UnitBirthBar")
                                        continue;


                                foreach (var bp in BREAKPOINTS)
                                {
                                    if (bp.Value > 0 && gameloop > bp.Value) continue;


                                    if (track.UNITS.ContainsKey(playerid))
                                    {
                                        if (track.UNITS[playerid].ContainsKey(bp.Key))
                                        {
                                            if (track.UNITS[playerid][bp.Key].ContainsKey(unit)) track.UNITS[playerid][bp.Key][unit] = track.UNITS[playerid][bp.Key][unit] + 1;
                                            else track.UNITS[playerid][bp.Key].Add(unit, 1);
                                        }
                                        else
                                        {
                                            track.UNITS[playerid].Add(bp.Key, new Dictionary<string, int>());
                                            track.UNITS[playerid][bp.Key].Add(unit, 1);
                                        }
                                    }
                                    else
                                    {
                                        track.UNITS.Add(playerid, new Dictionary<string, Dictionary<string, int>>());
                                        track.UNITS[playerid].Add(bp.Key, new Dictionary<string, int>());
                                        track.UNITS[playerid][bp.Key].Add(unit, 1);
                                    }

                                }


                                if (track.PLAYERS[playerid].REALPOS == 0)
                                {
                                    int x = int.Parse(pydic["m_x"].ToString());
                                    int y = int.Parse(pydic["m_y"].ToString());

                                    int pos = area.GetPos(x, y);

                                    if (pos > 0)
                                    {
                                        foreach (dsplayer fpl in track.PLAYERS.Values)
                                        {
                                            if (fpl.REALPOS == pos)
                                            {
                                                if (UNITPOS.ContainsKey(fpl.POS)) Program.Log(id + " Double pos: X: " + x + " Y: " + y + " POS:" + track.PLAYERS[playerid].POS + " REALPOS: " + pos + " (DX: " + UNITPOS[fpl.POS].x + " DY: " + UNITPOS[fpl.POS].y + " DPOS: " + fpl.POS + " DREALPOS: " + fpl.REALPOS + ")");
                                            }
                                        }
                                        if (!UNITPOS.ContainsKey(playerid)) UNITPOS.Add(playerid, new REPvec(x, y));
                                        track.PLAYERS[playerid].REALPOS = pos;
                                    }
                                }

                                if (fix == true)
                                {
                                    if (unit == "StukovInfestedBunker") track.PLAYERS[playerid].ARMY += 375;
                                    else if (unit == "HornerAssaultGalleon") track.PLAYERS[playerid].ARMY += 475;

                                }
                            }
                        }
                    }
                }
                else if (pydic.ContainsKey("m_stats"))
                {
                    int playerid = int.Parse(pydic["m_playerId"].ToString());
                    int gameloop = int.Parse(pydic["_gameloop"].ToString());
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
                        if (track.PLAYERS[playerid].REALPOS > 0) pos = track.PLAYERS[playerid].REALPOS;
                        else pos = track.PLAYERS[playerid].POS;

                        PythonDictionary pystats = pydic["m_stats"] as PythonDictionary;
                        track.PLAYERS[playerid].KILLSUM = (int)pystats["m_scoreValueMineralsKilledArmy"];
                        track.PLAYERS[playerid].INCOME += Convert.ToDouble(pystats["m_scoreValueMineralsCollectionRate"]) / 9.15;

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
                        if (pos > 0)
                        {
                            if (!track.Inc.FS.ContainsKey(pos)) track.Inc.FS[pos] = 0;
                            int income = (int)pystats["m_scoreValueMineralsCollectionRate"];
                            if (pos == 1)
                            {
                                if (track.SUMT1 > track.SUMT2)
                                {
                                    track.Inc.T1 = true;
                                    track.Inc.T2 = false;
                                    track.Inc.MidT1 += 160;
                                }
                                else
                                {
                                    track.Inc.T1 = false;
                                    track.Inc.T2 = true;
                                    track.Inc.MidT2 += 160;
                                }
                                track.SUMT1 = income;
                                track.SUMT2 = 0;
                                mid = track.Inc.MidT1;
                            }
                            else if (pos <= 3)
                            {
                                track.SUMT1 += income;
                                mid = track.Inc.MidT1;
                            }
                            else if (pos > 3)
                            {
                                track.SUMT2 += income;
                                mid = track.Inc.MidT2;
                            }

                            if (pos <= 3 && track.Inc.T1) income -= 60;
                            else if (pos > 3 && track.Inc.T2) income -= 60;


                            if (income < 470) gas = 0; // base income
                            else if (income < 500) gas = 1;
                            else if (income < 530 && gameloop > 2240) gas = 2;
                            else if (income < 560 && gameloop > 4480) gas = 3;
                            else if (income < 590 && gameloop > 13440) gas = 4;


                            bool playerspawn = false;
                            if (spawn == 0 && (pos == 1 || pos == 4)) playerspawn = true;
                            if (spawn == 480 && (pos == 2 || pos == 5)) playerspawn = true;
                            if (spawn == 960 && (pos == 3 || pos == 6)) playerspawn = true;
                            if (playerspawn == true) track.PLAYERS[playerid].ARMY += (int)pystats["m_scoreValueMineralsUsedActiveForces"];
                        }

                        replay.DURATION = int.Parse(pydic["_gameloop"].ToString());
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
                            else
                            {
                                if (gas > track.UNITS[playerid][bp.Key]["Gas"])
                                {
                                    if (track.Inc.FS[pos] > 3)
                                    {
                                        track.UNITS[playerid][bp.Key]["Gas"] = gas;
                                        track.Inc.FS[pos] = 0;
                                    }
                                    else
                                    {
                                        track.Inc.FS[pos]++;
                                    }
                                }
                            }
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

            foreach (dsplayer pl in replay.PLAYERS)
            {
                if (track.UNITS.ContainsKey(pl.POS)) pl.UNITS = track.UNITS[pl.POS];

                if (pl.UNITS != null && pl.UNITS.ContainsKey("ALL") && pl.UNITS["ALL"].ContainsKey("Mid"))
                {
                    if (pl.TEAM == replay.WINNER)
                    {
                        if (pl.UNITS["ALL"]["Mid"] > replay.MIDTEAMWINNER) replay.MIDTEAMWINNER = pl.UNITS["ALL"]["Mid"];
                    }
                    else
                    {
                        if (pl.UNITS["ALL"]["Mid"] > replay.MIDTEAMSECOND) replay.MIDTEAMSECOND = pl.UNITS["ALL"]["Mid"];
                    }
                }
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

        private class REPvec
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
            public Dictionary<int, Dictionary<string, REPvec>> POS { get; set; } = new Dictionary<int, Dictionary<string, REPvec>>();

            public REParea()
            {

                POS.Add(1, new Dictionary<string, REPvec>());
                POS[1].Add("A", new REPvec(115, 202));
                POS[1].Add("B", new REPvec(154, 177));
                POS[1].Add("C", new REPvec(184, 208));
                POS[1].Add("D", new REPvec(153, 239));

                POS.Add(2, new Dictionary<string, REPvec>());
                POS[2].Add("A", new REPvec(151, 178));
                POS[2].Add("B", new REPvec(179, 151));
                POS[2].Add("C", new REPvec(210, 181));
                POS[2].Add("D", new REPvec(183, 208));

                POS.Add(3, new Dictionary<string, REPvec>());
                POS[3].Add("A", new REPvec(179, 151));
                POS[3].Add("B", new REPvec(206, 108));
                POS[3].Add("C", new REPvec(243, 150));
                POS[3].Add("D", new REPvec(210, 181));

                POS.Add(4, new Dictionary<string, REPvec>());
                POS[4].Add("A", new REPvec(6, 90));
                POS[4].Add("B", new REPvec(35, 56));
                POS[4].Add("C", new REPvec(69, 89));
                POS[4].Add("D", new REPvec(36, 122));

                POS.Add(5, new Dictionary<string, REPvec>());
                POS[5].Add("A", new REPvec(35, 56));
                POS[5].Add("B", new REPvec(57, 32));
                POS[5].Add("C", new REPvec(93, 65));
                POS[5].Add("D", new REPvec(69, 89));

                POS.Add(6, new Dictionary<string, REPvec>());
                POS[6].Add("A", new REPvec(57, 32));
                POS[6].Add("B", new REPvec(91, 0));
                POS[6].Add("C", new REPvec(126, 33));
                POS[6].Add("D", new REPvec(93, 65));

            }

            public int GetPos(int x, int y)
            {
                int pos = 0;
                bool indahouse = false;
                foreach (int plpos in POS.Keys)
                {
                    indahouse = PointInTriangle(x, y, POS[plpos]["A"].x, POS[plpos]["A"].y, POS[plpos]["B"].x, POS[plpos]["B"].y, POS[plpos]["C"].x, POS[plpos]["C"].y);
                    if (indahouse == false) indahouse = PointInTriangle(x, y, POS[plpos]["A"].x, POS[plpos]["A"].y, POS[plpos]["D"].x, POS[plpos]["D"].y, POS[plpos]["C"].x, POS[plpos]["C"].y);

                    if (indahouse == true)
                    {
                        pos = plpos;
                        break;
                    }
                }
                return pos;
            }


            private bool PointInTriangle(int Px, int Py, int Ax, int Ay, int Bx, int By, int Cx, int Cy)
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

            private int sign(int Ax, int Ay, int Bx, int By, int Cx, int Cy)
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
            public bool T1 { get; set; } = false;
            public bool T2 { get; set; } = false;
            public int MidT1 { get; set; } = 0;
            public int MidT2 { get; set; } = 0;
            public Dictionary<int, int> FS { get; set; } = new Dictionary<int, int>();
        }
    }
}

