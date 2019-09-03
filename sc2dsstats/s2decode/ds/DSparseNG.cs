using IronPython.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using sc2dsstats;
using sc2dsstats.Models;

namespace s2decode
{
    class DSparseNG
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

            dsreplay replay = new dsreplay();
            int failsafe_pos = 0;
            foreach (var player in details_dec["m_playerList"])
            {
                if (player["m_observe"] > 0) continue;

                failsafe_pos++;
                string name = "";
                Bytes bab = null;
                try
                {
                    bab = player["m_name"];
                }
                catch { }

                if (bab != null) name = Encoding.UTF8.GetString(bab.ToByteArray());
                else name = player["m_name"].ToString();

                Match m2 = s2parse.rx_subname.Match(name);
                if (m2.Success) name = m2.Groups[1].Value;
                Program.Log("Replay playername: " + name);
                dsplayer pl = new dsplayer();

                pl.NAME = name;
                pl.RACE = player["m_race"].ToString();
                Program.Log("Replay race: " + pl.RACE);
                pl.RESULT = int.Parse(player["m_result"].ToString());
                pl.TEAM = int.Parse(player["m_teamId"].ToString());
                try
                {
                    //pl.POS = int.Parse(player["m_workingSetSlotId"].ToString()) + 1;
                    pl.POS = failsafe_pos;
                }
                catch
                {
                    pl.POS = failsafe_pos;
                }
                replay.PLAYERS.Add(pl);
            }

            replay.PLAYERCOUNT = replay.PLAYERS.Count();

            long offset = long.Parse(details_dec["m_timeLocalOffset"].ToString());
            long timeutc = long.Parse(details_dec["m_timeUTC"].ToString());

            long georgian = timeutc + offset;
            DateTime gametime = DateTime.FromFileTime(georgian);
            replay.GAMETIME = double.Parse(gametime.ToString("yyyyMMddhhmmss"));
            Program.Log("Replay gametime: " + replay.GAMETIME);

            return replay;
        }

        public static dsreplay GetTrackerevents(string replay_file, dynamic trackerevents_dec, dsreplay replay)
        {
            bool fix = false;
            if (replay.GAMETIME <= 20190121000000) fix = true;

            bool isBrawl_set = false;
            HashSet<string> Mutation = new HashSet<string>();

            foreach (PythonDictionary pydic in trackerevents_dec)
            {
                if (pydic.ContainsKey("m_unitTypeName"))
                {
                    if (pydic.ContainsKey("m_controlPlayerId"))
                    {
                        int playerid = (int)pydic["m_controlPlayerId"];
                        int gameloop = (int)pydic["_gameloop"];

                        if (pydic["m_unitTypeName"].ToString().StartsWith("DeathBurst"))
                        {
                            replay.DURATION = gameloop;
                            if (playerid == 13)
                                replay.WINNER = 0;
                            else if (playerid == 14)
                                replay.WINNER = 1;
                            break;
                        }


                        if (playerid == 0 || playerid > 6) continue;
                        dsplayer pl = replay.PLAYERS.Where(x => x.POS == playerid).FirstOrDefault();
                        if (pl == null) continue;

                        Match m = rx_race2.Match(pydic["m_unitTypeName"].ToString());
                        if (m.Success && m.Groups[1].Value.Length > 0)
                        {
                            replay.PLAYERS[playerid - 1].RACE = m.Groups[1].Value;
                        }
                        else if (pydic.ContainsKey("m_creatorAbilityName") && pydic["m_creatorAbilityName"] == null)
                        {
                            if (gameloop < 480) continue;
                            if (pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitBornEvent")
                            {
                                string born_unit = pydic["m_unitTypeName"].ToString();

                                if (born_unit == "TrophyRiftPremium") continue;
                                if (born_unit == "MineralIncome") continue;
                                if (born_unit == "ParasiticBombRelayDummy") continue;
                                if (born_unit == "Biomass") continue;

                                //int fixloop = gameloop;
                                int fixloop = pl.LastSpawn;

                                /**
                                if (pl.SPAWNS.Count() > 0)
                                {
                                    int maxloop = pl.SPAWNS.ElementAt(pl.SPAWNS.Count() - 1).Key;
                                    if ((gameloop - maxloop) <= 470)
                                        fixloop = maxloop;
                                }
                                **/

                                if (!pl.SPAWNS.ContainsKey(fixloop)) pl.SPAWNS.Add(fixloop, new Dictionary<string, int>());
                                if (!pl.SPAWNS[fixloop].ContainsKey(born_unit)) pl.SPAWNS[fixloop].Add(born_unit, 1);
                                else pl.SPAWNS[fixloop][born_unit]++;

                                if (pl.REALPOS == null || pl.REALPOS == 0)
                                {
                                    int pos = 0;

                                    if (replay.PLAYERCOUNT == 2)
                                        pos = 1;
                                    else if ((gameloop - 480) % 1440 == 0)
                                        pos = 1;
                                    else if ((gameloop - 481) % 1440 == 0)
                                        pos = 1;
                                    else if ((gameloop - 960) % 1440 == 0)
                                        pos = 2;
                                    else if ((gameloop - 961) % 1440 == 0)
                                        pos = 2;
                                    else if ((gameloop - 1440) % 1440 == 0)
                                        pos = 3;
                                    else if ((gameloop - 1441) % 1440 == 0)
                                        pos = 3;

                                    if (replay.PLAYERCOUNT == 4 && pos == 3) pos = 1;

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
                else if (pydic.ContainsKey("m_unitTagIndex") && (int)pydic["m_unitTagIndex"] == 20 && pydic.ContainsKey("_event") && pydic["_event"].ToString() == "NNet.Replay.Tracker.SUnitOwnerChangeEvent")
                {
                    int gameloop = (int)pydic["_gameloop"];
                    int upkeepid = (int)pydic["m_upkeepPlayerId"];

                    KeyValuePair<int, int> Mid = new KeyValuePair<int, int>(0, 0);
                    if (upkeepid == 13)
                        Mid = new KeyValuePair<int, int>(gameloop, 0);
                    else if (upkeepid == 14)
                        Mid = new KeyValuePair<int, int>(gameloop, 1);

                    if (Mid.Key > 0)
                        replay.MIDDLE.Add(Mid);
                }
                else if (pydic.ContainsKey("m_stats"))
                {
                    int playerid = (int)pydic["m_playerId"];
                    int gameloop = (int)pydic["_gameloop"];
                    if (playerid == 0 || playerid > 6) continue;
                    dsplayer pl = replay.PLAYERS.Where(x => x.POS == playerid).FirstOrDefault();
                    if (pl == null) continue;

                    if (isBrawl_set == false)
                        isBrawl_set = true;

                    PythonDictionary pystats = pydic["m_stats"] as PythonDictionary;
                    M_stats m_stats = new M_stats();
                    //m_stats.FoodMade = (int)pystats["m_scoreValueFoodMade"];
                    m_stats.FoodUsed = (int)pystats["m_scoreValueFoodUsed"];
                    m_stats.MineralsCollectionRate = (int)pystats["m_scoreValueMineralsCollectionRate"];
                    m_stats.MineralsCurrent = (int)pystats["m_scoreValueMineralsCurrent"];
                    m_stats.MineralsFriendlyFireArmy = (int)pystats["m_scoreValueMineralsFriendlyFireArmy"];
                    //m_stats.MineralsFriendlyFireEconomy = (int)pystats["m_scoreValueMineralsFriendlyFireEconomy"];
                    m_stats.MineralsFriendlyFireTechnology = (int)pystats["m_scoreValueMineralsFriendlyFireTechnology"];
                    m_stats.MineralsKilledArmy = (int)pystats["m_scoreValueMineralsKilledArmy"];
                    //m_stats.MineralsKilledEconomy = (int)pystats["m_scoreValueMineralsKilledEconomy"];
                    m_stats.MineralsKilledTechnology = (int)pystats["m_scoreValueMineralsKilledTechnology"];
                    m_stats.MineralsLostArmy = (int)pystats["m_scoreValueMineralsLostArmy"];
                    //m_stats.MineralsLostEconomy = (int)pystats["m_scoreValueMineralsLostEconomy"];
                    //m_stats.MineralsLostTechnology = (int)pystats["m_scoreValueMineralsLostTechnology"];
                    m_stats.MineralsUsedActiveForces = (int)pystats["m_scoreValueMineralsUsedActiveForces"];
                    m_stats.MineralsUsedCurrentArmy = (int)pystats["m_scoreValueMineralsUsedCurrentArmy"];
                    //m_stats.MineralsUsedCurrentEconomy = (int)pystats["m_scoreValueMineralsUsedCurrentEconomy"];
                    m_stats.MineralsUsedCurrentTechnology = (int)pystats["m_scoreValueMineralsUsedCurrentTechnology"];
                    /**
                    m_stats.MineralsUsedInProgressArmy = (int)pystats["m_scoreValueMineralsUsedInProgressArmy"];
                    m_stats.MineralsUsedInProgressEconomy = (int)pystats["m_scoreValueMineralsUsedInProgressEconomy"];
                    m_stats.MineralsUsedInProgressTechnology = (int)pystats["m_scoreValueMineralsUsedInProgressTechnology"];
                    m_stats.VespeneCollectionRate = (int)pystats["m_scoreValueVespeneCollectionRate"];
                    m_stats.VespeneCurrent = (int)pystats["m_scoreValueVespeneCurrent"];
                    m_stats.VespeneFriendlyFireArmy = (int)pystats["m_scoreValueVespeneFriendlyFireArmy"];
                    m_stats.VespeneFriendlyFireEconomy = (int)pystats["m_scoreValueVespeneFriendlyFireEconomy"];
                    m_stats.VespeneFriendlyFireTechnology = (int)pystats["m_scoreValueVespeneFriendlyFireTechnology"];
                    m_stats.VespeneKilledArmy = (int)pystats["m_scoreValueVespeneKilledArmy"];
                    m_stats.VespeneKilledEconomy = (int)pystats["m_scoreValueVespeneKilledEconomy"];
                    m_stats.VespeneKilledTechnology = (int)pystats["m_scoreValueVespeneKilledTechnology"];
                    m_stats.VespeneLostArmy = (int)pystats["m_scoreValueVespeneLostArmy"];
                    m_stats.VespeneLostEconomy = (int)pystats["m_scoreValueVespeneLostEconomy"];
                    m_stats.VespeneLostTechnology = (int)pystats["m_scoreValueVespeneLostTechnology"];
                    m_stats.VespeneUsedActiveForces = (int)pystats["m_scoreValueVespeneUsedActiveForces"];
                    m_stats.VespeneUsedCurrentArmy = (int)pystats["m_scoreValueVespeneUsedCurrentArmy"];
                    m_stats.VespeneUsedCurrentEconomy = (int)pystats["m_scoreValueVespeneUsedCurrentEconomy"];
                    m_stats.VespeneUsedCurrentTechnology = (int)pystats["m_scoreValueVespeneUsedCurrentTechnology"];
                    m_stats.VespeneUsedInProgressArmy = (int)pystats["m_scoreValueVespeneUsedInProgressArmy"];
                    m_stats.VespeneUsedInProgressEconomy = (int)pystats["m_scoreValueVespeneUsedInProgressEconomy"];
                    m_stats.VespeneUsedInProgressTechnology = (int)pystats["m_scoreValueVespeneUsedInProgressTechnology"];
                    m_stats.WorkersActiveCount = (int)pystats["m_scoreValueWorkersActiveCount"];
                    **/

                    if (pl.STATS.Count() > 1)
                    {
                        int lastfood = pl.STATS.ElementAt(pl.STATS.Count() - 2).Value.FoodUsed;
                        if (pl.STATS.Last().Value.FoodUsed > lastfood)
                            if (gameloop - 160 - pl.LastSpawn > 470)
                            {
                                pl.LastSpawn = gameloop - 160;
                                //pl.ARMY += pl.STATS.Last().Value.MineralsUsedActiveForces + pl.STATS.Last().Value.MineralsUsedCurrentTechnology;
                                pl.ARMY += m_stats.MineralsUsedActiveForces / 2 + m_stats.MineralsUsedCurrentTechnology;

                                // some units from last spawn still fighting
                                if (lastfood > 0)
                                {
                                    //double wr = lastfood / pl.STATS.Last().Value.FoodUsed;

                                }
                            }
                    }

                    pl.STATS[gameloop] = m_stats;
                    
                    replay.DURATION = gameloop;
                    pl.PDURATION = gameloop;

                    int gas = 0;
                    int income = pl.STATS[gameloop].MineralsCollectionRate;
                    pl.INCOME += (double)income / 9.15;

                    //KeyValuePair<int, int> lastMid = GetMiddle(replay);
                    income = MiddleIncome(replay.MIDDLE, gameloop, pl.TEAM, income);

                    if (income < 470) gas = 0; // base income
                    else if (income < 500) gas = 1;
                    else if (income < 530 && gameloop > 2240) gas = 2;
                    else if (income < 560 && gameloop > 4480) gas = 3;
                    else if (income < 600 && gameloop > 13440) gas = 4;
                    if (gas > pl.GAS)
                        pl.GAS = gas;


                    int fixloop = pl.LastSpawn;

                    if (!pl.SPAWNS.ContainsKey(fixloop))
                        pl.SPAWNS.Add(fixloop, new Dictionary<string, int>());
    
                    pl.SPAWNS[fixloop]["Gas"] = pl.GAS;
                    if (pl.TEAM == 0)
                        pl.SPAWNS[fixloop]["Mid"] = GetMiddle(replay, true).Key;
                    else
                        pl.SPAWNS[fixloop]["Mid"] = GetMiddle(replay, true).Value;

                    if (pl.STATS.Count() > 0)
                    {
                        pl.SPAWNS[fixloop]["Upgrades"] = pl.STATS.ElementAt(pl.STATS.Count() - 1).Value.MineralsUsedCurrentTechnology;
                        //pl.ARMY += pl.STATS.ElementAt(pl.STATS.Count() - 1).Value.MineralsUsedActiveForces;
                    }

                }
                else if (isBrawl_set == false && pydic.ContainsKey("_gameloop") && (int)pydic["_gameloop"] == 0 && pydic.ContainsKey("m_upgradeTypeName"))
                {
                    if (pydic["m_upgradeTypeName"].ToString().StartsWith("Mutation"))
                        Mutation.Add(pydic["m_upgradeTypeName"].ToString());
                }
            }

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

            SetUnits(replay);

            // fail safe
            FixPos(replay);
            FixWinner(replay);
            

            return replay;
        }

        public static int MiddleIncome(List<KeyValuePair<int, int>> mid, int gameloop, int team, int income)
        {
            bool isMid = false;
            KeyValuePair<int, int> lastMid = mid.LastOrDefault();

            // no mid secured yet
            if (lastMid.Key == 0) isMid = false;

            // solid mid
            else if (gameloop - lastMid.Key > 160)
                if (lastMid.Value == team)
                    isMid = true;
                else
                    isMid = false;

            // split mid
            else
            {
                int midPlTeam = 0;
                int midOppTeam = 0;

                KeyValuePair<int, int> bakMid = mid.FirstOrDefault();
                foreach (var ent in mid)
                {
                    if (gameloop - ent.Key <= 160)
                    {
                        int lastloop = bakMid.Key;
                        if (bakMid.Key < gameloop - 160)
                            lastloop = gameloop - 160;


                        if (bakMid.Value == team)
                            midPlTeam += ent.Key - lastloop;
                        else
                            midOppTeam += ent.Key - lastloop;
                    }
                    bakMid = ent;
                }
                if (mid.LastOrDefault().Value == team)
                    midPlTeam += gameloop - mid.LastOrDefault().Key;
                else
                    midOppTeam += gameloop - mid.LastOrDefault().Key;

                double dmidPlTeam = (double)midPlTeam / 160;
                double dmidOppTeam = (double)midOppTeam / 160;

                double incChange = 60 * dmidPlTeam;
                return income -= (int)incChange;
            }


            if (isMid == true)
                return income -= 60;
            else
                return income;

        }

        public static KeyValuePair<int, int> GetMiddle(dsreplay replay, bool current = false)
        {
            double team1 = 0;
            double team2 = 0;
            KeyValuePair<int, int> lastmid = new KeyValuePair<int, int>();
            List<KeyValuePair<int, int>> mymid = new List<KeyValuePair<int, int>>(replay.MIDDLE);
            if (mymid.Count() >= 1)
            {
                int finalmid = mymid.ElementAt(mymid.Count() - 1).Value;
                lastmid = new KeyValuePair<int, int>(0, mymid[0].Value);
                mymid.RemoveAt(0);
                mymid.Add(new KeyValuePair<int, int>(replay.DURATION, finalmid));
            }

            foreach (var ent in mymid)
            {
                if (lastmid.Value == 0)
                    team1 += ent.Key - lastmid.Key;
                else
                    team2 += ent.Key - lastmid.Key;
                
                lastmid = ent;
            }

            KeyValuePair<int, int> currentMid = new KeyValuePair<int, int>();

            int mSec = 0;
            int mTeam = 0;

            if (mymid.Count() > 2)
            {
                mSec = mymid.ElementAt(mymid.Count() - 2).Key - mymid.ElementAt(mymid.Count() - 3).Key;
                mTeam = mymid.ElementAt(mymid.Count() - 2).Value;
                currentMid = new KeyValuePair<int, int>(mSec, mTeam);
            }
            if (current == true) { 
                //double midt1 = Math.Round(team1 * 100 / (double)replay.DURATION, 2);
                //double midt2 = Math.Round(team2 * 100 / (double)replay.DURATION, 2);
                return new KeyValuePair<int, int>((int)team1, (int)team2);
            }
            return currentMid;
        }

        public static void SetUnits(dsreplay rep)
        {
            KeyValuePair<int, int> Mid = GetMiddle(rep, true);

            foreach (dsplayer pl in rep.PLAYERS)
            {
                foreach (int gl in pl.SPAWNS.Keys)
                {
                    Dictionary<string, int> units = pl.SPAWNS[gl];

                    if (gl >= 20640 && gl <= 22080)
                        SetBp("MIN15", MIN15, gl, pl, units);
                    else if (gl >= 13440 && gl <= 14880)
                        SetBp("MIN10", MIN10, gl, pl, units);
                    else if (gl >= 6240 && gl <= 7680)
                        SetBp("MIN5", MIN5, gl, pl, units);
                }
                if (pl.SPAWNS.Count() > 0)
                {
                    pl.UNITS["ALL"] = pl.SPAWNS.ElementAt(pl.SPAWNS.Count() - 1).Value;
                    if (pl.UNITS["ALL"].Count() == 3 && pl.SPAWNS.Count() > 1)
                        pl.UNITS["ALL"] = pl.SPAWNS.ElementAt(pl.SPAWNS.Count() - 2).Value;
                }

                if (!pl.UNITS.ContainsKey("ALL"))
                    pl.UNITS.Add("ALL", new Dictionary<string, int>());

                if (pl.TEAM == 0)
                    pl.UNITS["ALL"]["Mid"] = Mid.Key;
                else
                    pl.UNITS["ALL"]["Mid"] = Mid.Value;
                

                if (pl.STATS.Count() > 0)
                    pl.KILLSUM = pl.STATS.ElementAt(pl.STATS.Count() - 1).Value.MineralsKilledArmy;

                pl.INCOME = Math.Round(pl.INCOME, 2);
            }

        }

        static void SetBp(string breakpoint, int min, int gameloop, dsplayer pl, Dictionary<string, int> units)
        {
            if (units.ContainsKey("Mid"))
            {
                int temp = min - gameloop;
                units["Mid"] += temp;
                if (units["Mid"] < 0)
                    units["Mid"] = 0;
            }
            pl.UNITS[breakpoint] = units;
        }

        public static void FixWinner(dsreplay replay)
        {
            foreach (dsplayer pl in replay.PLAYERS)
            {
                if (pl.RESULT == 1)
                {
                    replay.WINNER = pl.TEAM;
                    break;
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
            public bool T1 { get; set; } = false;
            public bool T2 { get; set; } = false;
            public int MidT1 { get; set; } = 0;
            public int MidT2 { get; set; } = 0;
            public Dictionary<int, int> FS { get; set; } = new Dictionary<int, int>();
        }


    }
}

