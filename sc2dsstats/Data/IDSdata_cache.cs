using pax.s2decode.Models;
using sc2dsstats.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sc2dsstats.Data
{
    public interface IDSdata_cache
    {
        Dictionary<string, List<dsreplay>> BUILD_REPLAYS { get; }
        Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>> BUILDCACHE { get; }
        Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>> BUILDDURCACHE { get; }
        Dictionary<string, Dictionary<string, Dictionary<string, List<dsreplay>>>> BUILDREPLAYSCACHE { get; }
        Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>> BUILDWRCACHE { get; }
        dsfilter FIL_INFO { get; }
        double FIL_WR { get; }
        Dictionary<string, Dictionary<string, Dictionary<string, dsfilter>>> FILTER { get; }
        Dictionary<string, dsfilter> filter_CACHE { get; }
        List<dsreplay> FILTERED_REPLAYS { get; }
        List<dsreplay> REPLAYS { get; }
        Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winrate_CACHE { get; }
        Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>> winratevs_CACHE { get; }

        string FixUnitName(string unit);
        void GenBuilds(string player, string startdate = null, string enddate = null);
        double GenWR(double wins, double games);
        double GenWr(int wins, int games);
        void GetDynData(DSdyn_filteroptions fil, out Dictionary<string, KeyValuePair<double, int>> winrate, out Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winratevs, out string info);
        Task Init(List<dsreplay> replays);
        Task InitBuilds();
    }
}