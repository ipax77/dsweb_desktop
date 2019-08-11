using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElectronNET.API;

namespace dsweb_electron6.Models
{
    public class AppUpdate
    {
        public void Update()
        {

            if (HybridSupport.IsElectronActive)
            {
                Console.WriteLine("Update ..");
                Electron.IpcMain.On("auto-update", async (args) =>
                {
                    var currentVersion = await Electron.App.GetVersionAsync();
                    Console.WriteLine(currentVersion);
                    var updateCheckResult = await Electron.AutoUpdater.CheckForUpdatesAndNotifyAsync();
                    Console.WriteLine(updateCheckResult);
                    var availableVersion = updateCheckResult.UpdateInfo.Version;
                    Console.WriteLine(availableVersion);
                    string information = $"Current version: {currentVersion} - available version: {availableVersion}";
                    Console.WriteLine(information);
                    var mainWindow = Electron.WindowManager.BrowserWindows.First();
                    Console.WriteLine(currentVersion);
                    Electron.IpcMain.Send(mainWindow, "auto-update-reply", information);

                });
            }
        }

        public async void UpdateReply()
        {
            var browserWindow = Electron.WindowManager.BrowserWindows.Last();
            var size = await browserWindow.GetSizeAsync();
            var position = await browserWindow.GetPositionAsync();
            string message = $"Size: {size[0]},{size[1]} Position: {position[0]},{position[1]}";

            var mainWindow = Electron.WindowManager.BrowserWindows.First();
            Electron.IpcMain.Send(mainWindow, "manage-window-reply", message);
        }

        public async Task<string> Test()
        {
            var currentVersion = await Electron.App.GetVersionAsync();
            var updateCheckResult = await Electron.AutoUpdater.CheckForUpdatesAndNotifyAsync();
            var availableVersion = updateCheckResult.UpdateInfo.Version;
            string information = $"Current version: {currentVersion} - available version: {availableVersion}";
            Thread.Sleep(1000);

            var mainWindow = Electron.WindowManager.BrowserWindows.First();
            return "bab";
        }
    }
}
