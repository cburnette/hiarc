using System.Threading.Tasks;
using Hiarc.Core.Models.Requests;
using Hiarc.Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Hiarc.Core.Database;
using Hiarc.Core.Models;
using Amazon.Kinesis;
using Amazon;
using System.Text;
using System.IO;
using Amazon.Kinesis.Model;
using Hiarc.Core.Events;
using System.Collections.Generic;
using Hiarc.Core.Events.Models;

namespace Hiarc.Api.REST.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : HiarcControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly HiarcSettings _hiarcSettings;
        private readonly IHiarcDatabase _hiarcDatabase;

        public UsersController( ILogger<UsersController> logger, 
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
        [HttpPost]
        public async Task<IActionResult> Create([FromBody]CreateUserRequest request)
        {
            try
            {
                if (request.Key == Auth.ADMIN_NAME_CLAIM_VALUE)
                {
                    _logger.LogError($"Attempted to create user with Admin name '{Auth.ADMIN_NAME_CLAIM_VALUE}'");
                    return StatusCode(StatusCodes.Status403Forbidden);
                }

                var newUser = await _hiarcDatabase.CreateUser(request);
                var uri = $"{_hiarcSettings.BaseUri}/users/{newUser.Key}";

                return Created(uri,newUser);
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpPut("{key}")]
        public async Task<IActionResult> Update(string key, [FromBody]UpdateUserRequest request)
        {
            try
            {
                var updatedUser = await _hiarcDatabase.UpdateUser(key, request);
                return Ok(updatedUser);
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
        [HttpGet("{key}")]
        public async Task<IActionResult> Get(string key)
        {
            try
            {
                var theUser = await _hiarcDatabase.GetUser(key);
                return Ok(theUser);            
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
                await _hiarcDatabase.DeleteUser(key);
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
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var allUsers = await _hiarcDatabase.GetAllUsers();
                return Ok(allUsers);             
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [Authorize]
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userKey = _contextAccessor.UserKeyFromContext();
            //var userRole = _contextAccessor.UserRoleFromContext();

            try
            {
                var theUser = await _hiarcDatabase.GetUser(userKey);
                return Ok(theUser);            
            }
            catch(InvalidOperationException ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status404NotFound);     
            } 
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpPost("find")]
        public async Task<IActionResult> FindUsers([FromBody]FindUsersRequest request)
        {
            try
            {
                var matchingUsers = await _hiarcDatabase.FindUsers(request);
                return Ok(matchingUsers);          
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpGet("{key}/groups")]
        public async Task<IActionResult> GetGroupsForUser(string key)
        {
            try
            {
                var groups = await _hiarcDatabase.GetGroupsForUser(key);
                return Ok(groups);           
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        } 

        [Authorize]
        [HttpGet("current/groups")]
        public async Task<IActionResult> GetGroupsForCurrentUser()
        {
            var userKey = _contextAccessor.UserKeyFromContext();

            try
            {
                var groups = await _hiarcDatabase.GetGroupsForUser(userKey);
                return Ok(groups);           
            }
            catch(InvalidOperationException ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden); //don't give away too much information (i.e. not found)
            } 
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }
    }
}
