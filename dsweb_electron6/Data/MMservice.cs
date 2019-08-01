using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dsweb_electron6.Models;
using dsweb_electron6.Data;
using System.Collections.Concurrent;
using NAudio.Wave;
using System.Threading;
using dsweb_electron6.Models;

namespace dsweb_electron6.Data
{
    public class MMservice
    {
        public WaveOutEvent SP { get; set; } = new WaveOutEvent();
        public bool ACCEPTED { get; set; } = false;
        public bool ALL_ACCEPTED { get; set; } = false;
        public bool DECLINED { get; set; } = false;
        public bool ALL_DECLINED { get; set; } = false;
        public bool SEARCHING { get; set; } = false;
        public bool GAMEFOUND { get; set; } = false;
        public bool AllowRandoms { get; set; } = false;
        public string RandomIsDisabled { get; set; } = "d-none";
        public string Serverinfo { get; set; } = "Offline";
        public string Serverbadge { get; set; } = "badge-offline";
        public string Info { get; set; } = "";
        public double Done { get; set; } = 0;
        public double MyELO { get; set; } = 0;
        public int MMID { get; set; } = 0;
        public string Server { get; set; } = "NA";
        public MMgame Game { get; set; } = new MMgame();
        public MMgame preGame { get; set; } = new MMgame();


        public ConcurrentDictionary<string, bool> Lobby = new ConcurrentDictionary<string, bool>();

        public TimeSpan _time { get; set; } = new TimeSpan(0);
        public int downtime = 0;

        public SEplayer seplayer { get; set; }

        public ConcurrentDictionary<int, MMgame> MMGameReady { get; set; } = new ConcurrentDictionary<int, MMgame>();
        public ConcurrentDictionary<int, MMgame> MMGameReport { get; set; } = new ConcurrentDictionary<int, MMgame>();
        public ConcurrentDictionary<int, dsreplay> DSGameReport { get; set; } = new ConcurrentDictionary<int, dsreplay>();

        StartUp _startUp;
        DSdataModel _dsData;

        public MMservice(StartUp startUp, DSdataModel dsData)
        {
            _startUp = startUp;
            _dsData = dsData;
            var audioFile = new AudioFileReader(_startUp.Conf.ExeDir + "/wwwroot/audio/ready.wav");
            SP.Init(audioFile);
        }

        public void Reset()
        {
            ACCEPTED = false;
            ALL_ACCEPTED = false;
            DECLINED = false;
            ALL_DECLINED = false;
            SEARCHING = false;
            GAMEFOUND = false;
            AllowRandoms = false;
            RandomIsDisabled = "d-none";
            _time = new TimeSpan(0);
            Serverinfo = "Offline";
            Serverbadge = "badge-danger";
            Done = 0;
            Info = "";
            Lobby = new ConcurrentDictionary<string, bool>();
            MMID = 0;
            Server = "NA";
        }

        public void FindGame(SEplayer _seplayer)
        {
            seplayer = _seplayer;
            Game = new MMgame();
            preGame = new MMgame();
            Info = "Connecting ...";
            Serverbadge = "badge-success";
            Serverinfo = "Online";
            _time = TimeSpan.FromSeconds(1);
            Task FindGame = Task.Factory.StartNew(() =>
            {
                var result = DSrest.LetmePlay(seplayer);
                if (result == null) Reset();
                else
                {
                    MyELO = Math.Round(result.EXP, 2);
                    Info = "Searching ...";
                    Searching();
                }
            }, TaskCreationOptions.AttachedToParent);


        }

        public void Searching()
        {
            SEARCHING = true;
            Task.Factory.StartNew(() =>
            {
                while (SEARCHING == true)
                {
                    Thread.Sleep(1000);
                    _time = _time.Add(TimeSpan.FromSeconds(1));
                    if (_time.TotalMinutes > 1 && AllowRandoms == false)
                    {
                        AllowRandoms = true;
                        RandomIsDisabled = "";
                        Info += " You can fill your lobby with randoms now if you want (check 'allow Randoms' top right)";
                    }
                    var res = DSrest.FindGame(seplayer.Name);
                    if (res == null) Reset();
                    else
                    {
                        if (res.Game != null)
                        {
                            SEARCHING = false;
                            GameFound(res.Game);
                        }
                        else if (res.Players != null && res.Players.Count > 0)
                        {
                            foreach (var pl in res.Players)
                            {
                                Lobby.AddOrUpdate(pl.Name, pl.Accepted, (key, oldValue) => pl.Accepted);
                            }
                        }
                    }
                }
            });
        }

        public void Accepted()
        {
            ACCEPTED = true;
            SP.Stop();
            DSrest.Accept(seplayer.Name, MMID);
        }

        public void Declined()
        {
            DECLINED = true;
            SP.Stop();
            DSrest.Decline(seplayer.Name, MMID);
            Reset();
        }

        public void GameFound(MMgame game)
        {
            GAMEFOUND = true;
            MMID = game.ID;
            SP.Play();
            ALL_DECLINED = false;
            ALL_ACCEPTED = false;
            ACCEPTED = false;
            DECLINED = false;
            Accept(game.ID);
            Lobby.Clear();
        }

        public void Accept(int id)
        {
            Task.Factory.StartNew(() =>
            {
                MMgame game = new MMgame();

                while (ALL_ACCEPTED == false && ALL_DECLINED == false)
                {
                    Thread.Sleep(250);
                    Info = "Waiting for all players to accept ...";
                    game = DSrest.Status(id);
                    preGame = game;
                    if (game == null)
                    {
                        //Console.WriteLine("game = null {0} => ({1})", seplayer.Name, id);
                        if (DECLINED == false && ACCEPTED == true)
                        {
                            ALL_DECLINED = true;
                            Info = "# Game not found :( - Searching again ..";
                            GAMEFOUND = false;
                            Thread.Sleep(2500);
                            Searching();
                            return;
                        }

                    }
                    else if (game.Declined == true)
                    {
                        //Console.WriteLine("game = declined {0} => {1} ({2})", seplayer.Name, game.ID, id);
                        List<BasePlayer> ilist = new List<BasePlayer>();
                        ilist.AddRange(game.Team1);
                        ilist.AddRange(game.Team2);
                        Lobby.Clear();
                        foreach (var pl in ilist)
                        {
                            Lobby.TryAdd(pl.Name, pl.Accepted);
                        }
                        Thread.Sleep(1500);
                        if (DECLINED == false && ACCEPTED == true)
                        {
                            ALL_DECLINED = true;

                            Info = "# Someone declined :( - Searching again ..";
                            GAMEFOUND = false;
                            Thread.Sleep(2500);
                            Searching();
                            return;
                        }
                    }
                    else
                    {
                        //Console.WriteLine("Some accepted = true {0} => {1} ({2})", seplayer.Name, game.ID, id);
                        List<BasePlayer> ilist = new List<BasePlayer>();
                        ilist.AddRange(game.Team1);
                        ilist.AddRange(game.Team2);
                        Lobby.Clear();
                        foreach (var pl in ilist)
                        {
                            Lobby.TryAdd(pl.Name, pl.Accepted);
                        }

                        if (game.Accepted == true)
                        {
                            GAMEFOUND = false;
                            Lobby.Clear();
                            Info = "Game ready!";
                            ALL_ACCEPTED = true;
                            GameReady(game);
                            return;
                        }
                    }

                    if (Done > 100) Reset();
                    else if (Done > 85)
                        if (ACCEPTED == false)
                            Declined();

                    if (DECLINED == true)
                    {
                        Info = "We declined/timed out :(";
                        GAMEFOUND = false;
                        DSrest.Decline(seplayer.Name, MMID);
                        ALL_DECLINED = true;
                    }

                    Thread.Sleep(250);
                    _time = _time.Add(TimeSpan.FromSeconds(0.5));
                    Done += 1.428571429;
                }
            });
        }

        public void Exit(string name)
        {
            Reset();
            DSrest.ExitQ(name);
        }

        public void GameReady(MMgame game)
        {
            Reset();
            Game = game;
            MMGameReady.TryAdd(Game.ID, Game);
            MMID = Game.ID;
            Server = Game.Server;

            string mypos = "";
            string creator = "Player1";

            int j = 0;
            int games = 0;
            string mmid = game.ID.ToString();
            foreach (var pl in game.Players())
            {
                j++;
                if (pl.Name == seplayer.Name) mypos = "Player" + j;
                if (pl.Games > games)
                {
                    creator = "Player" + j;
                    games = pl.Games;
                }
            }

            if (creator == mypos.ToString())
            {
                Info = "Game found! You have been elected to be the lobby creator. Please open your Starcraft 2 client on the " + game.Server + " server and create a private Direct Strike Lobby. " +
                    "Join the Channel sc2dsmm by typing ‘/join sc2dsmm’ in the Starcraft 2 chat and post the lobby link combined with the MMID by typing ‘/lobbylink " +
                    mmid + "’ (without the quotes). Have fun! :)";
            }
            else
            {
                Info = "Game found! Player " + creator + " has been elected to be the lobby creator. Please open your Starcraft 2 client on the " + game.Server + " server and join the Channel" +
                    " sc2dsmm by typing ‘/join sc2dsmm’ in the Starcraft 2 chat. Wait for the lobby link combined with the MMID " +
                    mmid + " and click on it. Have fun! :)";
            }
        }

        public void FindValidReps()
        {
            List<dsreplay> replays = _dsData.Replays.OrderByDescending(o => o.GAMETIME).Take(50).ToList();
            Dictionary<int, Dictionary<dsreplay, int>> Validrep = new Dictionary<int, Dictionary<dsreplay, int>>();
            int valid = 0;
            foreach (int id in MMGameReady.Keys)
            {
                if (DSGameReport.ContainsKey(id)) continue;

                foreach (var rep in replays)
                {
                    valid = 0;
                    int validt1 = 0;
                    int validt2 = 0;
                    foreach (var pl in rep.PLAYERS)
                    {
                        foreach (var mpl in MMGameReady[id].Team1)
                        {
                            if (pl.NAME == mpl.Name)
                            {
                                valid++;
                                if (pl.TEAM == 0) validt1++;
                                else validt2++;
                            }
                        }
                        if (validt1 > 0 && validt2 == 0) valid += validt1;
                        else if (validt2 > 0 && validt1 == 0) valid += validt2;

                        validt1 = 0;
                        validt2 = 0;
                        foreach (var mpl in MMGameReady[id].Team2)
                        {
                            if (pl.NAME == mpl.Name)
                            {
                                valid++;
                                if (pl.TEAM == 0) validt1++;
                                else validt2++;
                            }
                        }
                        if (validt1 > 0 && validt2 == 0) valid += validt1;
                        else if (validt2 > 0 && validt1 == 0) valid += validt2;
                    }
                    if (!Validrep.ContainsKey(id)) Validrep.Add(id, new Dictionary<dsreplay, int>());
                    if (valid >= 2)
                    {
                        Validrep[id].Add(rep, valid);
                    }
                }

                dsreplay reprep = new dsreplay();
                try
                {
                    reprep = Validrep[id].OrderByDescending(o => o.Value).First().Key;
                }
                catch { }

                if (reprep.PLAYERS.Count > 0) DSGameReport.TryAdd(id, reprep);
            }

            foreach (int id in DSGameReport.Keys)
            {
                if (DSGameReport[id].REPORTED > 0) continue;
                MMGameReport[id] = DSrest.Report(DSGameReport[id], id);
                if (MMGameReport[id] != null) DSGameReport[id].REPORTED = 1;
                else MMGameReport[id] = new MMgame();
               
            }

            Info = DSGameReport.Keys.Where(x => DSGameReport[x].ID > 0).Count() + " valid replay(s) found.";
        }
    }
}
