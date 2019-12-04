using Microsoft.VisualBasic;
using paxgame3.Client.Data;
using paxgame3.Client.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace paxgame3.Client.Service
{
    public static class OppService
    {
        static List<string> Bots = new List<string>()
        {
            "Bot1TvZ",
            "Bot1ZvT",
        };


        static List<UnitUpgrades> UpgradesTvZ = new List<UnitUpgrades>()
        {
            UnitUpgrades.GroundArmor,
            UnitUpgrades.GroundMeleeAttac,
            UnitUpgrades.GroundAttac,
            UnitUpgrades.GroundArmor,
            UnitUpgrades.GroundMeleeAttac,
            UnitUpgrades.GroundAttac,
            UnitUpgrades.GroundArmor,
            UnitUpgrades.GroundMeleeAttac,
            UnitUpgrades.GroundAttac
        };

        static List<UnitAbilities> AbilityUpgradesTvZ = new List<UnitAbilities>()
        {
            UnitAbilities.MetabolicBoost,
            UnitAbilities.CentrifugalHooks,
            UnitAbilities.AdrenalGlands
        };

        static List<UnitUpgrades> UpgradesZvT = new List<UnitUpgrades>()
        {
            UnitUpgrades.GroundAttac,
            UnitUpgrades.GroundArmor,
            UnitUpgrades.GroundAttac,
            UnitUpgrades.GroundArmor,
            UnitUpgrades.GroundAttac,
            UnitUpgrades.GroundArmor,
        };

        static List<UnitAbilities> AbilityUpgradesZvT = new List<UnitAbilities>()
        {
            UnitAbilities.CombatShield,
            UnitAbilities.Stimpack,
            UnitAbilities.ConcussiveShells
        };

        public static Player CreateEnemy(double gameid, int z_rows_b, int z_columns_b, int z_rows_z, int z_columns_z, int z_rows_r, int z_columns_r, bool z_metabolicboost, bool z_adrenalglance)
        {
            Player opp = new Player();
            opp.ID = 999999;
            opp.Name = "Opp";
            opp.Pos = 4;
            opp.Race = UnitRace.Zerg;
            opp.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == opp.Race));

            if (z_metabolicboost)
                opp.AbilityUpgrades.Add(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.MetabolicBoost));
            if (z_adrenalglance)
                opp.AbilityUpgrades.Add(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.AdrenalGlands));

            int k = 0;
            for (int j = 1; j <= z_rows_b; j++)
            {
                for (int i = 1; i < z_columns_b; i++)
                {
                    k++;
                    //Unit u = new Unit("Zergling", 4, new Vector2(Battlefield.Xmax - j, i));
                    //Unit u = new Unit("Baneling", 4, new Vector2(Battlefield.Xmax - j, i));
                    Unit u = UnitPool.Units.SingleOrDefault(x => x.Name == "Baneling").DeepCopy();
                    u.BuildPos = new Vector2(Battlefield.Xmax - j, i);
                    u.Status = UnitStatuses.Spawned;
                    u.ID = UnitID.GetID(gameid);
                    u.SerPos = new Vector2Ser();
                    u.SerPos.x = u.BuildPos.X;
                    u.SerPos.y = u.BuildPos.Y;
                    u.Ownerplayer = opp;
                    if (u.Bonusdamage != null)
                        u.Bonusdamage.Ownerplayer = u.Ownerplayer;
                    opp.Units.Add(u);
                }
            }
            for (int j = 1; j <= z_rows_z; j++)
            {
                for (int i = 1; i < z_columns_z; i++)
                {
                    k++;
                    //Unit u = new Unit("Zergling", 4, new Vector2(Battlefield.Xmax - j, i));
                    //Unit u = new Unit("Zergling", 4, new Vector2(Battlefield.Xmax - j, i));
                    Unit u = UnitPool.Units.SingleOrDefault(x => x.Name == "Zergling").DeepCopy();
                    u.BuildPos = new Vector2(Battlefield.Xmax - j, i);
                    u.Status = UnitStatuses.Spawned;
                    u.ID = UnitID.GetID(gameid);
                    u.SerPos = new Vector2Ser();
                    u.SerPos.x = u.BuildPos.X;
                    u.SerPos.y = u.BuildPos.Y;
                    u.Ownerplayer = opp;
                    if (u.Bonusdamage != null)
                        u.Bonusdamage.Ownerplayer = u.Ownerplayer;
                    opp.Units.Add(u);
                }
            }
            for (int j = 1; j <= z_rows_r; j++)
            {
                for (int i = 1; i < z_columns_r; i++)
                {
                    k++;
                    //Unit u = new Unit("Roach", 4, new Vector2(Battlefield.Xmax - j, i));
                    Unit u = UnitPool.Units.SingleOrDefault(x => x.Name == "Roach").DeepCopy();
                    u.BuildPos = new Vector2(Battlefield.Xmax - j, i);
                    u.Owner = opp.Pos;
                    u.Status = UnitStatuses.Spawned;
                    u.ID = UnitID.GetID(gameid);
                    u.SerPos = new Vector2Ser();
                    u.SerPos.x = u.BuildPos.X;
                    u.SerPos.y = u.BuildPos.Y;
                    u.Ownerplayer = opp;
                    if (u.Bonusdamage != null)
                        u.Bonusdamage.Ownerplayer = u.Ownerplayer;
                    opp.Units.Add(u);
                }
            }
            return opp;
        }

        public static async Task BotRandom(double gameid, Player _opp, float upgrademins = 0)
        {
            List<Unit> Units = new List<Unit>(UnitPool.Units.Where(x => x.Cost > 0 && x.Race == _opp.Race));
            HashSet<UnitUpgrades> Upgrades = new HashSet<UnitUpgrades>();
            HashSet<UnitAbilities> Abilities = new HashSet<UnitAbilities>();
            Dictionary<UnitAbilities, int> AbilityCount = new Dictionary<UnitAbilities, int>();

            Random rnd = new Random();
            while (_opp.MineralsCurrent > 0)
            {
                Units = new List<Unit>(Units.Where(x => x.Cost <= _opp.MineralsCurrent));
                if (Units.Count() == 0)
                    break;

                int doups = rnd.Next(0, Units.Count());
                Unit unit = Units.ElementAt(doups);
                if (_opp.MineralsCurrent >= unit.Cost)
                {
                    Unit myunit = unit.DeepCopy();
                    _opp.MineralsCurrent -= myunit.Cost;
                    myunit.ID = UnitID.GetID(_opp.GameID);
                    myunit.Status = UnitStatuses.Placed;
                    myunit.Owner = _opp.Pos;
                    myunit.Ownerplayer = _opp;
                    _opp.Units.Add(myunit);

                    UnitAbility imageability = myunit.Abilities.SingleOrDefault(x => x.Type.Contains(UnitAbilityTypes.Image));
                    if (imageability != null)
                        if (_opp.AbilityUpgrades.SingleOrDefault(x => x.Ability == imageability.Ability) != null)
                            myunit.Image = imageability.Image;

                }
            }

            foreach (Unit unit in _opp.Units.Where(x => x.Status != UnitStatuses.Available))
            {
                Upgrades.Add(unit.ArmorType);
                Upgrades.Add(unit.AttacType);
                foreach (UnitAbility ability in unit.Abilities.Where(x => x.Cost > 0))
                {
                    Abilities.Add(ability.Ability);
                    if (_opp.AbilityUpgrades.SingleOrDefault(s => s.Ability == ability.Ability) == null)
                        if (!AbilityCount.ContainsKey(ability.Ability))
                            AbilityCount[ability.Ability] = unit.Cost;
                        else
                            AbilityCount[ability.Ability] += unit.Cost;

                }
            }

            int UpgradesPossible = Upgrades.Count() * 3;
            foreach (UnitUpgrade upgrade in _opp.Upgrades)
            {
                UpgradesPossible -= upgrade.Level;
                if (upgrade.Level == 3)
                    Upgrades.Remove(upgrade.Upgrade);
            }

            int AbilityUpgradesPossible = Abilities.Count();
            foreach (UnitAbility ability in _opp.AbilityUpgrades)
            {
                AbilityUpgradesPossible -= 1;
                Abilities.Remove(ability.Ability);
            }
            float minsavailableforupgrades = upgrademins;
            if (upgrademins > 0)
            {
                while (minsavailableforupgrades > 0)
                {
                    if (rnd.Next(100) < 50)
                        break;

                    if (AbilityUpgradesPossible > 0 && AbilityCount.Count() > 0)
                    {
                        AbilityCount = new Dictionary<UnitAbilities, int>(AbilityCount.OrderBy(o => o.Value));

                        UnitAbility ability1 = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == AbilityCount.Last().Key).DeepCopy();

                        List<Unit> RemoveUnits = new List<Unit>(_opp.Units.Where(x => x.Status == UnitStatuses.Placed && x.Abilities.SingleOrDefault(s => s.Ability == ability1.Ability) == null));
                        if (RemoveUnits.Sum(s => s.Cost) <= ability1.Cost)
                            while (RemoveUnits.Sum(s => s.Cost) <= ability1.Cost)
                                RemoveUnits.Add(_opp.Units.Where(x => x.Status == UnitStatuses.Placed && x.Abilities.SingleOrDefault(s => s.Ability == ability1.Ability) != null).First());

                        while (_opp.MineralsCurrent <= ability1.Cost)
                        {
                            _opp.Units.Remove(RemoveUnits.First());
                            _opp.MineralsCurrent += RemoveUnits.First().Cost;
                            RemoveUnits.Remove(RemoveUnits.First());
                        }
                        AbilityCount.Remove(AbilityCount.Single(s => s.Key == ability1.Ability).Key);
                        AbilityUpgradeUnit(ability1, _opp);
                        minsavailableforupgrades -= ability1.Cost;
                    }
                }
            }
            //await PositionRandom(_opp.Units, _opp.Pos);
        }

        public static async Task PositionRandom1(List<Unit> Units, int pos)
        {
            int[,] Pos = new int[10, Battlefield.Ymax];
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < Battlefield.Ymax; j++)
                    Pos[i, j] = 0;

            int mod = 0;
            if (pos > 3)
                mod = Battlefield.Xmax - 10;

            Vector2 lastvec = Vector2.Zero;
            foreach (Unit myunit in Units.Where(x => x.Status == UnitStatuses.Placed || x.Status == UnitStatuses.Spawned))
            {
                Vector2 vec = Vector2.Zero;

                while (vec == Vector2.Zero)
                {
                    Random rnd = new Random();
                    int i = rnd.Next(0, 9);
                    int j = rnd.Next(0, Battlefield.Ymax);
                    int d = rnd.Next(7, 33);
                    if (Pos[i, j] == 0)
                    {
                        vec = new Vector2(i + mod, j);
                        if (lastvec != Vector2.Zero)
                        {
                            float lastdistance = Vector2.DistanceSquared(vec, lastvec);
                            for (int k = 0; k <= d; k++)
                            {
                                do
                                {
                                    i = rnd.Next(0, 9);
                                    j = rnd.Next(0, Battlefield.Ymax);
                                } while (Pos[i, j] == 1);

                                float distance = Vector2.DistanceSquared(new Vector2(i + mod, j), lastvec);

                                if (distance < lastdistance)
                                    vec = new Vector2(i + mod, j);

                                lastdistance = distance;
                            }
                        }
                        Pos[(int)vec.X - mod, (int)vec.Y] = 1;

                        if (lastvec == Vector2.Zero)
                            lastvec = new Vector2(vec.X, vec.Y);
                    }
                }


                myunit.BuildPos = new Vector2(vec.X, vec.Y);
                myunit.RealPos = myunit.BuildPos;
                myunit.Pos = myunit.BuildPos;
                myunit.SerPos = new Vector2Ser();
                myunit.SerPos.x = myunit.BuildPos.X;
                myunit.SerPos.y = myunit.BuildPos.Y;
                myunit.RelPos = MoveService.GetRelPos(myunit.RealPos);
            }
        }

        public static async Task PositionRandom2(List<Unit> Units, int pos)
        {

            int[,] Pos = new int[10, Battlefield.Ymax];
            for (int i = 0; i < 20; i++)
                for (int j = 0; j < Battlefield.Ymax + 9; j++)
                    Pos[i, j] = 0;

            int mod = 0;
            if (pos > 3)
                mod = Battlefield.Xmax - 20;

            Vector2 lastvec = Vector2.Zero;
            foreach (Unit myunit in Units.Where(x => x.Status == UnitStatuses.Placed || x.Status == UnitStatuses.Spawned))
            {
                Vector2 vec = Vector2.Zero;

                while (vec == Vector2.Zero)
                { 
                    Random rnd = new Random();
                    int i = rnd.Next(0, 19);
                    int j = rnd.Next(0, Battlefield.Ymax + 9);
                    int d = rnd.Next(7, 33);
                    if (Pos[i, j] == 0)
                    {
                        float newi = i;
                        float newj = j;
                        float X = 0;
                        float Y = 0;    

                        if (j > 0 && j % 4 == 0) {
                            newj = j % 4;
                            newi = i / 2;

                            X = (float)newi + 1;
                            Y = (newj * 2) + 1.5f;
                            if (newi % 2 != 0)
                            {
                                Y -= 1;

                            }                           
                        } else {
                            newj -= j % 4;
                            X = ((float)newi / 2) + 0.5f;
                            Y = newj + 0.5f;
                            if (newi % 2 != 0)
                                Y -= 0.5f;  
                        }

  

                        vec = new Vector2(i + mod, j);
                        if (lastvec != Vector2.Zero)
                        {
                            float lastdistance = Vector2.DistanceSquared(vec, lastvec);
                            for (int k = 0; k <= d; k++)
                            {
                                do
                                {
                                    i = rnd.Next(0, 9);
                                    j = rnd.Next(0, Battlefield.Ymax);
                                } while (Pos[i, j] == 1);

                                float distance = Vector2.DistanceSquared(new Vector2(i + mod, j), lastvec);

                                if (distance < lastdistance)
                                    vec = new Vector2(i + mod, j);

                                lastdistance = distance;
                            }
                        }
                        Pos[(int)vec.X - mod, (int)vec.Y] = 1;

                        if (lastvec == Vector2.Zero)
                            lastvec = new Vector2(vec.X, vec.Y);
                    }
                }


                myunit.BuildPos = new Vector2(vec.X, vec.Y);
                myunit.RealPos = myunit.BuildPos;
                myunit.Pos = myunit.BuildPos;
                myunit.SerPos = new Vector2Ser();
                myunit.SerPos.x = myunit.BuildPos.X;
                myunit.SerPos.y = myunit.BuildPos.Y;
                myunit.RelPos = MoveService.GetRelPos(myunit.RealPos);
            }
        }

        public static async Task PositionRandom(List<Unit> Units, int pos)
        {
            int[,] Pos = new int[20, Battlefield.Ymax - 5];
            for (int i = 0; i < 20; i++)
                for (int j = 0; j < Battlefield.Ymax - 5; j++)
                    Pos[i, j] = 0;

            int[,] PosTwo = new int[9, (Battlefield.Ymax / 2) - 3];
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < (Battlefield.Ymax / 2) - 3; j++)
                    PosTwo[i, j] = 0;

            int mod = 0;
            if (pos > 3)
                mod = Battlefield.Xmax - 10;

            Vector2 lastvec = Vector2.Zero;
            foreach (Unit myunit in Units.Where(x => x.Status == UnitStatuses.Placed || x.Status == UnitStatuses.Spawned))
            {
                if (myunit.BuildSize == 1) {
                    Vector2 vec = Vector2.Zero;

                    while (vec == Vector2.Zero)
                    {
                        Random rnd = new Random();
                        int i = rnd.Next(0, 19);
                        int j = rnd.Next(0, Battlefield.Ymax - 5);
                        int d = rnd.Next(7, 33);
                        if (Pos[i, j] == 0)
                        {
                            float X = ((float)i / 2) + 0.5f;
                            float Y = j + 0.5f + 2;
                            if (i % 2 != 0)
                                Y -= 0.5f;

                            if (pos > 3)
                            {
                                X = X + (float)Battlefield.Xmax - 10;
                            }
                            vec = new Vector2(X, Y);
                            if (lastvec != Vector2.Zero)
                            {
                                float lastdistance = Vector2.DistanceSquared(vec, lastvec); 
                                for (int k = 0; k <= d; k++)
                                {
                                    do
                                    {
                                        i = rnd.Next(0, 9);
                                        j = rnd.Next(0, Battlefield.Ymax - 5);
                                    } while (Pos[i, j] == 1);
                                    float Xd = ((float)i / 2) + 0.5f;
                                    float Yd = j + 0.5f + 2;
                                    if (i % 2 != 0)
                                        Yd -= 0.5f;

                                    if (pos > 3)
                                    {
                                        Xd = Xd + (float)Battlefield.Xmax - 10;
                                    }
                                    float distance = Vector2.DistanceSquared(new Vector2(Xd, Yd), lastvec);
                                    
                                    if (distance < lastdistance)
                                        vec = new Vector2(Xd, Yd);
                                    
                                    lastdistance = distance;
                                }
                            }
                            Pos[i, j] = 1;

                            int itwo = 0;
                            int jtwo = 0;
                            if (i > 0 && j > 0)
                            {
                                if (i % 2 == 0)
                                {
                                    if ((j + 1) % 4 == 0)
                                    {
                                        jtwo = (j + 1) / 4;
                                        itwo = i / 2;
                                    }
                                } else
                                {
                                    if (j % 4 == 0)
                                    {
                                        jtwo = j / 4;
                                        itwo = (i - 1) / 2;
                                    }
                                }
                            }
                            if (itwo < 9 && jtwo < Battlefield.Ymax / 2)
                                PosTwo[itwo, jtwo] = 1;

                            if (lastvec == Vector2.Zero)
                                lastvec = new Vector2(vec.X, vec.Y);
                        }
                    }


                    myunit.BuildPos = new Vector2(vec.X, vec.Y);
                    myunit.RealPos = myunit.BuildPos;
                    myunit.Pos = myunit.BuildPos;
                    myunit.SerPos = new Vector2Ser();
                    myunit.SerPos.x = myunit.BuildPos.X;
                    myunit.SerPos.y = myunit.BuildPos.Y;
                    myunit.RelPos = MoveService.GetRelPos(myunit.RealPos);
                } else if (myunit.BuildSize == 2) {
                    Vector2 vec = Vector2.Zero;

                    while (vec == Vector2.Zero)
                    {
                        Random rnd = new Random();
                        int i = rnd.Next(0, 8);
                        int j = rnd.Next(0, (Battlefield.Ymax / 2) - 1 - 3);
                        int d = rnd.Next(7, 33);
                        if (PosTwo[i, j] == 0)
                        {
                            if (i % 2 == 0 && j == (Battlefield.Ymax / 2) - 1 - 3)
                            {
                                continue;
                            }
                            float X = (float)i + 1;
                            float Y = (j * 2) + 1.5f + 2;
                            if (i % 2 != 0)
                            {
                                Y -= 1;

                            }
                            if (pos > 3)
                            {
                                X = X + (float)Battlefield.Xmax - 10;
                            }
                            vec = new Vector2(X, Y);
                            if (lastvec != Vector2.Zero)
                            {
                                float lastdistance = Vector2.DistanceSquared(vec, lastvec); 
                                for (int k = 0; k <= d; k++)
                                {
                                    do
                                    {
                                        i = rnd.Next(0, 8);
                                        j = rnd.Next(0, (Battlefield.Ymax / 2) - 1 - 3);
                                    } while ((i % 2 == 0 && j == (Battlefield.Ymax / 2) - 1) && PosTwo[i, j] == 1);


                                    float Xd = (float)i + 1;
                                    float Yd = (j * 2) + 1.5f + 2;
                                    if (i % 2 != 0)
                                    {
                                        Yd -= 1;
                                    }
                                    if (pos > 3)
                                    {
                                        Xd = Xd + (float)Battlefield.Xmax - 10;
                                    }
                                    vec = new Vector2(X, Y);

                                    float distance = Vector2.DistanceSquared(new Vector2(Xd, Yd), lastvec);
                                    
                                    if (distance < lastdistance)
                                        vec = new Vector2(Xd, Yd);
                                    
                                    lastdistance = distance;
                                }
                            }
                            PosTwo[i, j] = 1;

                            /* 
                            0|0 => 0|3, 1|2, 1|4, 2|3
                            1|4 => 2|9, 3|8, 3|10, 4|9
                            2|7 => 4|15, 5|14, 5|16, 6|15
                             */

                            Pos[i * 2, j * 2 + 1] = 1;
                            Pos[i * 2 + 1, j * 2] = 1;
                            Pos[i * 2 + 1, j * 2 + 2] = 1;
                            Pos[i * 2 + 2, j * 2 + 1] = 1;

                            if (lastvec == Vector2.Zero)
                                lastvec = new Vector2(vec.X, vec.Y);
                        }
                    }


                    myunit.BuildPos = new Vector2(vec.X, vec.Y);
                    myunit.RealPos = myunit.BuildPos;
                    myunit.Pos = myunit.BuildPos;
                    myunit.SerPos = new Vector2Ser();
                    myunit.SerPos.x = myunit.BuildPos.X;
                    myunit.SerPos.y = myunit.BuildPos.Y;
                    myunit.RelPos = MoveService.GetRelPos(myunit.RealPos);                   
                }
            }
        }

        public static void Bot1TvZ_BB(double gameid, Player _player, Player _opp)
        {
            int[,] Pos = new int[10, Battlefield.Ymax];

            for (int i = 0; i < 10; i++)
                for (int j = 0; j < Battlefield.Ymax; j++)
                    Pos[i, j] = 0;




            int marines = 0;
            int reaper = 0;
            int marauder = 0;

            foreach (Unit unit in _player.Units.Where(x => x.Status == UnitStatuses.Spawned || x.Status == UnitStatuses.Placed))
            {
                if (unit.Name == "Marine")
                    marines++;
                else if (unit.Name == "Marauder")
                    marauder++;
                else if (unit.Name == "Reaper")
                    reaper++;
            }

            int zerglings = 0;
            int banelings = 0;
            int roaches = 0;
            int queens = 0;

            foreach (Unit unit in _opp.Units.Where(x => x.Status == UnitStatuses.Spawned))
            {
                if (unit.Name == "Zergling")
                    zerglings++;
                else if (unit.Name == "Baneling")
                    banelings++;
                else if (unit.Name == "Roach")
                    roaches++;
                else if (unit.Name == "Queen")
                    queens++;

                int x = (int)unit.BuildPos.X;
                if (x > 10)
                    x = x - Battlefield.Xmax + 10;
                Pos[x, (int)unit.BuildPos.Y] = unit.ID;
            }

            if (_player.Upgrades.Count() >= _opp.Upgrades.Count())
            {
                List<UnitUpgrades> list = new List<UnitUpgrades>(UpgradesTvZ);
                for (int i = 0; i < _opp.Upgrades.Count(); i++)
                    list.Remove(list.First());

                if (list.Count() > 0)
                {
                    UnitUpgrades upgrade = list.First();
                    if (_opp.Units.Where(x => x.AttacType == upgrade).Count() + _opp.Units.Where(x => x.ArmorType == upgrade).Count() > 25)
                        UpgradeUnit(_opp, upgrade);
                }
            }

            if (_player.AbilityUpgrades.Count() >= _opp.AbilityUpgrades.Count())
            {
                List<UnitAbilities> list = new List<UnitAbilities>(AbilityUpgradesTvZ);
                for (int i = 0; i < _opp.Upgrades.Count(); i++)
                    list.Remove(list.First());

                if (list.Count() > 0)
                {
                    UnitAbilities upgrade = list.First();
                    UnitAbility ability = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == upgrade).DeepCopy();
                    if (_opp.AbilityUpgrades.SingleOrDefault(x => x.Ability == ability.Ability) == null)
                    {
                        if (_opp.Units.Where(x => x.Abilities.SingleOrDefault(y => y.Ability == ability.Ability) != null).Count() > 15)
                            if (_opp.MineralsCurrent >= ability.Cost)
                            {
                                _opp.MineralsCurrent -= ability.Cost;
                                _opp.AbilityUpgrades.Add(ability);
                            }
                    }
                }
            }


            if (_opp.MineralsCurrent > 0)
                _opp.MineralsCurrent -= AddUnit(gameid, "Zergling", GetPos(Pos, _opp.Pos), _opp);
            if (_opp.MineralsCurrent > 0)
                _opp.MineralsCurrent -= AddUnit(gameid, "Baneling", GetPos(Pos, _opp.Pos), _opp);
            if (_opp.MineralsCurrent > 0)
                _opp.MineralsCurrent -= AddUnit(gameid, "Roach", GetPos(Pos, _opp.Pos), _opp);


            if (marines >= marauder && marauder >= reaper)
            {
                while (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Baneling", GetPos(Pos, _opp.Pos), _opp);
            }
            else if (reaper >= marines && marines >= marauder)
            {
                while (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Roach", GetPos(Pos, _opp.Pos), _opp);
            }
            else if (marauder >= marines && marines >= reaper)
            {
                while (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Zergling", GetPos(Pos, _opp.Pos), _opp);
            }
            else
            {
                while (_opp.MineralsCurrent > 0)
                {
                    if (_opp.MineralsCurrent > 0)
                        _opp.MineralsCurrent -= AddUnit(gameid, "Zergling", GetPos(Pos, _opp.Pos), _opp);
                    if (_opp.MineralsCurrent > 0)
                        _opp.MineralsCurrent -= AddUnit(gameid, "Baneling", GetPos(Pos, _opp.Pos), _opp);
                    if (_opp.MineralsCurrent > 0)
                        _opp.MineralsCurrent -= AddUnit(gameid, "Roach", GetPos(Pos, _opp.Pos), _opp);
                }
            }
        }


        public static void Bot1TvZ(double gameid, Player _player, Player _opp)
        {
            int[,] Pos = new int[10, Battlefield.Ymax];

            for (int i = 0; i < 10; i++)
                for (int j = 0; j < Battlefield.Ymax; j++)
                    Pos[i, j] = 0;




            int marines = 0;
            int reaper = 0;
            int marauder = 0;

            foreach (Unit unit in _player.Units.Where(x => x.Status == UnitStatuses.Spawned))
            {
                if (unit.Name == "Marine")
                    marines++;
                else if (unit.Name == "Marauder")
                    marauder++;
                else if (unit.Name == "Reaper")
                    reaper++;
            }

            int zerglings = 0;
            int banelings = 0;
            int roaches = 0;
            int queens = 0;

            foreach (Unit unit in _opp.Units.Where(x => x.Status == UnitStatuses.Spawned))
            {
                if (unit.Name == "Zergling")
                    zerglings++;
                else if (unit.Name == "Baneling")
                    banelings++;
                else if (unit.Name == "Roach")
                    roaches++;
                else if (unit.Name == "Queen")
                    queens++;

                int x = (int)unit.BuildPos.X;
                if (x > 10)
                    x = x - Battlefield.Xmax + 10;
                Pos[x, (int)unit.BuildPos.Y] = unit.ID;
            }

            if (_player.Upgrades.Count() >= _opp.Upgrades.Count())
            {
                List<UnitUpgrades> list = new List<UnitUpgrades>(UpgradesTvZ);
                for (int i = 0; i < _opp.Upgrades.Count(); i++)
                    list.Remove(list.First());

                if (list.Count() > 0)
                {
                    UnitUpgrades upgrade = list.First();
                    if (_opp.Units.Where(x => x.AttacType == upgrade).Count() + _opp.Units.Where(x => x.ArmorType == upgrade).Count() > 25)
                        UpgradeUnit(_opp, upgrade);
                }
            }

            if (_player.AbilityUpgrades.Count() >= _opp.AbilityUpgrades.Count())
            {
                List<UnitAbilities> list = new List<UnitAbilities>(AbilityUpgradesTvZ);
                for (int i = 0; i < _opp.Upgrades.Count(); i++)
                    list.Remove(list.First());

                if (list.Count() > 0)
                {
                    UnitAbilities upgrade = list.First();
                    UnitAbility ability = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == upgrade).DeepCopy();
                    if (_opp.AbilityUpgrades.SingleOrDefault(x => x.Ability == ability.Ability) == null)
                    {
                        if (_opp.Units.Where(x => x.Abilities.SingleOrDefault(y => y.Ability == ability.Ability) != null).Count() > 15)
                            if (_opp.MineralsCurrent >= ability.Cost)
                            {
                                _opp.MineralsCurrent -= ability.Cost;
                                _opp.AbilityUpgrades.Add(ability);
                            }
                    }
                }
            }


            if (_opp.MineralsCurrent > 0)
                _opp.MineralsCurrent -= AddUnit(gameid, "Zergling", GetPos(Pos, _opp.Pos), _opp);
            if (_opp.MineralsCurrent > 0)
                _opp.MineralsCurrent -= AddUnit(gameid, "Baneling", GetPos(Pos, _opp.Pos), _opp);
            if (_opp.MineralsCurrent > 0)
                _opp.MineralsCurrent -= AddUnit(gameid, "Roach", GetPos(Pos, _opp.Pos), _opp);


            if (zerglings <= 1)
            {
                while (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Zergling", GetPos(Pos, _opp.Pos), _opp);
            }else if (queens < 5) 
            {
                while (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Queen", GetPos(Pos, _opp.Pos), _opp);
            }
            else if (marines >= marauder && marauder >= reaper)
            {
                while (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Baneling", GetPos(Pos, _opp.Pos), _opp);
            } else if (reaper >= marines && marines >= marauder) {
                while (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Roach", GetPos(Pos, _opp.Pos), _opp);
            } else if (marauder >= marines && marines >= reaper)
            {
                while (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Zergling", GetPos(Pos, _opp.Pos), _opp);
            } else
            {
                while (_opp.MineralsCurrent > 0)
                {
                    if (_opp.MineralsCurrent > 0)
                        _opp.MineralsCurrent -= AddUnit(gameid, "Zergling", GetPos(Pos, _opp.Pos), _opp);
                    if (_opp.MineralsCurrent > 0)
                        _opp.MineralsCurrent -= AddUnit(gameid, "Baneling", GetPos(Pos, _opp.Pos), _opp);
                    if (_opp.MineralsCurrent > 0)
                        _opp.MineralsCurrent -= AddUnit(gameid, "Roach", GetPos(Pos, _opp.Pos), _opp);
                }
            }
        }

        public static void Bot1ZvT(double gameid, Player _player, Player _opp)
        {
            int marines = 0;
            int reaper = 0;
            int marauder = 0;
            int[,] Pos = new int[10, Battlefield.Ymax];

            for (int i = 0; i < 10; i++)
                for (int j = 0; j < Battlefield.Ymax; j++)
                    Pos[i, j] = 0;

            foreach (Unit unit in _opp.Units.Where(x => x.Status == UnitStatuses.Spawned))
            {
                if (unit.Name == "Marine")
                    marines++;
                else if (unit.Name == "Marauder")
                    marauder++;
                else if (unit.Name == "Reaper")
                    reaper++;

                int x = (int)unit.BuildPos.X;
                if (x > 10)
                    x = x - Battlefield.Xmax + 10;
                Pos[x, (int)unit.BuildPos.Y] = unit.ID;
            }

            int zerglings = 0;
            int banelings = 0;
            int roaches = 0;
            int queens = 0;

            
            foreach (Unit unit in _player.Units.Where(x => x.Status == UnitStatuses.Spawned))
            {
                if (unit.Name == "Zergling")
                    zerglings++;
                else if (unit.Name == "Baneling")
                    banelings++;
                else if (unit.Name == "Roach")
                    roaches++;
                else if (unit.Name == "Queen")
                    queens++;
            }

            if (_player.Upgrades.Count() >= _opp.Upgrades.Count())
            {
                List<UnitUpgrades> list = new List<UnitUpgrades>(UpgradesZvT);
                for (int i = 0; i < _opp.Upgrades.Count(); i++)
                    list.Remove(list.First());

                if (list.Count() > 0)
                {
                    UnitUpgrades upgrade = list.First();
                    if (_opp.Units.Where(x => x.AttacType == upgrade).Count() + _opp.Units.Where(x => x.ArmorType == upgrade).Count() > 10)
                        UpgradeUnit(_opp, upgrade);
                }
                else
                {
                    // TODO: Ability upgrades
                }
            }

            if (_player.AbilityUpgrades.Count() >= _opp.AbilityUpgrades.Count())
            {
                List<UnitAbilities> list = new List<UnitAbilities>(AbilityUpgradesZvT);
                for (int i = 0; i < _opp.Upgrades.Count(); i++)
                    list.Remove(list.First());

                if (list.Count() > 0)
                {
                    UnitAbilities upgrade = list.First();
                    UnitAbility ability = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == upgrade).DeepCopy();
                    if (_opp.AbilityUpgrades.SingleOrDefault(x => x.Ability == ability.Ability) == null)
                    {
                        if (_opp.Units.Where(x => x.Abilities.SingleOrDefault(y => y.Ability == ability.Ability) != null).Count() > 12)
                            if (_opp.MineralsCurrent >= ability.Cost)
                            {
                                _opp.MineralsCurrent -= ability.Cost;
                                _opp.AbilityUpgrades.Add(ability);
                            }
                    }
                }
                else
                {
                    // TODO: Ability upgrades
                }
            }

            if (_opp.MineralsCurrent > 0)
                _opp.MineralsCurrent -= AddUnit(gameid, "Marine", GetPos(Pos, _opp.Pos), _opp);
            if (_opp.MineralsCurrent > 0)
                _opp.MineralsCurrent -= AddUnit(gameid, "Marauder", GetPos(Pos, _opp.Pos), _opp);
            if (_opp.MineralsCurrent > 0 && reaper < _opp.Units.Count() / 10)
                _opp.MineralsCurrent -= AddUnit(gameid, "Reaper", GetPos(Pos, _opp.Pos), _opp);

            if (marines <= 1)
            {
                while (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Marine", GetPos(Pos, _opp.Pos), _opp);
            }
            else if (queens >= roaches && roaches >= zerglings && zerglings > banelings)
            {
                while (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Marine", GetPos(Pos, _opp.Pos), _opp);
            }
            else if (roaches >= zerglings && queens >= banelings)
            {
                while (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Marauder", GetPos(Pos, _opp.Pos), _opp);
            }
            else if (zerglings + banelings > ((queens + roaches) * 6))
            {
                if (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Reaper", GetPos(Pos, _opp.Pos), _opp);
            }

            while (_opp.MineralsCurrent > 0)
            {
                if (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Marine", GetPos(Pos, _opp.Pos), _opp);
                if (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Marine", GetPos(Pos, _opp.Pos), _opp);
                if (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Marine", GetPos(Pos, _opp.Pos), _opp);
                if (_opp.MineralsCurrent > 0)
                    _opp.MineralsCurrent -= AddUnit(gameid, "Marauder", GetPos(Pos, _opp.Pos), _opp);
            }
        }

        public static Vector2 GetPos (int[,] Pos, int playerpos)
        {
            int mod = 0;
            if (playerpos > 3)
                mod = Battlefield.Xmax - 10;

            for (int i = 0; i < 10; i++)
                for (int j = 0; j < Battlefield.Ymax; j++)
                    if (Pos[i, j] == 0)
                    {
                        Pos[i, j] = 1;
                        return new Vector2(i + mod, j);
                    }

            return new Vector2(5 + mod, Battlefield.Ymax / 2);
        }

        public static Vector2 GetPos_deprecated(Dictionary<float, List<float>> Pos)
        {
            float x = Pos.OrderBy(o => o.Key).First().Key;
            float y = 1;
            if (Pos[x].Count() > 0)
                y = Pos[x].OrderBy(o => o).Last() + 1;

            if (y == Battlefield.Ymax)
            {
                if (Pos.First().Key == 1)
                    x += 1;
                else
                    x -= 1;
                y = 1;
                Pos[x] = new List<float>();
                Pos[x].Add(1);
            }
            else
                Pos[x].Add(y);

            return new Vector2(x, y);
        }

        public static int AddUnit(double gameid, string unitname, Vector2 pos, Player opp)
        {
            //Unit u = new Unit(unitname, _player.Pos, pos);
            Unit u = UnitPool.Units.SingleOrDefault(x => x.Name == unitname).DeepCopy();
            u.BuildPos = pos;
            u.Owner = opp.Pos;
            u.Status = UnitStatuses.Spawned;
            u.ID = UnitID.GetID(gameid);
            u.SerPos = new Vector2Ser();
            u.SerPos.x = u.BuildPos.X;
            u.SerPos.y = u.BuildPos.Y;
            u.Ownerplayer = opp;
            if (u.Bonusdamage != null)
                u.Bonusdamage.Ownerplayer = u.Ownerplayer;
            opp.Units.Add(u);
            return u.Cost;
        }

        public static void UpgradeUnit(Player _player, UnitUpgrades upgrade)
        {
            (int cost, int lvl) = GetUpgradeCost(_player, upgrade);
            _player.MineralsCurrent -= cost;

            Upgrade myupgrade = UpgradePool.Upgrades.Where(x => x.Race == _player.Race && x.Name == upgrade).FirstOrDefault();
            if (myupgrade == null) return;

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
        }

        public static void AbilityUpgradeUnit(UnitAbility ability, Player _player)
        {
            _player.MineralsCurrent -= ability.Cost;
            _player.AbilityUpgrades.Add(ability);

            if (ability.Type.Contains(UnitAbilityTypes.Image))
                foreach (Unit unit in _player.Units.Where(x => x.Abilities.SingleOrDefault(y => y.Ability == ability.Ability) != null))
                    unit.Image = ability.Image;
        }

        public static (int, int) GetUpgradeCost(Player _player, UnitUpgrades upgrade)
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
    }
}
