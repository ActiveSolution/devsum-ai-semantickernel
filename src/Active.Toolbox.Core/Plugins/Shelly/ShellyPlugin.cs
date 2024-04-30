using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Globalization;

namespace Active.Toolbox.Core.Plugins.Shelly
{
    public class ShellyPlugin(ShellyPluginConfig config)
    {
        [KernelFunction("Shelly_GetAllSensors"), Description("Get all temperature sensors in the office, as a comma separated string")]
        [return: Description("A comma separated string of all sensors")]
        public string GetAllSensors()
        {
            return $"öppet kontor, flexibel yta, fokusyta, kreativ yta";
        }

        [KernelFunction("Shelly_GetTemperature"), Description("Get the temperature from a sensor at the office")]
        public async Task<string> GetTemperature(
            [Description("The name of the sensor. Can be either 'öppet kontor', 'flexibel yta', 'fokusyta' or 'kreativ yta'")] string sensor)
        {
            string deviceId = MapRoomToDeviceId(sensor);
            if (deviceId == string.Empty)
            {
                throw new Exception($"Room '{sensor}' is not a valid room");
            }

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{config.Url}/device/status");
            var collection = new List<KeyValuePair<string, string>>
            {
                new("id", deviceId),
                new("auth_key", config.AuthKey)
            };
            var content = new FormUrlEncodedContent(collection);
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string resp = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(resp);
            double temperature = (double)json["data"]["device_status"]["tmp"]["value"];

            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            return temperature.ToString(nfi);
        }

        private string MapRoomToDeviceId(string room)
        {
            switch (room.ToLower())
            {
                case "öppet kontor":
                    return "701ae2";
                case "flexibel yta":
                    return "ca5e9b";
                case "fokusyta":
                    return "c9f389";
                case "kreativ yta":
                    return "ca61a1";
                default:
                    return string.Empty;
            }
        }
    }
}
