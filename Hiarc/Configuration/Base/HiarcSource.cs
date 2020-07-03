using Microsoft.Extensions.Configuration;

namespace Hiarc.Configuration.Base
{
    public class HiarcSource : IConfigurationSource
    {
        public HiarcSource(HiarcOptions options)
        {

        }
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new HiarcProvider(this);
        }
    }
}