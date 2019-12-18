using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.Extensions.Configuration;
using sc2dsstats.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using paxgame3.Client.Models;
using paxgame3.Client.Data;

namespace sc2dsstats.Data
{
    public class StartUp
    {
        private IConfiguration _config;
        public UserConfig Conf { get; set; } = new UserConfig();
        public bool FIRSTRUN { get; set; } = false;
        public bool SAMPLEDATA { get; set; } = false;
        public static string VERSION { get; } = "1.4.4";
        private bool INIT = false;
        public string FirstRunInfo { get; set; } = "";
        public string UpdateInfo { get; set; } = VERSION;
        public bool Resized { get; set; } = false;
        public List<string> StatPlayers { get; set; } = new List<string>();
        public List<string> StatFolders { get; set; } = new List<string>();

        // paxgame
        public static bool DEBUG = false;
        public static string DebugInfo = "";

        public double PlayerID = 1000;
        public double GameID = 1000;
        public object GameIDObject = new object();
        public const float Battlefieldmodifier = 4;
        public const int Income = 500;
        private bool isInit = false;
        public Dictionary<string, string> Auth { get; set; } = new Dictionary<string, string>();
        public Dictionary<double, Player> Players { get; set; } = new Dictionary<double, Player>();
        public Dictionary<double, GameHistory> Games { get; set; } = new Dictionary<double, GameHistory>();
        static Regex regexItem = new Regex("^[a-zA-Z0-9_]*$");

        public ConcurrentBag<FinalStat> Stats { get; set; } = new ConcurrentBag<FinalStat>();

        public StartUp(IConfiguration config)
        {
            _config = config;

            UpgradePool.Init();
            AbilityPool.Init();
            UnitPool.Init();
            AbilityPool.PoolInit();
            InitGame();
            //_gamedb = db;

        }

        public void Save()
        {
            Dictionary<string, UserConfig> temp = new Dictionary<string, UserConfig>();
            temp.Add("Config", Conf);

            var option = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(temp, option);
            File.WriteAllText(Program.myConfig, json);
        }

        public bool Reset()
        {
            if (File.Exists(Program.myJson_file))
            {
                string bak = Program.myJson_file + "_bak";
                int ii = 0;
                while (File.Exists(bak))
                {
                    bak = Program.myJson_file + "_" + ii + "_bak";
                    ii++;
                }
                try
                {
                    File.Move(Program.myJson_file, bak);
                }
                catch
                {
                    return false;
                }
                finally
                {
                    File.Delete(Program.myJson_file);
                    File.Create(Program.myJson_file).Dispose();
                }
                Conf.FullSend = true;
                Save();
            }
            return true;
        }

        public async Task Init()
        {
            if (INIT == true) return;
            INIT = true;
            if (!File.Exists(Program.myConfig))
            {
                Helper(Conf);
                string exedir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                Conf.ExeDir = exedir;
                Conf.Version = VERSION;
                Save();
                FirstRun();
            }
            else
            {
                var bab = _config.GetChildren();

                await Task.Run(() =>
                {
                    _config.Bind("Config", Conf);
                    string exedir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    Conf.ExeDir = exedir;
                    Conf.Version = VERSION;
                    Program.workdir = Conf.WorkDir;
                    Program.myJson_file = Conf.WorkDir + "/data.json";
                    if (!File.Exists(Program.myJson_file))
                        File.Create(Program.myJson_file).Dispose();
                    Program.myScan_log = Conf.WorkDir + "/log.txt";
                });


                if (Conf.NewVersion1_4_1 == true)
                {
                    FirstRunInfo = "<h3>Patchnotes</h3><br />";
                    FirstRunInfo += "<p>" +
                        "- Version 1.4.1 New feature: A-Move simulator" + "<br />" +
                        "<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/M6noTYbdSp4\" frameborder=\"0\" allow=\"autoplay; encrypted-media\" allowfullscreen></iframe>";
                    
                    Conf.NewVersion1_4_1 = false;
                    Save();
                }

            }
            StatPlayers = new List<string>(Conf.Players);
            StatFolders = new List<string>(Conf.Replays);
            await Resize();
        }

        public async Task<bool> CheckForUpdate()
        {
            if (Resized == false) return false;
            UpdateCheckResult result = new UpdateCheckResult();
            try
            {
                result = await Electron.AutoUpdater.CheckForUpdatesAsync();
            }
            catch { }
            Console.WriteLine(result.UpdateInfo.Version);
            Console.WriteLine(result.UpdateInfo.ReleaseDate);

            if (VERSION == result.UpdateInfo.Version)
                return false;
            else
            {
                UpdateInfo = String.Format("{0} ({1}): {2}", result.UpdateInfo.Version, result.UpdateInfo.ReleaseDate, result.UpdateInfo.ReleaseNotes.FirstOrDefault());
                return true;
            }
        }

        public async Task QuitAndInstall()
        {
            UpdateCheckResult result = await Electron.AutoUpdater.CheckForUpdatesAndNotifyAsync();

            Electron.AutoUpdater.OnUpdateDownloaded += AutoUpdater_OnUpdateDownloaded;
        }

        private void AutoUpdater_OnUpdateDownloaded(UpdateInfo obj)
        {
            try
            {
                Electron.AutoUpdater.QuitAndInstall(false, true);
            }
            catch
            {
                Electron.AutoUpdater.OnUpdateDownloaded -= AutoUpdater_OnUpdateDownloaded;
            }
        }

        async Task Resize()
        {
            BrowserWindow browserWindow = null;
            int failsafe = 16;
            await Task.Run(() =>
            {
                do
                {
                    Thread.Sleep(250);
                    browserWindow = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                    if (browserWindow != null)
                    {
                        try
                        {
                            browserWindow.SetPosition(0, 0);
                            browserWindow.SetSize(1920, 1024);
                            browserWindow.SetMenuBarVisibility(false);
                        }
                        catch
                        {
                        }
                    }
                    failsafe--;
                } while (browserWindow == null && failsafe > 0);
                if (failsafe > 0)
                    Resized = true;
            });

        }

        public void FirstRun()
        {
            FIRSTRUN = true;
            if (!File.Exists(Program.myJson_file))
                File.Create(Program.myJson_file).Dispose();
        }

        public UserConfig Helper(UserConfig conf)
        {
            if (File.Exists(Program.myConfig))
            {
                Console.WriteLine(_config["Players"]);
            }

            var doc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string sc2_dir = doc + "/StarCraft II";
            int Count = 0;
            HashSet<string> Players = new HashSet<string>();
            HashSet<string> Folders = new HashSet<string>();

            if (Directory.Exists(sc2_dir))
            {

                List<string> files = new List<string>();
                foreach (var file in Directory.GetFiles(sc2_dir))
                {
                    string target = "";

                    try
                    {
                        if (Path.GetExtension(file)?.Equals(".lnk", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            target = GetShortcutTarget(file);
                        }
                    }
                    finally
                    {
                    }

                    if (target.Length > 0)
                    {
                        string rep_dir = target + @"\Replays\Multiplayer";
                        string link = Path.GetFileName(file);
                        Match m = Regex.Match(link, @"(.*)_\d+\@\d+\.lnk$", RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            Players.Add(m.Groups[1].Value.ToString());
                        }

                        if (Directory.Exists(rep_dir))
                        {
                            Folders.Add(rep_dir);
                            files.AddRange(Directory.GetFiles(rep_dir, "*.SC2Replay", SearchOption.AllDirectories).ToList());
                        }
                    }
                }
                Console.WriteLine("SC2 Players added:");
                foreach (var ent in Players.OrderBy(x => x))
                {
                    Console.WriteLine(ent);
                }
                Console.WriteLine();
                Console.WriteLine("Replay folders added:");
                foreach (var ent in Folders.OrderBy(x => x))
                {
                    Console.WriteLine(ent);
                }
                Count = files.Where(x => Path.GetFileName(x).StartsWith("Direct Strike")).Count();
                Console.WriteLine();
                Console.WriteLine("Direct Strike replays found: " + Count);
            }

            conf.Players = Players.OrderBy(x => x).ToList();
            conf.Replays = Folders.OrderBy(x => x).ToList();
            conf.WorkDir = Program.workdir;
            return conf;
        }

        private string GetShortcutTarget(string file)
        {
            try
            {
                if (System.IO.Path.GetExtension(file).ToLower() != ".lnk")
                {
                    throw new Exception("Supplied file must be a .LNK file");
                }

                FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
                using (System.IO.BinaryReader fileReader = new BinaryReader(fileStream))
                {
                    fileStream.Seek(0x14, SeekOrigin.Begin);     // Seek to flags
                    uint flags = fileReader.ReadUInt32();        // Read flags
                    if ((flags & 1) == 1)
                    {                      // Bit 1 set means we have to
                                           // skip the shell item ID list
                        fileStream.Seek(0x4c, SeekOrigin.Begin); // Seek to the end of the header
                        uint offset = fileReader.ReadUInt16();   // Read the length of the Shell item ID list
                        fileStream.Seek(offset, SeekOrigin.Current); // Seek past it (to the file locator info)
                    }

                    long fileInfoStartsAt = fileStream.Position; // Store the offset where the file info
                                                                 // structure begins
                    uint totalStructLength = fileReader.ReadUInt32(); // read the length of the whole struct
                    fileStream.Seek(0xc, SeekOrigin.Current); // seek to offset to base pathname
                    uint fileOffset = fileReader.ReadUInt32(); // read offset to base pathname
                                                               // the offset is from the beginning of the file info struct (fileInfoStartsAt)
                    fileStream.Seek((fileInfoStartsAt + fileOffset), SeekOrigin.Begin); // Seek to beginning of
                                                                                        // base pathname (target)
                    long pathLength = (totalStructLength + fileInfoStartsAt) - fileStream.Position - 2; // read
                                                                                                        // the base pathname. I don't need the 2 terminating nulls.
                    char[] linkTarget = fileReader.ReadChars((int)pathLength); // should be unicode safe
                    var link = new string(linkTarget);

                    int begin = link.IndexOf("\0\0");
                    if (begin > -1)
                    {
                        int end = link.IndexOf("\\\\", begin + 2) + 2;
                        end = link.IndexOf('\0', end) + 1;

                        string firstPart = link.Substring(0, begin);
                        string secondPart = link.Substring(end);

                        return firstPart + secondPart;
                    }
                    else
                    {
                        return link;
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        // paxgame
        public async Task InitGame()
        {
            // TODO get players/games form server/db/localstorage
            isInit = true;
            if (isInit == true) return;

            foreach (var file in Directory.EnumerateFiles("/data/paxplayers"))
            {
                if (File.Exists(file))
                {
                    Player pl = JsonSerializer.Deserialize<Player>(File.ReadAllText(file));
                    if (pl != null)
                    {
                        Players[pl.ID] = pl;
                        if (pl.ID > PlayerID)
                            PlayerID = pl.ID;
                    }
                }
            }

            foreach (var file in Directory.EnumerateFiles("/data/paxgames"))
            {
                if (File.Exists(file))
                {
                    GameHistory game = JsonSerializer.Deserialize<GameHistory>(File.ReadAllText(file));
                    if (game != null)
                    {
                        Games[game.ID] = game;
                        if (game.ID > GameID)
                            GameID = game.ID;
                    }
                }
            }

            if (File.Exists("/data/paxstats/stats.json"))
            {
                foreach (var line in File.ReadAllLines("/data/paxstats/stats.json"))
                {
                    FinalStat stat = JsonSerializer.Deserialize<FinalStat>(line);
                    if (stat != null)
                        Stats.Add(stat);

                }
            }

            isInit = true;
        }

        public double GetGameID()
        {
            lock (GameIDObject)
            {
                return ++GameID;
            }
        }

        public double GetPlayerID()
        {
            lock (GameIDObject)
            {
                return ++PlayerID;
            }
        }

        public async Task SaveGame(GameHistory game, Player player)
        {
            await SavePlayer(player);

            var json = JsonSerializer.Serialize(game);
            //File.WriteAllText("/data/paxgames/" + game.ID + ".json", json);

            Games[game.ID] = game;
        }

        public async Task SavePlayer(Player player)
        {
            if (player.ID == 0)
                player.ID = ++PlayerID;

            var json = JsonSerializer.Serialize(player);
            //File.WriteAllText("/data/paxplayers/" + player.ID + ".json", json);
            Players[player.ID] = player;
        }

        public async Task<Player> PlayerInit(Player _player)
        {
            if (_player.Name.StartsWith("Anonymous") && _player.Name.Contains('#'))
            {
                int i = 1;
                while (Players.Values.SingleOrDefault(x => x.Name == _player.Name) != null)
                {
                    _player.Name = "Anonymous#" + i.ToString();
                    i++;
                }
            }

            GameHistory _game = new GameHistory();

            _player.Tier = 1;
            _player.Units.Clear();
            _player.Upgrades.Clear();
            _player.AbilityUpgrades.Clear();
            _player.AbilitiesDeactivated.Clear();
            _player.GameID = 0;
            _player.Pos = 1;
            _player.inGame = true;
            _player.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _player.Race));

            _game = new GameHistory();
            _game.Mode = _player.Mode;
            _game.ID = GetGameID();
            _game.UnitID = 3000;
            _game.Players.Add(_player);

            //_player.MineralsCurrent = Income;
            _player.MineralsCurrent = 5000;

            // TODO
            Player opp = new Player();
            if (_player.Mode.Mode.StartsWith("Bot"))
            {

                opp.ID = ++PlayerID;
                opp.Name = _player.Mode.Mode;
                opp.Pos = 4;
                opp.GameID = _game.ID;
                if (_player.Mode.Mode.EndsWith("1"))
                    opp.Race = UnitRace.Zerg;
                else if (_player.Mode.Mode.EndsWith("2"))
                    opp.Race = UnitRace.Terran;
                else if (_player.Mode.Mode.EndsWith("3"))
                {
                    Random rnd = new Random();
                    int r = rnd.Next(0, 2);
                    opp.Race = (UnitRace)r;
                }
                opp.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == opp.Race));
                opp.MineralsCurrent = Income;
                opp.Game = _game;
                opp.GameID = _game.ID;
                Players[opp.ID] = opp;

                _game.Players.Add(opp);
            }
            _player.GameID = _game.ID;
            _player.Game = _game;
            return opp;
        }

        public async Task<GameHistory> GetGame(double id)
        {
            if (Games.ContainsKey(id))
                return Games[id];
            else
                return null;
        }

        public async Task<(GameHistory, Player)> ResumeGame(Player pl)
        {
            if (pl.inGame == false) return (null, null);
            Player opp = new Player();
            GameHistory game = await GetGame(pl.GameID);
            if (game != null)
            {
                Player rpl = game.Players.Where(x => x.Pos == pl.Pos).FirstOrDefault();

                // TODO
                opp = game.Players.Where(x => x.Pos == 4).FirstOrDefault();
                Players[opp.ID] = opp;

                game.Players.Remove(rpl);
                game.Players.Add(pl);

                foreach (Player mypl in game.Players)
                {
                    int posmod = 1;
                    if (game.Players.Count() == 2 && mypl.Pos > 3)
                        if (mypl.Pos > 3)
                            posmod += 2;
                    await game.Spawns.Last()[mypl.Pos - posmod].SetBuild(mypl);
                    if (mypl.Pos != pl.Pos)
                        opp = mypl;

                }
            }




            return (game, opp);
        }


        public async Task<(GameHistory, Player)> ResumeGame_deprecated(Player pl)
        {
            if (pl.inGame == false) return (null, null);
            Player opp = new Player();
            GameHistory game = await GetGame(pl.GameID);
            if (game != null)
            {
                await Task.Run(() =>
                {
                    Player rpl = game.Players.Where(x => x.Pos == pl.Pos).FirstOrDefault();

                    // TODO
                    opp = game.Players.Where(x => x.Pos == 4).FirstOrDefault();
                    Players[opp.ID] = opp;

                    game.Players.Remove(rpl);
                    game.Players.Add(pl);

                    /*
                    foreach (Player gpl in game.Players)
                    {
                        gpl.inGame = true;
                        gpl.Units.Clear();
                        foreach (var unit in game.Spawns.Where(x => x.PlayerPos == gpl.Pos).OrderBy(o => o.Spawn).Last().Units)
                        {
                            if (unit.SerPos == null)
                                continue;
                            Unit myunit = UnitPool.Units.Where(x => x.Name == unit.Name).FirstOrDefault().DeepCopy();
                            myunit.ID = UnitID.GetID(game.ID);
                            myunit.Kills = unit.Kills;
                            myunit.DamageDone = unit.DamageDone;
                            myunit.Status = UnitStatuses.Spawned;
                            myunit.BuildPos = new System.Numerics.Vector2(unit.SerPos.x, unit.SerPos.y);
                            myunit.Pos = myunit.BuildPos;
                            myunit.RealPos = myunit.BuildPos;
                            myunit.SerPos = new Vector2Ser();
                            myunit.SerPos.x = unit.SerPos.x;
                            myunit.SerPos.y = unit.SerPos.y;
                            myunit.Owner = gpl.Pos;
                            myunit.Ownerplayer = gpl;
                            gpl.Units.Add(myunit);
                            
                        }
                        gpl.Units.AddRange(UnitPool.Units.Where(x => x.Race == gpl.Race));
                        Players[gpl.ID] = gpl;
                    }*/
                });
            }
            return (game, opp);
        }

        public void EndGame(GameHistory game)
        {
            game.Spawns.Clear();
        }

        public void FinalStats(FinalStat stat)
        {
            lock (Stats)
            {
                var json = JsonSerializer.Serialize(stat);
                //File.AppendAllText("/data/paxstats/stats.json", json + Environment.NewLine);
                Stats.Add(stat);
            }
        }

        public static bool CheckName(string name)
        {
            if (name == null) return false;
            if (name.Contains(".."))
                return false;
            if (!name.Any(ch => Char.IsLetterOrDigit(ch) || ch == '_'))
                return false;
            if (name.Length > 30)
                return false;

            if (regexItem.IsMatch(name))
                return true;
            else
                return false;
        }

        public (Player, string) GetPlayer(string name, string authname)
        {
            string msg = "";
            Player player = new Player();
            player.Race = UnitRace.Terran;
            Player pl = Players.Values.SingleOrDefault(x => x.Name == name);

            if (pl != null && name != "Anonymous#0")
            {
                if (authname != null && authname.Length > 0)
                {
                    if (pl.AuthName == authname)
                        player = pl;
                    else
                    {
                        return (null, "Playername already taken. Please choose an other name.");
                    }
                }
                else
                {
                    if (pl.AuthName != null && pl.AuthName.Length > 0)
                    {
                        return (null, "Playername already taken. Please choose an other name.");
                    }
                    else
                    {
                        player = pl;
                    }
                }
            }
            else
            {
                if (authname != null && authname.Length > 0)
                {
                    Auth[name] = authname;
                    player.Name = name;
                    player.AuthName = name;
                }
                else
                {
                    player.Name = name;
                }
            }

            return (player, msg);
        }

        public static string GetName(string name)
        {
            return name;
        }

        public static string GetName(UnitRace name)
        {
            return name.ToString();
        }
    }
}
