namespace Hiarc.Core.Models.Requests
{
    public class CreateUserTokenRequest
    {
        public string Key { get; set; }
        public double? ExpirationMinutes { get; set; }
    }
}