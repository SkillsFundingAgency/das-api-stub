using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Text.Json.Serialization;

namespace SFA.DAS.WireMockServiceWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var configBuilder = new ConfigurationBuilder()
                .AddConfiguration(Configuration)
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables();

            var config = configBuilder.Build();

            services.Configure<ApiStubOptions>(config.GetSection(ApiStubOptions.ConfigSection));

            services
                .AddControllers()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SFA.DAS.WireMockServiceWeb", Version = "v1" });
            });

            ConfigureWireMockService(services);

            services.AddSingleton<IDataRepository, DataRepository>();

            services.AddApplicationInsightsTelemetry(config["APPINSIGHTS_INSTRUMENTATIONKEY"]);
            services.AddLogging(logging => logging.AddConsole().AddDebug());
        }

        private static void ConfigureWireMockService(IServiceCollection services)
        {
            services.AddSingleton<IWireMockHttpService>(provider =>
            {
                var opts = provider.GetService<IOptions<ApiStubOptions>>().Value;
                var repo = provider.GetService<IDataRepository>();
                var httpClient = new WireMockHttpClient { BaseAddress = new Uri(opts.WireMockServiceApiBaseUrl) };
                return new WireMockHttpService(httpClient, repo);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SFA.DAS.WireMockServiceWeb v1"));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}
