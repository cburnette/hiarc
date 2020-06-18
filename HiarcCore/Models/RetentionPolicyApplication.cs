using System;

namespace Hiarc.Core.Models 
{
    public class RetentionPolicyApplication
    {
        public RetentionPolicy RetentionPolicy { get; set; }
        public DateTimeOffset AppliedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }

        // public Dictionary<string, object> ToDictionary()
        // {
        //     var eventProps = new Dictionary<string,object>
        //     {
        //         {"StorageService", this.StorageService},
        //         {"StorageId", this.StorageId},
        //         {"CreatedAt", this.CreatedAt},
        //         {"CreatedBy", this.CreatedBy}
        //     };

        //     return eventProps;
        // }
    }
}