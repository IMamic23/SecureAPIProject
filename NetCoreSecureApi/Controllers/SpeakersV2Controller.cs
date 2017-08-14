using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using NetCoreSecureApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetCoreSecureApi.Controllers
{
    [Route("api/camps/{moniker}/speakers")]
    [ApiVersion("2.0")]
    public class SpeakersV2Controller : SpeakersController
    {
        public SpeakersV2Controller(ICampRepository repository,
            ILogger<SpeakersController> logger,
            IMapper mapper,
            UserManager<CampUser> userManager)
            : base(repository, logger, mapper, userManager)
        {
        }

        [MapToApiVersion("2.0")]
        public override IActionResult GetWithCount(string moniker, bool includeTalks = false)
        {
            try
            {
                var speakers = includeTalks ? _repository.GetSpeakersByMonikerWithTalks(moniker) : _repository.GetSpeakersByMoniker(moniker);

                return Ok(new
                {
                    currentTime = DateTime.UtcNow,
                    count = speakers.Count(),
                    result = _mapper.Map<IEnumerable<Speaker2Model>>(speakers),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Threw error while fetching speakers: {ex}");
            }
            return BadRequest();
        }
    }
}
