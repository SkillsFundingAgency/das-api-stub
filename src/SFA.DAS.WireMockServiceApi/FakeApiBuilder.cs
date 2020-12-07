using System;
using System.Security.Cryptography.X509Certificates;
using WireMock.Server;
using WireMock.Settings;

namespace SFA.DAS.WireMockServiceApi
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
            using X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            IWireMockServerSettings settings = new WireMockServerSettings()
            {
                Port = port,
                StartAdminInterface = true,
                UseSSL = true,
                CertificateSettings = new WireMockCertificateSettings()
                {
                    X509StoreName = store.Name
                }
            };
            store.Close();

            try
            {
                _server = WireMockServer.Start(settings);
            }
            catch (Exception e)
            {
                Console.WriteLine("Available certificates: " + store.Certificates);
                throw;
            }
        }
    }
}
