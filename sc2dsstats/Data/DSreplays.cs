using pax.s2decode.Models;
using sc2dsstats_rc2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace sc2dsstats.Data
{
    public class DSreplays
    {
        public List<dsreplay> Replays = new List<dsreplay>();
        public Dictionary<string, int> Skip = new Dictionary<string, int>();
        public HashSet<string> Todo = new HashSet<string>();
        public Dictionary<string, string> ReplayFolder { get; set; } = new Dictionary<string, string>();

        public int ID { get; set; } = 0;
        public bool INIT = false;
        public bool INITdone = false;

        public StartUp _startUp;
        IDSdata_cache _dsdata;

        public DSreplays(StartUp startUp, IDSdata_cache dsdata)
        {
            _startUp = startUp;
            _dsdata = dsdata;
            Init();
        }

        public async Task Init()
        {
            if (INIT == true) return;
            INIT = true;
            await LoadData();
            await LoadSkip();

            foreach (var ent in _startUp.Conf.Replays)
            {
                string reppath = ent;
                if (reppath.EndsWith("/") || reppath.EndsWith("\\"))
                    reppath.Remove(reppath.Length - 1);
                var plainTextBytes = Encoding.UTF8.GetBytes(reppath);
                MD5 md5 = new MD5CryptoServiceProvider();
                string reppath_md5 = BitConverter.ToString(md5.ComputeHash(plainTextBytes));
                ReplayFolder[reppath] = reppath_md5;
            }
            INITdone = true;
        }

        public async Task LoadData(bool DoUpload = false)
        {
            if (File.Exists(Program.myJson_file))
            {
                if (_startUp.SAMPLEDATA == true)
                {
                    _startUp.SAMPLEDATA = false;
                    try
                    {
                        _startUp.Conf.Players.Remove("player");
                    }
                    catch { }
                }
                int maxid = 0;
                await Task.Run(() =>
                {
                    Replays.Clear();

                    foreach (string fileContents in File.ReadLines(Program.myJson_file, Encoding.UTF8))
                    {
                        dsreplay rep = null;
                        try
                        {
                            rep = JsonSerializer.Deserialize<dsreplay>(fileContents);
                        }
                        catch { }
                        if (rep != null)
                        {
                            rep.Init();
                            //foreach (var pl in rep.PLAYERS)
                            //{
                            //    if (_startUp.Conf.Players.Contains(pl.NAME))
                            //        pl.NAME = "player";
                            //}
                            Replays.Add(rep);
                            if (rep.ID > maxid) maxid = rep.ID;
                        }
                    }
                });
                ID = maxid;
                await _dsdata.Init(Replays);
                await NewReplays();

                if (DoUpload == true)
                {
                    if (_startUp.Conf.Uploadcredential == true && _startUp.Conf.Autoupload_v1_1_10 == true)
                    {
                        await Task.Run(() =>
                        {
                            try
                            {
                                DSrest.AutoUpload(_startUp, this);
                            }
                            catch
                            {

                            }
                        });
                    }
                }
            }
        }

        public void LoadSampleData()
        {
            string samplejson = _startUp.Conf.ExeDir + "/Json/sample.json";
            if (File.Exists(samplejson))
            {
                lock (Replays)
                {
                    Replays.Clear();
                    foreach (string fileContents in File.ReadLines(samplejson))
                    {
                        dsreplay rep = null;
                        try
                        {
                            rep = JsonSerializer.Deserialize<dsreplay>(fileContents);
                            if (rep != null)
                            {
                                rep.Init();
                                Replays.Add(rep);
                            }
                        }
                        catch { }
                    }
                }
                _startUp.Conf.Players.Add("player");
                _dsdata.Init(Replays);
            }
        }

        public async Task LoadSkip()
        {
            await Task.Run(() =>
            {
                if (File.Exists(Program.workdir + "/skip.json"))
                {
                    TextReader reader = new StreamReader(Program.workdir + "/skip.json", Encoding.UTF8);
                    string fileContents;
                    while ((fileContents = reader.ReadLine()) != null)
                    {
                        try
                        {
                            Skip = JsonSerializer.Deserialize<Dictionary<string, int>>(fileContents);
                        }
                        catch { }
                    }
                    reader = null;
                }
            });
        }

        public async Task<int> NewReplays()
        {
            return await Task.Run(() =>
            {
                lock (Todo)
                {
                    Todo.Clear();

                    if (_startUp.SAMPLEDATA == true) return 0;

                    HashSet<string> replist = new HashSet<string>();

                    foreach (dsreplay rep in Replays)
                    {
                        replist.Add(rep.REPLAY);
                    }


                    foreach (var dir in _startUp.Conf.Replays)
                    {
                        if (Directory.Exists(dir))
                        {
                            var plainTextBytes = Encoding.UTF8.GetBytes(dir);
                            MD5 md5 = new MD5CryptoServiceProvider();
                            string reppath_md5 = BitConverter.ToString(md5.ComputeHash(plainTextBytes));

                            foreach (var fileName in Directory.GetFiles(dir, "Direct Strike*.SC2Replay", SearchOption.AllDirectories))
                            {
                                string id = Path.GetFileNameWithoutExtension(fileName);
                                string repid = reppath_md5 + "/" + id;
                                if (Skip.Keys.Contains(repid) && Skip[repid] > 4) continue;
                                if (replist.Contains(repid)) continue;
                                Todo.Add(fileName);
                            }
                        }
                    }
                }
                return Todo.Count();
            });
        }
    }
}
