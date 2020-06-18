namespace Hiarc.Core.Settings.Events.AWS
{
    public class KinesisSettings
    {
        public string AccessKeyId { get; set; }
        public string SecretAccessKey { get; set; }
        public string RegionSystemName { get; set; }
        public string Stream { get; set; }
    }
}