using FluentAssertions;
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
        private FakeApi _fakeApi;
        private HttpClient _client;

        [OneTimeSetUp]
        public async Task Setup()
        {
            await DataRepository.CreateTableStorage();
            _fakeApi = FakeApiBuilder.Create(8088).WithHealthCheck().Build();
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:8088") };
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            _fakeApi.Dispose();
            _client.Dispose();
            await DataRepository.DropTableStorage();
        }

        [Test]
        public async Task wiremock_health_endpoint_returns_Ok()
        {
            var url = "/health";
            var response = await _client.GetStringAsync(url);
            response.Should().Be("{\"result\": \"OK\"}");
        }

        [Test]
        public async Task wiremock_admin_endpoint_returns_Ok()
        {
            var url = "/__admin/mappings";
            var response = await _client.GetAsync(url);
            response.StatusCode.Should().Be(200);
        }

        [Test]
        public async Task new_GET_route_returns_expected_data_from_storage()
        {
            // Arrange
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            var url = "/external-api/data?v=1.0";
            await DataRepository.InsertOrReplace(HttpMethod.Get, url, expected);

            // Act
            await _fakeApi.Refresh();

            // Assert
            var response = await _client.GetFromJsonAsync<TestObject>(url);
            response.Should().BeEquivalentTo(expected);
        }


        [Test]
        public async Task new_POST_route_returns_expected_data_from_storage()
        {
            // Arrange
            var expected = new TestObject { Key = 456, Value = "String value 456" };
            var url = "/external-api/data?v=1.0&id=456";
            await DataRepository.InsertOrReplace(HttpMethod.Post, url, expected);

            // Act
            await _fakeApi.Refresh();

            // Assert
            var response = await _client.PostAsync(url, new StringContent(""));
            var returnResult = response.Content.ReadAsStringAsync().Result;
            returnResult.Should().BeEquivalentTo(JsonSerializer.Serialize(expected));
        }
    }
}