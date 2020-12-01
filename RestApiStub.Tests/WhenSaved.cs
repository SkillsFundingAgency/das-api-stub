using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
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
    public class WhenSaved
    {
        private TestServer _testServer;
        private HttpClient _webApiClient;
        private HttpClient _fakeApiClient;

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
        public async Task stores_data_in_local_storage()
        {
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const HttpMethod httpMethod = HttpMethod.Get;

            var key = Uri.EscapeDataString("data?v=1.0&id=" + expected.Key);
            var url = $"api-stub/save?httpMethod={httpMethod}&url={key}";

            var response = await _webApiClient.PostAsJsonAsync(url, expected);
            response.EnsureSuccessStatusCode();

            var data = await DataRepository.GetJsonData(httpMethod, key);

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

            var data = await DataRepository.GetJsonData(httpMethod, key);

            data.Should().BeEquivalentTo(JsonSerializer.Serialize(expected));
        }

        [Test]
        public async Task saved_data_is_available_via_fakeApi_after_save()
        {
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const HttpMethod httpMethod = HttpMethod.Get;

            const string key = "/get-some-data";
            var url = $"api-stub/save?httpMethod={httpMethod}&url={key}";

            var response = await _webApiClient.PostAsJsonAsync(url, expected);
            response.EnsureSuccessStatusCode();

            var data = await _fakeApiClient.GetFromJsonAsync<TestObject>(key);

            data.Should().BeEquivalentTo(expected);
        }
    }
}