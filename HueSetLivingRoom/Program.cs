﻿using HueApi;
using HueApi.Models;
using HueLivingRoom;

namespace HueSetLivingRoom
{
    internal class Program
    {
        static async Task<int> Main(string bridgeIp, string key, string command)
        {
            // create the api object
            var localHueApi = new LocalHueApi(bridgeIp, key);
            LivingRoom livingRoom = new LivingRoom(localHueApi);

            switch (command)
            {
                case "on":
                    await livingRoom.TurnLightsOn();
                    break;
                case "off":
                    await livingRoom.TurnLightsOff();
                    break;
                case "toggle":
                    await ToggleLights(livingRoom);
                    break;
                default:
                    break;
            }

            await Console.Out.WriteLineAsync("done");
            return 0;
        }

        private static async Task ToggleLights(LivingRoom livingRoom)
        {
            bool isOn = await livingRoom.IsOn();
            if (isOn)
            {
                await livingRoom.TurnLightsOff();
            }
            else
            {
                await livingRoom.TurnLightsOn();
            }
        }
    }
}
