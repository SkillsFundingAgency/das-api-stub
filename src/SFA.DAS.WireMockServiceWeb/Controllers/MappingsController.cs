using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceWeb.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class MappingsController : ControllerBase
    {
        private readonly ILogger<MappingsController> _logger;
        private readonly IWireMockHttpService _service;
        private readonly IDataRepository _repository;

        public MappingsController(ILogger<MappingsController> logger, IWireMockHttpService service, IDataRepository repository)
        {
            _logger = logger;
            _service = service;
            _repository = repository;
        }

        [HttpGet]
        [Route("database")]
        public IActionResult GetMappingsFromDatabase()
        {
            _logger.LogInformation("[api-stub/database] called");
            var mappings = _repository.GetAll();
            return new JsonResult(mappings);
        }

        [HttpGet]
        [Route("wiremock")]
        public async Task<IActionResult> GetMappingsFromWireMock()
        {
            _logger.LogInformation("[api-stub/wiremock] called");
            var mappings = await _service.GetMappings();
            return Content(mappings);
        }
    }
}
