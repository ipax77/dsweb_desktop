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

namespace sc2dsstats
{
    public class Program
    {
        public static int DEBUG = 0; // #0 = off, #1 = console, #2 = console+logfile, #3 = debug
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        //public static string workdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\sc2dsstats_v1.1";
        public static string workdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\sc2dsstats_web";
        public static string myScan_log = workdir + "/log.txt";
        public static string myJson_file = workdir + "/data.json";
        public static string myConfig = workdir + "/config.json";

        public static void Main(string[] args)
        {
            if (!Directory.Exists(workdir)) Directory.CreateDirectory(workdir);
            if (!File.Exists(workdir + "/appsettings.json") && File.Exists("appsettings.json"))
                File.Copy("appsettings.json", workdir + "/appsettings.json");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(workdir);
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("config.json", optional: true, reloadOnChange: false);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseElectron(args).UseStartup<Startup>();
                    //webBuilder.UseStartup<Startup>()
                    //.UseElectron(args)
                    //.Build();
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
