using System.Collections.Generic;

namespace paxgame3.Client.Models
{
    public class BuildUnitContainer
    {
        public List<Unit> UnitsAvailable { get; set; } = new List<Unit>();
        public List<Unit> UnitsPlaced { get; set; } = new List<Unit>();
        public Unit Payload { get; set; }
    }
}
