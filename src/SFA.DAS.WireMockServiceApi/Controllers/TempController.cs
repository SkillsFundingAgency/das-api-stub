using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceApi.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class TempController : ControllerBase
    {
        private readonly ILogger<TempController> _logger;

        public TempController(ILogger<TempController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("start")]
        public async Task<IActionResult> StartWiremock()
        {
            _logger.LogInformation("[api-stub/start] called");
            try
            {
                FakeApiBuilder.Create(Settings.WireMockPort).Build();
            }
            catch (Exception e)
            {
                _logger.LogError("[api-stub/start]" + e);
                throw;

            }
            return Ok();
        }
    }
}
