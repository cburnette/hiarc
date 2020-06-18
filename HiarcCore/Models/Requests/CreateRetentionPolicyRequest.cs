namespace Hiarc.Core.Models.Requests
{
    public class CreateRetentionPolicyRequest : CreateOrUpdateEntityRequest
    {
        public uint Seconds { get; set; }
    }
}