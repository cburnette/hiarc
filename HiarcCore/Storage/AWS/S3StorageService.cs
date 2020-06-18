using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Hiarc.Core.Settings.Storage.AWS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hiarc.Core.Storage.AWS
{
    public class S3StorageService : IStorageService
    {
        public readonly S3Settings S3Settings;
        private readonly ILogger<StorageServiceProvider> _logger;
        private readonly RegionEndpoint _region;
        private const int CHUNK_SIZE = 6291456; // 6 MB

        public string Type { get; }
        public string Name { get; }
        public bool SupportsDirectDownload { get; }
        public bool SupportsDirectUpload { get; }

        public S3StorageService(string name, IOptions<S3Settings> s3Settings, ILogger<StorageServiceProvider> logger)
        {
            Type = StorageServiceProvider.AWS_S3;
            Name = name;
            S3Settings = s3Settings.Value;
            SupportsDirectDownload = true;
            SupportsDirectUpload = true;
            _logger = logger;
            _region = RegionEndpoint.GetBySystemName(S3Settings.RegionSystemName);
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

            var fileTransferUtility = new TransferUtility(S3Settings.AccessKeyId, S3Settings.SecretAccessKey, _region);

            var request = new TransferUtilityUploadRequest
            {
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                BucketName = S3Settings.Bucket,
                Key = uniqueName,
                InputStream = fileStream,
                PartSize = CHUNK_SIZE
            };

            await fileTransferUtility.UploadAsync(request);

            var info = new S3FileInformation { StorageIdentifier = uniqueName };
            return info;
        }

        public async Task<Stream> RetrieveFile(string identifier)
        {
            using var client = new AmazonS3Client(S3Settings.AccessKeyId, S3Settings.SecretAccessKey, _region);
            var request = new GetObjectRequest
            {
                BucketName = S3Settings.Bucket,
                Key = identifier
            };

            var response = await client.GetObjectAsync(request);
            return response.ResponseStream;
        }

        public async Task<string> GetDirectDownloadUrl(string identifier, int expiresInSeconds)
        {
            using var client = new AmazonS3Client(S3Settings.AccessKeyId, S3Settings.SecretAccessKey, _region);
            var request = new GetPreSignedUrlRequest
            {
                BucketName = S3Settings.Bucket,
                Key = identifier,
                Expires = DateTime.Now.AddSeconds(expiresInSeconds)
            };

            string urlString = "";
            await Task.Run(() => { urlString = client.GetPreSignedURL(request); });
            return urlString;
        }

        public async Task<string> GetDirectUploadUrl(string identifier, int expiresInSeconds)
        {
            using var client = new AmazonS3Client(S3Settings.AccessKeyId, S3Settings.SecretAccessKey, _region);
            var request = new GetPreSignedUrlRequest
            {
                BucketName = S3Settings.Bucket,
                Key        = identifier,
                Verb       = HttpVerb.PUT,
                Expires    = DateTime.Now.AddSeconds(expiresInSeconds)
            };

            string urlString = "";
            await Task.Run(() => { urlString = client.GetPreSignedURL(request); });
            return urlString;
        }

        public async Task<IFileInformation> CopyFileToSameServiceType(string identifier, IStorageService destinationService)
        {
            var destination = (S3StorageService)destinationService;

            var uniqueName = Guid.NewGuid().ToString();
            using var client = new AmazonS3Client(S3Settings.AccessKeyId, S3Settings.SecretAccessKey, _region);
            var request = new CopyObjectRequest()
            {
                SourceBucket = S3Settings.Bucket,
                SourceKey = identifier,
                DestinationBucket = destination.S3Settings.Bucket,
                DestinationKey = uniqueName
            };

            await client.CopyObjectAsync(request);

            var info = new S3FileInformation { StorageIdentifier = uniqueName };
            return info; 
        }

        public async Task<bool> DeleteFile(string identifier)
        {
            using var client = new AmazonS3Client(S3Settings.AccessKeyId, S3Settings.SecretAccessKey, _region);
            var request = new DeleteObjectRequest
            {
                BucketName = S3Settings.Bucket,
                Key = identifier
            };

            await client.DeleteObjectAsync(request);
            return true;
        }
    }
}

// Example policy for S3 bucket to prevent files being uploaded that don't specify server side encryption
// {
//   "Version": "2012-10-17",
//   "Id": "PutObjPolicy",
//   "Statement": [
//     {
//       "Sid": "DenyIncorrectEncryptionHeader",
//       "Effect": "Deny",
//       "Principal": "*",
//       "Action": "s3:PutObject",
//       "Resource": "arn:aws:s3:::YourBucket/*",
//       "Condition": {
//         "StringNotEquals": {
//           "s3:x-amz-server-side-encryption": "AES256"
//         }
//       }
//     },
//     {
//       "Sid": "DenyUnEncryptedObjectUploads",
//       "Effect": "Deny",
//       "Principal": "*",
//       "Action": "s3:PutObject",
//       "Resource": "arn:aws:s3:::YourBucket/*",
//       "Condition": {
//         "Null": {
//           "s3:x-amz-server-side-encryption": "true"
//         }
//       }
//     }
//   ]
// }