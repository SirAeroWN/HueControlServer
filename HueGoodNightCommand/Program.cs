using HueApi;
using HueApi.Models.Requests;
using HueApi.Models;
using HueBedroom;

namespace HueGoodNightCommand
{
    internal class Program
    {
        static async Task<int> Main(string bridgeIp, string key)
        {
            // create the api object
            var localHueApi = new LocalHueApi(bridgeIp, key);
            Bedroom bedroom = new Bedroom(localHueApi);

            await bedroom.SetScene("To Bed");

            int interval = 60;
            await Console.Out.WriteLineAsync($"waiting first {interval} seconds..");
            await Task.Delay(interval * 1000);

            await bedroom.SetScene("Rest");

            await Console.Out.WriteLineAsync($"waiting next {interval} seconds..");
            await Task.Delay(interval * 1000);

            await bedroom.TurnLightsOff();

            await Console.Out.WriteLineAsync("done");
            return 0;
        }
    }
}
