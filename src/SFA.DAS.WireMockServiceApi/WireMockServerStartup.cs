using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WireMock.Settings;

namespace SFA.DAS.WireMockServiceApi
{
    public class WireMockServerStartup
    {
        public WireMockServerStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(logging => logging.AddConsole().AddDebug());

            services.AddTransient<IWireMockService, WireMockService>();
            services.Configure<WireMockServerSettings>(Configuration.GetSection("WireMockServerSettings"));

            services.AddHostedService<App>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}