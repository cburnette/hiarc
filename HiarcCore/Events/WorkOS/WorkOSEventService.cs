using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Hiarc.Core.Events.Models;
using Hiarc.Core.Settings.Events.WorkOS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hiarc.Core.Events.WorkOS
{
    public class WorkOSEventService : IEventService
    {
        private readonly string _name;
        private readonly WorkOSSettings _workOSSettings;
        private readonly ILogger<HiarcEventServiceProvider> _logger;
        private readonly IHttpClientFactory clientFactory;
        private const string WORKOS_EVENTS_URL = "https://api.workos.com/events";


        public WorkOSEventService(string name, IOptions<WorkOSSettings> workOSSettings, ILogger<HiarcEventServiceProvider> logger)
        {
            _name = name;
            _workOSSettings = workOSSettings.Value;
            _logger = logger;

            var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            clientFactory = serviceProvider.GetService<IHttpClientFactory>();
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
                // test code
                var metadata = new Dictionary<string,object>();
                metadata.Add("query", new Dictionary<string,object> { {"action", "user.created"}, {"search", "system"} });
                metadata.Add("blahblah", "yo yo yo");

                var workOSEvent = new WorkOSEvent() {
                    group = "hiarcdb.com",
                    location = "192.168.86.34",
                    action = "user.created",
                    action_type = "create",
                    actor_name = "admin",
                    actor_id = "admin",
                    target_name = "Annie Easley",
                    target_id = "user_01DEQWZNQT8Y47HDPSJKQS1J3F",
                    metadata = metadata,
                    occurred_at = theEvent.Timestamp
                };

                var serializedEvent = JsonSerializer.Serialize(workOSEvent);
                var httpContent = new StringContent(serializedEvent);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                
                var response = await WorkOSClient.PostAsync(WORKOS_EVENTS_URL, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug($"Successfully sent event '{theEvent.Event}' to '{this.Name}'. Payload: {serializedEvent}");
                }
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send event to '{this.Name}'. Response Body: {body}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send event to '{this.Name}'. Exception: {ex.Message}");
            }
        }

        private HttpClient WorkOSClient
        {
            get
            {
                var client = clientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _workOSSettings.SecretKey);
                return client;
            }
        }
    }

    public class WorkOSEvent
    {
        public string group { get; set; }
        public string location { get; set; }
        public string action { get; set; }
        public string action_type { get; set; }
        public string actor_name { get; set; }
        public string actor_id { get; set; }
        public string target_name { get; set; }
        public string target_id { get; set; }
        public Dictionary<string,object> metadata { get; set; }
        public DateTimeOffset occurred_at { get; set; }
    }
}