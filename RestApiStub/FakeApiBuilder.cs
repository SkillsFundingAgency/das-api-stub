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
    }
}
