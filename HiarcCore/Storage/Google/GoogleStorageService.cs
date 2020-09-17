using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Hiarc.Core.Settings.Storage.Google;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hiarc.Core.Storage.Google
{
    public class GoogleStorageService : IStorageService
    {
        public readonly GoogleSettings GoogleSettings;
        private readonly ILogger<StorageServiceProvider> _logger;
        private readonly StorageClient _googleClient;
        private const int CHUNK_SIZE =  UploadObjectOptions.MinimumChunkSize * 24; // 6 MB

        public string Type { get; }
        public string Name { get; }
        
        public bool SupportsDirectDownload { get; }
        public bool AllowDirectDownload { get; set; }
        
        public bool SupportsDirectUpload { get; }
        public bool AllowDirectUpload { get; set; }

        public GoogleStorageService(string name, IOptions<GoogleSettings> googleSettings, bool allowDirectDownload, bool allowDirectUpload, ILogger<StorageServiceProvider> logger)
        {
            Type = StorageServiceProvider.GOOGLE_STORAGE;
            Name = name;
            GoogleSettings = googleSettings.Value;
            SupportsDirectDownload = false;
            AllowDirectDownload = allowDirectDownload;
            SupportsDirectUpload = false;
            AllowDirectUpload = allowDirectUpload;
            _logger = logger;

            // use a tool like this to escape the Google Service Account Credentials JSON file so you can add it to appsettings.json:
            // https://www.freeformatter.com/json-escape.html
            var sacByteArray = Encoding.UTF8.GetBytes(GoogleSettings.ServiceAccountCredential);
            var sacStream = new MemoryStream(sacByteArray);
            var credential = GoogleCredential.FromServiceAccountCredential(ServiceAccountCredential.FromServiceAccountData(sacStream));
            _googleClient = StorageClient.Create(credential);
        }

        public async Task<IFileInformation> StoreFile(Stream fileStream)
        {
            var uniqueName = Guid.NewGuid().ToString();
            // try
            // {
            //     uniqueName = uniqueName.ApplyExtension(name, contentType);
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogWarning(ex.Message);
            // }

            var uploadOptions = new UploadObjectOptions { ChunkSize = CHUNK_SIZE };
            await _googleClient.UploadObjectAsync(GoogleSettings.Bucket, uniqueName,"application/octet-stream", fileStream, uploadOptions);
            
            var info = new GoogleFileInformation { StorageIdentifier = uniqueName };

            return info;
        }

        public async Task<Stream> RetrieveFile(string identifier)
        {
            var stream = new MemoryStream();
            await _googleClient.DownloadObjectAsync(GoogleSettings.Bucket, identifier, stream);
            stream.Position = 0;

            return stream;
        }

        public async Task<string> GetDirectDownloadUrl(string identifier, int expiresInSeconds)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetDirectUploadUrl(string identifier, int expiresInSeconds)
        {
            throw new NotImplementedException();
        }

        public async Task<IFileInformation> CopyFileToSameServiceType(string identifier, IStorageService destinationService)
        {
            var destination = (GoogleStorageService)destinationService;

            var uniqueName = Guid.NewGuid().ToString();
            
            await _googleClient.CopyObjectAsync(GoogleSettings.Bucket, identifier, destinationBucket: destination.GoogleSettings.Bucket, destinationObjectName: uniqueName);

            var info = new GoogleFileInformation { StorageIdentifier = uniqueName };
            return info;  
        }

        public async Task<bool> DeleteFile(string identifier)
        {
            await _googleClient.DeleteObjectAsync(GoogleSettings.Bucket, identifier);
            return true;
        }
    }
}
