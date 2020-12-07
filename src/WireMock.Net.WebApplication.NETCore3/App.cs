using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WireMock.Net.WebApplication
{
    public class App : IHostedService
    {
        private readonly IWireMockService _service;

        public App(IWireMockService service)
        {
            _service = service;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("STARTING WIREMOCK");
            _service.Start();
            Console.WriteLine($"STARTED WIREMOCK");
            foreach (var serverUrl in _service.Server.Urls)
            {
                Console.WriteLine($"STARTED WIREMOCK ON: {serverUrl}");
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _service.Stop();
            return Task.CompletedTask;
        }
    }
}