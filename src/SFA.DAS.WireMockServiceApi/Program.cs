using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SFA.DAS.WireMockServiceApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<WireMockServerStartup>();
                });
    }

    //private static IHostBuilder CreateHostBuilder(string[] args)
    //        => Host.CreateDefaultBuilder(args)
    //            .ConfigureServices((host, services) => ConfigureServices(services, host.Configuration));

}
