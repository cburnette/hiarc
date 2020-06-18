using System;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hiarc.Core.Events.Models;
using Hiarc.Core.Settings.Events.Azure;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hiarc.Core.Events.Azure
{
    public class ServiceBusEventService : IEventService
    {
        private readonly string _name;
        private readonly ServiceBusSettings _serviceBusSettings;
        private readonly ILogger<HiarcEventServiceProvider> _logger;

        public ServiceBusEventService(string name, IOptions<ServiceBusSettings> serviceBusSettings, ILogger<HiarcEventServiceProvider> logger)
        {
            _name = name;
            _serviceBusSettings = serviceBusSettings.Value;
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
                var topicClient = new TopicClient(_serviceBusSettings.ConnectionString, _serviceBusSettings.Topic);
                
                var serializedEvent = JsonSerializer.Serialize(theEvent);
                var message = new Message(Encoding.UTF8.GetBytes(serializedEvent))
                {
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString()
                };

                await topicClient.SendAsync(message);
                _logger.LogDebug($"Successfully sent event '{theEvent.Event}' to '{this.Name}'. Payload: {serializedEvent}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send event to '{this.Name}'. Exception: {ex.Message}");
            }
        }
    }
}