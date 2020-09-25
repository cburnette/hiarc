using System;
using System.Collections.Generic;

namespace Hiarc.Core.Models 
{
    public class RetentionPolicy : Entity
    {
        /*
            (Based on Google Cloud Retention Periods which are in seconds)

            * A minute is considered to be 60 seconds.
            * A hour is considered to be 3,600 seconds.
            * A day is considered to be 86,400 seconds.
            * A month is considered to be 31 days, which is 2,678,400 seconds.
            * A year is considered to be 365.25 days, which is 31,557,600 seconds.

            You can set a maximum retention period of 3,155,760,000 seconds (100 years)
        */

        public const uint RETENTION_PERIOD_MINUTE = 60;
        public const uint RETENTION_PERIOD_HOUR   = 3600;
        public const uint RETENTION_PERIOD_DAY    = 86400;
        public const uint RETENTION_PERIOD_MONTH  = 2678400;
        public const uint RETENTION_PERIOD_YEAR   = 31557600;
        public const uint RETENTION_PERIOD_MAX    = 3155760000;

        public uint Seconds { get; set; }

        public RetentionPolicy()
        {
            this.Type = Entity.TYPE_RETENTION_POLICY;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var eventProps = base.ToDictionary();
            eventProps.Add("Seconds", this.Seconds);
            return eventProps;
        }
    }
}