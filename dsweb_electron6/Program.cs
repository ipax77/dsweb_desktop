using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElectronNET.API;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Resources;
using dsweb_electron6.Models;

namespace dsweb_electron6
{
    public class Program
    {
        public static int DEBUG = 1; // #0 = off, #1 = console, #2 = console+logfile, #3 = debug
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        public static string workdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\sc2dsstats_web";
        public static string myScan_log = workdir + "/log.txt";
        public static string myJson_file = workdir + "/data.json";
        public static string myConfig = workdir + "/config.json";

        public static void Main(string[] args)
        {
            if (!Directory.Exists(workdir)) Directory.CreateDirectory(workdir);
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(workdir);
                    config.AddJsonFile(
                        "config.json", optional: true, reloadOnChange: false);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseElectron(args).UseStartup<Startup>();
                    //webBuilder.UseStartup<Startup>();
                });


        public static object Log(string msg, int Debug = 1)
        {
            if (DEBUG > 0 && DEBUG >= Debug)
                Console.WriteLine(msg);
            if (DEBUG > 1 && DEBUG >= Debug)
            {
                _readWriteLock.EnterWriteLock();
                File.AppendAllText(myScan_log, msg + Environment.NewLine);
                _readWriteLock.ExitWriteLock();
            }
            return null;
        }
    }


}
