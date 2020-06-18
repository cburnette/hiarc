using System;
using Hiarc.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hiarc.Api.REST.Controllers
{
    public abstract class HiarcControllerBase : ControllerBase
    {
        protected ObjectResult BuildErrorResponse<LoggerType>(Exception ex, ILogger<LoggerType> logger, int statusCode = StatusCodes.Status500InternalServerError)
        {
            logger.LogError(ex.Message);
            var error = new Error() { Message=ex.Message };
            return StatusCode(statusCode, error);
        }
    }
}