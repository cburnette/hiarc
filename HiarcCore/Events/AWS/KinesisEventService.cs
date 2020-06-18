using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Hiarc.Core.Events.Models;
using Hiarc.Core.Settings.Events.AWS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hiarc.Core.Events.AWS
{
    public class KinesisEventService : IEventService
    {
        private readonly string _name;
        private readonly KinesisSettings _kinesisSettings;
        private readonly ILogger<HiarcEventServiceProvider> _logger;
        private readonly RegionEndpoint _region;

        public KinesisEventService(string name, IOptions<KinesisSettings> kinesisSettings, ILogger<HiarcEventServiceProvider> logger)
        {
            _name = name;
            _kinesisSettings = kinesisSettings.Value;
            _logger = logger;
            _region = RegionEndpoint.GetBySystemName(_kinesisSettings.RegionSystemName);
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public async Task SendEvent(IHiarcEvent theEvent)
        {
            try
            {
                var accessKeyId = _kinesisSettings.AccessKeyId;
                var secretAccessKey = _kinesisSettings.SecretAccessKey;
                var streamName = _kinesisSettings.Stream;
                var partitionKey = theEvent.Event;
                var kinesisClient = new AmazonKinesisClient(accessKeyId, secretAccessKey, _region);
                
                var serializedEvent = JsonSerializer.Serialize(theEvent);
                using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(theEvent.Event));

                var requestRecord = new PutRecordRequest() { StreamName = streamName, PartitionKey = partitionKey, Data = memoryStream };
                var responseRecord = await kinesisClient.PutRecordAsync(requestRecord);

                _logger.LogDebug($"Successfully sent event '{theEvent.Event}' to '{this.Name}'. Payload: {serializedEvent}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send event '{theEvent.Event}' to '{this.Name}'. Exception: {ex.Message}");
            }
        }
    }
}