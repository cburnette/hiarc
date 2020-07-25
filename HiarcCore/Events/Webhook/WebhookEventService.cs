using System;
using System.Dynamic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hiarc.Core.Events.Models;
using Hiarc.Core.Settings.Events.Webhook;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hiarc.Core.Events.Webhook
{
    public class WebhookEventService : IEventService
    {
        private readonly string _name;
        private readonly WebhookSettings _webhookSettings;
        private readonly ILogger<HiarcEventServiceProvider> _logger;
        private readonly IHttpClientFactory clientFactory;
        private const string HIARC_WEBHOOK_SIGNATURE_HEADER_NAME = "X-HIARC-SIGNATURE";

        public WebhookEventService(string name, IOptions<WebhookSettings> webhookSettings, ILogger<HiarcEventServiceProvider> logger)
        {
            _name = name;
            _webhookSettings = webhookSettings.Value;
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
                var serializedEvent = JsonSerializer.Serialize(theEvent);
                var httpContent = new StringContent(serializedEvent);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                if (!string.IsNullOrWhiteSpace(_webhookSettings.Secret))
                {
                    // https://stripe.com/docs/webhooks/signatures
                    var unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var payload = $"{unixNow}.{serializedEvent}";

                    var encoding = new UTF8Encoding();
                    var keyBytes = encoding.GetBytes(_webhookSettings.Secret);
                    var textBytes = encoding.GetBytes(payload);

                    using HMACSHA256 hash = new HMACSHA256(keyBytes);
                    var hashBytes = hash.ComputeHash(textBytes);
                    var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    var signature = $"t={unixNow},v1={hashString}";

                    httpContent.Headers.Add(HIARC_WEBHOOK_SIGNATURE_HEADER_NAME, signature);
                }
                
                var response = await WebhookClient.PostAsync(_webhookSettings.URL, httpContent);  

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

        private HttpClient WebhookClient
        {
            get
            {
                var client = clientFactory.CreateClient();
                return client;
            }
        }
    }
}