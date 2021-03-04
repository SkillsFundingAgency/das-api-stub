using System;
using System.Net.Http;
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
    }

    public class WireMockHttpClient : HttpClient { }

    public class WireMockHttpService : IWireMockHttpService
    {
        private readonly WireMockHttpClient _client;
        private readonly IDataRepository _repository;
        private readonly WireMockServer _mockServer;

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
            await ResetMappings();
            var routes = _repository.GetAll();

            foreach (var route in routes)
            {
                AddWireMockMapping(new RouteDefinition(route));
            }

            foreach (var mapping in _mockServer.MappingModels)
            {
                var response = await _client.PostAsJsonAsync("/__admin/mappings", mapping);
                response.EnsureSuccessStatusCode();
            }
        }

        private async Task ResetMappings()
        {
            var response = await _client.PostNothingAsync("/__admin/mappings/reset");
            response.EnsureSuccessStatusCode();
            _mockServer.ResetMappings();
        }

        private void AddWireMockMapping(RouteDefinition route)
        {
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
                .RespondWith(
                    Response.Create()
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(route.Data)
                        .WithStatusCode(route.HttpStatusCode)
                    );
        }
    }
}