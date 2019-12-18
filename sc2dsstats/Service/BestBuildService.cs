using paxgame3.Client.Data;
using paxgame3.Client.Models;
using paxgame3.Client.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Numerics;
using sc2dsstats.Data;
using pax.s2decode.Models;
using System.Diagnostics.CodeAnalysis;

namespace paxgame3.Client.Service
{
    public static class BBService
    {
        private static BlockingCollection<BBuildJob> _jobs_build;
        private static BlockingCollection<BBuildJob> _jobs_position;
        private static BlockingCollection<int> _jobs_random;
        private static CancellationTokenSource source = new CancellationTokenSource();
        private static CancellationToken token = source.Token;
        private static ManualResetEvent _empty = new ManualResetEvent(false);
        private static int CORES = 8;
        private static RefreshBB _refreshBB;
        private static StartUp _startUp;
        private static int MaxValue = 0;
        private static object locker = new Object();
        public const string mlgamesFile = "/data/ml/mlgames.txt";

        public static TimeSpan Elapsed { get; set; } = new TimeSpan(0);

        public static DateTime START { get; set; }
        public static DateTime END { get; set; }

        public static int THREADS = 0;

        public static int BUILDS = 0;
        public static int POSITIONS = 0;

        public static bool Running = false;

        public static Vector2 center = new Vector2(128, 119);



        public static async Task GetBestBuild([NotNull] Player player, [NotNull] Player opp, [NotNull] StartUp startUp, [NotNull] RefreshBB refresh, int builds = 100, int positions = 200, int cores = 8)
        {
            if (Running == true)
                return;

            Running = true;
            _refreshBB = refresh;
            _startUp = startUp;
            _refreshBB.WorstBuild = new BBuild();
            _refreshBB.WorstStats = new Stats();
            _refreshBB.BestStats = new Stats();
            _refreshBB.BestBuild = new BBuild();
            _refreshBB.BestStatsOpp = new Stats();
            _refreshBB.WorstStatsOpp = new Stats();
            _refreshBB.Bplayer = new BBuild();
            _refreshBB.Bopp = new BBuild();
            await _refreshBB.Bplayer.GetBuild(player).ConfigureAwait(false);
            opp.Units.Clear();
            opp.Upgrades.Clear();
            opp.AbilityUpgrades.Clear();
            opp.Tier = 1;
            opp.Units.AddRange(UnitPool.Units.Where(x => x.Race == opp.Race && x.Cost > 0));
            await _refreshBB.Bopp.GetBuild(opp).ConfigureAwait(false);
            _refreshBB.Bopp.MineralsCurrent = player.MineralsCurrent;
            if (_refreshBB.Bopp.MineralsCurrent < 0)
                _refreshBB.Bopp.MineralsCurrent *= -1;
            _refreshBB.BestBuild = _refreshBB.Bopp;

            source = new CancellationTokenSource();
            token = source.Token;
            _empty = new ManualResetEvent(false);
            CORES = cores;
            MaxValue = 0;

            BUILDS = builds;
            POSITIONS = positions;
            _refreshBB.TOTAL_DONE = 0;
            _refreshBB.TOTAL = builds * positions + builds;

            START = DateTime.UtcNow;
            END = DateTime.MinValue;

            _jobs_build = new BlockingCollection<BBuildJob>();
            _jobs_position = new BlockingCollection<BBuildJob>();

            foreach (Unit unit in player.Units.Where(y => y.Status != UnitStatuses.Available))
                MaxValue += unit.Cost;

            GameHistory game = new GameHistory();
            game.ID = _startUp.GetGameID();
            game.battlefield = new Battlefield();
            Player myplayer = player.Deepcopy();
            Player myopp = opp.Deepcopy();
            await _refreshBB.Bplayer.SetBuild(myplayer).ConfigureAwait(false);
            await _refreshBB.Bopp.SetBuild(myopp).ConfigureAwait(false);
            game.Players.Add(myplayer);
            myplayer.Game = game;
            game.Players.Add(myopp);
            myopp.Game = game;
            GameService2.GenFight(game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];
            _refreshBB.BestStats = result;
            _refreshBB.BestStatsOpp = oppresult;

            for (int i = 0; i < BUILDS; i++)
            {
                BBuildJob job = new BBuildJob();
                job.PlayerBuild = _refreshBB.Bplayer;
                job.OppBuild = _refreshBB.Bopp;
                _jobs_build.Add(job);
            }

            for (int i = 0; i < 1; i++)
            {
                Thread thread = new Thread(OnHandlerStartBuild)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }

            for (int i = 0; i < 8; i++)
            {
                Thread thread = new Thread(OnHandlerStartPosition)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }

            while (!_empty.WaitOne(1000))
            {
                Console.WriteLine(_jobs_position.Count + _jobs_build.Count);
                _refreshBB.Update = !_refreshBB.Update;
                if (!_jobs_position.Any())
                    break;
            }
            END = DateTime.UtcNow;
            Running = false;
            _refreshBB.Update = !_refreshBB.Update;

        }

        public static async Task GetBestPosition([NotNull] Player player, [NotNull] Player opp, [NotNull] StartUp startUp, [NotNull] RefreshBB refresh, int builds = 100, int positions = 200, int cores = 8)
        {
            if (Running == true)
                return;

            Running = true;
            _refreshBB = refresh;
            _startUp = startUp;
            _refreshBB.WorstBuild = new BBuild();
            _refreshBB.WorstStats = new Stats();
            _refreshBB.BestStats = new Stats();
            _refreshBB.BestBuild = new BBuild();
            _refreshBB.BestStatsOpp = new Stats();
            _refreshBB.WorstStatsOpp = new Stats();
            _refreshBB.Bplayer = new BBuild();
            _refreshBB.Bopp = new BBuild();
            await _refreshBB.Bplayer.GetBuild(player).ConfigureAwait(false);
            await _refreshBB.Bopp.GetBuild(opp).ConfigureAwait(false);
            _refreshBB.BestBuild = _refreshBB.Bopp;

            source = new CancellationTokenSource();
            token = source.Token;
            _empty = new ManualResetEvent(false);
            CORES = cores;
            MaxValue = 0;

            BUILDS = builds;
            POSITIONS = positions;
            _refreshBB.TOTAL_DONE = 0;
            _refreshBB.TOTAL = positions;

            START = DateTime.UtcNow;
            END = DateTime.MinValue;

            _jobs_build = new BlockingCollection<BBuildJob>();
            _jobs_position = new BlockingCollection<BBuildJob>();

            foreach (Unit unit in player.Units.Where(y => y.Status != UnitStatuses.Available))
                MaxValue += unit.Cost;

            GameHistory game = new GameHistory();
            game.ID = _startUp.GetGameID();
            game.battlefield = new Battlefield();
            Player myplayer = player.Deepcopy();
            Player myopp = opp.Deepcopy();
            await _refreshBB.Bplayer.SetBuild(myplayer).ConfigureAwait(false);
            await _refreshBB.Bopp.SetBuild(myopp).ConfigureAwait(false);
            game.Players.Add(myplayer);
            myplayer.Game = game;
            game.Players.Add(myopp);
            myopp.Game = game;
            GameService2.GenFight(game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];
            _refreshBB.BestStats = result;
            _refreshBB.BestStatsOpp = oppresult;

            for (int i = 0; i < POSITIONS; i++)
            {
                BBuildJob job = new BBuildJob();
                job.PlayerBuild = new BBuild();
                await job.PlayerBuild.GetBuild(player).ConfigureAwait(false);
                job.OppBuild = new BBuild();
                await job.OppBuild.GetBuild(opp).ConfigureAwait(false);
                _jobs_position.Add(job);
            }

            for (int i = 0; i < 8; i++)
            {
                Thread thread = new Thread(OnHandlerStartPosition)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }

            while (!_empty.WaitOne(1000))
            {
                Console.WriteLine(_jobs_position.Count() + _jobs_build.Count());
                _refreshBB.Update = !_refreshBB.Update;
                if (!_jobs_position.Any())
                    break;
            }
            END = DateTime.UtcNow;
            Running = false;
            _refreshBB.Update = !_refreshBB.Update;
        }

        public static async Task GetRandomFights([NotNull] Player player, [NotNull] Player opp, [NotNull] StartUp startUp, [NotNull] RefreshBB refresh, int builds = 100, int positions = 200, int cores = 8)
        {
            if (Running == true)
                return;

            Running = true;
            _refreshBB = refresh;
            _startUp = startUp;
            _refreshBB.WorstBuild = new BBuild();
            _refreshBB.WorstStats = new Stats();
            _refreshBB.BestStats = new Stats();
            _refreshBB.BestBuild = new BBuild();
            _refreshBB.BestStatsOpp = new Stats();
            _refreshBB.WorstStatsOpp = new Stats();
            _refreshBB.Bplayer = new BBuild();
            _refreshBB.Bopp = new BBuild();
            await _refreshBB.Bplayer.GetBuild(player).ConfigureAwait(false);
            await _refreshBB.Bopp.GetBuild(opp).ConfigureAwait(false);

            source = new CancellationTokenSource();
            token = source.Token;
            _empty = new ManualResetEvent(false);
            CORES = cores;
            MaxValue = 0;

            BUILDS = builds;
            POSITIONS = positions;
            _refreshBB.TOTAL_DONE = 0;
            _refreshBB.TOTAL = positions;

            START = DateTime.UtcNow;
            END = DateTime.MinValue;

            _jobs_random = new BlockingCollection<int>();

            for (int i = 0; i < 40000; i++)
                _jobs_random.Add(i);

            for (int i = 0; i < 8; i++)
            {
                Thread thread = new Thread(OnHandlerStartRandom)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }

            while (!_empty.WaitOne(1000))
            {
                Console.WriteLine(_jobs_random.Count());
                _refreshBB.Update = !_refreshBB.Update;
                if (!_jobs_random.Any())
                    break;
            }
            END = DateTime.UtcNow;
            Running = false;
            _refreshBB.Update = !_refreshBB.Update;
        }

        public static RandomGame RandomFight(StartUp startUp, int minerals = 2000, bool save = false)
        {
            _startUp = startUp;
            Player _player = new Player();
            _player.Name = "Player#1";
            _player.Pos = 1;
            _player.ID = _startUp.GetPlayerID();
            _player.Race = UnitRace.Terran;
            _player.inGame = true;
            _player.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _player.Race));

            Player _opp = new Player();
            _opp.Name = "Player#2";
            _opp.Pos = 4;
            _opp.ID = _startUp.GetPlayerID();
            _opp.Race = UnitRace.Terran;
            _opp.inGame = true;
            _opp.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _opp.Race));

            GameHistory game = new GameHistory();
            _player.Game = game;
            _player.Game.ID = _startUp.GetGameID();
            _player.Game.Players.Add(_player);
            _player.Game.Players.Add(_opp);

            _opp.Game = _player.Game;

            _player.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _player.Race && x.Cost > 0));
            _opp.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _opp.Race && x.Cost > 0));

            _player.MineralsCurrent = minerals;
            _opp.MineralsCurrent = minerals;

            OppService.BPRandom(_player).GetAwaiter().GetResult();
            OppService.BPRandom(_opp).GetAwaiter().GetResult();

            BBuild bplayer = new BBuild(_player);
            BBuild bopp = new BBuild(_opp);
            bplayer.SetBuild(_player).GetAwaiter().GetResult();
            bopp.SetBuild(_opp).GetAwaiter().GetResult();

            GameService2.GenFight(_player.Game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];

            RandomResult result1 = new RandomResult();
            result1.DamageDone = oppresult.DamageDone;
            result1.MineralValueKilled = oppresult.MineralValueKilled;
            RandomResult result2 = new RandomResult();
            result2.DamageDone = result.DamageDone;
            result2.MineralValueKilled = result.MineralValueKilled;

            RandomGame rgame = new RandomGame();
            rgame.player1 = bplayer;
            rgame.player2 = bopp;
            rgame.result1 = result1;
            rgame.result2 = result2;
            rgame.Result = game.Stats.Last().winner;

            if (save == true)
                SaveGame(rgame, _player, _opp);

            return rgame;
        }

        public static void SaveGame(RandomGame game, Player p1, Player p2)
        {
            float reward = 0;
            double mod1 = game.result1.MineralValueKilled - game.result2.MineralValueKilled;
            mod1 = mod1 / 1000;

            double mod2 = game.result1.DamageDone - game.result2.DamageDone;
            mod2 = mod2 / 10000;

            float rewardp1 = (float)mod1;
            rewardp1 += (float)mod2;

            float rewardp2 = (float)mod1 * -1;
            rewardp2 -= (float)mod2;

            if (game.Result == 0)
            {
                rewardp1 += 1;
                rewardp2 += 1;
            }
            else if (game.Result == 1)
            {
                rewardp1 += 2;
                rewardp2 += 0;
            }
            else if (game.Result == 2)
            {
                rewardp1 += 0;
                rewardp2 += 2;
            }

            List<string> presult = new List<string>();
            presult.Add(rewardp1 + "," + BBuild.PrintMatrix(game.player1.GetMatrix(p1)));
            presult.Add(rewardp2 + "," + BBuild.PrintMatrix(game.player2.GetMatrix(p2)));

            lock (locker)
            {
                File.AppendAllLines(mlgamesFile, presult);
            }
        }

        public static RESTResult RESTFight(string p1, string p2, StartUp startUp)
        {
            _startUp = startUp;
            Player _player = new Player();
            _player.Name = "Player#1";
            _player.Pos = 1;
            _player.ID = _startUp.GetPlayerID();
            _player.Race = UnitRace.Terran;
            _player.inGame = true;
            _player.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _player.Race));

            Player _opp = new Player();
            _opp.Name = "Player#2";
            _opp.Pos = 4;
            _opp.ID = _startUp.GetPlayerID();
            _opp.Race = UnitRace.Terran;
            _opp.inGame = true;
            _opp.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _opp.Race));

            GameHistory game = new GameHistory();
            _player.Game = game;
            _player.Game.ID = _startUp.GetGameID();
            _player.Game.Players.Add(_player);
            _player.Game.Players.Add(_opp);

            _opp.Game = _player.Game;


            //OppService.BPRandom(_player).GetAwaiter().GetResult();
            //OppService.BPRandom(_opp).GetAwaiter().GetResult();

            BBuild bplayer = new BBuild(_player);
            BBuild bopp = new BBuild(_opp);
            bplayer.SetString(p1, _player);
            bopp.SetString(p2, _opp);

            GameService2.GenFight(_player.Game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];

            RESTResult rgame = new RESTResult();
            rgame.Result = game.Stats.Last().winner;
            rgame.DamageP1 = oppresult.DamageDone;
            rgame.MinValueP1 = oppresult.MineralValueKilled;
            rgame.DamageP2 = result.DamageDone;
            rgame.MinValueP2 = result.MineralValueKilled;

            return rgame;
        }

        public static RESTResult RESTFight(string p1, int minerals, StartUp startUp)
        {
            Player _opp = new Player();
            _opp.Name = "Player#2";
            _opp.Pos = 4;
            _opp.ID = startUp.GetPlayerID();
            _opp.Race = UnitRace.Terran;
            _opp.inGame = true;
            _opp.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _opp.Race));
            GameHistory game = new GameHistory();
            _opp.Game = game;
            _opp.Game.ID = startUp.GetGameID();
            _opp.Game.Players.Add(_opp);

            _opp.MineralsCurrent = minerals;
            OppService.BPRandom(_opp).GetAwaiter().GetResult();
            BBuild bopp = new BBuild(_opp);

            return RESTFight(p1, bopp.GetString(_opp), startUp);
        }

        public static void BuildJob(object obj)
        {
            BBuildJob job = obj as BBuildJob;
            GameHistory game = new GameHistory();
            game.ID = _startUp.GetGameID();
            game.battlefield = new Battlefield();
            Player myplayer = new Player();
            myplayer.Game = game;
            Player myopp = new Player();
            myopp.Game = game;

            job.PlayerBuild.SetBuild(myplayer).GetAwaiter().GetResult();
            job.OppBuild.SetBuild(myopp).GetAwaiter().GetResult();
            game.Players.Add(myplayer);
            myplayer.Game = game;
            game.Players.Add(myopp);
            myopp.Game = game;
            OppService.BPRandom(myopp).GetAwaiter().GetResult();
            BBuild bbuild = new BBuild();
            bbuild.GetBuild(myopp).GetAwaiter().GetResult();
            bbuild.SetBuild(myopp).GetAwaiter().GetResult();
            GameService2.GenFight(game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];
            lock (_refreshBB)
            {
                int check = CheckResult(result, oppresult);
                if (check == 1 || check == 2)
                    _refreshBB.BestBuild = bbuild;
                else if (check == 3)
                    _refreshBB.WorstBuild = bbuild;
            }

            for (int i = 0; i < POSITIONS; i++)
            {
                BBuildJob pjob = new BBuildJob();
                pjob.PlayerBuild = job.PlayerBuild;
                pjob.OppBuild = bbuild;
                _jobs_position.Add(pjob);
            }

            Interlocked.Increment(ref _refreshBB.TOTAL_DONE);
        }

        public static void PositionJob(object obj)
        {
            BBuildJob job = obj as BBuildJob;
            GameHistory game = new GameHistory();
            game.ID = _startUp.GetGameID();
            game.battlefield = new Battlefield();
            Player myplayer = new Player();
            myplayer.Game = game;
            Player myopp = new Player();
            myopp.Game = game;

            job.PlayerBuild.SetBuild(myplayer).GetAwaiter().GetResult();
            job.OppBuild.SetBuild(myopp).GetAwaiter().GetResult();
            game.Players.Add(myplayer);
            myplayer.Game = game;
            game.Players.Add(myopp);
            myopp.Game = game;
            //BBuild bbuild = OppService.BPRandom(game.Players.SingleOrDefault(x => x.Pos == 4)).GetAwaiter().GetResult();
            myopp = OppService.PRandom(game.Players.SingleOrDefault(x => x.Pos == 4), job.OppBuild).GetAwaiter().GetResult();
            BBuild bbuild = new BBuild();
            bbuild.GetBuild(myopp).GetAwaiter().GetResult();
            GameService2.GenFight(game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];
            lock (_refreshBB)
            {
                int check = CheckResult(result, oppresult);
                if (check == 1 || check == 2)
                    _refreshBB.BestBuild = bbuild;
                else if (check == 3)
                    _refreshBB.WorstBuild = bbuild;
            }
            Interlocked.Increment(ref _refreshBB.TOTAL_DONE);
        }

        public static int CheckResult(Stats result, Stats oppresult)
        {
            int check = 0;
            if (result.MineralValueKilled >= _refreshBB.BestStats.MineralValueKilled)
            {
                if (result.MineralValueKilled > _refreshBB.BestStats.MineralValueKilled)
                {
                    Console.WriteLine("setting Bestbuild");

                    _refreshBB.BestStats.MineralValueKilled = result.MineralValueKilled;
                    _refreshBB.BestStats.DamageDone = result.DamageDone;

                    check = 1;
                }
                else
                {
                    if (result.MineralValueKilled == _refreshBB.BestStats.MineralValueKilled)
                    {
                        if (_refreshBB.BestStatsOpp.MineralValueKilled == 0 || (oppresult.MineralValueKilled < _refreshBB.BestStatsOpp.MineralValueKilled))
                        {
                            _refreshBB.BestStatsOpp.MineralValueKilled = oppresult.MineralValueKilled;
                            _refreshBB.BestStatsOpp.DamageDone = oppresult.DamageDone;

                            Console.WriteLine("setting very Bestbuild");

                            _refreshBB.BestStats.MineralValueKilled = result.MineralValueKilled;
                            _refreshBB.BestStats.DamageDone = result.DamageDone;

                            check = 2;
                        }
                    }
                }
                /*
                if (_refreshBB.BestStats.MineralValueKilled == MaxValue)
                {
                    Running = false;
                    _refreshBB.Update = !_refreshBB.Update;
                    StopIt();
                    return;
                }
                */
            }

            if (_refreshBB.WorstStats.MineralValueKilled == 0 || result.MineralValueKilled < _refreshBB.WorstStats.MineralValueKilled)
            {
                _refreshBB.WorstStats.MineralValueKilled = result.MineralValueKilled;
                if (_refreshBB.WorstStats.MineralValueKilled == 0)
                    _refreshBB.WorstStats.MineralValueKilled = 1;
                _refreshBB.WorstStats.DamageDone = result.DamageDone;
                check = 3;
            }

            return check;
        }


        private static void OnHandlerStartBuild(object obj)
        {
            if (token.IsCancellationRequested == true)
                return;

            try
            {
                foreach (var job in _jobs_build.GetConsumingEnumerable(token))
                {
                    BuildJob(job);
                }
            }
            catch (OperationCanceledException)
            {
                END = DateTime.UtcNow;
            }
            _empty.Set();
        }

        private static void OnHandlerStartPosition(object obj)
        {
            if (token.IsCancellationRequested == true)
                return;
            try
            {
                foreach (var job in _jobs_position.GetConsumingEnumerable(token))
                {
                    PositionJob(job);
                }
            }
            catch (OperationCanceledException)
            {
                END = DateTime.UtcNow;
            }
        }

        private static void OnHandlerStartRandom(object obj)
        {
            if (token.IsCancellationRequested == true)
                return;
            try
            {
                foreach (var job in _jobs_random.GetConsumingEnumerable(token))
                {
                    RandomFight(_startUp, 2000, true);
                }
            }
            catch (OperationCanceledException)
            {
                END = DateTime.UtcNow;
            }
        }

        public static void StopIt()
        {
            try
            {
                source.Cancel();
            }
            catch { }
            finally
            {
                //source.Dispose();
            }
        }

        public static Vector2 mirrorImage(Vector2 vec)
        {
            float a = 1;
            float b = 0;
            float c = ((float)Battlefield.Xmax / -2);
            float x1 = vec.X;
            float y1 = vec.Y;
            float temp = -2 * (a * x1 + b * y1 + c) /
                               (a * a + b * b);
            float x = temp * a + x1;
            float y = temp * b + y1;
            return new Vector2(x, y);
        }

        public static int UpgradeUnit(UnitUpgrades upgrade, Player _player)
        {
            (int cost, int lvl) = GetUpgradeCost(upgrade, _player);

            Upgrade myupgrade = UpgradePool.Upgrades.Where(x => x.Race == _player.Race && x.Name == upgrade).FirstOrDefault();
            if (myupgrade == null) return 0;

            UnitUpgrade plup = _player.Upgrades.Where(x => x.Upgrade == myupgrade.Name).FirstOrDefault();
            if (plup != null)
            {
                if (plup.Level < 3)
                    plup.Level++;
            }
            else
            {
                UnitUpgrade newup = new UnitUpgrade();
                newup.Upgrade = myupgrade.Name;
                newup.Level = 1;
                _player.Upgrades.Add(newup);
            }
            return cost;
        }

        public static (int, int) GetUpgradeCost(UnitUpgrades upgrade, Player _player)
        {
            Upgrade myupgrade = UpgradePool.Upgrades.Where(x => x.Race == _player.Race && x.Name == upgrade).FirstOrDefault();

            if (myupgrade == null) return (0, 0);

            if (_player.Upgrades != null && _player.Upgrades.Count() > 0)
            {
                UnitUpgrade plup = _player.Upgrades.Where(x => x.Upgrade == myupgrade.Name).FirstOrDefault();
                if (plup != null)
                {
                    if (plup.Level == 3)
                    {
                        return (0, plup.Level);
                    }
                    else
                        return (myupgrade.Cost.SingleOrDefault(x => x.Key == plup.Level + 1).Value, plup.Level);
                }
            }

            return (myupgrade.Cost[0].Value, 1);
        }

        public static int AbilityUpgradeUnit(UnitAbility ability, Player _player)
        {
            _player.AbilityUpgrades.Add(ability.DeepCopy());
            if (ability.Tandem != null)
                foreach (UnitAbilities myab in ability.Tandem)
                {
                    UnitAbility tandemability = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == myab);
                    if (tandemability != null)
                    {
                        _player.AbilityUpgrades.Add(tandemability.DeepCopy());
                        if (tandemability.Type.Contains(UnitAbilityTypes.Image))
                            foreach (Unit unit in _player.Units.Where(x => x.Abilities.SingleOrDefault(y => y.Ability == tandemability.Ability) != null))
                                unit.Image = tandemability.Image;

                    }
                }

            if (ability.Ability == UnitAbilities.Tier3)
                _player.Tier = 3;
            else if (ability.Ability == UnitAbilities.Tier2)
                _player.Tier = 2;

            if (ability.Type.Contains(UnitAbilityTypes.Image))
                foreach (Unit unit in _player.Units.Where(x => x.Abilities.SingleOrDefault(y => y.Ability == ability.Ability) != null))
                    unit.Image = ability.Image;



            return ability.Cost;
        }

        public static Dictionary<int, Dictionary<int, List<UnitAbility>>> GetAbilityUpgrades(dsreplay replay)
        {
            Dictionary<int, Dictionary<int, List<UnitAbility>>> Upgrades = new Dictionary<int, Dictionary<int, List<UnitAbility>>>();
            foreach (dsplayer pl in replay.PLAYERS)
            {
                Upgrades[pl.POS] = new Dictionary<int, List<UnitAbility>>();
                foreach (var ent in pl.AbilityUpgrades)
                {
                    int gameloop = ent.Key;
                    foreach (var upgrades in ent.Value)
                    {
                        UnitAbility a = AbilityPool.Map(upgrades);
                        if (a != null)
                        {
                            if (!Upgrades[pl.POS].ContainsKey(gameloop))
                                Upgrades[pl.POS][gameloop] = new List<UnitAbility>();
                            Upgrades[pl.POS][gameloop].Add(a);
                        }
                    }
                }
            }
            return Upgrades;
        }

        public static Dictionary<int, Dictionary<int, List<UnitUpgrade>>> GetUpgrades(dsreplay replay)
        {
            Dictionary<int, Dictionary<int, List<UnitUpgrade>>> Upgrades = new Dictionary<int, Dictionary<int, List<UnitUpgrade>>>();
            foreach (dsplayer pl in replay.PLAYERS)
            {
                Upgrades[pl.POS] = new Dictionary<int, List<UnitUpgrade>>();
                foreach (var ent in pl.Upgrades)
                {
                    int gameloop = ent.Key;
                    foreach (var upgrades in ent.Value)
                    {
                        UnitUpgrade u = UpgradePool.Map(upgrades);
                        if (u != null)
                        {
                            if (!Upgrades[pl.POS].ContainsKey(gameloop))
                                Upgrades[pl.POS][gameloop] = new List<UnitUpgrade>();
                            Upgrades[pl.POS][gameloop].Add(u);
                        }
                    }
                }
            }
            return Upgrades;
        }

        public static (Dictionary<int, List<Unit>>, Dictionary<int, HashSet<int>>) GetUnits(dsreplay replay, double gameid)
        {
            //var json = File.ReadAllText("/data/unitst1p3.json");
            //var bab = JsonSerializer.Deserialize<List<UnitEvent>>(json);

            List<Unit> Units = new List<Unit>();
            List<Vector2> vecs = new List<Vector2>();
            List<UnitEvent> UnitEvents = replay.UnitBorn;

            int maxdiff = 0;
            int temploop = 0;
            Dictionary<int, List<Unit>> spawns = new Dictionary<int, List<Unit>>();
            Dictionary<int, HashSet<int>> plspawns = new Dictionary<int, HashSet<int>>();
            foreach (var unit in UnitEvents)
            {
                int diff = unit.Gameloop - temploop;

                if (temploop == 0)
                    spawns.Add(unit.Gameloop, new List<Unit>());
                else if (diff > 3)
                    spawns.Add(unit.Gameloop, new List<Unit>());

                if (unit.Gameloop - temploop > maxdiff)
                    maxdiff = unit.Gameloop - temploop;

                temploop = unit.Gameloop;

                int pos = unit.PlayerId;
                int realpos = replay.PLAYERS.SingleOrDefault(x => x.POS == pos).REALPOS;
                /*
                if (unit.PlayerId > 3)
                    pos = unit.PlayerId - 3;
                else if (unit.PlayerId <= 3)
                    pos = unit.PlayerId + 3;
                */
                spawns.Last().Value.Add(UnitEventToUnit(unit, realpos, gameid));

                if (!plspawns.ContainsKey(pos))
                    plspawns[pos] = new HashSet<int>();

                plspawns[pos].Add(spawns.Last().Key);
            }

            return (spawns, plspawns);
        }

        public static Unit UnitEventToUnit(UnitEvent unit, int pos, double gameid)
        {
            Vector2 vec = MoveService.RotatePoint(new Vector2(unit.x, unit.y), center, -45);
            float newx = 0;

            // postition 1-3 => 4-6 and 4-6 => 1-3 ...
            if (pos > 3)
                newx = ((vec.X - 62.946175f) / 2);
            else if (pos <= 3)
                newx = ((vec.X - 177.49748f) / 2);

            float newy = vec.Y - 107.686295f;

            // Team2
            // Ymax: 131,72792 => Battlefield.YMax
            // Ymin: 107,686295 => 0

            // Xmax: 78,502525 => 10
            // Xmin: 62,946175 => 0

            // Fix Names
            if (unit.Name.EndsWith("Lightweight"))
                unit.Name = unit.Name.Replace("Lightweight", "");

            float verynewx = 0;
            float verynewy = 0;
            Unit punit = null;
            Unit myunit = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name);
            if (myunit == null)
            {
                myunit = UnitPool.Units.SingleOrDefault(x => x.Name == "NA").DeepCopy();
                myunit.Name = unit.Name;
            }

            if (myunit != null)
            {
                punit = myunit.DeepCopy();
                punit.ID = UnitID.GetID(gameid);

                newx = MathF.Round((MathF.Round(newx * 2, MidpointRounding.AwayFromZero) / 2), 1);
                newy = MathF.Round((MathF.Round(newy * 2, MidpointRounding.AwayFromZero) / 2), 1);

                verynewx = newx;
                verynewy = newy;

                if (punit.BuildSize == 1)
                {
                    if (verynewx % 1 != 0)
                    {
                        //minvalues
                        if (verynewy < 0.5f)
                            verynewy = 0.5f;

                        // only .5 yvlaues allowed
                        if (verynewy % 1 == 0)
                            verynewy += 0.5f;

                    }
                    else
                    {
                        if (verynewx == 0)
                            verynewx += 1;

                        // only .0 yvalues allowed
                        if (verynewy % 1 != 0)
                            verynewy += 0.5f;
                    }


                }
                else if (punit.BuildSize == 2)
                {
                    // only .0 xvalues allowed
                    if (verynewx % 1 != 0)
                        verynewx += 0.5f;

                    // only .5 yvalues allowd
                    if (verynewy % 1 == 0)
                        verynewy += 0.5f;

                    // 0 and even rows
                    if (verynewx % 2 == 0)
                    {
                        // minvalues
                        if (verynewx < 1)
                            verynewx = 1;
                        if (verynewy < 1.5f)
                            verynewy = 1.5f;

                        // only -.5 odd values allowed
                        if ((verynewy - 0.5f) % 2 != 0)
                            verynewy += 1;

                    }
                    else
                    {
                        //minvalues
                        if (verynewx < 1.5f)
                            verynewx = 1.5f;
                        if (verynewy < 1)
                            verynewy = 1;

                        // only -.5 even values allowd
                        if ((verynewy - 0.5f) % 2 == 0)
                            verynewy += 1;
                    }
                }

                if (pos <= 3)
                    verynewx = verynewx + Battlefield.Xmax - 10;

                punit.BuildPos = new Vector2(verynewx, verynewy + 2);
                punit.Owner = pos;
                punit.Status = UnitStatuses.Placed;


            }
            return punit;
        }
    }
}
