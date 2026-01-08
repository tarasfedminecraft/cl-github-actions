using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CL_CLegendary_Launcher_.Class
{
    public class EulaConfig
    {
        [JsonProperty("last_updated")]
        public DateTime LastUpdated { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public static class EulaService
    {
        private const string EulaUrl = "YOUR_EULA_URL_HERE"; 

        public static async Task<EulaConfig> GetEulaAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string url = $"{EulaUrl}";

                    string json = await client.GetStringAsync(url);

                    var settings = new JsonSerializerSettings
                    {
                        DateFormatString = "dd.MM.yyyy"
                    };

                    return JsonConvert.DeserializeObject<EulaConfig>(json, settings);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ПОМИЛКА EULA: {ex.Message}");
                return null;
            }
        }

        public static bool IsEulaOutdated(DateTime serverDate)
        {
            return Settings1.Default.EulaAcceptedDate < serverDate;
        }
    }
}
