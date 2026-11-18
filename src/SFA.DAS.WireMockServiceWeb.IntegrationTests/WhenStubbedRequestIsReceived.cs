using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SFA.DAS.Testing.AzureStorageEmulator;
using SFA.DAS.WireMockService.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceWeb.IntegrationTests
{
    public class WhenStubbedRequestIsReceived
    {
        private readonly string _tableName = $"Test{Guid.NewGuid():N}";
        private DataRepository _repository;
        private TestServer _firstInstance;
        private TestServer _secondInstance;
        private HttpClient _firstInstanceClient;
        private HttpClient _secondInstanceClient;

        [OneTimeSetUp]
        public async Task Setup()
        {
            AzureStorageEmulatorManager.StartStorageEmulator();

            _repository = new DataRepository(Options.Create(new ApiStubOptions { StorageTableName = _tableName }));
            await _repository.CreateTableStorage();

            // Two instances over one table stand in for the stub app scaled out behind a load balancer
            _firstInstance = BuildStubAppInstance();
            _secondInstance = BuildStubAppInstance();
            _firstInstanceClient = _firstInstance.CreateClient();
            _secondInstanceClient = _secondInstance.CreateClient();
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            _firstInstanceClient?.Dispose();
            _secondInstanceClient?.Dispose();
            _firstInstance?.Dispose();
            _secondInstance?.Dispose();
            await _repository.DropTableStorage();
        }

        [Test]
        public async Task a_saved_mapping_is_served_by_every_instance()
        {
            var path = $"/{Guid.NewGuid()}";
            await _repository.InsertOrReplace("Get", path, new TestObject { Key = 1, Value = "saved" });

            foreach (var client in new[] { _firstInstanceClient, _secondInstanceClient })
            {
                var response = await client.GetAsync(path);

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                (await response.Content.ReadAsStringAsync()).Should().Be("{\"Key\":1,\"Value\":\"saved\"}");
            }
        }

        [Test]
        public async Task a_request_with_an_equivalent_url_is_served()
        {
            var path = $"/{Guid.NewGuid()}";
            await _repository.InsertOrReplace("Get", $"{path.ToUpperInvariant()}?b=2&a=1", "saved");

            var response = await _firstInstanceClient.GetAsync($"{path}?a=1&b=2");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public async Task the_response_is_json()
        {
            var path = $"/{Guid.NewGuid()}";
            await _repository.InsertOrReplace("Get", path, "saved");

            var response = await _firstInstanceClient.GetAsync(path);

            response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        }

        [Test]
        public async Task the_saved_status_code_is_returned()
        {
            var path = $"/{Guid.NewGuid()}";
            await _repository.InsertOrReplace("Put", path, new { }, HttpStatusCode.Created);

            var response = await _firstInstanceClient.PutAsync(path, null);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Test]
        public async Task a_no_content_mapping_is_served_without_a_body()
        {
            var path = $"/{Guid.NewGuid()}";
            await _repository.InsertOrReplace("Delete", path, new { }, HttpStatusCode.NoContent);

            var response = await _firstInstanceClient.DeleteAsync(path);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        }

        [Test]
        public async Task a_request_using_a_different_method_is_not_found()
        {
            var path = $"/{Guid.NewGuid()}";
            await _repository.InsertOrReplace("Get", path, "saved");

            var response = await _firstInstanceClient.PostAsync(path, null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task an_unmatched_request_is_not_found()
        {
            var response = await _firstInstanceClient.GetAsync($"/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await response.Content.ReadAsStringAsync()).Should().Contain("No matching mapping found");
        }

        private TestServer BuildStubAppInstance()
        {
            var settings = new Dictionary<string, string>
            {
                { "ApiStubSettings:StorageTableName", _tableName }
            };

            var builder = new WebHostBuilder()
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(settings))
                .UseStartup<WireMockServiceApi.Startup>();

            return new TestServer(builder);
        }
    }
}
