using IronPython.Runtime;
using Newtonsoft.Json;
using s2decode.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using dsweb_electron6;

namespace s2decode
{
    public static class s2parse
    {
        public static Regex rx_subname = new Regex(@"<sp\/>(.*)$", RegexOptions.Singleline);
        public static ConcurrentDictionary<string, List<s2player>> s2players { get; set; } = new ConcurrentDictionary<string, List<s2player>>();

        public static void GetInit(string replay_file, dynamic init_dec)
        {
            string id = Path.GetFileNameWithoutExtension(replay_file);
            string reppath = Path.GetDirectoryName(replay_file);
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(reppath);
            MD5 md5 = new MD5CryptoServiceProvider();
            string reppath_md5 = System.BitConverter.ToString(md5.ComputeHash(plainTextBytes));
            string repid = reppath_md5 + "/" + id;
            s2players.TryAdd(repid, new List<s2player>());

            Dictionary<int, string> info = new Dictionary<int, string>();


            /**
            var json = JsonConvert.SerializeObject(init_dec, Formatting.Indented);
            File.WriteAllText(Program.workdir + "/" + id + "_init.json", json);
            **/

            int maxplayer = init_dec["m_syncLobbyState"]["m_lobbyState"]["m_maxUsers"];
            string mod = init_dec["m_syncLobbyState"]["m_gameDescription"]["m_modFileSyncChecksum"].ToString();
            string map = init_dec["m_syncLobbyState"]["m_gameDescription"]["m_mapFileSyncChecksum"].ToString();


            Console.WriteLine("ID: {0}, Max: {1}, map: {2}, mod: {3}", id, maxplayer, map, mod);
            int i = 0;
            foreach (var ent in init_dec["m_syncLobbyState"]["m_userInitialData"])
            {
                string name = ent["m_name"].ToString();
                if (name.Length == 0) continue;
                info[i] = String.Format("{0} => {1}", i, name);
                s2players[repid].Add(new s2player(name));
                s2players[repid][i].POS = i + 1;
                i++;

            }
            i = 0;
            foreach (var ent in init_dec["m_syncLobbyState"]["m_lobbyState"]["m_slots"])
            {

                int team = (int)ent["m_teamId"];
                int userid = -1;
                try
                {
                    userid = (int)ent["m_userId"];
                }
                catch
                {
                    continue;
                }
                int slotid = (int)ent["m_workingSetSlotId"];
                int race = 0;
                try
                {
                    race = (int)ent["m_racePref"]["m_race"];
                }
                catch
                {

                }
                info[i] += String.Format(", Team: {0}, UserID: {1}, SlotID: {2}, Race: {3}", team, userid, slotid, race);
                /**
                s2players[repid][i]._init.Team = team;
                s2players[repid][i]._init.UserID = userid + 1;
                s2players[repid][i]._init.SlotID = slotid + 1;
                s2players[repid][i]._init.Race = race;
                **/
                i++;
            }

            foreach (var ent in info.Keys)
            {
                Program.Log(info[ent]);
            }
        }

        public static s2replay GetDetails(string replay_file, dynamic details_dec)
        {
            string id = Path.GetFileNameWithoutExtension(replay_file);
            string reppath = Path.GetDirectoryName(replay_file);
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(reppath);
            MD5 md5 = new MD5CryptoServiceProvider();
            string reppath_md5 = System.BitConverter.ToString(md5.ComputeHash(plainTextBytes));
            string repid = reppath_md5 + "/" + id;

            s2replay replay = new s2replay();

            replay.REPLAY = repid;
            Program.Log("Replay id: " + repid);
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

                if (name.Length == 0) continue;
                Match m2 = rx_subname.Match(name);
                if (m2.Success) name = m2.Groups[1].Value;
                Program.Log("Replay playername: " + name);

                s2player pl = new s2player();
                if (s2players.ContainsKey(repid))
                {
                    pl = s2players[repid][failsafe_pos - 1];
                }

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

            var json = JsonConvert.SerializeObject(details_dec, Formatting.Indented);
            File.WriteAllText(Program.workdir + "/bab/analyzes/" + id + "_details.json", json);
            return replay;
        }

        public static void GetTrackerevents(string replay_file, dynamic trackerevents_dec)
        {
            string id = Path.GetFileNameWithoutExtension(replay_file);

            /**
            List<string> Upgrades = new List<string>();
            foreach (PythonDictionary pydic in trackerevents_dec)
            {
                if (pydic.ContainsKey("m_upgradeTypeName"))
                {
                    Upgrades.Add(pydic["m_upgradeTypeName"].ToString());
                }
            }
            File.WriteAllLines(Program.workdir + "/bab/analyzes/" + id + "_tracker_upgrades.json", Upgrades.Distinct().OrderBy(o => o));
            **/

            var json = JsonConvert.SerializeObject(trackerevents_dec, Formatting.Indented);
            File.WriteAllText(Program.workdir + "/bab/analyzes/" + id + "_tracker.json", json);
        }
    }
}
