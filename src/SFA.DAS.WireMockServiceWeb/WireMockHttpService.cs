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
        WireMockServer _mockServer = WireMockServer.Start();

        private readonly WireMockHttpClient _client;
        private readonly IDataRepository _repository;

        public WireMockHttpService(WireMockHttpClient client, IDataRepository repository)
        {
            _client = client;
            _repository = repository;
        }

        public async Task<string> GetMappings()
        {
            return await _client.GetStringAsync("/__admin/mappings");
        }


        public async Task Refresh()
        {
            //MockServer.ResetMappings();
            //var routes = await DataRepository.GetAll();

            //foreach (var route in routes)
            //{
            //    ConfigureRoute(new RouteDefinition(route));
            //}
        }

        private void ConfigureRoute(RouteDefinition route)
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
                        .WithStatusCode(HttpStatusCode.OK));
        }
    }
}