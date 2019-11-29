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

namespace paxgame3.Client.Service
{
    public static class BestBuildService
    {
        private static BlockingCollection<GameHistory> _jobs_build;
        private static BlockingCollection<GameHistory> _jobs_position;
        private static CancellationTokenSource source = new CancellationTokenSource();
        private static CancellationToken token = source.Token;
        private static ManualResetEvent _empty = new ManualResetEvent(false);
        private static int CORES = 8;
        private static RefreshBB _refreshBB;
        private static int MaxValue = 0;
        public static TimeSpan Elapsed { get; set; } = new TimeSpan(0);

        public static DateTime START { get; set; }
        public static DateTime END { get; set; }

        public static int THREADS = 0;

        public static int BUILDS = 0;
        public static int POSITIONS = 0;

        public static bool Running = false;

        public static Vector2 center = new Vector2(128, 119);

        

        public static async Task GetBestBuild(GameHistory _game, StartUp _startUp, RefreshBB _refresh, int builds = 100, int positions = 200, int cores = 8)
        {
            if (Running == true)
                return;

            Running = true;
            _refreshBB = _refresh;
            _refreshBB.WorstBuild = new BBuild();
            _refreshBB.WorstStats = new Stats();
            _refreshBB.BestStats = new Stats();
            _refreshBB.BestBuild = new BBuild();
            _refreshBB.BestStatsOpp = new Stats();
            _refreshBB.WorstStatsOpp = new Stats();


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

            _jobs_build = new BlockingCollection<GameHistory>();
            _jobs_position = new BlockingCollection<GameHistory>();

            foreach (Unit unit in _game.Players.Single(x => x.Pos == 1).Units.Where(y => y.Status != UnitStatuses.Available))
                MaxValue += unit.Cost;

            for (int i = 0; i < BUILDS; i++)
            {
                GameHistory game = new GameHistory();
                game.ID = i + 1;
                game.battlefield = new Battlefield();

                foreach (Player pl in _game.Players)
                {
                    Player newpl = new Player();
                    newpl.Name = pl.Name;
                    newpl.Pos = pl.Pos;
                    newpl.ID = pl.ID;
                    newpl.Race = pl.Race;
                    newpl.inGame = true;
                    newpl.Units = new List<Unit>();
                    newpl.MineralsCurrent = pl.MineralsCurrent;
                    newpl.Upgrades = new List<UnitUpgrade>(pl.Upgrades);
                    newpl.AbilityUpgrades = new List<UnitAbility>(pl.AbilityUpgrades);
                    newpl.Tier = pl.Tier;
                    newpl.Stats = new Dictionary<int, paxgame3.Client.Models.M_stats>();
                    foreach (Unit unit in pl.Units)
                    {
                        newpl.Units.Add(unit.DeepCopy());
                        unit.Ownerplayer = newpl;
                        if (unit.Bonusdamage != null)
                            unit.Bonusdamage.Ownerplayer = newpl;
                    }

                    newpl.Game = game;
                    game.Players.Add(newpl);
                }

                _jobs_build.Add(game);
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
                Console.WriteLine(_jobs_position.Count() + _jobs_build.Count());
                _refreshBB.Update = !_refreshBB.Update;
                if (_jobs_position.Count() == 0)
                    break;
            }
            END = DateTime.UtcNow;
            Running = false;
            _refreshBB.Update = !_refreshBB.Update;

        }

        public static async Task GetBestPosition(GameHistory _game, StartUp _startUp, RefreshBB _refresh, int positions = 200, int cores = 8)
        {
            if (Running == true)
                return;

            Running = true;
            _refreshBB = _refresh;
            _refreshBB.WorstBuild = new BBuild();
            _refreshBB.WorstStats = new Stats();
            _refreshBB.BestStats = new Stats();
            _refreshBB.BestBuild = new BBuild();
            _refreshBB.BestStatsOpp = new Stats();
            _refreshBB.WorstStatsOpp = new Stats();


            source = new CancellationTokenSource();
            token = source.Token;
            _empty = new ManualResetEvent(false);
            CORES = cores;
            MaxValue = 0;

            BUILDS = 1;
            POSITIONS = positions;
            _refreshBB.TOTAL_DONE = 0;
            _refreshBB.TOTAL = BUILDS * POSITIONS;

            START = DateTime.UtcNow;
            END = DateTime.MinValue;

            _jobs_build = new BlockingCollection<GameHistory>();
            _jobs_position = new BlockingCollection<GameHistory>();

            foreach (Unit unit in _game.Players.Single(x => x.Pos == 1).Units.Where(y => y.Status != UnitStatuses.Available))
                MaxValue += unit.Cost;

            _game.battlefield = new Battlefield();
            _game.battlefield.Units = new List<Unit>();
            foreach (Unit unit in _game.battlefield.Units)
                _game.battlefield.Units.Add(unit.DeepCopy());
            _game.battlefield.Units = GameService2.ShuffleUnits(_game.Players);

            for (int i = 0; i < POSITIONS; i++)
            {
                GameHistory game = new GameHistory();
                game.ID = _game.ID;
                game.battlefield = new Battlefield();
                game.battlefield.Units = new List<Unit>();
                foreach (Unit unit in _game.battlefield.Units)
                    game.battlefield.Units.Add(unit.DeepCopy());

                foreach (Player pl in _game.Players)
                {
                    Player newpl = new Player();
                    newpl.Name = pl.Name;
                    newpl.Pos = pl.Pos;
                    newpl.ID = pl.ID;
                    newpl.Race = pl.Race;
                    newpl.inGame = true;
                    newpl.Units = new List<Unit>();
                    newpl.MineralsCurrent = pl.MineralsCurrent;
                    newpl.Upgrades = new List<UnitUpgrade>(pl.Upgrades);
                    newpl.AbilityUpgrades = new List<UnitAbility>(pl.AbilityUpgrades);
                    newpl.Tier = pl.Tier;
                    newpl.Stats = new Dictionary<int, paxgame3.Client.Models.M_stats>();
                    newpl.Units = new List<Unit>(game.battlefield.Units.Where(x => x.Owner == newpl.Pos));

                    newpl.Game = game;
                    game.Players.Add(newpl);
                }

                _jobs_position.Add(game);
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
                if (_jobs_position.Count() == 0)
                    break;
            }
            END = DateTime.UtcNow;
            Running = false;
            _refreshBB.Update = !_refreshBB.Update;
        }



        public static void BuildJob(object obj)
        {
            GameHistory _game = obj as GameHistory;
            Player opp = _game.Players.Single(x => x.Pos == 4);
            OppService.BotRandom(_game.ID, opp, _game.Players.First().MineralsCurrent - MaxValue).GetAwaiter();
            _game.battlefield.Units = GameService2.ShuffleUnits(_game.Players);

            for (int i = 0; i < POSITIONS; i++)
            {
                GameHistory game = new GameHistory();
                game.ID = _game.ID;
                game.battlefield = new Battlefield();
                game.battlefield.Units = new List<Unit>();
                foreach (Unit unit in _game.battlefield.Units)
                    game.battlefield.Units.Add(unit.DeepCopy());
                
                foreach (Player pl in _game.Players)
                {
                    Player newpl = new Player();
                    newpl.Name = pl.Name;
                    newpl.Pos = pl.Pos;
                    newpl.ID = pl.ID;
                    newpl.Race = pl.Race;
                    newpl.inGame = true;
                    newpl.Units = new List<Unit>();
                    newpl.MineralsCurrent = pl.MineralsCurrent;
                    newpl.Upgrades = new List<UnitUpgrade>(pl.Upgrades);
                    newpl.AbilityUpgrades = new List<UnitAbility>(pl.AbilityUpgrades);
                    newpl.Tier = pl.Tier;
                    newpl.Stats = new Dictionary<int, paxgame3.Client.Models.M_stats>();
                    newpl.Units = new List<Unit>(game.battlefield.Units.Where(x => x.Owner == newpl.Pos));

                    newpl.Game = game;
                    game.Players.Add(newpl);
                }
                
                _jobs_position.Add(game);
            }
            Interlocked.Increment(ref _refreshBB.TOTAL_DONE);
        }

        public static void PositionJob(object obj)
        {
            GameHistory _game = obj as GameHistory;
            OppService.PositionRandom(_game.battlefield.Units.Where(x => x.Owner == 4).ToList(), 4).GetAwaiter().GetResult();
            GameService2.GenFight(_game, false).GetAwaiter().GetResult();
            StatsService.GenRoundStats(_game, false).GetAwaiter().GetResult();
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = _game.Stats.Last().Damage[1];
            result.MineralValueKilled = _game.Stats.Last().Killed[1];
            oppresult.DamageDone = _game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = _game.Stats.Last().Killed[0];
            Player mypl = _game.Players.Single(x => x.Pos == 4);

            float mvk = 0;
            float dd = 0;
            float k = 0;
            foreach (Unit unit in _game.battlefield.Units.Where(x => x.Owner == mypl.Pos && x.Race != UnitRace.Defence))
            {
                mvk += unit.MineralValueKilledRound;
                dd += unit.DamageDoneRound;
                k += unit.Kills;
            }

            lock (_refreshBB)
            {
                if (result.MineralValueKilled >= _refreshBB.BestStats.MineralValueKilled)
                {
                    if (result.MineralValueKilled > _refreshBB.BestStats.MineralValueKilled)
                    {
                        Console.WriteLine("setting Bestbuild");

                        _refreshBB.BestStats.MineralValueKilled = result.MineralValueKilled;
                        _refreshBB.BestStats.DamageDone = result.DamageDone;

                        BBuild temp = new BBuild();
                        temp.GetBuild(mypl).GetAwaiter().GetResult();
                        _refreshBB.BestBuild = temp;
                    } else
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

                                BBuild temp = new BBuild();
                                temp.GetBuild(mypl).GetAwaiter().GetResult();
                                _refreshBB.BestBuild = temp;

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
                    _refreshBB.WorstBuild.GetBuild(mypl).GetAwaiter().GetResult();
                }
            }
            _game = null;
            Interlocked.Increment(ref _refreshBB.TOTAL_DONE);
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

        // function which finds coordinates 
        // of mirror image. 
        // This function return a pair of double 
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
            _player.AbilityUpgrades.Add(ability);

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

        public static Dictionary<int, Dictionary<int, List<UnitUpgrade>>> GetUpgrades (dsreplay replay)
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
            List<UnitEvent> UnitEvents  = replay.UnitBorn;

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
                newx = (vec.X - 177.49748f) / 2;

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

                punit.BuildPos = new Vector2(verynewx, verynewy);
                punit.Owner = pos;
                punit.Status = UnitStatuses.Placed;
                

            }
            return punit;
        }
    }
}
