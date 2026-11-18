namespace SFA.DAS.WireMockService.Data
{
    public class ApiStubOptions
    {
        public const string ConfigSection = "ApiStubSettings";

        public string StorageAccountConnectionString { get; set; } = "UseDevelopmentStorage=true";
        public string EnvironmentName { get; set; } = "DEV";
        public string StorageTableName { get; set; } = "WireMockServiceApiData";
    }
}
