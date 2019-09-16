using System;
using System.Collections.Generic;
using pax.s2decode.Models;

namespace sc2dsstats.Models
{
    [Serializable]
    public class ReplayDetails
    {
        public List<KeyValuePair<int, int>> MIDDLE { get; set; } = new List<KeyValuePair<int, int>>();
        public List<PlayerDetails> PLAYERS { get; set; } = new List<PlayerDetails>();
    }

    [Serializable]
    public class PlayerDetails
    {
        public int REALPOS { get; set; }
        public Dictionary<int, Dictionary<string, int>> SPAWNS { get; set; } = new Dictionary<int, Dictionary<string, int>>();
        public Dictionary<int, M_stats> STATS { get; set; } = new Dictionary<int, M_stats>();
    }
}
