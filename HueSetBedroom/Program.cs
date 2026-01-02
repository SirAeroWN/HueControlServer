using HueApi;
using HueBedroom;
using MQTTnet.Client;
using MQTTnet;
using System.Diagnostics;

namespace HueSetBedroom
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
            Bedroom bedroom = new Bedroom(localHueApi, mqttClient);

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
                    await Morning(bedroom);
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

        private static async Task Morning(Bedroom bedroom)
        {
            await bedroom.SetScene("morning");
            await bedroom.PowerFanOff();
        }

        private static async Task GoodNight(Bedroom bedroom)
        {
            Task fanTask = TurnOnFan();

            await bedroom.SetScene("To Bed");

            int interval = 30;
            await Console.Out.WriteLineAsync($"waiting first {interval} seconds..");
            await Task.Delay(interval * 1000);

            await bedroom.SetScene("Rest");

            await Console.Out.WriteLineAsync($"waiting next {interval} seconds..");
            await Task.Delay(interval * 1000);

            await bedroom.TurnLightsOff();

            await fanTask;
        }

        private static async Task TurnOnFan()
        {
            string script = """
            import broadlink
            device = broadlink.hello('192.168.1.211')
            device.auth()
            onOffPacket = b\"&\x00Z\x00\x93\x93\x111\x102\x110\x112\x101\x111\x10T\x0f2\x110\x11S\x102\x11R\x110\x112\x11R\x110\x112\x92\x94\x102\x101\x111\x111\x111\x102\x10S\x110\x112\x10S\x110\x11R\x112\x101\x11R\x112\x0f\x00\x01\xba\x00\x01&K\x10\x00\x06\x81\x00\x01'J\x11\x00\r\x05\"
            speedPacket = b\"&\x00Z\x00\x93\x94\x102\x101\x111\x111\x110\x112\x10S\x101\x112\x101\x11R\x112\x0f2\x110\x11S\x10T\x0f2\x93\x93\x112\x101\x111\x102\x110\x111\x11S\x101\x111\x111\x11R\x111\x111\x102\x10S\x11R\x11\x00\x01\xb9\x00\x01&K\x11\x00\x06\x81\x00\x01'J\x10\x00\r\x05\"
            device.send_data(onOffPacket)
            device.send_data(speedPacket)
            """;
            script = script.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Aggregate((x, y) => x + "; " + y);
            Process p = new Process();
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                UseShellExecute = true,
                CreateNoWindow = true,
                FileName = "/usr/bin/python3",
                Arguments = $"-c \"{script}\""
            };
            p.StartInfo = psi;

            await Console.Out.WriteLineAsync("Starting script");

            p.Start();

            await p.WaitForExitAsync();

            await Console.Out.WriteLineAsync($"Script exited with {p.ExitCode}");

            return;
        }
    }
}
