using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using dsweb_electron6.Data;
using dsweb_electron6.Models;
using ElectronNET.API;
using dsweb_electron6.Interfaces;

namespace dsweb_electron6
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<StartUp>();
            services.AddSingleton<ScanStateChange>();
            services.AddSingleton<IDSdata_cache, DSdata_cache>();
            services.AddScoped<MMservice>();
            services.AddScoped<dsotfng>();
            services.AddScoped<DSdataModel>();
            services.AddScoped<ChartService>();
            services.AddScoped<DSdyn>();
            services.AddScoped<ChartStateChange>();
            services.AddScoped<DSdyn_filteroptions>();
            services.AddScoped<BuildsService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
            Task.Run(async () => await Electron.WindowManager.CreateWindowAsync());
        }
    }
}
