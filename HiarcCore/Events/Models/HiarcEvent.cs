using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Hiarc.Core.Events.Models 
{
    public class HiarcEvent : IHiarcEvent
    {
        public string Event { get; set; }
        public string Uid { get; set; }
        public Dictionary<string,object> Properties { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public HiarcEvent(string eventName, Dictionary<string,object> properties)
        {
            this.Event = eventName;
            this.Uid = Guid.NewGuid().ToString();
            this.Properties = properties;
            this.Timestamp = DateTime.UtcNow;
        }
    }
}