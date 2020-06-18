using Hiarc.Core.Settings.Events;
using Hiarc.Core.Settings.Storage;

namespace Hiarc.Core.Settings
{
    public class HiarcSettings
    {
        public string BaseUri { get; set; }
        public string JwtSigningKey { get; set; }
        public string AdminApiKey { get; set; }
        public string ForceHTTPS { get; set; }
        public int JWTTokenExpirationMinutes { get; set; }
        public HiarcDatabaseSettings Database { get; set; }
        public StorageServiceSetting[] StorageServices { get; set; }
        public EventServiceSetting[] EventServices { get; set; }
    }

    public class HiarcDatabaseSettings
    {
        public string Uri { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
