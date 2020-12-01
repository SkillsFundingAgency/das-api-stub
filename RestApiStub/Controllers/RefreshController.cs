using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace RestApiStub.Controllers
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

        [HttpGet]
        [Route("refresh")]
        public async Task<IActionResult> Refresh()
        {
            // TODO: Call FakeApiRefresh
            return Ok();
        }
    }
}
