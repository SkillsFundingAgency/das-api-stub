using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceWeb.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class DeleteController : ControllerBase
    {
        private readonly ILogger<SaveController> _logger;
        private readonly IWireMockHttpService _service;
        private readonly IDataRepository _repository;

        public DeleteController(ILogger<SaveController> logger, IWireMockHttpService service, IDataRepository repository)
        {
            _logger = logger;
            _service = service;
            _repository = repository;
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult> Delete([FromQuery] HttpMethod httpMethod, [FromQuery] string url)
        {
            _logger.LogInformation("[api-stub/save] called with parameters {httpMethod}, {url}", httpMethod, url);
            if (url == null) throw new ArgumentException(nameof(url));

            await _repository.Delete(httpMethod, url);
            await _service.Refresh();

            return Ok();
        }
    }
}
