using System.Net;
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
            _mockServer = WireMockServer.Start();
        }

        public async Task<string> GetMappings()
        {
            return await _client.GetStringAsync("/__admin/mappings");
        }

        public async Task Refresh()
        {
            await ResetMappings();
            var routes = await _repository.GetAll();

            foreach (var route in routes)
            {
                AddWireMockMapping(new RouteDefinition(route));
            }

            foreach (var mappings in _mockServer.MappingModels)
            {
                var response = await _client.PostAsJsonAsync("/__admin/mappings", mappings);
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
            _mockServer.ResetMappings();
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
                        .WithStatusCode(HttpStatusCode.OK));
        }
    }
}