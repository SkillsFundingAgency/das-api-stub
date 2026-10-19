using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace SFA.DAS.WireMockServiceWeb
{
    public interface IWireMockHttpService
    {
        Task<string> GetMappings();
        Task Refresh();
        Task AddOrUpdateMapping(DataRepository.MappingData route);
        Task RemoveMapping(DataRepository.MappingData route);
    }

    public class WireMockHttpClient : HttpClient { }

    public class WireMockHttpService : IWireMockHttpService
    {
        private readonly WireMockHttpClient _client;
        private readonly IDataRepository _repository;
        private readonly WireMockServer _mockServer;

        // Refresh resets the mockServer, so concurrent calls cause mapping loss; anything building mappings on it must honour this lock
        private readonly SemaphoreSlim _mockServerLock = new SemaphoreSlim(1, 1);

        public WireMockHttpService(WireMockHttpClient client, IDataRepository repository)
        {
            _client = client;
            _repository = repository;
            try
            {
                _mockServer = WireMockServer.Start(Guid.NewGuid().ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("FAILED TO START WIREMOCK SERVICE");
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<string> GetMappings()
        {
            return await _client.GetStringAsync("/__admin/mappings");
        }

        public async Task Refresh()
        {
            await _mockServerLock.WaitAsync();
            try
            {
                await ResetMappings();
                var routes = _repository.GetAll();

                foreach (var route in routes)
                {
                    AddWireMockMapping(route);
                }

                foreach (var mapping in _mockServer.MappingModels)
                {
                    var response = await _client.PostAsJsonAsync("/__admin/mappings", mapping);
                    response.EnsureSuccessStatusCode();
                }
            }
            finally
            {
                _mockServerLock.Release();
            }
        }

        public async Task AddOrUpdateMapping(DataRepository.MappingData route)
        {
            await _mockServerLock.WaitAsync();
            try
            {
                var guid = MappingGuid(route);
                AddWireMockMapping(route);
                var mapping = _mockServer.MappingModels.Single(m => m.Guid == guid);
                _mockServer.DeleteMapping(guid);

                // PUT adds the mapping or replaces it in place, so other mappings keep being served throughout
                var response = await _client.PutAsJsonAsync($"/__admin/mappings/{guid}", mapping);
                response.EnsureSuccessStatusCode();
            }
            finally
            {
                _mockServerLock.Release();
            }
        }

        public async Task RemoveMapping(DataRepository.MappingData route)
        {
            await _mockServerLock.WaitAsync();
            try
            {
                var response = await _client.DeleteAsync($"/__admin/mappings/{MappingGuid(route)}");

                // NotFound means WireMock was not serving it, which is the outcome we want
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    response.EnsureSuccessStatusCode();
                }
            }
            finally
            {
                _mockServerLock.Release();
            }
        }

        // Derived from the route, so save, delete and refresh all address the same WireMock mapping
        private static Guid MappingGuid(DataRepository.MappingData route)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"{route.HttpMethod.ToUpperInvariant()}_{route.Url}"));
            return new Guid(hash);
        }

        private async Task ResetMappings()
        {
            var response = await _client.PostNothingAsync("/__admin/mappings/reset");
            response.EnsureSuccessStatusCode();
            _mockServer.ResetMappings();
        }

        private void AddWireMockMapping(DataRepository.MappingData mappingData)
        {
            var route = new RouteDefinition(mappingData);
            var request = Request
                .Create()
                .UsingMethod(route.HttpMethod)
                .WithPath(route.BaseUrl);

            foreach (var (key, value) in route.Parameters)
            {
                request.WithParam(key, value);
            }

            _mockServer
                .Given(request)
                .WithGuid(MappingGuid(mappingData))
                .RespondWith(
                    Response.Create()
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(route.Data)
                        .WithStatusCode(route.HttpStatusCode)
                    );
        }
    }
}