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
    public class ClassificationsController : HiarcControllerBase
    {
        private readonly ILogger<ClassificationsController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly HiarcSettings _hiarcSettings;
        private readonly IHiarcDatabase _hiarcDatabase;

        public ClassificationsController(ILogger<ClassificationsController> logger, 
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
                var theClassification = await _hiarcDatabase.GetClassification(key);
                return Ok(theClassification);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                var statusCode = StatusCodes.Status500InternalServerError;
                var error = new Error() { Message=ex.Message };
                return StatusCode(statusCode, error);
            }   
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var allClassifications = await _hiarcDatabase.GetAllClassifications();
                return Ok(allClassifications);             
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody]CreateClassificationRequest request)
        {
            try
            {
                var newClassification = await _hiarcDatabase.CreateClassification(request);
                var uri = $"{_hiarcSettings.BaseUri}/classifications/{newClassification.Key}";
                return Created(uri,newClassification);
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpPut("{key}")]
        public async Task<IActionResult> Update(string key, [FromBody]UpdateClassificationRequest request)
        {
            try
            {
                var updatedClassification = await _hiarcDatabase.UpdateClassification(key, request);
                return Ok(updatedClassification);
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
        [HttpPost("find")]
        public async Task<IActionResult> FindClassifications([FromBody]FindClassificationsRequest request)
        {
            try
            {
                var matchingClassifications = await _hiarcDatabase.FindClassifications(request);
                return Ok(matchingClassifications);          
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }
    }
}
