using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace RestApiStub.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class SaveController : ControllerBase
    {
        private readonly ILogger<SaveController> _logger;

        public SaveController(ILogger<SaveController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("save")]
        public async Task<ActionResult> Save([FromQuery] HttpMethod httpMethod, [FromQuery] string url, [FromBody] object jsonData)
        {
            if (url == null) throw new ArgumentException(nameof(httpMethod));
            if (jsonData == null) throw new ArgumentException(nameof(httpMethod));

            await DataRepository.InsertOrReplace(httpMethod, url, jsonData);

            return NoContent();
        }
    }
}
