using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Hiarc.Core.Events.AWS;
using Hiarc.Core.Events.Azure;
using Hiarc.Core.Settings;
using Hiarc.Core.Settings.Events.AWS;
using Hiarc.Core.Settings.Events.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hiarc.Core.Events.Models;
using Hiarc.Core.Settings.Events.Google;
using Hiarc.Core.Events.Google;
using Hiarc.Core.Models;
using Hiarc.Core.Settings.Events.Webhook;
using Hiarc.Core.Events.Webhook;

namespace Hiarc.Core.Events
{

    public class HiarcEventServiceProvider : IEventServiceProvider
    {
        public const string AWS_KINESIS = "AWS-Kinesis";
        public const string AZURE_SERVICE_BUS = "Azure-ServiceBus";
        public const string GOOGLE_PUBSUB = "Google-PubSub";
        public const string WEBHOOK = "Webhook";

        public const string EVENT_USER_CREATED = "userCreated";
        public const string EVENT_USER_UPDATED = "userUpdated";
        public const string EVENT_GROUP_CREATED = "groupCreated";
        public const string EVENT_COLLECTION_CREATED = "collectionCreated";
        public const string EVENT_FILE_CREATED = "fileCreated";
        public const string NEW_VERSION_OF_FILE_CREATED = "newVersionOfFileCreated";
        public const string ADDED_USER_TO_GROUP = "addedUserToGroup";
        public const string ADDED_CHILD_TO_COLLECTION = "addedChildToCollection";
        public const string ADDED_GROUP_TO_COLLECTION = "addedGroupToCollection";
        public const string ADDED_FILE_TO_COLLECTION = "addedFileToCollecton";

        private List<IEventService> _eventServices;
        private readonly HiarcSettings _hiarcSettings;
        private readonly ILogger<HiarcEventServiceProvider> _logger;

        public HiarcEventServiceProvider(IOptions<HiarcSettings> hiarcSettings,
                                    ILogger<HiarcEventServiceProvider> logger)
        {
            _hiarcSettings = hiarcSettings.Value;
            _logger = logger;

            this.ConfigureServices();
        }

        public async Task SendEvent(IHiarcEvent theEvent)
        {
            foreach (var es in _eventServices)
            {
                await Task.Run(() => {
                    es.SendEvent(theEvent);
                });
            }
        }    

        public async Task SendUserCreatedEvent(User user)
        {
            await SendEntityEvent(EVENT_USER_CREATED, user);
        }

        public async Task SendUserUpdatedEvent(User user)
        {
            await SendEntityEvent(EVENT_USER_UPDATED, user);
        }

        public async Task SendGroupCreatedEvent(Group group)
        {
            await SendEntityEvent(EVENT_GROUP_CREATED, group);
        }

        public async Task SendCollectionCreatedEvent(Collection collection)
        {
            await SendEntityEvent(EVENT_COLLECTION_CREATED, collection);
        }

        public async Task SendFileCreatedEvent(File file)
        {
            await SendEntityEvent(EVENT_FILE_CREATED, file);
        }

        public async Task SendAddedUserToGroupEvent(User user, Group group)
        {
            var eventProps = new Dictionary<string, object>
            {
                { "User", user.ToDictionary() },
                { "Group", group.ToDictionary() }
            };

            var theEvent = new HiarcEvent(ADDED_USER_TO_GROUP, eventProps);
            await this.SendEvent(theEvent);
        }

        public async Task SendAddedChildToCollectionEvent(Collection child, Collection parent)
        {
            var eventProps = new Dictionary<string, object>
            {
                { "Parent", parent.ToDictionary() },
                { "Child", child.ToDictionary() }
            };

            var theEvent = new HiarcEvent(ADDED_CHILD_TO_COLLECTION, eventProps);
            await this.SendEvent(theEvent);
        }

        public async Task SendAddedGroupToCollectionEvent(Group group, Collection collection)
        {
            var eventProps = new Dictionary<string, object>
            {
                { "Group", group.ToDictionary() },
                { "Collection", collection.ToDictionary() }
            };

            var theEvent = new HiarcEvent(ADDED_GROUP_TO_COLLECTION, eventProps);
            await this.SendEvent(theEvent);
        }

        public async Task SendAddedFileToCollectionEvent(File file, Collection collection)
        {
            var eventProps = new Dictionary<string, object>
            {
                { "File", file.ToDictionary() },
                { "Collection", collection.ToDictionary() }
            };

            var theEvent = new HiarcEvent(ADDED_FILE_TO_COLLECTION, eventProps);
            await this.SendEvent(theEvent);
        }

        public async Task SendNewVersionOfFileCreatedEvent(File file, FileVersion fileVersion)
        {
            var eventProps = new Dictionary<string, object>
            {
                { "File", file.ToDictionary() },
                { "FileVersion", fileVersion.ToDictionary() }
            };

            var theEvent = new HiarcEvent(NEW_VERSION_OF_FILE_CREATED, eventProps);
            await this.SendEvent(theEvent);
        }

        private async Task SendEntityEvent<T>(string eventName, T entity, Dictionary<string,object> additionalProps = null) where T:Entity
        {
            var eventProps = entity.ToDictionary();

            if (additionalProps != null)
            {
                foreach(var item in additionalProps)
                {
                    eventProps.Add(item.Key, item.Value);
                }
            }

            var theEvent = new HiarcEvent(eventName, eventProps);
            await this.SendEvent(theEvent);
        }

        private void ConfigureServices()
        {
            _eventServices = new List<IEventService>();

            foreach (var es in _hiarcSettings.EventServices)
            {
                if (!es.Enabled)
                {
                    continue;
                }

                if (es.Provider == AWS_KINESIS)
                {
                    var settings = new KinesisSettings
                    {
                        AccessKeyId = ((dynamic)es.Config).AccessKeyId,
                        SecretAccessKey = ((dynamic)es.Config).SecretAccessKey,
                        RegionSystemName = ((dynamic)es.Config).RegionSystemName,
                        Stream = ((dynamic)es.Config).Stream
                    };
                    IOptions<KinesisSettings> kinesisSettings = Options.Create(settings);

                    IEventService kinesisService = new KinesisEventService(es.Name, kinesisSettings, _logger);
                    _eventServices.Add(kinesisService);
                }
                else if (es.Provider == AZURE_SERVICE_BUS)
                {
                    var settings = new ServiceBusSettings
                    {
                        ConnectionString = ((dynamic)es.Config).ConnectionString,
                        Topic = ((dynamic)es.Config).Topic
                    };
                    IOptions<ServiceBusSettings> serviceBusSettings = Options.Create(settings);

                    IEventService serviceBusService = new ServiceBusEventService(es.Name, serviceBusSettings, _logger);
                    _eventServices.Add(serviceBusService);
                }
                else if (es.Provider == GOOGLE_PUBSUB)
                {
                    var settings = new PubSubSettings
                    {
                        ServiceAccountCredential = ((dynamic)es.Config).ServiceAccountCredential,
                        ProjectId = ((dynamic)es.Config).ProjectId,
                        Topic = ((dynamic)es.Config).Topic
                    };
                    IOptions<PubSubSettings> pubSubSettings = Options.Create(settings);

                    IEventService pubSubService = new PubSubEventService(es.Name, pubSubSettings, _logger);
                    _eventServices.Add(pubSubService);
                }
                else if (es.Provider == WEBHOOK)
                {
                    var settings = new WebhookSettings
                    {
                        URL = ((dynamic)es.Config).URL,
                        Secret = ((dynamic)es.Config).Secret
                    };
                    IOptions<WebhookSettings> webhookSettings = Options.Create(settings);

                    IEventService webhookService = new WebhookEventService(es.Name, webhookSettings, _logger);
                    _eventServices.Add(webhookService);
                }
                else
                {
                    throw new Exception($"Unsupported event service provider: {es.Provider}");
                }
            }
        }
    }
}