using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.PubSub.V1;
using Grpc.Auth;
using Hiarc.Core.Events.Models;
using Hiarc.Core.Settings.Events.Google;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Google.Cloud.PubSub.V1.PublisherClient;

namespace Hiarc.Core.Events.Google
{
    public class PubSubEventService : IEventService
    {
        private readonly string _name;
        private readonly PubSubSettings _pubSubSettings;
        private readonly ILogger<HiarcEventServiceProvider> _logger;

        public PubSubEventService(string name, IOptions<PubSubSettings> pubSubSettings, ILogger<HiarcEventServiceProvider> logger)
        {
            _name = name;
            _pubSubSettings = pubSubSettings.Value;
            _logger = logger;
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
                var serializedEvent = JsonSerializer.Serialize(theEvent);     

                var topicName = new TopicName(_pubSubSettings.ProjectId, _pubSubSettings.Topic);
                var sacByteArray = System.Text.Encoding.UTF8.GetBytes(_pubSubSettings.ServiceAccountCredential);
                var sacStream = new MemoryStream(sacByteArray);
                var credential = GoogleCredential.FromServiceAccountCredential(ServiceAccountCredential.FromServiceAccountData(sacStream));
                var createSettings = new ClientCreationSettings(credentials: credential.ToChannelCredentials());
                var publisher = await CreateAsync(topicName, clientCreationSettings: createSettings);
                
                var messageId = await publisher.PublishAsync(serializedEvent);
                await publisher.ShutdownAsync(TimeSpan.FromSeconds(10));

                _logger.LogDebug($"Successfully sent event '{theEvent.Event}' to '{this.Name}'. Payload: {serializedEvent}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send event '{theEvent.Event}' to '{this.Name}'. Exception: {ex.Message}");
            }
        }
    }
}