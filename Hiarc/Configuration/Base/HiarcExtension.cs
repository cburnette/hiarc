using System;
using Microsoft.Extensions.Configuration;

namespace Hiarc.Configuration.Base
{
    public static class HiarcExtension
    {
        public static IConfigurationBuilder AddHiarc(this IConfigurationBuilder configuration,
        Action<HiarcOptions> options = null)
        {

            var configOptions = new HiarcOptions();

            options?.Invoke(configOptions);
            configuration.Add(new HiarcSource(configOptions));
            return configuration;
        }
    }
}