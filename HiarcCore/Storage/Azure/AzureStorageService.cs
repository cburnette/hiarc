using System;
using System.IO;
using System.Threading.Tasks;
using Hiarc.Core.Settings.Storage.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hiarc.Core.Storage.Azure
{
    public class AzureStorageService : IStorageService
    {
        public readonly CloudBlobContainer BlobContainer;
        private readonly AzureSettings _azureSettings;
        private readonly ILogger<StorageServiceProvider> _logger;
        private readonly CloudStorageAccount _storageAccount;
        private readonly CloudBlobClient _blobClient;

        public string Type { get; }
        public string Name { get; }
        public bool SupportsDirectDownload { get; }
        public bool SupportsDirectUpload { get; }

        public AzureStorageService(string name, IOptions<AzureSettings> azureSettings, ILogger<StorageServiceProvider> logger)
        {
            Type = StorageServiceProvider.AZURE_BLOB_STORAGE;
            Name = name;
            SupportsDirectDownload = false;
            SupportsDirectUpload = false;
            _azureSettings = azureSettings.Value;
            _logger = logger;
            _storageAccount = CloudStorageAccount.Parse(_azureSettings.StorageConnectionString);
            _blobClient = _storageAccount.CreateCloudBlobClient();
            BlobContainer = _blobClient.GetContainerReference(_azureSettings.Container);
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

            var blockBlob = BlobContainer.GetBlockBlobReference(uniqueName);
            await blockBlob.UploadFromStreamAsync(fileStream);

            var info = new AzureFileInformation { StorageIdentifier = uniqueName };

            return info;
        }

        public async Task<Stream> RetrieveFile(string identifier)
        {
            var blockBlob = BlobContainer.GetBlockBlobReference(identifier);
            return await blockBlob.OpenReadAsync();
        }

        public async Task<string> GetDirectDownloadUrl(string identifier, int expiresInSeconds)
        {
            // var userDelegationKey = await _blobClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, DateTimeOffset.Now.AddSeconds(expiresInSeconds));
            // SharedAccessBlobPolicy blah = new SharedAccessBlobPolicy();
            // SharedAccessBlobPermissions perms = new SharedAccessBlobPermissions();
            // perms.
            // BlobContainer.GetSharedAccessSignature()
            return null;
        }

        public async Task<string> GetDirectUploadUrl(string identifier, int expiresInSeconds)
        {
            return null;
        }

        public async Task<IFileInformation> CopyFileToSameServiceType(string identifier, IStorageService destinationService)
        {
            var destination = (AzureStorageService)destinationService;

            var uniqueName = Guid.NewGuid().ToString();
            CloudBlockBlob sourceBlob = null;

            try
            {
                sourceBlob = BlobContainer.GetBlockBlobReference(identifier);
                await sourceBlob.AcquireLeaseAsync(null);
                var destinationBlob = destination.BlobContainer.GetBlockBlobReference(uniqueName);
                await destinationBlob.StartCopyAsync(sourceBlob);     
            }
            finally
            {
                // Break the lease on the source blob.
                if (sourceBlob != null)
                {
                    await sourceBlob.FetchAttributesAsync();

                    if (sourceBlob.Properties.LeaseState != LeaseState.Available)
                    {
                        await sourceBlob.BreakLeaseAsync(new TimeSpan(0));
                    }
                }
            }

            var info = new AzureFileInformation { StorageIdentifier = uniqueName };
            return info; 
        }

        public async Task<bool> DeleteFile(string identifier)
        {
            var blockBlob = BlobContainer.GetBlockBlobReference(identifier);
            await blockBlob.DeleteAsync();

            return true;
        }
    }
}
