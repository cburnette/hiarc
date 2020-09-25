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
    public class LegalHoldsController : HiarcControllerBase
    {
        private readonly ILogger<LegalHoldsController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly HiarcSettings _hiarcSettings;
        private readonly IHiarcDatabase _hiarcDatabase;

        public LegalHoldsController(ILogger<LegalHoldsController> logger, 
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
                var theLegalHold = await _hiarcDatabase.GetLegalHold(key);
                return Ok(theLegalHold);
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody]CreateLegalHoldRequest request)
        {
            try
            {
                var newLegalHold = await _hiarcDatabase.CreateLegalHold(request);
                var uri = $"{_hiarcSettings.BaseUri}/legalholds/{newLegalHold.Key}";
                return Created(uri,newLegalHold);
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        // [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        // [HttpPut("{key}")]
        // public async Task<IActionResult> Update(string key, [FromBody]UpdateRetentionPolicyRequest request)
        // {
        //     try
        //     {
        //         var updatedRetentionPolicy = await _hiarcDatabase.UpdateRetentionPolicy(key, request);
        //         return Ok(updatedRetentionPolicy);
        //     }
        //     catch(ArgumentException ex)
        //     {
        //         _logger.LogWarning(ex.Message);
        //         return StatusCode(StatusCodes.Status403Forbidden);
        //     }
        //     catch(InvalidOperationException ex)
        //     {
        //         _logger.LogError(ex.Message);
        //         return StatusCode(StatusCodes.Status403Forbidden);
        //     } 
        //     catch(Exception ex)
        //     {
        //         return BuildErrorResponse(ex, _logger);
        //     }
        // }

        // [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        // [HttpGet]
        // public async Task<IActionResult> GetAll()
        // {
        //     try
        //     {
        //         var allPolicies = await _hiarcDatabase.GetAllRetentionPolicies();
        //         return Ok(allPolicies);             
        //     }
        //     catch(Exception ex)
        //     {
        //         return BuildErrorResponse(ex, _logger);
        //     }   
        // }

        // [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        // [HttpPost("find")]
        // public async Task<IActionResult> FindRetentionPolicies([FromBody]FindRetentionPoliciesRequest request)
        // {
        //     try
        //     {
        //         var matchingPolicies = await _hiarcDatabase.FindRetentionPolicies(request);
        //         return Ok(matchingPolicies);          
        //     }
        //     catch(Exception ex)
        //     {
        //         return BuildErrorResponse(ex, _logger);
        //     }   
        // }
    }
}
