﻿using paxgame3.Client.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace paxgame3.Client.Data
{
    public static class UpgradePool
    {
        public static List<Upgrade> Upgrades = new List<Upgrade>();

        public static void Init()
        {
            //Upgrades = JsonSerializer.Deserialize<List<Upgrade>>(File.ReadAllText("/data/upgrades.json"));
            Build();
        }


        public static void Build()
        {

            Upgrade u1 = new Upgrade();
            u1.Name = UnitUpgrades.GroundArmor;
            u1.Race = UnitRace.Terran;
            KeyValuePair<int, int> lvl1 = new KeyValuePair<int, int>(1, 125);
            KeyValuePair<int, int> lvl2 = new KeyValuePair<int, int>(2, 175);
            KeyValuePair<int, int> lvl3 = new KeyValuePair<int, int>(3, 250);
            u1.Cost.Add(lvl1);
            u1.Cost.Add(lvl2);
            u1.Cost.Add(lvl3);
            u1.ID = 1;

            Upgrade u2 = new Upgrade();
            u2.Name = UnitUpgrades.GroundAttac;
            u2.Race = UnitRace.Terran;
            KeyValuePair<int, int> lvl11 = new KeyValuePair<int, int>(1, 125);
            KeyValuePair<int, int> lvl22 = new KeyValuePair<int, int>(2, 175);
            KeyValuePair<int, int> lvl33 = new KeyValuePair<int, int>(3, 250);
            u2.Cost.Add(lvl11);
            u2.Cost.Add(lvl22);
            u2.Cost.Add(lvl33);
            u2.ID = 2;

            Upgrade u3 = new Upgrade();
            u3.Name = UnitUpgrades.GroundMeleeAttac;
            u3.Race = UnitRace.Zerg;
            KeyValuePair<int, int> lvl111 = new KeyValuePair<int, int>(1, 100);
            KeyValuePair<int, int> lvl222 = new KeyValuePair<int, int>(2, 150);
            KeyValuePair<int, int> lvl333 = new KeyValuePair<int, int>(3, 225);
            u3.Cost.Add(lvl111);
            u3.Cost.Add(lvl222);
            u3.Cost.Add(lvl333);
            u3.ID = 10;

            Upgrade u25 = new Upgrade();
            u25.Name = UnitUpgrades.GroundAttac;
            u25.Race = UnitRace.Zerg;
            KeyValuePair<int, int> lvl251 = new KeyValuePair<int, int>(1, 125);
            KeyValuePair<int, int> lvl252 = new KeyValuePair<int, int>(2, 175);
            KeyValuePair<int, int> lvl253 = new KeyValuePair<int, int>(3, 250);
            u25.Cost.Add(lvl111);
            u25.Cost.Add(lvl222);
            u25.Cost.Add(lvl333);
            u25.ID = 11;

            Upgrade u4 = new Upgrade();
            u4.Name = UnitUpgrades.GroundArmor;
            u4.Race = UnitRace.Zerg;
            KeyValuePair<int, int> lvl01 = new KeyValuePair<int, int>(1, 150);
            KeyValuePair<int, int> lvl02 = new KeyValuePair<int, int>(2, 175);
            KeyValuePair<int, int> lvl03 = new KeyValuePair<int, int>(3, 250);
            u4.Cost.Add(lvl01);
            u4.Cost.Add(lvl02);
            u4.Cost.Add(lvl03);
            u4.ID = 12;

            Upgrade p1 = new Upgrade();
            p1.Name = UnitUpgrades.GroundArmor;
            p1.Race = UnitRace.Protoss;
            KeyValuePair<int, int> plvl1 = new KeyValuePair<int, int>(1, 100);
            KeyValuePair<int, int> plvl2 = new KeyValuePair<int, int>(2, 150);
            KeyValuePair<int, int> plvl3 = new KeyValuePair<int, int>(3, 200);
            p1.Cost.Add(plvl1);
            p1.Cost.Add(plvl2);
            p1.Cost.Add(plvl3);
            p1.ID = 20;

            Upgrade p2 = new Upgrade();
            p2.Name = UnitUpgrades.GroundAttac;
            p2.Race = UnitRace.Protoss;
            KeyValuePair<int, int> p2lvl1 = new KeyValuePair<int, int>(1, 150);
            KeyValuePair<int, int> p2lvl2 = new KeyValuePair<int, int>(2, 175);
            KeyValuePair<int, int> p2lvl3 = new KeyValuePair<int, int>(3, 250);
            p2.Cost.Add(p2lvl1);
            p2.Cost.Add(p2lvl2);
            p2.Cost.Add(p2lvl3);
            p2.ID = 21;

            Upgrade p3 = new Upgrade();
            p3.Name = UnitUpgrades.ShieldArmor;
            p3.Race = UnitRace.Protoss;
            KeyValuePair<int, int> p3lvl1 = new KeyValuePair<int, int>(1, 150);
            KeyValuePair<int, int> p3lvl2 = new KeyValuePair<int, int>(2, 175);
            KeyValuePair<int, int> p3lvl3 = new KeyValuePair<int, int>(3, 250);
            p3.Cost.Add(p3lvl1);
            p3.Cost.Add(p3lvl2);
            p3.Cost.Add(p3lvl3);
            p3.ID = 22;

            Upgrades.Add(u1);
            Upgrades.Add(u2);
            Upgrades.Add(u3);
            Upgrades.Add(u4);
            Upgrades.Add(p1);
            Upgrades.Add(p2);
            Upgrades.Add(p3);

            JsonSerializerOptions opt = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            //var json = JsonSerializer.Serialize(Upgrades, opt);
            //File.WriteAllText("/data/upgrades.json", json);

        }
    }


}
