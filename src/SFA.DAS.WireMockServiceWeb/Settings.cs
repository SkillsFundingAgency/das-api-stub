﻿using Microsoft.Extensions.Configuration;
using System;

namespace SFA.DAS.WireMockServiceWeb
{
    public static class Settings
    {
        public static int WireMockPort { get; set; } = 8089;
        public static string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
        public static string EnvironmentName { get; set; } = "DEV";
        public static string StorageTableName { get; set; } = "WireMockServiceApiData";
        public static string WireMockServiceApiBaseUrl { get; set; }

        public static void Set(IConfiguration config)
        {

            var connectionString = config.GetConnectionString("SharedStorageAccountConnectionString");
            if (!string.IsNullOrEmpty(connectionString)) ConnectionString = connectionString;

            var environment = config.GetValue<string>("EnvironmentName");
            if (!string.IsNullOrEmpty(environment)) EnvironmentName = environment;

            var port = config.GetValue<int?>("WireMockPort");
            if (port.HasValue) WireMockPort = port.Value;

            var wireMockServiceApiBaseUrl = config.GetValue<string>("WireMockServiceApiBaseUrl");
            if (!string.IsNullOrEmpty(WireMockServiceApiBaseUrl))
                throw new InvalidOperationException($"Configuration setting for: {nameof(WireMockServiceApiBaseUrl)} is missing");
            WireMockServiceApiBaseUrl = wireMockServiceApiBaseUrl;
        }
    }
}