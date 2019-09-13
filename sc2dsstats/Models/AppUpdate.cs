using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace sc2dsstats.Models
{
    public static class AppUpdate
    {


        public static async Task<bool> CheckForUpdate(StartUp _startUp)
        {
            UpdateCheckResult result = new UpdateCheckResult();
            try
            {
                result = await Electron.AutoUpdater.CheckForUpdatesAsync();
            } catch { }
            Console.WriteLine(result.UpdateInfo.Version);
            Console.WriteLine(result.UpdateInfo.ReleaseDate);

            //if (_startUp.VERSION == result.UpdateInfo.Version)
            //    return false;
            //else
                return false;
        }

        public static async Task QuitAndInstall()
        {
            UpdateCheckResult result = await Electron.AutoUpdater.CheckForUpdatesAndNotifyAsync();
            
            Electron.AutoUpdater.QuitAndInstall(false, true);
        }
    }
}
