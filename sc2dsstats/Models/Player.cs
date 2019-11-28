using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace paxgame3.Client.Models
{
    [Serializable]
    public class Player
    {
        public double ID { get; set; } = 0;
        public string Name { get; set; } = "";
        public string AuthName { get; set; }
        public int Pos { get; set; }
        public UnitRace Race { get; set; } = UnitRace.Terran;
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public List<Unit> Units { get; set; } = new List<Unit>();
        public int Tier { get; set; } = 1;
        public int MineralsCurrent { get; set; }
        public List<UnitUpgrade> Upgrades { get; set; } = new List<UnitUpgrade>();
        public List<UnitAbility> AbilityUpgrades { get; set; } = new List<UnitAbility>();
        public HashSet<UnitAbilities> AbilitiesDeactivated { get; set; } = new HashSet<UnitAbilities>();
        public bool inGame { get; set; } = false;
        public double GameID { get; set; }
        public BBuild LastSpawn { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public GameHistory Game { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public GameMode Mode { get; set; } = new GameMode();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public Dictionary<int, M_stats> Stats { get; set; } = new Dictionary<int, M_stats>();

        public Player()
        {

        }

        public Player Deepcopy()
        {
            Player pl = new Player();
            pl.ID = ID;
            pl.Name = Name;
            pl.AuthName = AuthName;
            pl.Pos = Pos;
            pl.Race = Race;
            pl.Units = new List<Unit>(Units);
            pl.Tier = Tier;
            pl.MineralsCurrent = MineralsCurrent;
            pl.Upgrades = new List<UnitUpgrade>(Upgrades);
            pl.AbilityUpgrades = new List<UnitAbility>(AbilityUpgrades);
            // no ability deactivated copy ..
            pl.inGame = inGame;
            pl.GameID = GameID;
            pl.LastSpawn = LastSpawn;
            pl.Game = Game; // no deepcopy
            pl.Mode = Mode; // no deepcopy
            pl.Stats = new Dictionary<int, M_stats>(Stats);

            return pl;
        }
    }
}
