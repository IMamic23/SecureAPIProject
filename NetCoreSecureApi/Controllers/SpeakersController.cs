using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using NetCoreSecureApi.Filters;
using NetCoreSecureApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace NetCoreSecureApi.Controllers
{
    [Route("api/camps/{moniker}/speakers")]
    [ValidateModel]
    [ApiVersion("1.0")]
    [ApiVersion("1.1")]
    public class SpeakersController : BaseController
    {
        protected readonly ICampRepository _repository;
        protected readonly ILogger<SpeakersController> _logger;
        protected readonly IMapper _mapper;
        protected readonly UserManager<CampUser> _userManager;

        public SpeakersController(
            ICampRepository repository,
            ILogger<SpeakersController> logger,
            IMapper mapper,
            UserManager<CampUser> userManager)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Get(string moniker, bool includeTalks = false)
        {
            try
            {
                var speakers = includeTalks ? _repository.GetSpeakersByMonikerWithTalks(moniker) : _repository.GetSpeakersByMoniker(moniker);

                return Ok(_mapper.Map<IEnumerable<SpeakerModel>>(speakers));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Threw error while fetching speakers: {ex}");
            }
            return BadRequest();
        }

        [HttpGet]
        [MapToApiVersion("1.1")]
        public virtual IActionResult GetWithCount(string moniker, bool includeTalks = false)
        {
            try
            {
                var speakers = includeTalks ? _repository.GetSpeakersByMonikerWithTalks(moniker) : _repository.GetSpeakersByMoniker(moniker);

                return Ok(new { count = speakers.Count(), result = _mapper.Map<IEnumerable<SpeakerModel>>(speakers) });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Threw error while fetching speakers: {ex}");
            }
            return BadRequest();
        }

        [HttpGet("{id}", Name = "SpeakerGet")]
        public IActionResult Get(string moniker, int id, bool includeTalks = false)
        {
            try
            {
                var speaker = includeTalks ? _repository.GetSpeakerWithTalks(id) : _repository.GetSpeaker(id);
                if (speaker == null)
                    return NotFound();
                if (speaker.Camp.Moniker != moniker)
                    return BadRequest("Speaker not in specific Camp");

                return Ok(_mapper.Map<SpeakerModel>(speaker));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Threw error while fetching speaker: {ex}");
            }
            return BadRequest();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Post(string moniker, [FromBody]SpeakerModel model)
        {
            try
            {
                var camp = _repository.GetCampByMoniker(moniker);
                if (camp == null)
                    return BadRequest("Couldn't find camp");

                var speaker = _mapper.Map<Speaker>(model);
                speaker.Camp = camp;

                var campUser = await _userManager.FindByNameAsync(this.User.Identity.Name);
                if(campUser != null)
                {
                    speaker.User = campUser;
                    _repository.Add(speaker);

                    if (await _repository.SaveAllAsync())
                    {
                        var url = Url.Link("SpeakerGet", new
                        {
                            moniker = camp.Moniker,
                            id = speaker.Id
                        });
                        return Created(url, _mapper.Map<SpeakerModel>(speaker));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown while adding speaker: {ex}");
            }
            return BadRequest("Couldn't add new speaker");
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string moniker, int id, [FromBody]SpeakerModel model)
        {
            try
            {
                var speaker = _repository.GetSpeaker(id);
                if (speaker == null)
                    return NotFound();
                if (speaker.Camp.Moniker != moniker)
                    return BadRequest("Speaker and Camp do not match");

                if (speaker.User.UserName == User.Identity.Name)
                    return Forbid();

                _mapper.Map(model, speaker);

                if (await _repository.SaveAllAsync())
                    return Ok(_mapper.Map<SpeakerModel>(model));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown while adding a speaker: {ex}");
            }

            return BadRequest("Couldn't update a speaker");
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string moniker, int id)
        {
            try
            {
                var speaker = _repository.GetSpeaker(id);
                if (speaker == null)
                    return NotFound();
                if (speaker.Camp.Moniker != moniker)
                    return BadRequest("Speaker and Camp do not match");

                if (speaker.User.UserName == User.Identity.Name)
                    return Forbid();

                _repository.Delete(speaker);

                if (await _repository.SaveAllAsync())
                    return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown while deleting a speaker: {ex}");
            }
            return BadRequest("Couldn't delete a speaker");
        }
    }
}
