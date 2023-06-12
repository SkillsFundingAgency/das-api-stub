using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using WireMock.Admin.Requests;
using WireMock.Logging;

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
            using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
            {
                _logger.LogDebug(formatString, args);
                _telemetryClient.TrackEvent(string.Format(formatString, args));
            }
        }

        public void Info(string formatString, params object[] args)
        {
            using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
            {
                _logger.LogInformation(formatString, args);
                _telemetryClient.TrackEvent(string.Format(formatString, args));
            }
        }

        public void Warn(string formatString, params object[] args)
        {
            using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
            {
                _logger.LogWarning(formatString, args);
                _telemetryClient.TrackEvent(string.Format(formatString, args));
            }
        }

        public void Error(string formatString, params object[] args)
        {
            using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
            {
                _logger.LogError(formatString, args);
                _telemetryClient.TrackEvent(string.Format(formatString, args));
            }
        }
        
        public void Error(string formatString, Exception exception)
        {
            using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
            {
                _logger.LogError(formatString, exception.Message);
                _telemetryClient.TrackEvent(string.Format(formatString, exception.Message));
            }
        }

        public void DebugRequestResponse(LogEntryModel logEntryModel, bool isAdminRequest)
        {
            using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
            {
                var message = JsonConvert.SerializeObject(logEntryModel, Formatting.Indented);
                _logger.LogDebug("Admin[{0}] {1}", isAdminRequest, message);
                _telemetryClient.TrackEvent(message);
            }
        }

    }
}
