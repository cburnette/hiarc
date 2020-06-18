using Hiarc.Core.Database;
using Hiarc.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HiarcGRPC
{
    public partial class HiarcService2 : HiarcService.HiarcServiceBase
    {
        private readonly ILogger<HiarcService2> _logger;
        private readonly HiarcSettings _hiarcSettings;
        private readonly IHiarcDatabase _hiarcDatabase;
        
        public HiarcService2(ILogger<HiarcService2> logger,
                            IOptions<HiarcSettings> hiarcSettings,
                            IHiarcDatabase hiarcDatabase)
        {
            _logger = logger;
            _hiarcDatabase = hiarcDatabase;
            _hiarcSettings = hiarcSettings.Value;
        }
    }
}
