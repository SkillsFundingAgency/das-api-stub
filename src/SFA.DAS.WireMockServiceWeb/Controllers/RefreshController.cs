using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.WireMockServiceWeb.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class RefreshController : ControllerBase
    {
        private readonly ILogger<RefreshController> _logger;

        public RefreshController(ILogger<RefreshController> logger)
        {
            _logger = logger;
        }

        // Kept only because the release pipeline's post-deploy step calls it. The stub app reads every request
        // straight from table storage, so there is no longer anything to synchronise.
        [HttpGet]
        [Route("refresh")]
        public IActionResult Refresh()
        {
            _logger.LogInformation("[api-stub/refresh] called; nothing to refresh");
            return Ok();
        }
    }
}
