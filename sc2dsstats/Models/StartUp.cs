using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ElectronNET.API;
using System.Threading;

namespace sc2dsstats.Models
{
    public class StartUp
    {
        private IConfiguration _config;
        public UserConfig Conf { get; set; } = new UserConfig();
        public bool FIRSTRUN { get; set; } = false;
        public bool SAMPLEDATA { get; set; } = false;
        public static string VERSION { get; } = "v1.1.7";
        private bool INIT = false;
        public string FirstRunInfo { get; set; } = "";

        public StartUp(IConfiguration config)
        {
            _config = config;
        }

        public void Save()
        {
            Dictionary<string, UserConfig> temp = new Dictionary<string, UserConfig>();
            temp.Add("Config", Conf);
            var json = JsonConvert.SerializeObject(temp, Formatting.Indented);
            File.WriteAllText(Program.myConfig, json);
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

                await Task.Run(() => { 
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

                if (Conf.ForceRescanV1_7 == true)
                {
                    FirstRunInfo = "Version 1.7 has a more accurate way to calculate Army value, middle income and other details. We have to rescan all replays to avoid duplicates.";
                    if (File.Exists(Program.myJson_file))
                    {
                        try
                        {
                            File.Move(Program.myJson_file, Program.myJson_file + "_bakV1_7");
                        }
                        catch { }
                    }

                    try
                    {
                        File.Create(Program.myJson_file).Dispose();
                    }
                    catch { }
                    Conf.ForceRescanV1_7 = false;
                    Save();
                }
            }

            await Resize();
            //AppUpdate appup = new AppUpdate();
            //appup.Update();
        }

        async Task Resize()
        {
            BrowserWindow browserWindow = null;
            int failsafe = 16;
            await Task.Run(() => {
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
    }
}
