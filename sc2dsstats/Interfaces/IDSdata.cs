using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using sc2dsstats.Data;
using sc2dsstats.Models;
using Microsoft.AspNetCore.Mvc;

namespace sc2dsstats.Interfaces
{
    public interface IDSdata
    {
        Dictionary<string, KeyValuePair<double, int>> WINRATE { get; }
        Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> WINRATEVS { get; }
        Dictionary<string, KeyValuePair<double, int>> WINRATE_PLAYER { get; }
        Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> WINRATEVS_PLAYER { get; }
    }

    public interface IDSdata_cache
    {
        List<Models.dsreplay> REPLAYS { get; }
        List<Models.dsreplay> FILTERED_REPLAYS { get; }
        Models.dsfilter FIL_INFO { get; }
        double FIL_WR { get; }

        Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winrate_CACHE { get; }
        Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>> winratevs_CACHE { get; }
        Dictionary<string, dsfilter> filter_CACHE { get; }

        Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>>> BUILDCACHE { get; }
        Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<double, int>>>>> BUILDWRCACHE { get; }
        Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, double>>>> BUILDDURCACHE { get; }
        Dictionary<string, Dictionary<string, Dictionary<string, List<dsreplay>>>> BUILDREPLAYSCACHE { get; }

        Dictionary<string, Dictionary<string, Dictionary<string, Models.dsfilter>>> FILTER { get; }
        Dictionary<string, List<Models.dsreplay>> BUILD_REPLAYS { get; }

        Task Init(List<dsreplay> replays);
        Task InitBuilds();

        void GenBuilds(string player, string startdate = null, string enddate = null);
        void GetDynData(DSdyn_filteroptions fil,
                        out Dictionary<string, KeyValuePair<double, int>> winrate,
                        out Dictionary<string, Dictionary<string, KeyValuePair<double, int>>> winratevs,
                        out string info
                        );

    }

    public interface IDSladder
    {
        ConcurrentDictionary<string, MMplayer> MMplayers { get; }
        ConcurrentDictionary<string, MMplayer> MMraces { get; }
    }

    public interface IMyGame : INotifyPropertyChanged
    {
        int gameid { get;  }
        void SetID(int id);
    }
}
