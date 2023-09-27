﻿using Microsoft.Extensions.Logging;
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

        public Logger(ILogger logger)
        {
            _logger = logger;
        }

        public void Debug(string formatString, params object[] args)
        {
            _logger.LogDebug(formatString, args);
        }

        public void Info(string formatString, params object[] args)
        {
            _logger.LogInformation(formatString, args);
        }

        public void Warn(string formatString, params object[] args)
        {
            _logger.LogWarning(formatString, args);
        }

        public void Error(string formatString, params object[] args)
        {
            _logger.LogError(formatString, args);
        }
        
        public void Error(string formatString, Exception exception)
        {
            _logger.LogError(formatString, exception.Message);
        }

        public void DebugRequestResponse(LogEntryModel logEntryModel, bool isAdminRequest)
        {
            var message = JsonConvert.SerializeObject(logEntryModel, Formatting.Indented);
            _logger.LogDebug("Admin[{0}] {1}", isAdminRequest, message);
        }

    }
}