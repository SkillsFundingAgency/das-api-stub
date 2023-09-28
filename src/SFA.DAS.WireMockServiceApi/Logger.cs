using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using WireMock.Admin.Requests;
using WireMock.Logging;
using System.Collections.Generic;

namespace SFA.DAS.WireMockServiceApi
{
    public class Logger : IWireMockLogger
    {
        private readonly ILogger _logger;
        private readonly TelemetryClient _telemetryClient;

        public Logger(ILogger logger, TelemetryClient telemetryClient)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        public void Debug(string formatString, params object[] args)
        {
            _logger.LogDebug(formatString, args);
            _telemetryClient.TrackEvent(string.Format(formatString, args));            
        }

        public void Info(string formatString, params object[] args)
        {
            _logger.LogInformation(formatString, args);
            _telemetryClient.TrackEvent(string.Format(formatString, args));
        }

        public void Warn(string formatString, params object[] args)
        {
            _logger.LogWarning(formatString, args);
            _telemetryClient.TrackEvent(string.Format(formatString, args));
        }

        public void Error(string formatString, params object[] args)
        {
            _logger.LogError(formatString, args);
            _telemetryClient.TrackEvent(string.Format(formatString, args));            
        }
        
        public void Error(string formatString, Exception exception)
        {
            _logger.LogError(formatString, exception.Message);
            _telemetryClient.TrackEvent(string.Format(formatString, exception.Message));
        }

        public void DebugRequestResponse(LogEntryModel logEntryModel, bool isAdminRequest)
        {
            if (logEntryModel == null || logEntryModel.Request == null || logEntryModel.Response == null)
            {
                return;
            }

            using (_telemetryClient.StartOperation<RequestTelemetry>(logEntryModel.Request.Path))
            {
                var eventData = new Dictionary<string, string>
                {
                    { "Response", JsonConvert.SerializeObject(logEntryModel.Response) }
                };
                _telemetryClient.TrackEvent(logEntryModel.Request.Path, eventData);
            }
        }
    }
}
