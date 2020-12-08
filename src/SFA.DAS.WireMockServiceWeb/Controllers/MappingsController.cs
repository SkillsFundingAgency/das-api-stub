using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceWeb.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class MappingsController : ControllerBase
    {
        private readonly ILogger<MappingsController> _logger;
        private readonly IWireMockHttpService _service;

        public MappingsController(ILogger<MappingsController> logger, IWireMockHttpService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet]
        [Route("mappings")]
        public async Task<IActionResult> GetMappings()
        {
            _logger.LogInformation("[api-stub/mappings] called");
            string mappings;
            try
            {
                mappings = await _service.GetMappings();
            }
            catch (Exception e)
            {
                _logger.LogError("[api-stub/mappings]" + e);
                throw;

            }
            return Content(mappings);
        }
    }

}
