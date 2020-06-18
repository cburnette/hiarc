using System.IO;
using System.Threading.Tasks;

namespace Hiarc.Core.Storage
{
    public interface IStorageService
    {
        const int DEFAULT_EXPIRES_IN_SECONDS=10; //need to put this in config

        string Type { get; }
        string Name { get; }
        bool SupportsDirectDownload { get; }
        bool SupportsDirectUpload { get; }
        Task<IFileInformation> StoreFile(Stream fileStream);
        Task<Stream> RetrieveFile(string identifier);
        Task<string> GetDirectDownloadUrl(string identifier, int expiresInSeconds);
        Task<string> GetDirectUploadUrl(string identifier, int expiresInSeconds);
        Task<IFileInformation> CopyFileToSameServiceType(string identifier, IStorageService destinationService);
        Task<bool> DeleteFile(string identifier);
    }
}