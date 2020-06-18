using System.Collections.Generic;

namespace Hiarc.Core.Models.Requests
{
    public abstract class CreateOrUpdateEntityRequest
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}