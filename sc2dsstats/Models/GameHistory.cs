using paxgame3.Client.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace paxgame3.Client.Models
{
    [Serializable]
    public class GameHistory
    {
        public double ID { get; set; }
        public int Spawn { get; set; } = 1;
        public List<Player> Players { get; set; } = new List<Player>();
        public Battlefield battlefield { get; set; }
        public List<List<BBuild>> Spawns { get; set; } = new List<List<BBuild>>();
        public DateTime Gametime { get; set; } = DateTime.UtcNow;
        public Version Version { get; set; } = new Version("0.0.1");
        public int UnitID = 0;
        public GameMode Mode { get; set; }
        public List<StatsRound> Stats { get; set; } = new List<StatsRound>();
        [JsonIgnore]
        public string Style { get; set; }
        [JsonIgnore]
        public List<Unit> Units { get; set; }
        [JsonIgnore]
        public List<KeyValuePair<float, float>> Health { get; set; }

        public GameHistory()
        {
        }

        public object ShallowCopy()
        {
            return this.MemberwiseClone();
        }

        public async Task SaveSpawn()
        {
            Spawns.Add(new List<BBuild>());
            foreach (var pl in Players.OrderBy(o => o.Pos))
            {
                BBuild build = new BBuild();
                await build.GetBuild(pl);
                Spawns.Last().Add(build);
            }
        }

        [Serializable]
        public class GameSpawn
        {
            public int ID { get; set; }
            public int PlayerPos { get; set; }
            public int Spawn { get; set; }
            public List<UnitBase> Units { get; set; } = new List<UnitBase>();

            public GameSpawn()
            {

            }

            public GameSpawn(int plpos, int spawn) : this()
            {
                PlayerPos = plpos;
                Spawn = spawn;
            }

        }
    }
}
