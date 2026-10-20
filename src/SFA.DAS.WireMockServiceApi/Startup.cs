using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.WireMockService.Data;

namespace SFA.DAS.WireMockServiceApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ApiStubOptions>(Configuration.GetSection(ApiStubOptions.ConfigSection));
            services.AddSingleton<IDataRepository, DataRepository>();
            services.AddApplicationInsightsTelemetry();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<StubResponseMiddleware>();
        }
    }
}
