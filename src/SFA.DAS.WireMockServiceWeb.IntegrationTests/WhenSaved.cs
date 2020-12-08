using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SFA.DAS.Testing.AzureStorageEmulator;
using SFA.DAS.WireMockServiceApi;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace SFA.DAS.WireMockServiceWeb.IntegrationTests
{
    public class WhenSaved
    {
        private TestServer _testServer;
        private HttpClient _wireMockApiClient;
        private HttpClient _webApiClient;
        private TestServer _wireMockServer;
        private string _mapping;
        private IDataRepository DataRepository;

        [OneTimeSetUp]
        public void Setup()
        {
            AzureStorageEmulatorManager.StartStorageEmulator();
            var builder = new WebHostBuilder().UseStartup<Startup>();
            _testServer = new TestServer(builder);
            _webApiClient = _testServer.CreateClient();

            IDictionary<string, string> settings = new Dictionary<string, string>();
            settings.Add("WireMockServerSettings:Port", "8888");
            settings.Add("WireMockServerSettings:StartAdminInterface", "true");

            var wireMockHostBuilder = new WebHostBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.AddInMemoryCollection(settings);
                })
                .UseStartup<WireMockServerStartup>();
            _wireMockServer = new TestServer(wireMockHostBuilder);
            _wireMockApiClient = new HttpClient { BaseAddress = new Uri("http://localhost:8888") };

            DataRepository = (IDataRepository)_testServer.Services.GetService(typeof(IDataRepository));
        }


        [Test]
        public async Task stores_data_in_local_storage()
        {
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const HttpMethod httpMethod = HttpMethod.Get;

            var key = Uri.EscapeDataString("data?v=1.0&id=" + expected.Key);
            var url = $"api-stub/save?httpMethod={httpMethod}&url={key}";

            var response = await _webApiClient.PostAsJsonAsync(url, expected);
            response.EnsureSuccessStatusCode();

            var data = await DataRepository.GetData(httpMethod, key);

            data.Should().BeEquivalentTo(JsonSerializer.Serialize(expected));
        }

        [Test]
        public async Task overwrites_existing_data_in_local_storage_when_same_key_is_used()
        {
            var original = new TestObject { Key = 123, Value = "String value 123" };
            const HttpMethod httpMethod = HttpMethod.Get;

            var key = Uri.EscapeDataString("data?v=1.0&id=12345");
            var url = $"api-stub/save?httpMethod={httpMethod}&url={key}";

            var response = await _webApiClient.PostAsJsonAsync(url, original);
            response.EnsureSuccessStatusCode();

            var expected = new TestObject { Key = 789, Value = "String value 789" };

            response = await _webApiClient.PostAsJsonAsync(url, expected);
            response.EnsureSuccessStatusCode();

            var data = await DataRepository.GetData(httpMethod, key);

            data.Should().BeEquivalentTo(JsonSerializer.Serialize(expected));
        }

        [Test]
        public async Task saved_data_is_available_after_save_and_refresh()
        {
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const HttpMethod httpMethod = HttpMethod.Get;

            var key = $"/{Guid.NewGuid()}";
            var url = $"api-stub/save?httpMethod={httpMethod}&url={key}&refresh=true";

            var response = await _webApiClient.PostAsJsonAsync(url, expected);
            response.EnsureSuccessStatusCode();

            var data = await _wireMockApiClient.GetFromJsonAsync<TestObject>(key);

            data.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task saved_data_is_not_available_after_save_without_refresh()
        {
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const HttpMethod httpMethod = HttpMethod.Get;

            var key = $"/{Guid.NewGuid()}";
            var url = $"api-stub/save?httpMethod={httpMethod}&url={key}&refresh=false";

            var response = await _webApiClient.PostAsJsonAsync(url, expected);
            response.EnsureSuccessStatusCode();

            _wireMockApiClient.GetAsync(key).Result.StatusCode.Should().Be(HttpStatusCode.NotFound);

        }
    }
}