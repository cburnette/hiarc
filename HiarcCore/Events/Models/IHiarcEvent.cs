using System;
using System.Collections.Generic;

namespace Hiarc.Core.Events.Models 
{
    public interface IHiarcEvent
    {
        string Event { get; set; }
        string Uid { get; set; }
        Dictionary<string,object> Properties { get; set; }
        DateTimeOffset Timestamp { get; set; }
    }
}