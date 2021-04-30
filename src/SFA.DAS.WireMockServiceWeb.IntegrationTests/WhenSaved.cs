using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SFA.DAS.Testing.AzureStorageEmulator;
using SFA.DAS.WireMockServiceApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace SFA.DAS.WireMockServiceWeb.IntegrationTests
{
    public class WhenSaved
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
        public async Task stores_data_in_local_storage()
        {
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const HttpMethod httpMethod = HttpMethod.Get;

            var key = Uri.EscapeDataString("data?v=1.0&id=" + expected.Key);
            var url = $"api-stub/save?httpMethod={httpMethod}&url={key}";

            var response = await _webApiClient.PostAsJsonAsync(url, expected);
            response.EnsureSuccessStatusCode();

            var data = _dataRepository.GetData(httpMethod, key);

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

            var data = _dataRepository.GetData(httpMethod, key);

            data.Should().BeEquivalentTo(JsonSerializer.Serialize(expected));
        }

        [Test]
        public async Task saved_data_is_available_after_save_and_refresh()
        {
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const HttpMethod httpMethod = HttpMethod.Get;

            var key = $"/{Guid.NewGuid()}";
            var url = $"api-stub/save?httpMethod={httpMethod}&url={key}";

            var response = await _webApiClient.PostAsJsonAsync(url, expected);
            response.EnsureSuccessStatusCode();

            var data = await _wireMockApiClient.GetFromJsonAsync<TestObject>(key);

            data.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task mapping_can_be_stored_without_body()
        {
            const HttpMethod mappingHttpMethod = HttpMethod.Post;
            const string mappingUrl = "/businesscentral/payments/requests?api-version=2020-10-01";

            HttpContent nullContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _webApiClient.PostAsync($"api-stub/save?httpMethod={mappingHttpMethod}&url={mappingUrl}", nullContent);
            response.EnsureSuccessStatusCode();

            var result = await _wireMockApiClient.PostAsync(mappingUrl, null);
            result.StatusCode.Should().Be(200);
        }

        [Test]
        public async Task mapping_can_be_stored_with_a_custom_return_http_status_code()
        {
            const HttpMethod mappingHttpMethod = HttpMethod.Post;
            var mappingUrl = $"/{Guid.NewGuid()}";
            const HttpStatusCode mappingHttpCode = HttpStatusCode.Accepted;

            var response = await _webApiClient.PostAsJsonAsync($"api-stub/save?httpMethod={mappingHttpMethod}&url={mappingUrl}&httpStatusCode=202", "");
            response.EnsureSuccessStatusCode();

            var result = await _wireMockApiClient.PostAsync(mappingUrl, null);
            result.StatusCode.Should().Be(mappingHttpCode);
        }

        [Test]
        public async Task can_find_mappings_by_url()
        {
            var expected = new[] { "abc123", "---abc", "12abc21", "cnbc", "a-b-c" };
            foreach (var url in expected)
            {
                var save = await _webApiClient.PostAsJsonAsync($"api-stub/save?httpMethod=Get&url={url}", "");
                save.EnsureSuccessStatusCode();
            }

            var actual = await _webApiClient.GetFromJsonAsync<DataRepository.MappingData[]>("api-stub/find?url=abc");
            Debug.Assert(actual != null, nameof(actual) + " != null");
            actual.Select(a => a.Url).Should().BeEquivalentTo(expected.Take(3));
        }
    }
}