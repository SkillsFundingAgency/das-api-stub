using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
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
        private HttpClient _testClient;

        [OneTimeSetUp]
        public void Setup()
        {
            var builder = new WebHostBuilder().UseStartup<Startup>();

            _testServer = new TestServer(builder);
            _testClient = _testServer.CreateClient();
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            await DataRepository.DropTableStorage();
        }

        [Test]
        public async Task stores_data_in_local_storage()
        {
            var expected = new TestObject { Key = 123, Value = "String value 123" };
            const HttpMethod httpMethod = HttpMethod.Get;

            var key = Uri.EscapeDataString("data?v=1.0&id=" + expected.Key);
            var url = $"api-stub/save?httpMethod={httpMethod}&url={key}";

            var response = await _testClient.PostAsJsonAsync(url, expected);
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

            var response = await _testClient.PostAsJsonAsync(url, original);
            response.EnsureSuccessStatusCode();

            var expected = new TestObject { Key = 789, Value = "String value 789" };

            response = await _testClient.PostAsJsonAsync(url, expected);
            response.EnsureSuccessStatusCode();

            var data = await DataRepository.GetJsonData(httpMethod, key);

            data.Should().BeEquivalentTo(JsonSerializer.Serialize(expected));
        }
    }
}