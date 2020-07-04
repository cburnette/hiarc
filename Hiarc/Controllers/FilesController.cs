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
using Hiarc.Core;
using System.IO;
using System.Text.Json;
using Hiarc.Core.Storage;
using static Hiarc.Auth;
using System.Collections.Generic;

namespace Hiarc.Api.REST.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class FilesController : HiarcControllerBase
    {
        private readonly ILogger<FilesController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IStorageServiceProvider _storageServiceProvider;
        private readonly HiarcSettings _hiarcSettings;
        private readonly IHiarcDatabase _hiarcDatabase;

        public FilesController( ILogger<FilesController> logger, 
                                IHttpContextAccessor contextAccessor,
                                IStorageServiceProvider storageServiceProvider,
                                IOptions<HiarcSettings> hiarcSettings,
                                IHiarcDatabase hiarcDatabase)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
            _storageServiceProvider = storageServiceProvider;
            _hiarcSettings = hiarcSettings.Value;
            _hiarcDatabase = hiarcDatabase;
        }

        [HttpGet("{fileKey}")]
        public async Task<IActionResult> Get(string fileKey)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.ReadOnlyOrHigher);

                if (userCanAccessFile)
                {
                    var theFile = await _hiarcDatabase.GetFile(fileKey);
                    return Ok(theFile);
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [HttpGet("{fileKey}/versions")]
        public async Task<IActionResult> GetVersions(string fileKey)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.ReadOnlyOrHigher);

                if (userCanAccessFile)
                {
                    var theFileVersions = await _hiarcDatabase.GetFileVersions(fileKey);
                    return Ok(theFileVersions);
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [HttpGet("{fileKey}/retentionpolicies")]
        public async Task<IActionResult> GetRetentionPolicies(string fileKey)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.CoOwner);

                if (userCanAccessFile)
                {
                    var theRetentionPolicies = await _hiarcDatabase.GetRetentionPolicyApplicationsForFile(fileKey);
                    return Ok(theRetentionPolicies);
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [DisableRequestSizeLimit]
        [HttpPost()]
        public async Task<IActionResult> Create([FromForm]string request, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0) return BadRequest();

                var userKey = _contextAccessor.UserKeyFromContext();

                //var contentType = file.ContentType;
                var createFileRequest = JsonSerializer.Deserialize<CreateFileRequest>(request);
                var storageService = _storageServiceProvider.Service(createFileRequest.StorageService);
                createFileRequest.StorageService = storageService.Name; // fill in the StorageService field in the case that null was passed (for Default)
                
                IFileInformation versionInfo = null;
                using var stream = file.OpenReadStream();
                versionInfo = await storageService.StoreFile(stream);

                var newFile = await _hiarcDatabase.CreateFile(createFileRequest, userKey, versionInfo.StorageIdentifier);
                var uri = $"{_hiarcSettings.BaseUri}/files/{newFile.Key}";

                return Created(uri, newFile);
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpPut("{fileKey}")]
        public async Task<IActionResult> Update(string fileKey, [FromBody]UpdateFileRequest request)
        {
            try
            {
                var originalFile = await _hiarcDatabase.GetFile(fileKey);

                // check that update has the same file extension as the original
                if (request.Name != null && Path.GetExtension(originalFile.Name) != Path.GetExtension(request.Name))
                {
                    _logger.LogError($"Attemped to update file name with incorrect file extension: FileKey='{fileKey}', Name='{originalFile.Name}', New Name='{request.Name}'");
                    return BadRequest($"New name file extension must be '{Path.GetExtension(originalFile.Name)}'");
                }

                var updatedFile = await _hiarcDatabase.UpdateFile(fileKey, request);
                return Ok(updatedFile);
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

        [HttpDelete("{fileKey}")]
        public async Task<IActionResult> Delete(string fileKey)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.ReadWriteOrHigher);

                if (userCanAccessFile)
                {
                    await _hiarcDatabase.DeleteFile(fileKey);
                    return Ok();       
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }      
            }
            catch(InvalidOperationException ex)
            {
                _logger.LogWarning(ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            catch(Exception ex)
            {
               return BuildErrorResponse(ex, _logger);
            }
        }

        [HttpGet("{fileKey}/collections")]
        public async Task<IActionResult> GetCollectionsForFile(string fileKey)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.ReadOnlyOrHigher);

                if (userCanAccessFile)
                {
                    var theFile = await _hiarcDatabase.GetCollectionsForFile(fileKey);
                    return Ok(theFile);
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [HttpPut("{fileKey}/attach")]
        public async Task<IActionResult> AttachToExistingFile(string fileKey, [FromBody]AttachToExistingFileRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                var storageId = request.StorageId;
                var createFileRequest = new CreateFileRequest() { Key=fileKey, Name=request.Name, StorageService=request.StorageService };

                var newFile = await _hiarcDatabase.CreateFile(createFileRequest, userKey, storageId);
                
                var uri = $"{_hiarcSettings.BaseUri}/files/{newFile.Key}";
                return Created(uri, newFile);
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [HttpPut("{fileKey}/copy")]
        public async Task<IActionResult> Copy(string fileKey, [FromBody]CopyFileRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.ReadOnlyOrHigher);

                if (userCanAccessFile)
                {
                    var latestFileVersion = await _hiarcDatabase.GetLatestVersionForFile(fileKey);
                    var sourceStorageService = _storageServiceProvider.Service(latestFileVersion.StorageService);
                    var destinationStorageService = _storageServiceProvider.Service(request.StorageService);

                    IFileInformation versionInfo;

                    if (sourceStorageService.Type == destinationStorageService.Type)
                    {
                        versionInfo = await sourceStorageService.CopyFileToSameServiceType(latestFileVersion.StorageId, destinationStorageService);
                    }
                    else
                    {
                        using var stream = await sourceStorageService.RetrieveFile(latestFileVersion.StorageId);
                        versionInfo = await destinationStorageService.StoreFile(stream);
                    }
                    
                    var newFileRequest = new CreateFileRequest() { Key=request.Key, StorageService=destinationStorageService.Name };
                    var copyOfFile = await _hiarcDatabase.CreateFile(newFileRequest, userKey, versionInfo.StorageIdentifier);
                    var uri = $"{_hiarcSettings.BaseUri}/files/{copyOfFile.Key}";

                    return Created(uri, copyOfFile);
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [DisableRequestSizeLimit]
        [HttpPut("{fileKey}/versions")]
        public async Task<IActionResult> AddVersion([FromForm]string request, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0) return BadRequest();

                var userKey = _contextAccessor.UserKeyFromContext();
                var addVersionToFileRequest = JsonSerializer.Deserialize<AddVersionToFileRequest>(request);
                bool userCanAccessFile = await UserCanAccessFile(userKey, addVersionToFileRequest.Key, _hiarcDatabase, AccessLevelGroup.UploadOnlyOrHigher);

                if (userCanAccessFile)
                {
                    var originalFile = await _hiarcDatabase.GetFile(addVersionToFileRequest.Key);

                    // check that new version is the same file extension as the original
                    if (Path.GetExtension(originalFile.Name) != Path.GetExtension(file.FileName))
                    {
                        _logger.LogError("Attemped to upload new version of file with incorrect file extension");
                        return BadRequest($"New version file extension must be '{Path.GetExtension(originalFile.Name)}'");
                    }
        
                    IStorageService storageService;
                    if (addVersionToFileRequest.StorageService == null)
                    {
                        // if the request StorageService property is null then use the StorageService from the previous version (i.e. stays in the same service)
                        var latestFileVersion = await _hiarcDatabase.GetLatestVersionForFile(addVersionToFileRequest.Key);
                        storageService = _storageServiceProvider.Service(latestFileVersion.StorageService);
                    }
                    else
                    {
                        storageService = _storageServiceProvider.Service(addVersionToFileRequest.StorageService);
                    }
                
                    IFileInformation versionInfo = null;
                    using var stream = file.OpenReadStream();
                    versionInfo = await storageService.StoreFile(stream);

                    var updatedFile = await _hiarcDatabase.AddVersionToFile(originalFile.Key, storageService.Name, versionInfo.StorageIdentifier, userKey);
                    var uri = $"{_hiarcSettings.BaseUri}/files/{originalFile.Key}";

                    return Created(uri, updatedFile);
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [HttpGet("{fileKey}/download")]
        public async Task<IActionResult> Download(string fileKey)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.ReadOnlyOrHigher);

                if (userCanAccessFile)
                {
                    var theFile = await _hiarcDatabase.GetFile(fileKey);
                    var latestFileVersion = await _hiarcDatabase.GetLatestVersionForFile(fileKey);
                    var storageService = _storageServiceProvider.Service(latestFileVersion.StorageService);
                    _logger.LogDebug($"storageService: {storageService}");

                    if (storageService.SupportsDirectDownload)
                    {
                        var directDownloadUrl = await storageService.GetDirectDownloadUrl(latestFileVersion.StorageId, IStorageService.DEFAULT_EXPIRES_IN_SECONDS);
                        return new RedirectResult(directDownloadUrl, false); 
                    }
                    else
                    {
                        _logger.LogDebug($"about to retrieve file");
                        using var stream = await storageService.RetrieveFile(latestFileVersion.StorageId);
                        _logger.LogDebug($"stream: {stream}");
                        return File(stream, "application/octet-stream", theFile.Name);
                    }    
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [Authorize(Policy = Auth.REQUIRES_ADMIN)]
        [HttpPost("directuploadurl")]
        public async Task<IActionResult> CreateDirectUploadUrl([FromBody]CreateDirectUploadUrlRequest request, [FromQuery(Name = "expiresInSeconds")] int? expiresInSeconds)
        {
            try
            {
                var expiresIn = expiresInSeconds ?? IStorageService.DEFAULT_EXPIRES_IN_SECONDS;

                if (_storageServiceProvider.Service(request.StorageService).SupportsDirectUpload)
                {
                    var storageId = Guid.NewGuid().ToString();
                    var directUploadUrl = await _storageServiceProvider.Service(request.StorageService).GetDirectUploadUrl(storageId, expiresIn);
                    var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
                    var response = new FileDirectUpload {   DirectUploadUrl=directUploadUrl, 
                                                            ExpiresAt=expiresAt, StorageId=storageId, 
                                                            StorageService=_storageServiceProvider.Service(request.StorageService).Name 
                                                        };
                    return Ok(response);
                }
                else
                {
                    return StatusCode(StatusCodes.Status405MethodNotAllowed);
                }
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        
        [HttpGet("{fileKey}/directdownloadurl")]
        public async Task<IActionResult> GetDirectDownloadUrl(string fileKey, [FromQuery(Name = "expiresInSeconds")] int? expiresInSeconds)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.ReadOnlyOrHigher);

                if (userCanAccessFile)
                {
                    var expiresIn = expiresInSeconds ?? IStorageService.DEFAULT_EXPIRES_IN_SECONDS;
                    var latestFileVersion = await _hiarcDatabase.GetLatestVersionForFile(fileKey);

                    if (_storageServiceProvider.Service(latestFileVersion.StorageService).SupportsDirectDownload)
                    {
                        var directDownloadUrl = await _storageServiceProvider.Service(latestFileVersion.StorageService).GetDirectDownloadUrl(latestFileVersion.StorageId, expiresInSeconds: expiresIn);
                        var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
                        var response = new FileDirectDownload { Key=fileKey, DirectDownloadUrl=directDownloadUrl, ExpiresAt=expiresAt };
                        return Ok(response);
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status405MethodNotAllowed);
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }   
        }

        [HttpPut("{fileKey}/users")]
        public async Task<IActionResult> AddUserToFile(string fileKey, [FromBody]AddUserToFileRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.CoOwner);

                if (userCanAccessFile)
                {
                    await _hiarcDatabase.AddUserToFile(fileKey, request);
                    return Ok(new Empty());   
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                } 
            }
            catch(ArgumentException argEx)
            {
                return BuildErrorResponse(argEx, _logger, StatusCodes.Status400BadRequest);
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [HttpPut("{fileKey}/groups")]
        public async Task<IActionResult> AddGroupToFile(string fileKey, [FromBody]AddGroupToFileRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.CoOwner);

                if (userCanAccessFile)
                {
                    await _hiarcDatabase.AddGroupToFile(fileKey, request);
                    return Ok(new Empty());   
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                } 
            }
            catch(ArgumentException argEx)
            {
                return BuildErrorResponse(argEx, _logger, StatusCodes.Status400BadRequest);
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [HttpPut("{fileKey}/retentionpolicies")]
        public async Task<IActionResult> AddRetentionPolicyToFile(string fileKey, [FromBody]AddRetentionPolicyToFileRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.CoOwner);

                if (userCanAccessFile)
                {
                    await _hiarcDatabase.AddRetentionPolicyToFile(fileKey, request);
                    return Ok(new Empty());
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                } 
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }

        [HttpPut("{fileKey}/classifications")]
        public async Task<IActionResult> AddClassificationToFile(string fileKey, [FromBody]AddClassificationToFileRequest request)
        {
            try
            {
                var userKey = _contextAccessor.UserKeyFromContext();
                bool userCanAccessFile = await UserCanAccessFile(userKey, fileKey, _hiarcDatabase, AccessLevelGroup.CoOwner);

                if (userCanAccessFile)
                {
                    await _hiarcDatabase.AddClassificationToFile(fileKey, request);
                    return Ok(new Empty());
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                } 
            }
            catch(Exception ex)
            {
                return BuildErrorResponse(ex, _logger);
            }
        }
    }
}