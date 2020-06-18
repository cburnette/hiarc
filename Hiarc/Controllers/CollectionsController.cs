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
using static Hiarc.Auth;

namespace Hiarc.Api.REST.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class CollectionsController : HiarcControllerBase
    {
        private readonly ILogger<CollectionsController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly HiarcSettings _hiarcSettings;
        private readonly IHiarcDatabase _hiarcDatabase;

        public CollectionsController( ILogger<CollectionsController> logger, 
                                IHttpContextAccessor contextAccessor,
                                IOptions<HiarcSettings> hiarcSettings,
                                IHiarcDatabase hiarcDatabase)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
            _hiarcSettings = hiarcSettings.Value;
            _hiarcDatabase = hiarcDatabase;
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> Get(string key)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessCollection = await UserCanAccessCollection(userKey, key, _hiarcDatabase, AccessLevelGroup.ReadOnlyOrHigher);

                if (userCanAccessCollection)
                {
                    var theCollection = await _hiarcDatabase.GetCollection(key);
                    return Ok(theCollection);
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
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
                var userKey = _contextAccessor.UserKeyFromContext();
                if (userKey != Auth.ADMIN_NAME_CLAIM_VALUE)
                {
                    _logger.LogWarning($"Attempt to access endpoint that requires Admin with '{Auth.AS_USER_HEADER_NAME}' header populated.");
                    return StatusCode(StatusCodes.Status403Forbidden);
                }

                var allCollections = await _hiarcDatabase.GetAllCollections();
                return Ok(allCollections);
            }
            catch(Exception ex)
            {
               return BuildErrorResponse(ex, _logger);
            }   
        }

        [HttpGet("{key}/files")]
        public async Task<IActionResult> GetFiles(string key)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessCollection = await UserCanAccessCollection(userKey, key, _hiarcDatabase, AccessLevelGroup.ReadOnlyOrHigher);

                if (userCanAccessCollection)
                {
                    var theFiles = await _hiarcDatabase.GetFilesForCollection(key);
                    return Ok(theFiles);
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }    
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

        [HttpGet("{key}/children")]
        public async Task<IActionResult> GetChildren(string key)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessCollection = await UserCanAccessCollection(userKey, key, _hiarcDatabase, AccessLevelGroup.ReadOnlyOrHigher);

                if (userCanAccessCollection)
                {
                    var childCollections = await _hiarcDatabase.GetChildCollectionsForCollection(key);
                    return Ok(childCollections);
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }    
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

        [HttpGet("{key}/items")]
        public async Task<IActionResult> GetItems(string key)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessCollection = await UserCanAccessCollection(userKey, key, _hiarcDatabase, AccessLevelGroup.ReadOnlyOrHigher);

                if (userCanAccessCollection)
                {
                    var theItems = await _hiarcDatabase.GetItemsForCollection(key);
                    return Ok(theItems);
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }    
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody]CreateCollectionRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();

                var newCollection = await _hiarcDatabase.CreateCollection(request, userKey);
                var uri = $"{_hiarcSettings.BaseUri}/collections/{newCollection.Key}";
                return Created(uri,newCollection);
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

        [HttpPut("{key}")]
        public async Task<IActionResult> Update(string key, [FromBody]UpdateCollectionRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessCollection = await UserCanAccessCollection(userKey, key, _hiarcDatabase, AccessLevelGroup.ReadWriteOrHigher);

                if (userCanAccessCollection)
                {
                    var updatedCollection = await _hiarcDatabase.UpdateCollection(key, request);
                    return Ok(updatedCollection);
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                } 
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

        [HttpDelete("{key}")]
        public async Task<IActionResult> Delete(string key)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessCollection = await UserCanAccessCollection(userKey, key, _hiarcDatabase, AccessLevelGroup.ReadWriteOrHigher);

                if (userCanAccessCollection)
                {
                    await _hiarcDatabase.DeleteCollection(key);
                    return Ok(); 
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }         
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

        [HttpPost("find")]
        public async Task<IActionResult> FindCollections([FromBody]FindCollectionsRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();

                //TODO: need to filter out collections the user doesn't have access to
                var matchingCollections = await _hiarcDatabase.FindCollections(request);
                
                return Ok(matchingCollections);          
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [HttpDelete("{key}/files/{fileKey}")]
        public async Task<IActionResult> RemoveFile(string key, string fileKey)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessCollection = await UserCanAccessCollection(userKey, key, _hiarcDatabase, AccessLevelGroup.ReadWriteOrHigher);

                if (userCanAccessCollection)
                {
                    await _hiarcDatabase.RemoveFileFromCollection(key, fileKey);
                    return Ok();
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }        
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

        [HttpPut("{key}/children/{childKey}")]
        public async Task<IActionResult> AddChild(string key, string childKey)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessCollection = await UserCanAccessCollection(userKey, key, _hiarcDatabase, AccessLevelGroup.ReadWriteOrHigher);

                if (userCanAccessCollection)
                {
                    await _hiarcDatabase.AddChildToCollection(key, childKey);
                    return Ok(new Empty());
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                } 
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

        [HttpPut("{key}/users")]
        public async Task<IActionResult> AddUser(string key, [FromBody]AddUserToCollectionRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessCollection = await UserCanAccessCollection(userKey, key, _hiarcDatabase, AccessLevelGroup.ReadWriteOrHigher);

                if (userCanAccessCollection)
                {
                    await _hiarcDatabase.AddUserToCollection(key, request);
                    return Ok(new Empty());
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                } 
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

        [HttpPut("{key}/groups")]
        public async Task<IActionResult> AddGroup(string key, [FromBody]AddGroupToCollectionRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessCollection = await UserCanAccessCollection(userKey, key, _hiarcDatabase, AccessLevelGroup.ReadWriteOrHigher);

                if (userCanAccessCollection)
                {
                    await _hiarcDatabase.AddGroupToCollection(key, request);
                    return Ok(new Empty());
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                } 
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

        [HttpPut("{key}/files")]
        public async Task<IActionResult> AddFile(string key, [FromBody]AddFileToCollectionRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessCollection = await UserCanAccessCollection(userKey, key, _hiarcDatabase, AccessLevelGroup.ReadWriteOrHigher);

                if (userCanAccessCollection)
                {
                    await _hiarcDatabase.AddFileToCollection(key, request);
                    return Ok(new Empty());
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                } 
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
    }
}
