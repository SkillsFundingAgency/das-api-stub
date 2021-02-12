using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SFA.DAS.Testing.AzureStorageEmulator;
using SFA.DAS.WireMockServiceApi;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace SFA.DAS.WireMockServiceWeb.IntegrationTests
{
    public class WhenDeleted
    {
        private const int Port = 8888;
        private TestServer _testServer;
        private HttpClient _wireMockApiClient;
        private HttpClient _webApiClient;
        private TestServer _wireMockServer;
        private IDataRepository _dataRepository;

        [OneTimeSetUp]
        public void Setup()
        {
            AzureStorageEmulatorManager.StartStorageEmulator();
            BuildWireMockClient();
            BuildWireMockHost();
        }
        private void BuildWireMockClient()
        {
            var settings = new Dictionary<string, string>
            {
                {"ApiStubSettings:WireMockServiceApiBaseUrl", $"http://localhost:{Port}"}
            };

            var builder = new WebHostBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.AddInMemoryCollection(settings);
                })
                .UseStartup<Startup>();

            _testServer = new TestServer(builder);
            _webApiClient = _testServer.CreateClient();
            _dataRepository = (IDataRepository)_testServer.Services.GetService(typeof(IDataRepository));
        }

        private void BuildWireMockHost()
        {
            IDictionary<string, string> settings = new Dictionary<string, string>();
            settings.Add("WireMockServerSettings:Port", $"{Port}");
            settings.Add("WireMockServerSettings:StartAdminInterface", "true");

            var wireMockHostBuilder = new WebHostBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(settings);
                })
                .UseStartup<WireMockServerStartup>();
            _wireMockServer = new TestServer(wireMockHostBuilder);
            _wireMockApiClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{Port}") };
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            _wireMockApiClient?.Dispose();
            _webApiClient?.Dispose();
            _testServer?.Dispose();
            _wireMockServer?.Dispose();
            await _dataRepository.DropTableStorage();
        }

        [Test]
        public async Task deletes_existing_data_in_local_storage_when_same_key_is_used()
        {
            const HttpMethod httpMethod = HttpMethod.Get;

            var key = Uri.EscapeDataString("/data?v=1.0&id=12345");

            await _dataRepository.InsertOrReplace(httpMethod, key, "test");

            var response = await _webApiClient.DeleteAsync($"api-stub/delete?httpMethod={httpMethod}&url={key}");
            response.EnsureSuccessStatusCode();

            var data = _dataRepository.GetData(httpMethod, key);
            data.Should().BeNullOrEmpty();
        }
    }
}