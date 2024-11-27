using HueApi;
using HueApi.Models;
using HueOffice;
using MQTTnet.Client;
using MQTTnet;

namespace HueSetOffice
{
    internal class Program
    {
#if DEBUG
        private static string _server = "olympus-homelab.duckdns.org";
#else
        private static string _server = "mqtt";
#endif

        static async Task<int> Main(string bridgeIp, string key, string command)
        {
            // create the api object
            var localHueApi = new LocalHueApi(bridgeIp, key);
            // create the mqtt client
            using IMqttClient mqttClient = new MqttFactory().CreateMqttClient();
            MqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(_server, port: 1883).Build();
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            Office office = new Office(localHueApi, mqttClient);

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
                case "winkwink":
                    await office.SetScene("winkwink");
                    break;
                default:
                    break;
            }

            await mqttClient.DisconnectAsync();

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