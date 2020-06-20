using System;
using System.Threading.Tasks;

namespace Hiarc.Configuration.Strategies
{
    public interface IHiarcConfigurationStrategy
    {
        Task GetHiarcConfigurationAsync(Action<string, string> set);
        void GetHiarcConfiguration(Action<string, string> set);
    }
}