using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Hiarc.Core.Database;
using Hiarc.Core.Models;
using Hiarc.Core.Models.Requests;
using Hiarc.Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Hiarc.Api.REST.Controllers
{
    [Authorize(Policy = Hiarc.Auth.REQUIRES_ADMIN)]
    [ApiController]
    [Route("[controller]")]
    public class TokensController : HiarcControllerBase
    {
        private readonly ILogger<TokensController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly HiarcSettings _hiarcSettings;
        private readonly IHiarcDatabase _hiarcDatabase;

        public TokensController( ILogger<TokensController> logger, 
                                IHttpContextAccessor contextAccessor,
                                IOptions<HiarcSettings> hiarchSettings,
                                IHiarcDatabase hiarcDatabase
                              )
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
            _hiarcSettings = hiarchSettings.Value;
            _hiarcDatabase = hiarcDatabase;
        }
   
        // //For now at least, don't allow JWT tokens for Admin; you must use the admin token in the X-Hiarc-Api-Key header
        // [HttpGet("admin")]
        // public IActionResult GetAdminToken()
        // {
        //     var tokenHandler = new JwtSecurityTokenHandler();
        //     var key = Encoding.ASCII.GetBytes(_hiarcSettings.JwtSigningKey);
        //     var tokenDescriptor = new SecurityTokenDescriptor
        //     {
        //         Subject = new ClaimsIdentity(new Claim[] 
        //         {
        //             new Claim(ClaimTypes.Role, Auth.ADMIN_ROLE_CLAIM_VALUE),
        //             new Claim(ClaimTypes.Name, Auth.ADMIN_NAME_CLAIM_VALUE)
        //         }),
        //         Expires = DateTime.UtcNow.AddDays(7),
        //         SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        //     };

        //     var token = tokenHandler.CreateToken(tokenDescriptor);
        //     return Content(tokenHandler.WriteToken(token));
        // }

        [HttpPost("user")]
        public async Task<IActionResult> CreateUserToken([FromBody]CreateUserTokenRequest request)
        {
            try
            {
                // validate that the user exists in the database
                if(!await _hiarcDatabase.IsValidUserKey(request.Key))
                {
                    _logger.LogWarning($"A token was requested for user key '{request.Key}' but the user key does not exist");
                    return StatusCode(StatusCodes.Status404NotFound);
                }

                var utcNow = DateTime.UtcNow;
                var expiresInMinutes = request.ExpirationMinutes ?? _hiarcSettings.JWTTokenExpirationMinutes;
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtKey = Encoding.ASCII.GetBytes(_hiarcSettings.JwtSigningKey);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[] 
                    {
                        new Claim(ClaimTypes.Role, Auth.USER_ROLE_CLAIM_VALUE),
                        new Claim(ClaimTypes.Name, request.Key)
                    }),
                    Expires = utcNow.AddMinutes(expiresInMinutes),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(jwtKey), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var JWT = tokenHandler.WriteToken(token);
                var credentials = new UserCredentials 
                { 
                    UserKey = request.Key, 
                    BearerToken = JWT, 
                    CreatedAt = utcNow, 
                    ExpiresAt = tokenDescriptor.Expires.Value 
                };

                return Ok(credentials);
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }    
        }
    }
}
