using HueApi;
using HueApi.Models;
using HueOffice;

namespace HueSetOffice
{
    internal class Program
    {
        static async Task<int> Main(string bridgeIp, string key, string command)
        {
            // create the api object
            var localHueApi = new LocalHueApi(bridgeIp, key);
            Office office = new Office(localHueApi);

            switch (command)
            {
                case "on":
                    await office.TurnLightsOn();
                    break;
                case "off":
                    await office.TurnLightsOff();
                    break;
                case "toggle":
                    await ToggleLights(office);
                    break;
                default:
                    break;
            }

            await Console.Out.WriteLineAsync("done");
            return 0;
        }

        private static async Task ToggleLights(Office office)
        {
            bool isOn = await office.IsOn();
            if (isOn)
            {
                await office.TurnLightsOff();
            }
            else
            {
                await office.TurnLightsOn();
            }
        }
    }
}