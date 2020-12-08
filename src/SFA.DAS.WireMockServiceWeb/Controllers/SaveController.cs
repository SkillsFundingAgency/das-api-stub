using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceWeb.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class SaveController : ControllerBase
    {
        private readonly ILogger<SaveController> _logger;
        private readonly FakeApi _fakeApi;

        public SaveController(ILogger<SaveController> logger, FakeApi fakeApi)
        {
            _logger = logger;
            _fakeApi = fakeApi;
        }

        [HttpPost]
        [Route("save")]
        public async Task<ActionResult> Save([FromQuery] HttpMethod httpMethod, [FromQuery] string url, [FromQuery] bool refresh, [FromBody] object jsonData)
        {
            _logger.LogInformation("[api-stub/save] called with parameters {httpMethod}, {url}, {jsonData}", httpMethod, url, jsonData);
            try
            {
                if (url == null) throw new ArgumentException(nameof(url));
                if (jsonData == null) throw new ArgumentException(nameof(jsonData));

                await DataRepository.InsertOrReplace(httpMethod, url, jsonData);
                if (refresh)
                {
                    await _fakeApi.Refresh();
                }
            }
            catch (Exception e)
            {
                _logger.LogError("[api-stub/save]" + e);
                throw;
            }

            return NoContent();
        }
    }
}
