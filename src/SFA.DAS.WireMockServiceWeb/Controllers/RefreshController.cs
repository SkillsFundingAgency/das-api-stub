using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceWeb.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class RefreshController : ControllerBase
    {
        private readonly ILogger<RefreshController> _logger;
        private readonly IWireMockHttpService _service;

        public RefreshController(ILogger<RefreshController> logger, IWireMockHttpService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet]
        [Route("refresh")]
        public async Task<IActionResult> Refresh()
        {
            _logger.LogInformation("[api-stub/refresh] called");
            try
            {
                await _service.Refresh();
            }
            catch (Exception e)
            {
                _logger.LogError("[api-stub/refresh]" + e);
                throw;
            }
            return Ok();
        }
    }
}
