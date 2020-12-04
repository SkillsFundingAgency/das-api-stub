using Microsoft.Extensions.Configuration;

namespace SFA.DAS.RestApiStub
{
    public static class Settings
    {
        public static int WireMockPort { get; set; } = 8086;
        public static string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
        public static string Environment { get; set; } = "DEV";

        public static void Set(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("AzureWebJobsStorage");
            if (!string.IsNullOrEmpty(connectionString)) ConnectionString = connectionString;

            var environment = config.GetValue<string>("EnvironmentName");
            if (!string.IsNullOrEmpty(environment)) Environment = environment;

            var port = config.GetValue<int?>("WireMockPort");
            if (port.HasValue) WireMockPort = port.Value;
        }
    }
}
