using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Hiarc.Core;
using Hiarc.Core.Database;
using Hiarc.Core.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hiarc
{
    public static class Auth
    {
        public const string API_KEY_HEADER_NAME = "X-Hiarc-Api-Key";
        public const string AS_USER_HEADER_NAME = "X-Hiarc-User-Key";

        public const string API_AUTHENTICATION_SCHEME = "ApiKey";
        public const string JWT_BEARER_SCHEME = "JWTBearer";

        public const string REQUIRES_ADMIN = "AdminPolicy";

        public const string ADMIN_NAME_CLAIM_VALUE = Core.Admin.DEFAULT_ADMIN_NAME;
        public const string ADMIN_ROLE_CLAIM_VALUE = "Admin";
        public const string USER_ROLE_CLAIM_VALUE = "User";

        public static string UserKeyFromContext(this IHttpContextAccessor contextAccessor)
        {
            return contextAccessor.HttpContext.User.Claims.Single((c) => c.Type == ClaimTypes.Name).Value;
        }

        public static string UserRoleFromContext(this IHttpContextAccessor contextAccessor)
        {
            return contextAccessor.HttpContext.User.Claims.Single((c) => c.Type == ClaimTypes.Role).Value;
        }

        public static async Task<bool> UserCanAccessFile(string userKey, string fileKey, IHiarcDatabase hiarcDB, List<string> accessLevels)
        {
            return userKey == Auth.ADMIN_NAME_CLAIM_VALUE || await hiarcDB.UserCanAccessFile(userKey, fileKey, accessLevels);
        }

        public static async Task<bool> UserCanAccessCollection(string userKey, string collectionKey, IHiarcDatabase hiarcDB, List<string> accessLevels)
        {
            return userKey == Auth.ADMIN_NAME_CLAIM_VALUE || await hiarcDB.UserCanAccessCollection(userKey, collectionKey, accessLevels);
        }
    }
    
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddApiKeySupport(this AuthenticationBuilder authenticationBuilder, Action<ApiKeyAuthenticationOptions> options)
        {
            return authenticationBuilder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.DefaultScheme, options);
        }
    }

    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = Auth.API_AUTHENTICATION_SCHEME;
        public string Scheme => DefaultScheme;
        public string AuthenticationType = DefaultScheme;
    }

    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private readonly HiarcSettings _hiarchSettings;
        private readonly IHiarcDatabase _hiarcDatabase;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IOptions<HiarcSettings> hiarchSettings,
            IHiarcDatabase hiarcDatabase) : base(options, logger, encoder, clock)
            {
                _hiarchSettings = hiarchSettings.Value;
                _hiarcDatabase = hiarcDatabase;
            }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
                if (!Request.Headers.TryGetValue(Auth.API_KEY_HEADER_NAME, out var apiKeyHeaderValues))
                {
                    return AuthenticateResult.NoResult();
                }

                var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

                if (apiKeyHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(providedApiKey))
                {
                    return AuthenticateResult.NoResult();
                }

                if (string.Equals(providedApiKey, _hiarchSettings.AdminApiKey))
                {
                    Request.Headers.TryGetValue(Auth.AS_USER_HEADER_NAME, out var userKeyHeaderValues);
                    var userKey = userKeyHeaderValues.FirstOrDefault();

                    var claims = new List<Claim>() { new Claim(ClaimTypes.Role, Auth.ADMIN_ROLE_CLAIM_VALUE) };
                    if (string.IsNullOrWhiteSpace(userKey))
                    {
                        claims.Add(new Claim(ClaimTypes.Name, Auth.ADMIN_NAME_CLAIM_VALUE));
                    }
                    else
                    {
                        // validate that the user exists in the database
                        if(await _hiarcDatabase.IsValidUserKey(userKey))
                        {
                            claims.Add(new Claim(ClaimTypes.Name, userKey));
                        }
                        else
                        {
                            return AuthenticateResult.Fail("There was a problem authenticating");
                        }
                    }

                    var identity = new ClaimsIdentity(claims, Options.AuthenticationType);
                    var identities = new List<ClaimsIdentity> { identity };
                    var principal = new ClaimsPrincipal(identities);
                    var ticket = new AuthenticationTicket(principal, Options.Scheme);

                    return AuthenticateResult.Success(ticket);
                }
                else
                {
                    return AuthenticateResult.Fail($"Invalid {Auth.API_KEY_HEADER_NAME} value provided");
                }
        }
    }
}