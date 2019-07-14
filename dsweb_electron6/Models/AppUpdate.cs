using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectronNET.API;

namespace dsweb_electron6.Models
{
    public class AppUpdate
    {
        public async void Update()
        {

            if (HybridSupport.IsElectronActive)
            {

                Electron.IpcMain.On("auto-update", async (args) =>
                {
                    var currentVersion = await Electron.App.GetVersionAsync();
                    var updateCheckResult = await Electron.AutoUpdater.CheckForUpdatesAndNotifyAsync();
                    var availableVersion = updateCheckResult.UpdateInfo.Version;
                    string information = $"Current version: {currentVersion} - available version: {availableVersion}";

                    var mainWindow = Electron.WindowManager.BrowserWindows.First();
                    Electron.IpcMain.Send(mainWindow, "auto-update-reply", information);
                });
            }
        }

        private async void UpdateReply()
        {
            var browserWindow = Electron.WindowManager.BrowserWindows.Last();
            var size = await browserWindow.GetSizeAsync();
            var position = await browserWindow.GetPositionAsync();
            string message = $"Size: {size[0]},{size[1]} Position: {position[0]},{position[1]}";

            var mainWindow = Electron.WindowManager.BrowserWindows.First();
            Electron.IpcMain.Send(mainWindow, "manage-window-reply", message);
        }
    }
}
