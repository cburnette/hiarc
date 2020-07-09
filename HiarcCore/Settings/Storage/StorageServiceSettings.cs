using System.Dynamic;

namespace Hiarc.Core.Settings.Storage
{
    public class StorageServiceSetting
    {
        public string Provider { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public bool AllowDirectDownload { get; set; }
        public bool AllowDirectUpload { get; set; }
        public ExpandoObject Config { get; set; }
    }
}