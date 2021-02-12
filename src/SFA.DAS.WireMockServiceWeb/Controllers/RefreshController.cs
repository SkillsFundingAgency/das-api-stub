using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
            await _service.Refresh();
            return Ok();
        }
    }
}
