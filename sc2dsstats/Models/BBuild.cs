using paxgame3.Client.Data;
using paxgame3.Client.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace paxgame3.Client.Models
{
    [Serializable]
    public class BBuild
    {
        public HashSet<UnitAbilities> Abilities { get; set; } = new HashSet<UnitAbilities>();
        public HashSet<UnitUpgrades> Upgrades { get; set; } = new HashSet<UnitUpgrades>();
        public List<KeyValuePair<int, int>> UpgradesLevel { get; set; } = new List<KeyValuePair<int, int>>();
        public List<KeyValuePair<int, int>> Units { get; set; } = new List<KeyValuePair<int, int>>();
        public List<KeyValuePair<int, Vector2Ser>> Position { get; set; } = new List<KeyValuePair<int, Vector2Ser>>();
        public string Name { get; set; }
        public int Pos { get; set; }
        public int Race { get; set; }
        public int MineralsCurrent { get; set; }
        public int Tier { get; set; } = 1;

        public BBuild()
        {

        }

        public BBuild(Player player) : this()
        {
            this.GetBuild(player).GetAwaiter().GetResult();
        }

        ///<summary>
        ///Restore player from this
        ///</summary>
        public async Task<Player> SetBuild(Player pl)
        {
            pl.AbilityUpgrades.Clear();
            pl.Upgrades.Clear();
            pl.Units.Clear();
            pl.Units.AddRange(UnitPool.Units.Where(x => x.Race == pl.Race && x.Cost > 0));
            pl.Name = Name;
            pl.Pos = Pos;
            pl.Race = (UnitRace)Race;
            pl.MineralsCurrent = MineralsCurrent;
            pl.Tier = Tier;

            foreach (var ent in Abilities)
                pl.AbilityUpgrades.Add(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == ent).DeepCopy());

            foreach (var ent in Upgrades)
            {
                UnitUpgrade upgrade = new UnitUpgrade();
                upgrade.Upgrade = ent;
                upgrade.Level = UpgradesLevel.Where(x => x.Key == UpgradePool.Upgrades.SingleOrDefault(y => y.Race == pl.Race && y.Name == ent).ID).Last().Value;
                pl.Upgrades.Add(upgrade);
            }

            foreach (var ent in Units)
            {
                for (int i = 0; i < ent.Value; i++)
                {
                    Unit unit = UnitPool.Units.SingleOrDefault(x => x.ID == ent.Key).DeepCopy();
                    Vector2Ser vec = Position.Where(x => x.Key == unit.ID).ToList().ElementAt(i).Value;
                    unit.BuildPos = new System.Numerics.Vector2(vec.x, vec.y);
                    unit.RealPos = unit.BuildPos;
                    unit.Pos = unit.BuildPos;
                    unit.SerPos = new Vector2Ser();
                    unit.SerPos.x = unit.BuildPos.X;
                    unit.SerPos.y = unit.BuildPos.Y;
                    unit.RelPos = MoveService.GetRelPos(unit.RealPos);
                    unit.ID = UnitID.GetID(pl.Game.ID);
                    unit.Status = UnitStatuses.Spawned;
                    unit.Owner = pl.Pos;
                    unit.Ownerplayer = pl;
                    if (unit.Bonusdamage != null)
                        unit.Bonusdamage.Ownerplayer = pl;
                    pl.Units.Add(unit);
                }
            }



            return pl;
        }

        ///<summary>
        ///Save Player to this
        ///</summary>
        public async Task<BBuild> GetBuild(Player pl)
        {

            Abilities = new HashSet<UnitAbilities>();
            Upgrades = new HashSet<UnitUpgrades>();
            UpgradesLevel = new List<KeyValuePair<int, int>>();
            Units = new List<KeyValuePair<int, int>>();
            Position = new List<KeyValuePair<int, Vector2Ser>>();


            Dictionary<int, int> bunits = new Dictionary<int, int>();
            foreach (Unit unit in pl.Units.Where(x => x.Status == UnitStatuses.Spawned || x.Status == UnitStatuses.Placed))
            {
                Unit defaultunit = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name);
                if (defaultunit == null)
                    defaultunit = UnitPool.Units.SingleOrDefault(x => x.Name == "NA");
                int id = defaultunit.ID;
                if (!bunits.ContainsKey(id))
                    bunits[id] = 1;
                else
                    bunits[id]++;

                Vector2Ser vec = new Vector2Ser();
                vec.x = unit.BuildPos.X;
                vec.y = unit.BuildPos.Y;
                this.Position.Add(new KeyValuePair<int, Vector2Ser>(id, vec));
            }

            foreach (var ent in bunits)
                this.Units.Add(new KeyValuePair<int, int>(ent.Key, ent.Value));

            foreach (UnitAbility ability in pl.AbilityUpgrades)
                this.Abilities.Add(ability.Ability);

            foreach (UnitUpgrade upgrade in pl.Upgrades)
            {
                this.Upgrades.Add(upgrade.Upgrade);
                this.UpgradesLevel.Add(new KeyValuePair<int, int>(UpgradePool.Upgrades.SingleOrDefault(x => x.Race == pl.Race && x.Name == upgrade.Upgrade).ID, upgrade.Level));
            }

            this.Name = pl.Name;
            this.Pos = pl.Pos;
            this.Race = (int)pl.Race;
            this.MineralsCurrent = pl.MineralsCurrent;
            this.Tier = pl.Tier;
            return this;
        }

        public string GetString(Player pl)
        {
            string build = "";
            foreach (Unit unit in pl.Units.Where(x => x.Status == UnitStatuses.Spawned || x.Status == UnitStatuses.Placed))
            {
                Unit defaultunit = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name);
                if (defaultunit == null)
                    defaultunit = UnitPool.Units.SingleOrDefault(x => x.Name == "NA");
                int id = defaultunit.ID;

                Vector2 pos = new Vector2(unit.BuildPos.X, unit.BuildPos.Y);
                if (pl.Pos > 3)
                    pos = BBService.mirrorImage(unit.BuildPos);

                build += String.Format("{0}|{1}|{2},", id, pos.X, pos.Y);
            }

            //for (int i = 0; i < 50 - pl.Units.Count; i++)
            //    build += String.Format("{0}|{1}|{2},", 0, 0, 0);

            foreach (UnitUpgrade upgrade in pl.Upgrades)
            {
                build += String.Format("{0}|{1},", (int)upgrade.Upgrade, upgrade.Level);
            }

            //for (int i = 0; i < 5 - pl.Upgrades.Count; i++)
            //    build += String.Format("{0}|{1},", 0, 0);

            foreach (UnitAbility ability in pl.AbilityUpgrades)
            {
                build += String.Format("{0},", (int)ability.Ability);
            }

            //for (int i = 0; i < 5 - pl.AbilityUpgrades.Count; i++)
            //    build += String.Format("{0},", 0);

            if (build.Any())
                build = build.Remove(build.Length - 1, 1);

            return build;
        }

        public void SetString(string build, Player pl)
        {
            if (build == null)
                return;

            pl.Units.Clear();
            pl.Upgrades.Clear();
            pl.AbilityUpgrades.Clear();

            var ents = build.Split(",");
            foreach (var ent in ents)
            {

                if (ent.Count(x => x == '|') > 1)
                {
                    var unitents = ent.Split('|');
                    Unit unit = UnitPool.Units.SingleOrDefault(x => x.ID == int.Parse(unitents[0]));
                    if (unit != null)
                    {
                        Unit myunit = unit.DeepCopy();
                        myunit.BuildPos = new System.Numerics.Vector2(float.Parse(unitents[1]), float.Parse(unitents[2]));
                        if (pl.Pos > 3)
                            myunit.BuildPos = BBService.mirrorImage(myunit.BuildPos);
                        myunit.RealPos = unit.BuildPos;
                        myunit.Pos = unit.BuildPos;
                        myunit.SerPos = new Vector2Ser();
                        myunit.SerPos.x = unit.BuildPos.X;
                        myunit.SerPos.y = unit.BuildPos.Y;
                        myunit.RelPos = MoveService.GetRelPos(unit.RealPos);
                        myunit.ID = UnitID.GetID(pl.Game.ID);
                        myunit.Status = UnitStatuses.Spawned;
                        myunit.Owner = pl.Pos;
                        myunit.Ownerplayer = pl;
                        if (myunit.Bonusdamage != null)
                            myunit.Bonusdamage.Ownerplayer = pl;
                        pl.Units.Add(myunit);
                    }
                }
                else if (ent.Count(x => x == '|') == 1)
                {
                    var upgradeents = ent.Split('|');
                    UnitUpgrade upgrade = new UnitUpgrade();
                    upgrade.Upgrade = (UnitUpgrades)int.Parse(upgradeents[0]);
                    upgrade.Level = int.Parse(upgradeents[1]);
                    pl.Upgrades.Add(upgrade);
                }
                else
                {
                    try
                    {
                        pl.AbilityUpgrades.Add(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == (UnitAbilities)int.Parse(ent)).DeepCopy());
                    }
                    catch { }
                }
            }
        }

        public int[][][] GetMatrix(Player pl)
        {
            int[][][] build = new int[4][][];
            for (int i = 0; i < 4; i++)
            {
                build[i] = new int[20][];
                for (int j = 0; j < 20; j++)
                    build[i][j] = new int[60];
            }

            foreach (Unit unit in pl.Units.Where(x => x.Status == UnitStatuses.Spawned || x.Status == UnitStatuses.Placed))
            {
                Unit defaultunit = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name);
                if (defaultunit == null)
                    defaultunit = UnitPool.Units.SingleOrDefault(x => x.Name == "NA");
                int id = defaultunit.ID;

                Vector2 pos = new Vector2(unit.BuildPos.X, unit.BuildPos.Y);
                if (pl.Pos > 3)
                    pos = BBService.mirrorImage(unit.BuildPos);

                build[id][(int)(pos.X * 2)][(int)(pos.Y * 2)] = 1;
            }

            foreach (UnitUpgrade upgrade in pl.Upgrades)
            {
                build[0][(int)upgrade.Upgrade][upgrade.Level] = 1;
            }

            foreach (UnitAbility ability in pl.AbilityUpgrades)
            {
                build[0][0][(int)ability.Ability] = 1;
            }

            return build;
        }


        public void SetMatrix(int[][][] build, Player pl)
        {
            if (build == null)
                return;

            pl.Units.Clear();
            pl.Upgrades.Clear();
            pl.AbilityUpgrades.Clear();

            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 20; y++)
                    for (int z = 0; z < 60; z++)
                    {
                        if (build[x][y][z] == 1)
                        {
                            if (x > 0)
                            {
                                Unit unit = UnitPool.Units.SingleOrDefault(s => s.ID == x);
                                if (unit != null)
                                {
                                    Unit myunit = unit.DeepCopy();
                                    myunit.BuildPos = new System.Numerics.Vector2((float)y / 2, (float)z / 2);
                                    if (pl.Pos > 3)
                                        myunit.BuildPos = BBService.mirrorImage(myunit.BuildPos);
                                    myunit.RealPos = unit.BuildPos;
                                    myunit.Pos = unit.BuildPos;
                                    myunit.SerPos = new Vector2Ser();
                                    myunit.SerPos.x = unit.BuildPos.X;
                                    myunit.SerPos.y = unit.BuildPos.Y;
                                    myunit.RelPos = MoveService.GetRelPos(unit.RealPos);
                                    myunit.ID = UnitID.GetID(pl.Game.ID);
                                    myunit.Status = UnitStatuses.Spawned;
                                    myunit.Owner = pl.Pos;
                                    myunit.Ownerplayer = pl;
                                    if (myunit.Bonusdamage != null)
                                        myunit.Bonusdamage.Ownerplayer = pl;
                                    pl.Units.Add(myunit);
                                }
                            }
                            else if (x == 0 && y > 0)
                            {
                                UnitUpgrade upgrade = new UnitUpgrade();
                                upgrade.Upgrade = (UnitUpgrades)y;
                                upgrade.Level = z;
                                pl.Upgrades.Add(upgrade);
                            }
                            else
                            {
                                pl.AbilityUpgrades.Add(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == (UnitAbilities)z));
                            }
                        }
                    }
        }

        public static string PrintMatrix(int[][][] build)
        {
            string m = "";
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 20; y++)
                    for (int z = 0; z < 60; z++)
                    {
                        if (build[x][y][z] == 1)
                            m += "1";
                        else
                            m += "0";

                        m += ",";
                    }
            if (m.Any())
                m = m.Remove(m.Length - 1);
            return m;
        }

        public static string MatrixHeader()
        {
            string h = "RESULT,";
            int i = 0;
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 20; y++)
                    for (int z = 0; z < 60; z++)
                    {
                        i++;
                        h += "CELL" + i + "M,";
                    }
            if (h.Any())
                h = h.Remove(h.Length - 1);
            return h;
        }
    }

    public class BBuildJob
    {
        public BBuild PlayerBuild { get; set; }
        public BBuild OppBuild { get; set; }
    }

    public class RESTResult
    {
        public int Result { get; set; }
        public double DamageP1 { get; set; }
        public double MinValueP1 { get; set; }
        public double DamageP2 { get; set; }
        public double MinValueP2 { get; set; }
    }
}
