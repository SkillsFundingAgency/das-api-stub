using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace RestApiStub.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class RefreshController : ControllerBase
    {
        private readonly ILogger<RefreshController> _logger;
        private readonly FakeApi _fakeApi;

        public RefreshController(ILogger<RefreshController> logger, FakeApi fakeApi)
        {
            _logger = logger;
            _fakeApi = fakeApi;
        }

        [HttpGet]
        [Route("refresh")]
        public async Task<IActionResult> Refresh()
        {
            _logger.LogInformation("[api-stub/refresh] called");
            try
            {
                await _fakeApi.Refresh();
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
