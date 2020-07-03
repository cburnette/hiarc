using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hiarc.Core.Settings.Storage.IPFS;
using Ipfs.Http;
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

        private readonly IpfsClient _client;
        private readonly ILogger<StorageServiceProvider> _logger;

        public IPFSStorageService(string name, IOptions<IPFSSettings> ipfsSettings, ILogger<StorageServiceProvider> logger)
        {
            Type = StorageServiceProvider.IPFS;
            Name = name;
            IPFSSettings = ipfsSettings.Value;
            SupportsDirectDownload = false;
            SupportsDirectUpload = false;
            _client = new IpfsClient(IPFSSettings.Host);
            _logger = logger;
        }
        
        public async Task<IFileInformation> StoreFile(Stream fileStream)
        {
            var storedFile = await _client.FileSystem.AddAsync(fileStream);

            var info = new IPFSFileInformation { StorageIdentifier = storedFile.Id };
            return info;
        }

        public async Task<Stream> RetrieveFile(string identifier)
        {
            var cancel = default(CancellationToken);
            return await _client.PostDownloadAsync("cat", cancel, arg: identifier);
        }

        public async Task<string> GetDirectDownloadUrl(string identifier, int expiresInSeconds)
        {
            return null;
        }

        public async Task<string> GetDirectUploadUrl(string identifier, int expiresInSeconds)
        {
            return null;
        }

        public async Task<IFileInformation> CopyFileToSameServiceType(string identifier, IStorageService destinationService)
        {
            return null;
        }

        public async Task<bool> DeleteFile(string identifier)
        {
            return true;
        }
    }
}