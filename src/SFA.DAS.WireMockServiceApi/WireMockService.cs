//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using System.Threading;
//using WireMock.Net.StandAlone;
//using WireMock.Settings;

//namespace SFA.DAS.WireMockServiceApi
//{
//    public class WireMockService
//    {
//        private static int sleepTime = 30000;
//        private readonly ILogger _logger;
//        private readonly IWireMockServerSettings _settings;


//        public WireMockService(ILogger logger, IWireMockServerSettings settings)
//        {
//            _logger = logger;
//            _settings = settings;

//            _settings.Logger = new Logger(logger);
//        }

//        public void Run()
//        {
//            _logger.LogInformation("WireMock.Net server starting");

//            StandAloneApp.Start(_settings);

//            _logger.LogInformation($"WireMock.Net server settings {JsonConvert.SerializeObject(_settings)}");

//            while (true)
//            {
//                _logger.LogInformation("WireMock.Net server running");
//                Thread.Sleep(sleepTime);
//            }
//        }
//    }
//}
