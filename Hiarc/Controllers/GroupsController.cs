using System.Threading.Tasks;
using Hiarc.Core.Models;
using Hiarc.Core.Models.Requests;
using Hiarc.Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Hiarc.Core.Database;

namespace Hiarc.Api.REST.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GroupsController : HiarcControllerBase
    {
        private readonly ILogger<GroupsController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly HiarcSettings _hiarcSettings;
        private readonly IHiarcDatabase _hiarcDatabase;

        public GroupsController(ILogger<GroupsController> logger, 
                                IHttpContextAccessor contextAccessor,
                                IOptions<HiarcSettings> hiarcSettings,
                                IHiarcDatabase hiarcDatabase)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
            _hiarcSettings = hiarcSettings.Value;
            _hiarcDatabase = hiarcDatabase;
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpGet("{key}")]
        public async Task<IActionResult> Get(string key)
        {
            try
            {
                var theGroup = await _hiarcDatabase.GetGroup(key);
                return Ok(theGroup);
            }
            catch(InvalidOperationException ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden);
            } 
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var allGroups = await _hiarcDatabase.GetAllGroups();
                return Ok(allGroups);
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody]CreateGroupRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();

                var newGroup = await _hiarcDatabase.CreateGroup(request, userKey);
                var uri = $"{_hiarcSettings.BaseUri}/groups/{newGroup.Key}";
                return Created(uri,newGroup);
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpPut("{key}")]
        public async Task<IActionResult> Update(string key, [FromBody]UpdateGroupRequest request)
        {
            try
            {
                var updatedGroup = await _hiarcDatabase.UpdateGroup(key, request);
                return Ok(updatedGroup);
            }
            catch(InvalidOperationException ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden);
            } 
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpDelete("{key}")]
        public async Task<IActionResult> Delete(string key)
        {
            try
            {
                await _hiarcDatabase.DeleteGroup(key);
                return Ok();          
            }
            catch(InvalidOperationException ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden);
            } 
            catch(Exception ex)
            {
               return BuildErrorResponse(ex, _logger);
            }   
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpPut("{key}/users/{userKey}")]
        public async Task<IActionResult> AddUser(string key, string userKey)
        {
            try
            {
                await _hiarcDatabase.AddUserToGroup(key, userKey);
                return Ok(new Empty());
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpPost("find")]
        public async Task<IActionResult> FindGroups([FromBody]FindGroupsRequest request)
        {
            try
            {
                var matchingGroups = await _hiarcDatabase.FindGroups(request);
                return Ok(matchingGroups);          
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }
    }
}
