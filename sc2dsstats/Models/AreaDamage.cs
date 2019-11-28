using paxgame3.Client.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sc2dsstats.Data;

namespace paxgame3.Client.Models
{
    [Serializable]
    public class AreaDamage
    {
        public float Distance1 { get; set; } = 0.4687f / StartUp.Battlefieldmodifier;
        public float Distance2 { get; set; } = 0.7812f / StartUp.Battlefieldmodifier;
        public float Distance3 { get; set; } = 1.25f / StartUp.Battlefieldmodifier;
        public bool FriendlyFire = false;

        public object Shallowcopy()
        {
            return this.MemberwiseClone();
        }
    }
}
