using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WireMock.Server;
using WireMock.Settings;

namespace SFA.DAS.WireMockServiceApi
{
    public class WireMockService : IWireMockService
    {
        private readonly ILogger _logger;
        private readonly WireMockServerSettings _settings;

        public WireMockService(ILogger<WireMockService> logger, IOptions<WireMockServerSettings> settings, TelemetryClient telemetryClient, IConfiguration configuration)
        {
            _logger = logger;
            _settings = settings.Value;

            _settings.Logger = new Logger(logger, telemetryClient, configuration.GetValue<bool>("LogRequestBodies"));
        }
        
        public void Start()
        {
            _logger.LogInformation("WireMock.Net server starting");

            Server = WireMockServer.Start(_settings);
            
            _logger.LogInformation($"WireMock.Net server settings {JsonConvert.SerializeObject(_settings)}");
        }

        public void Stop()
        {
            _logger.LogInformation("WireMock.Net server stopping");
            Server?.Stop();
        }

        public WireMockServer Server { get; private set; }
    }
}