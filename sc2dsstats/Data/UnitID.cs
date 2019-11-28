using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace paxgame3.Client.Data
{
    public static class UnitID
    {
        static ConcurrentDictionary<double, int> IDs = new ConcurrentDictionary<double, int>();

        public static int GetID(double gameid)
        {
            lock (IDs)
            {
                if (IDs.ContainsKey(gameid))
                {
                    if (IDs[gameid] == 10000 - 1)
                        IDs[gameid] += 4;

                    return ++IDs[gameid];
                }
                else
                {
                    IDs.TryAdd(gameid, 1000);
                    return 1000;
                }
            }
        }
    }
}
