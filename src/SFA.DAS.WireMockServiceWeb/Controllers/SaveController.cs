using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceWeb.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class SaveController : ControllerBase
    {
        private readonly ILogger<SaveController> _logger;
        private readonly IWireMockHttpService _service;
        private readonly IDataRepository _repository;

        public SaveController(ILogger<SaveController> logger, IWireMockHttpService service, IDataRepository repository)
        {
            _logger = logger;
            _service = service;
            _repository = repository;
        }

        [HttpPost]
        [Route("save")]
        public async Task<ActionResult> Save([FromQuery] HttpMethod httpMethod, [FromQuery] string url, [FromBody] object jsonData, [FromQuery] HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            _logger.LogInformation("[api-stub/save] called with parameters {httpMethod}, {url}, {jsonData}, {httpStatusCode}", httpMethod, url, jsonData, httpStatusCode);
            if (url == null) throw new ArgumentException(nameof(url));

            await _repository.InsertOrReplace(httpMethod, url, jsonData, httpStatusCode);
            await _service.Refresh();

            return Ok();
        }
    }
}
