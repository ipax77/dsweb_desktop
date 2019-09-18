using System.Threading.Tasks;
using ElectronNET.API;
using EmbeddedBlazorContent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using sc2dsstats.Data;
using sc2dsstats.Interfaces;
using sc2dsstats.Models;


namespace sc2dsstats
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
            services.AddScoped<dsotfng>();
            services.AddScoped<DSdataModel>();
            services.AddScoped<ChartService>();
            services.AddScoped<GameChartService>();
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
                //app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEmbeddedBlazorContent(typeof(MatBlazor.BaseMatComponent).Assembly);
            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapBlazorHub<App>(selector: "app");
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            Task.Run(async () => await Electron.WindowManager.CreateWindowAsync());
        }
    }
}
