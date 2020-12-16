using System.Dynamic;

namespace HiarcCore.Settings.KeyStore
{
    public class KeyStoreServiceEncryptionSettings
    {
        public static readonly string INLINE = "inline";
        public string Provider { get; set; }
        public string Name { get; set; }
        public ExpandoObject Config { get; set; }
    }
}