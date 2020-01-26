using ElectronNET.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;

namespace sc2dsstats_rc2
{
    public class Program
    {
        public static int DEBUG = 0;
        public static string workdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\sc2dsstats_web";
        public static string myScan_log = workdir + "/log.txt";
        public static string myJson_file = workdir + "/data.json";
        public static string myDetails_file = workdir + "/details.json";
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
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddSerilog(new LoggerConfiguration().WriteTo.File(myScan_log).CreateLogger());
                    logging.AddConsole();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseElectron(args);
                });
    }
}
