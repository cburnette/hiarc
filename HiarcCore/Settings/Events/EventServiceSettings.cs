using System.Dynamic;

namespace Hiarc.Core.Settings.Events
{
    public class EventServiceSetting
    {
        public string Provider { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public ExpandoObject Config { get; set; }
    }
}