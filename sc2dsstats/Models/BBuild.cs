using paxgame3.Client.Data;
using paxgame3.Client.Service;
using System;
using System.Collections.Generic;
using System.Linq;
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

        ///<summary>
        ///Restore player from this
        ///</summary>
        public async Task<Player> SetBuild(Player pl)
        {
            pl.AbilityUpgrades.Clear();
            pl.Upgrades.Clear();
            pl.Units.Clear();

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
            pl.Units.AddRange(UnitPool.Units.Where(x => x.Race == pl.Race && x.Cost > 0));
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
            return this;
        }
    }
}
