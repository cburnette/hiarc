using System;
using System.Collections.Generic;

namespace Hiarc.Core.Models 
{
    public class FileVersion
    {
        public string StorageService { get; set; }
        public string StorageId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string CreatedBy { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var eventProps = new Dictionary<string,object>
            {
                {"StorageService", this.StorageService},
                {"StorageId", this.StorageId},
                {"CreatedAt", this.CreatedAt},
                {"CreatedBy", this.CreatedBy}
            };

            return eventProps;
        }
    }
}