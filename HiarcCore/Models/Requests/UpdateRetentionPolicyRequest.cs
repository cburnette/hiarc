using System.Collections.Generic;

namespace Hiarc.Core.Models.Requests
{
    public class UpdateRetentionPolicyRequest : CreateOrUpdateEntityRequest
    {
        public uint? Seconds { get; set; }
    }
}