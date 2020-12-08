using WireMock.Server;

namespace WireMock.Net.WebApplication
{
    public interface IWireMockService
    {
        void Start();

        void Stop();

        public WireMockServer Server { get; }
    }
}