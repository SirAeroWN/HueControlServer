using HueApi;
using HueBedroom;

namespace HueSetBedroom
{
    internal class Program
    {
        static async Task<int> Main(string bridgeIp, string key, string command)
        {
            // create the api object
            var localHueApi = new LocalHueApi(bridgeIp, key);
            Bedroom bedroom = new Bedroom(localHueApi);

            switch (command)
            {
                case "on":
                    await bedroom.TurnLightsOn();
                    break;
                case "off":
                    await bedroom.TurnLightsOff();
                    break;
                case "toggle":
                    await ToggleLights(bedroom);
                    break;
                case "goodnight":
                    await GoodNight(bedroom);
                    break;
                case "winkwink":
                    await bedroom.SetScene("winkwink");
                    break;
                case "morning":
                    await bedroom.SetScene("morning");
                    break;
                default:
                    break;
            }

            await Console.Out.WriteLineAsync("done");
            return 0;
        }

        private static async Task ToggleLights(Bedroom bedroom)
        {
            bool isOn = await bedroom.IsOn();
            if (isOn)
            {
                await bedroom.TurnLightsOff();
            }
            else
            {
                await bedroom.TurnLightsOn();
            }
        }

        private static async Task GoodNight(Bedroom bedroom)
        {
            await bedroom.SetScene("To Bed");

            int interval = 30;
            await Console.Out.WriteLineAsync($"waiting first {interval} seconds..");
            await Task.Delay(interval * 1000);

            await bedroom.SetScene("Rest");

            await Console.Out.WriteLineAsync($"waiting next {interval} seconds..");
            await Task.Delay(interval * 1000);

            await bedroom.TurnLightsOff();
        }
    }
}
