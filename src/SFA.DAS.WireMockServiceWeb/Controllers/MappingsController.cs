using SFA.DAS.WireMockService.Data;
﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceWeb.Controllers
{
    [ApiController]
    [Route("api-stub")]
    public class MappingsController : ControllerBase
    {
        private readonly ILogger<MappingsController> _logger;
        private readonly IDataRepository _repository;

        public MappingsController(ILogger<MappingsController> logger, IDataRepository repository)
        {
            _logger = logger;
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
        [Route("find")]
        public IActionResult FindMapping([FromQuery] string url)
        {
            _logger.LogInformation("[api-stub/find] called");
            var mappings = _repository.Find(url).ToList();
            return new JsonResult(mappings);
        }
    }
}
