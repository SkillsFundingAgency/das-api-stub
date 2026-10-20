using SFA.DAS.WireMockService.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SFA.DAS.Testing.AzureStorageEmulator;
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
        private readonly string _tableName = $"Test{Guid.NewGuid():N}";
        private TestServer _testServer;
        private TestServer _stubAppServer;
        private HttpClient _webApiClient;
        private HttpClient _stubAppClient;
        private IDataRepository _dataRepository;

        [OneTimeSetUp]
        public async Task Setup()
        {
            AzureStorageEmulatorManager.StartStorageEmulator();

            var settings = new Dictionary<string, string>
            {
                { "ApiStubSettings:StorageTableName", _tableName }
            };

            _testServer = new TestServer(new WebHostBuilder()
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(settings))
                .UseStartup<Startup>());
            _webApiClient = _testServer.CreateClient();
            _dataRepository = (IDataRepository)_testServer.Services.GetService(typeof(IDataRepository));
            await _dataRepository.CreateTableStorage();

            _stubAppServer = new TestServer(new WebHostBuilder()
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(settings))
                .UseStartup<WireMockServiceApi.Startup>());
            _stubAppClient = _stubAppServer.CreateClient();
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            _webApiClient?.Dispose();
            _stubAppClient?.Dispose();
            _testServer?.Dispose();
            _stubAppServer?.Dispose();
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

            var data = (await _dataRepository.Get(httpMethod.ToString(), key))?.Data;

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

            var data = (await _dataRepository.Get(httpMethod.ToString(), key))?.Data;

            data.Should().BeEquivalentTo(JsonSerializer.Serialize(expected));
        }

        [Test]
        public async Task saved_data_is_served_by_the_stub_app()
        {
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            var key = $"/{Guid.NewGuid()}";

            var response = await _webApiClient.PostAsJsonAsync($"api-stub/save?httpMethod={HttpMethod.Get}&url={key}", expected);
            response.EnsureSuccessStatusCode();

            var data = await _stubAppClient.GetFromJsonAsync<TestObject>(key);

            data.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task saving_an_existing_mapping_replaces_what_the_stub_app_serves()
        {
            var key = $"/{Guid.NewGuid()}";
            var url = $"api-stub/save?httpMethod={HttpMethod.Get}&url={key}";

            var response = await _webApiClient.PostAsJsonAsync(url, new TestObject { Key = 123, Value = "String value 123" });
            response.EnsureSuccessStatusCode();

            var expected = new TestObject { Key = 789, Value = "String value 789" };
            response = await _webApiClient.PostAsJsonAsync(url, expected);
            response.EnsureSuccessStatusCode();

            var data = await _stubAppClient.GetFromJsonAsync<TestObject>(key);
            data.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task mapping_can_be_stored_without_body()
        {
            const HttpMethod mappingHttpMethod = HttpMethod.Post;
            var mappingUrl = $"/{Guid.NewGuid()}?api-version=2020-10-01";

            HttpContent nullContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _webApiClient.PostAsync($"api-stub/save?httpMethod={mappingHttpMethod}&url={Uri.EscapeDataString(mappingUrl)}", nullContent);
            response.EnsureSuccessStatusCode();

            var result = await _stubAppClient.PostAsync(mappingUrl, null);
            result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public async Task mapping_can_be_stored_with_a_custom_return_http_status_code()
        {
            const HttpMethod mappingHttpMethod = HttpMethod.Post;
            var mappingUrl = $"/{Guid.NewGuid()}";

            var response = await _webApiClient.PostAsJsonAsync($"api-stub/save?httpMethod={mappingHttpMethod}&url={mappingUrl}&httpStatusCode=202", "");
            response.EnsureSuccessStatusCode();

            var result = await _stubAppClient.PostAsync(mappingUrl, null);
            result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        [Test]
        public async Task refresh_is_accepted_and_changes_nothing()
        {
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            var key = $"/{Guid.NewGuid()}";
            (await _webApiClient.PostAsJsonAsync($"api-stub/save?httpMethod={HttpMethod.Get}&url={key}", expected)).EnsureSuccessStatusCode();

            var response = await _webApiClient.GetAsync("api-stub/refresh");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await _stubAppClient.GetFromJsonAsync<TestObject>(key)).Should().BeEquivalentTo(expected);
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
