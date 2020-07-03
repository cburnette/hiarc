using Hiarc.Core.Settings;
using Hiarc.Core.Settings.Events;
using Hiarc.Core.Settings.Storage;

namespace Hiarc.Configuration.Strategies.Models
{
    public class HiarcSettingsModel
    {
        public string BaseUri { get; set; }
        public string JwtSigningKey { get; set; }
        public string AdminApiKey { get; set; }
        public bool ForceHTTPS { get; set; }
        public int JWTTokenExpirationMinutes { get; set; }
        public HiarcDatabaseSettings Database { get; set; }
        public StorageServiceSetting[] StorageServices { get; set; }
        public EventServiceSetting[] EventServices { get; set; }
    }
}