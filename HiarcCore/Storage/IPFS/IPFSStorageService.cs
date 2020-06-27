using System.IO;
using System.Threading.Tasks;
using Ipfs.Http;
using Microsoft.Extensions.Logging;

namespace Hiarc.Core.Storage.IPFS
{
    public class IPFSStorageService : IStorageService
    {
        private readonly ILogger<StorageServiceProvider> _logger;

        public string Type { get; }
        public string Name { get; }
        public bool SupportsDirectDownload { get; }
        public bool SupportsDirectUpload { get; }

        public IPFSStorageService(string name, ILogger<StorageServiceProvider> logger)
        {
            Type = StorageServiceProvider.IPFS;
            Name = name;
            SupportsDirectDownload = false;
            SupportsDirectUpload = false;
            _logger = logger;
        }
        
        public async Task<IFileInformation> StoreFile(Stream fileStream)
        {
            var ipfs = new IpfsClient("http://127.0.0.1:5005");
            var storedFile = await ipfs.FileSystem.AddAsync(fileStream);

            var info = new IPFSFileInformation { StorageIdentifier = storedFile.Id };
            return info;
        }

        public async Task<Stream> RetrieveFile(string identifier)
        {
            var ipfs = new IpfsClient("http://127.0.0.1:5005");
            return await ipfs.FileSystem.ReadFileAsync(identifier);
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