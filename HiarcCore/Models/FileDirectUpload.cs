using System;
using System.Collections.Generic;

namespace Hiarc.Core.Models 
{
    public class FileDirectUpload
    {
        public string DirectUploadUrl { get; set; }
        public string StorageId { get; set; }
        public string StorageService { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var eventProps = new Dictionary<string,object>
            {
                {"DirectUploadUrl", this.DirectUploadUrl },
                {"ExpiresAt", this.ExpiresAt }
            };

            return eventProps;
        }
    }
}