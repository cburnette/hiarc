using System;
using System.Collections.Generic;

namespace Hiarc.Core.Models 
{
    public class FileDirectDownload
    {
        public string Key { get; set; }
        public string DirectDownloadUrl { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var eventProps = new Dictionary<string,object>
            {
                {"Key", this.Key},
                {"DirectDownloadUrl", this.DirectDownloadUrl },
                {"ExpiresAt", this.ExpiresAt }
            };

            return eventProps;
        }
    }
}