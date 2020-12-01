using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace RestApiStub.Tests
{
    public class WhenRefreshed
    {
        private TestServer _testServer;
        private HttpClient _fakeApiClient;
        private HttpClient _webApiClient;

        [OneTimeSetUp]
        public void Setup()
        {
            var builder = new WebHostBuilder().UseStartup<Startup>();
            _testServer = new TestServer(builder);
            _webApiClient = _testServer.CreateClient();
            _fakeApiClient = new HttpClient { BaseAddress = new Uri("http://localhost:" + Settings.WireMockPort) };
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            var fakeApi = _testServer.Services.GetService<FakeApi>();
            fakeApi?.Dispose();
            _fakeApiClient.Dispose();
            _webApiClient.Dispose();
            _testServer.Dispose();
            await DataRepository.DropTableStorage();
        }

        [Test]
        public async Task wiremock_admin_endpoint_returns_Ok()
        {
            const string url = "/__admin/mappings";
            var response = await _fakeApiClient.GetAsync(url);
            response.StatusCode.Should().Be(200);
        }

        [Test]
        public async Task new_GET_route_returns_expected_data_from_storage()
        {
            // Arrange
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const string url = "/external-api/data";
            await DataRepository.InsertOrReplace(HttpMethod.Get, url, expected);

            // Act
            await _webApiClient.GetAsync("api-stub/refresh");

            // Assert
            var response = await _fakeApiClient.GetFromJsonAsync<TestObject>(url);
            response.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task new_GET_route_with_query_parameters_returns_expected_data_from_storage()
        {
            // Arrange
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const string url = "/external-api/data?v=1.0&id=123";
            await DataRepository.InsertOrReplace(HttpMethod.Get, url, expected);

            // Act
            await _webApiClient.GetAsync("api-stub/refresh");

            // Assert
            var response = await _fakeApiClient.GetFromJsonAsync<TestObject>(url);
            response.Should().BeEquivalentTo(expected);
        }

        [Test]
        public async Task new_POST_route_returns_expected_data_from_storage()
        {
            // Arrange
            var expected = new TestObject { Key = 456, Value = "String value 456" };
            const string url = "/external-api/data?v=1.0&id=456";
            await DataRepository.InsertOrReplace(HttpMethod.Post, url, expected);

            // Act
            await _webApiClient.GetAsync("api-stub/refresh");

            // Assert
            var response = await _fakeApiClient.PostAsync(url, new StringContent(""));
            var returnResult = response.Content.ReadAsStringAsync().Result;
            returnResult.Should().BeEquivalentTo(JsonSerializer.Serialize(expected));
        }

        [Test]
        public async Task removes_redundant_route_definitions()
        {
            // Arrange
            await DataRepository.InsertOrReplace(HttpMethod.Get, "/url1", "");
            await DataRepository.InsertOrReplace(HttpMethod.Get, "/url2", "");
            await DataRepository.InsertOrReplace(HttpMethod.Get, "/url3", "");

            await _webApiClient.GetAsync("api-stub/refresh");

            await DataRepository.DropTableStorage();
            await DataRepository.CreateTableStorage();
            await DataRepository.InsertOrReplace(HttpMethod.Get, "/url4", "");

            // Act
            await _webApiClient.GetAsync("api-stub/refresh");

            // Assert
            _fakeApiClient.GetAsync("/url1").Result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            _fakeApiClient.GetAsync("/url2").Result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            _fakeApiClient.GetAsync("/url3").Result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            _fakeApiClient.GetAsync("/url4").Result.StatusCode.Should().Be(StatusCodes.Status200OK);
        }
    }
}