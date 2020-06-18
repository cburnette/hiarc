namespace Hiarc.Core.Models.Requests
{
    public class CreateFileRequest : CreateOrUpdateEntityRequest
    {
        public string StorageService { get; set; }
    }
}