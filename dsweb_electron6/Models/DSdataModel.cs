using dsweb_electron6.Data;
using dsweb_electron6.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace dsweb_electron6.Models
{
    public class DSdataModel
    {
        public List<dsreplay> Replays = new List<dsreplay>();
        public Dictionary<string, int> Skip = new Dictionary<string, int>();
        public HashSet<string> Todo = new HashSet<string>();

        public int ID { get; set; } = 0;
        private bool INIT = false;

        public StartUp _startUp;
        IDSdata_cache _dsdata;
        DSdyn_filteroptions _options;

        public DSdataModel(StartUp startUp, IDSdata_cache dsdata, DSdyn_filteroptions options)
        {
            _options = options;
            _startUp = startUp;
            _dsdata = dsdata;

        }

        public async Task Init()
        {
            if (INIT == true) return;
            INIT = true;
            await LoadData();
            await LoadSkip();
        }

        public async Task LoadData()
        {
            if (File.Exists(Program.myJson_file))
            {
                if (_startUp.SAMPLEDATA == true)
                {
                    _startUp.SAMPLEDATA = false;
                    try
                    {
                        _startUp.Conf.Players.Remove("player");
                    } catch { }
                }
                await Task.Run(() => { 
                    lock (Replays)
                    {
                        Replays.Clear();
                        int maxid = 0;
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
                        ID = maxid;
                        NewReplays();
                        _dsdata.Init(Replays);
                        _options.DOIT = false;
                        _options.BeginAtZero = !_options.BeginAtZero;
                        _options.DOIT = true;
                        _options.BeginAtZero = !_options.BeginAtZero;
                    }
                });
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
                        } catch { }
                    }
                }
                _startUp.Conf.Players.Add("player");
                _dsdata.Init(Replays);
            }
        }

        public async Task LoadSkip()
        {
            await Task.Run(() => { 
                if (File.Exists(Program.workdir + "/skip.json")) {
                    TextReader reader = new StreamReader(Program.workdir + "/skip.json", Encoding.UTF8);
                    string fileContents;
                    while ((fileContents = reader.ReadLine()) != null)
                    {
                        try
                        {
                            Skip = JsonSerializer.Deserialize<Dictionary<string, int>>(fileContents);
                        } catch { }
                    }
                    reader = null;
                }
            });
        }

        public async Task NewReplays()
        {
            await Task.Run(() => { 
                lock (Todo)
                {
                    Todo.Clear();

                    if (_startUp.SAMPLEDATA == true) return;

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
            });
        }
    }
}
