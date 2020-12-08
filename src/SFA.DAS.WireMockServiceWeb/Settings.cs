namespace SFA.DAS.WireMockServiceWeb
{
    public class ApiStubOptions
    {
        public const string ConfigSection = "ApiStubSettings";

        public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
        public string EnvironmentName { get; set; } = "DEV";
        public string StorageTableName { get; set; } = "WireMockServiceApiData";
        public string WireMockServiceApiBaseUrl { get; set; } = "http://localhost:8089";
    }
}
