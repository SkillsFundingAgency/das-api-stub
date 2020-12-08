using WireMock.Server;

namespace SFA.DAS.WireMockServiceApi
{
    public interface IWireMockService
    {
        void Start();

        void Stop();

        public WireMockServer Server { get; }
    }
}