using System;
using System.Collections.Generic;
using System.Linq;
using Hiarc.Core.Settings;
using Hiarc.Core.Settings.Storage.Azure;
using Hiarc.Core.Settings.Storage.Google;
using Hiarc.Core.Settings.Storage.AWS;
using Hiarc.Core.Storage.Azure;
using Hiarc.Core.Storage.Google;
using Hiarc.Core.Storage.AWS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hiarc.Core.Storage.IPFS;

namespace Hiarc.Core.Storage
{

    public class StorageServiceProvider : IStorageServiceProvider
    {
        public const string AWS_S3 = "AWS-S3";
        public const string AZURE_BLOB_STORAGE = "Azure-Blob";
        public const string GOOGLE_STORAGE = "Google-Storage";
        public const string IPFS = "IPFS";

        private Dictionary<string, IStorageService> _storageServices;

        private readonly HiarcSettings _hiarcSettings;
        private readonly ILogger<StorageServiceProvider> _logger;

        public StorageServiceProvider(  IOptions<HiarcSettings> hiarcSettings,
                                        ILogger<StorageServiceProvider> logger)
        {
            _hiarcSettings = hiarcSettings.Value;
            _logger = logger;

            this.ConfigureServices();
        }

        public IStorageService Service(string name = null)
        {
            return name == null ? Default : _storageServices[name];
        }

        private IStorageService Default
        {
            get
            {
                if (_hiarcSettings.StorageServices.Length == 0)
                {
                    throw new Exception("No storage providers configured");
                }
                else if (_hiarcSettings.StorageServices.Length == 1)
                {
                    return _storageServices[_hiarcSettings.StorageServices[0].Name];
                }
                else
                {
                    var theDefault = _hiarcSettings.StorageServices.Single(ss => ss.IsDefault);
                    return _storageServices[theDefault.Name];
                }
            }
        }

        private void ConfigureServices()
        {
            _storageServices = new Dictionary<string, IStorageService>();

            foreach (var ss in _hiarcSettings.StorageServices)
            {
                if (ss.Provider == AWS_S3)
                {
                    var settings = new S3Settings
                    {
                        AccessKeyId = ((dynamic)ss.Config).AccessKeyId,
                        SecretAccessKey = ((dynamic)ss.Config).SecretAccessKey,
                        RegionSystemName = ((dynamic)ss.Config).RegionSystemName,
                        Bucket = ((dynamic)ss.Config).Bucket
                    };
                    IOptions<S3Settings> s3Settings = Options.Create(settings);

                    IStorageService s3Service = new S3StorageService(ss.Name, s3Settings, _logger);
                    _storageServices.Add(ss.Name, s3Service);
                }
                else if (ss.Provider == AZURE_BLOB_STORAGE)
                {
                    var settings = new AzureSettings
                    {
                        StorageConnectionString = ((dynamic)ss.Config).StorageConnectionString,
                        Container = ((dynamic)ss.Config).Container
                    };

                    IOptions<AzureSettings> azureSettings = Options.Create(settings);

                    IStorageService azureService = new AzureStorageService(ss.Name, azureSettings, _logger);
                    _storageServices.Add(ss.Name, azureService);
                }
                else if (ss.Provider == GOOGLE_STORAGE)
                {
                    var settings = new GoogleSettings
                    {
                        ServiceAccountCredential = ((dynamic)ss.Config).ServiceAccountCredential,
                        Bucket = ((dynamic)ss.Config).Bucket
                    };

                    IOptions<GoogleSettings> googleSettings = Options.Create(settings);

                    IStorageService googleService = new GoogleStorageService(ss.Name, googleSettings, _logger);
                    _storageServices.Add(ss.Name, googleService);
                }
                else if (ss.Provider == IPFS)
                {
                    IStorageService ipfsService = new IPFSStorageService(ss.Name, _logger);
                    _storageServices.Add(ss.Name, ipfsService);
                }
                else
                {
                    throw new Exception($"Unsupported storage service provider: {ss.Provider}");
                }
            }
        }
    }
}