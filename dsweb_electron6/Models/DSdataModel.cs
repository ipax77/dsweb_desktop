using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dsweb_electron6;
using System.IO;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using dsweb_electron6.Models;
using dsweb_electron6.Interfaces;
using System.Security.Cryptography;

namespace dsweb_electron6.Models
{
    public class DSdataModel
    {
        public List<dsreplay> Replays = new List<dsreplay>();
        public Dictionary<string, int> Skip = new Dictionary<string, int>();
        public HashSet<string> Todo = new HashSet<string>();

        public int ID { get; set; } = 0;
        public StartUp _startUp;
        IDSdata_cache _dsdata;

        public DSdataModel(StartUp startUp, IDSdata_cache dsdata)
        {
            _startUp = startUp;
            _dsdata = dsdata;
            LoadData();
            LoadSkip();
        }

        public void Init()
        {

        }

        public void LoadData()
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
                lock (Replays)
                {
                    Replays.Clear();
                    int maxid = 0;
                    foreach (string fileContents in File.ReadLines(Program.myJson_file))
                    {
                        dsreplay rep = null;
                        try
                        {
                            rep = JsonConvert.DeserializeObject<dsreplay>(fileContents);
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
                            rep = System.Text.Json.Serialization.JsonSerializer.Parse<dsreplay>(fileContents);
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

        public void LoadSkip()
        {
            if (File.Exists(Program.workdir + "/skip.json")) {
                TextReader reader = new StreamReader(Program.workdir + "/skip.json", Encoding.UTF8);
                string fileContents;
                while ((fileContents = reader.ReadLine()) != null)
                {
                    try
                    {
                        Skip = JsonConvert.DeserializeObject<Dictionary<string, int>>(fileContents);
                    } catch { }
                }
                reader = null;
            }
        }

        public void NewReplays()
        {
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
        }
    }
}
