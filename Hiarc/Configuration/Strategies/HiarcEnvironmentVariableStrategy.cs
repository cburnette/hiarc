using System;
using System.Text.Json;
using System.Threading.Tasks;
using Hiarc.Configuration.Strategies.Models;
using Hiarc.Core.Settings;
using Hiarc.Core.Settings.Events;
using Hiarc.Core.Settings.Storage;
using static Hiarc.Configuration.Strategies.HiarcConfigurationUtility;

namespace Hiarc.Configuration.Strategies
{
    public class HiarcEnvironmentVariableStrategy : IHiarcConfigurationStrategy
    {
        private const string HIARC = "HIARC_SETTINGS";
        public void GetHiarcConfiguration(Action<string, string> set)
        {
            var settingsBlob = Environment.GetEnvironmentVariable(HIARC);
            string convertedSettings;
            try
            {
                convertedSettings = System.Text.UTF8Encoding.UTF8.GetString(Convert.FromBase64String(settingsBlob));
            }
            catch (FormatException)
            {
                convertedSettings = settingsBlob;
            }
            var settings = JsonSerializer.Deserialize<HiarcSettingsModel>(convertedSettings, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            
            Load<HiarcSettingsModel>(settings, set);
            Load<HiarcDatabaseSettings>(settings.Database, set);

            for (int i = 0; i < settings.StorageServices.Length; i++)
            {
                var stt = settings.StorageServices[i];
                Load<StorageServiceSetting>(stt, set, i);

                ProcessStorageServices(stt, set, i);
            }

            for (int i = 0; i < settings.EventServices.Length; i++)
            {
                var ste = settings.EventServices[i];
                Load<EventServiceSetting>(ste, set, i);

                ProcessEventServices(ste, set, i);
            }
        }
        public Task GetHiarcConfigurationAsync(Action<string, string> set)
        {
            throw new NotImplementedException();
        }
    }
}