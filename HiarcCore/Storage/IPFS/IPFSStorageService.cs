using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hiarc.Core.Settings.Storage.IPFS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hiarc.Core.Storage.IPFS
{
    public class IPFSStorageService : IStorageService
    {
        public readonly IPFSSettings IPFSSettings;
        public string Type { get; }
        public string Name { get; }
        public bool SupportsDirectDownload { get; }
        public bool SupportsDirectUpload { get; }

        private readonly ILogger<StorageServiceProvider> _logger;

        public IPFSStorageService(string name, IOptions<IPFSSettings> ipfsSettings, ILogger<StorageServiceProvider> logger)
        {
            Type = StorageServiceProvider.IPFS;
            Name = name;
            IPFSSettings = ipfsSettings.Value;
            SupportsDirectDownload = false;
            SupportsDirectUpload = false;
            _logger = logger;
        }
        
        public async Task<IFileInformation> StoreFile(Stream fileStream)
        {
            // var storedFile = await _client.FileSystem.AddAsync(fileStream);

            // var info = new IPFSFileInformation { StorageIdentifier = storedFile.Id };
            // return info;

            throw new NotImplementedException();
        }

        public async Task<Stream> RetrieveFile(string identifier)
        {
            // var cancel = default(CancellationToken);
            // return await _client.PostDownloadAsync("cat", cancel, arg: identifier);
            
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteFile(string identifier)
        {
            throw new NotImplementedException();
        }
    }
}