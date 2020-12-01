using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace RestApiStub
{
    public class FakeApiBuilder
    {
        private readonly WireMockServer _server;
        public FakeApi Build()
        {
            return new FakeApi(_server);
        }

        public static FakeApiBuilder Create(int port)
        {
            return new FakeApiBuilder(port);
        }

        private FakeApiBuilder(int port)
        {
            _server = WireMockServer.StartWithAdminInterface(port);
        }

        public FakeApiBuilder WithHealthCheck()
        {
            _server
                .Given(
                    Request
                        .Create()
                        .WithPath("/health")
                        .UsingGet()
                )
                .RespondWith(
                    Response.Create()
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("{\"result\": \"OK\"}")
                        .WithStatusCode(HttpStatusCode.OK));

            return this;
        }
    }
}
