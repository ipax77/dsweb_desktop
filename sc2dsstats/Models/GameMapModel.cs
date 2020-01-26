using paxgamelib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.Models
{
    public class GameMapModel
    {
        public int ReplayID { get; set; } = 0;
        public Dictionary<int, List<Unit>> Spawns = new Dictionary<int, List<Unit>>();
        public Dictionary<int, HashSet<int>> plSpawns = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, Dictionary<int, List<UnitUpgrade>>> Upgrades = new Dictionary<int, Dictionary<int, List<UnitUpgrade>>>();
        public Dictionary<int, Dictionary<int, List<UnitAbility>>> AbilityUpgrades = new Dictionary<int, Dictionary<int, List<UnitAbility>>>();
    }
}
