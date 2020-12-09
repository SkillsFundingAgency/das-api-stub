using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SFA.DAS.Testing.AzureStorageEmulator;
using SFA.DAS.WireMockServiceApi;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace SFA.DAS.WireMockServiceWeb.IntegrationTests
{
    public class WhenRefreshed
    {
        private const int Port = 8888;
        private TestServer _testServer;
        private HttpClient _wireMockApiClient;
        private HttpClient _webApiClient;
        private TestServer _wireMockServer;
        private string _mapping;
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
        public async Task wiremock_admin_endpoint_returns_Ok()
        {
            const string url = "/__admin/mappings";
            var response = await _wireMockApiClient.GetAsync(url);
            response.StatusCode.Should().Be(200);
        }
        private async Task AddSampleMapping()
        {
            const string url = "/__admin/mappings";
            _mapping = @"{
                  ""request"": {
                    ""method"": ""GET"",
                    ""url"": ""/some/thing""
                  },
                  ""response"": {
                    ""body"": ""Hello world!"",
                    ""headers"": {
                      ""Content-Type"": ""text/plain""
                    },
                    ""status"": 200
                  }
                }";

            var response = await _wireMockApiClient.PostAsync(url, new StringContent(_mapping, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task api_stub_mappings_get_returns_wiremock_mappings()
        {
            // Arrange
            await AddSampleMapping();

            // Act
            var response = await _webApiClient.GetAsync("api-stub/mappings");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Contain("Hello world!");
        }

        [Test]
        public async Task api_stub_refresh_new_http_get_route_returns_expected_data_from_storage()
        {
            // Arrange
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const string url = "/external-api/data";
            await _dataRepository.InsertOrReplace(HttpMethod.Get, url, expected);

            // Act
            await _webApiClient.GetAsync("api-stub/refresh");
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // Assert

            await api_stub_mappings_get_returns_wiremock_mappings();

            var response = await _wireMockApiClient.GetFromJsonAsync<TestObject>(url);
            response.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task api_stub_refresh_new_http_get_route_with_wildcarded_url_returns_data_from_storage()
        {
            // Arrange
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const string url = "/external-api/wildcard-*";
            await _dataRepository.InsertOrReplace(HttpMethod.Get, url, expected);

            // Act
            await _webApiClient.GetAsync("api-stub/refresh");
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // Assert
            var response = await _wireMockApiClient.GetFromJsonAsync<TestObject>("/external-api/wildcard-test");
            response.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task api_stub_refresh_new_http_get_route_with_query_parameters_returns_expected_data_from_storage()
        {
            // Arrange
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const string url = "/external-api/data?v=1.0&id=123";
            await _dataRepository.InsertOrReplace(HttpMethod.Get, url, expected);

            // Act
            await _webApiClient.GetAsync("api-stub/refresh");
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // Assert
            var response = await _wireMockApiClient.GetFromJsonAsync<TestObject>(url);
            response.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task api_stub_refresh_new_http_post_route_returns_expected_data_from_storage()
        {
            // Arrange
            var expected = new TestObject { Key = 456, Value = "String value 456" };
            const string url = "/external-api/data?v=1.0&id=456";
            await _dataRepository.InsertOrReplace(HttpMethod.Post, url, expected);

            // Act
            await _webApiClient.GetAsync("api-stub/refresh");
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // Assert
            var response = await _wireMockApiClient.PostAsync(url, new StringContent(""));
            var returnResult = response.Content.ReadAsStringAsync().Result;
            returnResult.Should().BeEquivalentTo(JsonSerializer.Serialize(expected));
        }

        [Test]
        public async Task api_stub_refresh_removes_redundant_route_definitions()
        {
            // Arrange
            await _dataRepository.InsertOrReplace(HttpMethod.Get, "/url1", "");
            await _dataRepository.InsertOrReplace(HttpMethod.Get, "/url2", "");
            await _dataRepository.InsertOrReplace(HttpMethod.Get, "/url3", "");

            await _webApiClient.GetAsync("api-stub/refresh");

            await _dataRepository.DropTableStorage();
            await _dataRepository.CreateTableStorage();
            await _dataRepository.InsertOrReplace(HttpMethod.Get, "/url4", "");

            // Act
            await _webApiClient.GetAsync("api-stub/refresh");
            Thread.Sleep(TimeSpan.FromSeconds(1));

            // Assert
            _wireMockApiClient.GetAsync("/url1").Result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            _wireMockApiClient.GetAsync("/url2").Result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            _wireMockApiClient.GetAsync("/url3").Result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            _wireMockApiClient.GetAsync("/url4").Result.StatusCode.Should().Be(StatusCodes.Status200OK);
        }
    }
}