using SFA.DAS.WireMockService.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SFA.DAS.Testing.AzureStorageEmulator;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace SFA.DAS.WireMockServiceWeb.IntegrationTests
{
    public class WhenDeleted
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
        public async Task deletes_existing_data_in_local_storage_when_same_key_is_used()
        {
            const HttpMethod httpMethod = HttpMethod.Get;

            var key = Uri.EscapeDataString("/data?v=1.0&id=12345");

            await _dataRepository.InsertOrReplace(httpMethod.ToString(), key, "test");

            var response = await _webApiClient.DeleteAsync($"api-stub/delete?httpMethod={httpMethod}&url={key}");
            response.EnsureSuccessStatusCode();

            var data = (await _dataRepository.Get(httpMethod.ToString(), key))?.Data;
            data.Should().BeNullOrEmpty();
        }

        [Test]
        public async Task deleted_mapping_is_no_longer_served_by_the_stub_app()
        {
            const HttpMethod httpMethod = HttpMethod.Get;
            var key = $"/{Guid.NewGuid()}";

            var save = await _webApiClient.PostAsync($"api-stub/save?httpMethod={httpMethod}&url={key}", new StringContent("{}", Encoding.UTF8, "application/json"));
            save.EnsureSuccessStatusCode();
            (await _stubAppClient.GetAsync(key)).StatusCode.Should().Be(HttpStatusCode.OK);

            var response = await _webApiClient.DeleteAsync($"api-stub/delete?httpMethod={httpMethod}&url={key}");
            response.EnsureSuccessStatusCode();

            (await _stubAppClient.GetAsync(key)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task deleting_a_mapping_that_does_not_exist_is_not_found()
        {
            var response = await _webApiClient.DeleteAsync($"api-stub/delete?httpMethod={HttpMethod.Get}&url=/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
