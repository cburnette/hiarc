using System;
using System.Threading.Tasks;
using Hiarc.Core.Database;
using Hiarc.Core.Models;
using Hiarc.Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hiarc.Api.REST.Controllers
{
    [Authorize(Policy = Auth.REQUIRES_ADMIN)]
    [Route("[controller]")]
    public class AdminController : HiarcControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly HiarcSettings _hiarcSettings;
        private readonly IHiarcDatabase _hiarcDatabase;
        
        public AdminController( ILogger<AdminController> logger, 
                                IOptions<HiarcSettings> hiarchSettings,
                                IHiarcDatabase hiarcDatabase)
        {
            _logger = logger;
            _hiarcSettings = hiarchSettings.Value;
            _hiarcDatabase = hiarcDatabase;
        }

        [HttpPost("database/init")]
        public async Task<IActionResult> InitGraphDB()
        {
            try
            {
                await _hiarcDatabase.InitDatabase(Auth.ADMIN_NAME_CLAIM_VALUE);
                return Created(_hiarcSettings.BaseUri, "{}");
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }     
        }

        [HttpPut("database/reset")]
        public async Task<IActionResult> ResetGraphDB()
        {
            try
            {
                await _hiarcDatabase.ResetDatabase(Auth.ADMIN_NAME_CLAIM_VALUE);
                return Ok(new Empty());
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }     
        }
    }
}