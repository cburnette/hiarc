using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hiarc.Core.Models 
{
    public abstract class Entity
    {

        public const string TYPE_USER = "user";
        public const string TYPE_GROUP = "group";
        public const string TYPE_FILE = "file";
        public const string TYPE_COLLECTION = "collection";
        public const string TYPE_RETENTION_POLICY = "retentionPolicy";
        public const string TYPE_CLASSIFICATION = "classification";

        public string Type { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public virtual Dictionary<string, object> ToDictionary()
        {
            var eventProps = new Dictionary<string,object>
            {
                {"Type", this.Type},
                {"Key", this.Key},
                {"Name", this.Name},
                {"Description", this.Description},
                {"CreatedBy", this.CreatedBy},
                {"CreatedAt", this.CreatedAt},
                {"ModifiedAt", this.ModifiedAt}
            };

            return eventProps;
        }
    }
}