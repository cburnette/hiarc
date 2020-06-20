using System;
using System.Threading.Tasks;

namespace Hiarc.Configuration.Strategies
{
    public class HiarcConfigurationContext
    {
        public IHiarcConfigurationStrategy Strategy { get; set; }
        public HiarcConfigurationContext(IHiarcConfigurationStrategy strategy = null)
        {
            Strategy = strategy;
        }
        public async Task IncludeHiarcSettingsAsync(Action<string, string> set)
        {
            await this.Strategy.GetHiarcConfigurationAsync(set);
        }
        public void IncludeHiarcSettings(Action<string, string> set)
        {
            this.Strategy.GetHiarcConfiguration(set);
        }
    }
}