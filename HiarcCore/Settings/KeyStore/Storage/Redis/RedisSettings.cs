namespace HiarcCore.Settings.KeyStore.Storage.Redis
{
    public class RedisSettings
    {
        public string ConnectionString { get; set; }
        public string KeySuffix { get; set; } = "";
    }
}