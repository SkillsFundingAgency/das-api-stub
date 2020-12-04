using Microsoft.Extensions.Configuration;

namespace SFA.DAS.WireMockServiceApi
{
    public static class Settings
    {
        public static int WireMockPort { get; set; } = 8086;
        public static string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
        public static string EnvironmentName { get; set; } = "DEV";
        public static string StorageTableName { get; set; } = "WireMockServiceApiData";

        public static void Set(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("SharedStorageAccountConnectionString");
            if (!string.IsNullOrEmpty(connectionString)) ConnectionString = connectionString;

            var environment = config.GetValue<string>("EnvironmentName");
            if (!string.IsNullOrEmpty(environment)) EnvironmentName = environment;

            var port = config.GetValue<int?>("WireMockPort");
            if (port.HasValue) WireMockPort = port.Value;

            var storageTableName = config.GetValue<string>("StorageTableName");
            if (!string.IsNullOrEmpty(storageTableName)) StorageTableName = storageTableName;
        }
    }
}
