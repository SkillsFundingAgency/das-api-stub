using System;
using System.Net;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace SFA.DAS.WireMockServiceWeb
{
    public class FakeApi : IDisposable
    {
        private bool _disposed;

        public string BaseAddress { get; }

        public WireMockServer MockServer { get; }

        public FakeApi(WireMockServer mockServer)
        {
            MockServer = mockServer;
            BaseAddress = MockServer.Urls[0];
            Refresh().ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (MockServer.IsStarted)
                {
                    MockServer.Stop();
                }
                MockServer.Dispose();
            }

            _disposed = true;
        }

        public async Task Refresh()
        {
            MockServer.ResetMappings();
            var routes = await DataRepository.GetAll();

            foreach (var route in routes)
            {
                ConfigureRoute(new RouteDefinition(route));
            }
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

            MockServer
                .Given(request)
                .RespondWith(
                    Response.Create()
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(route.Data)
                        .WithStatusCode(HttpStatusCode.OK));
        }
    }
}
