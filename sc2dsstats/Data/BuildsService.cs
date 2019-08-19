using sc2dsstats.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.Data
{
    public class BuildsService
    {
        private DSdyn_filteroptions _options;

        public BuildsService(DSdyn_filteroptions options)
        {
            _options = options;
        }

        public void DefaultFilter()
        {
            DSdyn_filteroptions defoptions = new DSdyn_filteroptions();
            _options.DOIT = false;
            _options.Build = "ALL";
            _options.Duration = defoptions.Duration;
            _options.Leaver = defoptions.Leaver;
            _options.Army = defoptions.Army;
            _options.Kills = defoptions.Kills;
            _options.Income = defoptions.Income;
            _options.Startdate = defoptions.Startdate;
            _options.Enddate = defoptions.Enddate;
            _options.Interest = defoptions.Interest;
            _options.Vs = defoptions.Vs;
            _options.Player = defoptions.Player;
            _options.DOIT = true;
        }
    }
}
