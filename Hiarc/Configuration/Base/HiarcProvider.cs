using System;
using System.Threading.Tasks;
using Hiarc.Configuration.Strategies;
using Microsoft.Extensions.Configuration;

namespace Hiarc.Configuration.Base
{
    public class HiarcProvider : ConfigurationProvider
    {
        private const string STRATEGY_SETTING = "HIARC_CONFIG_STRATEGY";
        private const string ENVIRONMENT_STRATEGY = "env";
        private const string APPSETTINGS_STRATEGY = "app_settings";
        public HiarcSource Source { get; }
        public HiarcProvider(HiarcSource source)
        {
            Source = source;
        }

        public override void Load()
        {
            LoadAsync().Wait();
        }
        public async Task LoadAsync()
        {
            var loadSettings = Environment.GetEnvironmentVariable(STRATEGY_SETTING) != null ? Environment.GetEnvironmentVariable(STRATEGY_SETTING) : APPSETTINGS_STRATEGY;
            switch (loadSettings)
            {
                case APPSETTINGS_STRATEGY:
                    break;
                case ENVIRONMENT_STRATEGY:
                    var ctx = new HiarcConfigurationContext(new HiarcEnvironmentVariableStrategy());
                    ctx.IncludeHiarcSettings(Set);
                    break;
                default:
                    break;
            }
        }
    }
}