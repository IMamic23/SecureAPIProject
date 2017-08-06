using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace NetCoreSecureApi.Controllers
{
    [Route("api/[controller]")]
    public class OperationsController : Controller
    {
        private readonly ILogger<OperationsController> _logger;
        private readonly IConfigurationRoot _config;

        public OperationsController(ILogger<OperationsController> logger,
            IConfigurationRoot config)
        {
            _logger = logger;
            _config = config;
        }
        [HttpOptions("reloadConfig")]
        public IActionResult ReloadConfiguration()
        {
            try
            {
                _config.Reload();

                return Ok("Configuration reloaded");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown while reloading configuration: {ex}");
            }
            return BadRequest();
        }
    }
}
