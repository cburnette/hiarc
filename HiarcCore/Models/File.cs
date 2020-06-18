using System;
using System.Collections.Generic;

namespace Hiarc.Core.Models
{
    public class File : Entity
    {
        public int VersionCount { get; set; }

        public File()
        {
            this.Type = Entity.TYPE_FILE;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var eventProps = base.ToDictionary();
            eventProps.Add("VersionCount", this.VersionCount);
            return eventProps;
        }
    }
}