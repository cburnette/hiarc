namespace Hiarc.Core.Models.Requests
{
    public class AttachToExistingFileRequest
    {
        public string Name { get; set; }
        public string StorageService { get; set; }
        public string StorageId { get; set; }
    }
}