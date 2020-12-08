using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceWeb.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class StatusController : ControllerBase
    {
        private readonly ILogger<StatusController> _logger;
        private readonly HttpClient _client;

        public StatusController(ILogger<StatusController> logger, HttpClient client)
        {
            _logger = logger;
            _client = client;
        }

        [HttpGet]
        [Route("health")]
        public async Task<IActionResult> CheckHealth()
        {
            _logger.LogInformation("[api-stub/health] called");
            try
            {
                FakeApiBuilder.Create(Settings.WireMockPort).Build();
            }
            catch (Exception e)
            {
                _logger.LogError("[api-stub/health]" + e);
                throw;

            }
            return Ok();
        }
    }
}
