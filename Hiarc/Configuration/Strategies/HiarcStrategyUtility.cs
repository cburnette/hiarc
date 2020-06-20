using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hiarc.Configuration.Strategies.Models;
using Hiarc.Core.Events;
using Hiarc.Core.Settings;
using Hiarc.Core.Settings.Events;
using Hiarc.Core.Settings.Events.AWS;
using Hiarc.Core.Settings.Events.Azure;
using Hiarc.Core.Settings.Events.Google;
using Hiarc.Core.Settings.Events.Webhook;
using Hiarc.Core.Settings.Storage;
using Hiarc.Core.Settings.Storage.AWS;
using Hiarc.Core.Settings.Storage.Azure;
using Hiarc.Core.Settings.Storage.Google;
using Hiarc.Core.Storage;
using Microsoft.Extensions.Configuration;

namespace Hiarc.Configuration.Strategies
{
    public class HiarcConfigurationUtility
    {
        public const string HIARC_SETTING = "Hiarc";
        // public const string BASE_URI_SETTING = "BaseUri";
        // public const string JWT_SIGNING_KEY_SETTING = "JwtSigningKey";
        // public const string ADMIN_API_KEY_SETTING = "AdminApiKey";
        // public const string FORCE_HTTPS_SETTING = "ForceHTTPS";
        // public const string JWT_EXPIRATION_SETTING = "JWTTokenExpirationMinutes";
        public const string DATABASE_SETTING = "Database";
        // public const string DATABASE_URI_SETTING = "Uri";
        // public const string DATABASE_USERNAME_SETTING = "Username";
        // public const string DATABASE_PASSWORD_SETTING = "Password";
        public const string STORAGE_SERVICES_SETTING = "StorageServices";
        public const string EVENT_SERVICES_SETTING = "EventServices";
        public const string CONFIG_SETTING = "Config";
        public static readonly string HIARC_DATABASE_CONFIG_KEY = $"{HIARC_SETTING}{ConfigurationPath.KeyDelimiter}{DATABASE_SETTING}";
        public static readonly string HIARC_STORAGE_SERVICES_CONFIG_KEY = $"{HIARC_SETTING}{ConfigurationPath.KeyDelimiter}{STORAGE_SERVICES_SETTING}";
        public static readonly string HIARC_EVENT_SERVICES_CONFIG_KEY = $"{HIARC_SETTING}{ConfigurationPath.KeyDelimiter}{EVENT_SERVICES_SETTING}";

        public static List<Type> SCALARS = new List<Type>()
        {
            typeof(string),
            typeof(bool),
            typeof(int)
        };
        public static Dictionary<Type, string> PREFIXES = new Dictionary<Type, string>()
        {
            {typeof(HiarcSettingsModel), HIARC_SETTING},
            {typeof(HiarcDatabaseSettings), HIARC_DATABASE_CONFIG_KEY},
            {typeof(StorageServiceSetting), HIARC_STORAGE_SERVICES_CONFIG_KEY},
            {typeof(EventServiceSetting), HIARC_EVENT_SERVICES_CONFIG_KEY},
            {typeof(S3Settings), HIARC_STORAGE_SERVICES_CONFIG_KEY},
            {typeof(AzureSettings), HIARC_STORAGE_SERVICES_CONFIG_KEY},
            {typeof(GoogleSettings), HIARC_STORAGE_SERVICES_CONFIG_KEY},
            {typeof(KinesisSettings), HIARC_EVENT_SERVICES_CONFIG_KEY},
            {typeof(ServiceBusSettings), HIARC_EVENT_SERVICES_CONFIG_KEY},
            {typeof(PubSubSettings), HIARC_EVENT_SERVICES_CONFIG_KEY},
            {typeof(WebhookSettings), HIARC_EVENT_SERVICES_CONFIG_KEY}
        };
        public static List<Type> CONFIG_TYPES = new List<Type>()
        {
            typeof(S3Settings),
            typeof(AzureSettings),
            typeof(GoogleSettings),
            typeof(KinesisSettings),
            typeof(ServiceBusSettings),
            typeof(PubSubSettings),
            typeof(WebhookSettings)
        };
        public static IEnumerable<(string k, string v)> CreateConfigFromProperties(string prefix, object o, IEnumerable<PropertyInfo> props, int i = -1, bool withConfig = false)
        {
            var iprops = props.Where(p => HiarcConfigurationUtility.SCALARS.Contains(p.PropertyType));
            var kv = new List<(string, string)>();
            if (i >= 0)
            {
                prefix += $"{ConfigurationPath.KeyDelimiter}{i}";
            }
            foreach (var prop in iprops)
            {
                var k = prefix;
                var s = o.GetType().GetProperty(prop.Name);
                var val = s.GetValue(o).ToString();
                if (withConfig)
                {
                    k += $"{ConfigurationPath.KeyDelimiter}{CONFIG_SETTING}";
                }
                k += $"{ConfigurationPath.KeyDelimiter}{prop.Name}";
                kv.Add((k, val));
            }
            return kv;
        }
        public static void Load<T>(T settings, Action<string, string> set, int i = -1)
        {
            var prefix = PREFIXES[typeof(T)];
            IEnumerable<(string k, string v)> config;
            if (typeof(T) == typeof(StorageServiceSetting) || typeof(T) == typeof(EventServiceSetting))
            {
                config = HiarcConfigurationUtility.CreateConfigFromProperties(prefix, settings, typeof(T).GetProperties(), i);
            }
            else if (CONFIG_TYPES.Contains(typeof(T)))
            {
                config = HiarcConfigurationUtility.CreateConfigFromProperties(prefix, settings, typeof(T).GetProperties(), i, true);
            }
            else
            {
                config = HiarcConfigurationUtility.CreateConfigFromProperties(prefix, settings, typeof(T).GetProperties());
            }
            foreach (var p in config)
            {
                set(p.k, p.v);
            }
        }
        public static void ProcessStorageServices(StorageServiceSetting stt, Action<string, string> set, int i)
        {
            if (stt.Provider == StorageServiceProvider.AWS_S3)
            {
                Load<S3Settings>(new S3Settings
                {
                    AccessKeyId = ((dynamic)stt.Config).AccessKeyId.ToString(),
                    SecretAccessKey = ((dynamic)stt.Config).SecretAccessKey.ToString(),
                    RegionSystemName = ((dynamic)stt.Config).RegionSystemName.ToString(),
                    Bucket = ((dynamic)stt.Config).Bucket.ToString()
                }, set, i);
            }
            else if (stt.Provider == StorageServiceProvider.AZURE_BLOB_STORAGE)
            {
                Load<AzureSettings>(new AzureSettings
                {
                    StorageConnectionString = ((dynamic)stt.Config).StorageConnectionString.ToString(),
                    Container = ((dynamic)stt.Config).Container.ToString()
                }, set, i);
            }
            else if (stt.Provider == StorageServiceProvider.GOOGLE_STORAGE)
            {
                Load<GoogleSettings>(new GoogleSettings
                {
                    ServiceAccountCredential = ((dynamic)stt.Config).ServiceAccountCredential.ToString(),
                    Bucket = ((dynamic)stt.Config).Bucket.ToString()
                }, set, i);
            }
            else
            {
                throw new Exception($"Unsupported storage service provider: {stt.Provider}");
            }
        }

        public static void ProcessEventServices(EventServiceSetting es, Action<string, string> set, int i)
        {
            if (es.Provider == HiarcEventServiceProvider.AWS_KINESIS)
            {
                Load<KinesisSettings>(new KinesisSettings
                {
                    AccessKeyId = ((dynamic)es.Config).AccessKeyId.ToString(),
                    SecretAccessKey = ((dynamic)es.Config).SecretAccessKey.ToString(),
                    RegionSystemName = ((dynamic)es.Config).RegionSystemName.ToString(),
                    Stream = ((dynamic)es.Config).Stream.ToString()
                }, set, i);
            }
            else if (es.Provider == HiarcEventServiceProvider.AZURE_SERVICE_BUS)
            {
                Load<ServiceBusSettings>(new ServiceBusSettings
                {
                    ConnectionString = ((dynamic)es.Config).ConnectionString.ToString(),
                    Topic = ((dynamic)es.Config).Topic.ToString()
                }, set, i);
            }
            else if (es.Provider == HiarcEventServiceProvider.GOOGLE_PUBSUB)
            {
                Load<PubSubSettings>(new PubSubSettings
                {
                    ServiceAccountCredential = ((dynamic)es.Config).ServiceAccountCredential.ToString(),
                    ProjectId = ((dynamic)es.Config).ProjectId.ToString(),
                    Topic = ((dynamic)es.Config).Topic.ToString()
                }, set, i);
            }
            else if (es.Provider == HiarcEventServiceProvider.WEBHOOK)
            {
                Load<WebhookSettings>(new WebhookSettings
                {
                    URL = ((dynamic)es.Config).URL.ToString(),
                    Secret = ((dynamic)es.Config).Secret.ToString()
                }, set, i);
            }
            else
            {
                throw new Exception($"Unsupported storage service provider: {es.Provider}");
            }
        }
    }
}