using System.Dynamic;

namespace HiarcCore.Settings.KeyStore
{
    public class KeyStoreServiceStorageSettings
    {
        public static readonly string REDIS = "redis";
        public string Provider { get; set; }
        public string Name { get; set; }
        public ExpandoObject Config { get; set; }
    }
}