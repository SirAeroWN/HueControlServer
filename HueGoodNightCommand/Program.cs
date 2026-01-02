using HueApi;
using HueApi.Models.Requests;
using HueApi.Models;
using HueBedroom;
using System.Diagnostics;

namespace HueGoodNightCommand
{
    internal class Program
    {
        static async Task<int> Main(string bridgeIp, string key)
        {
            // create the api object
            var localHueApi = new LocalHueApi(bridgeIp, key);
            Bedroom bedroom = new Bedroom(localHueApi);

            Task fanTask = TurnOnFan();

            await bedroom.SetScene("To Bed");

            int interval = 60;
            await Console.Out.WriteLineAsync($"waiting first {interval} seconds..");
            await Task.Delay(interval * 1000);

            await bedroom.SetScene("Rest");

            await Console.Out.WriteLineAsync($"waiting next {interval} seconds..");
            await Task.Delay(interval * 1000);

            await bedroom.TurnLightsOff();

            await fanTask;

            await Console.Out.WriteLineAsync("done");
            return 0;
        }

        private static async Task TurnOnFan()
        {
            string script = """
            import broadlink
            device = broadlink.hello('192.168.1.211')
            device.auth()
            onOffPacket = b"&\x00Z\x00\x93\x93\x111\x102\x110\x112\x101\x111\x10T\x0f2\x110\x11S\x102\x11R\x110\x112\x11R\x110\x112\x92\x94\x102\x101\x111\x111\x111\x102\x10S\x110\x112\x10S\x110\x11R\x112\x101\x11R\x112\x0f\x00\x01\xba\x00\x01&K\x10\x00\x06\x81\x00\x01'J\x11\x00\r\x05"
            speedPacket = b"&\x00Z\x00\x93\x94\x102\x101\x111\x111\x110\x112\x10S\x101\x112\x101\x11R\x112\x0f2\x110\x11S\x10T\x0f2\x93\x93\x112\x101\x111\x102\x110\x111\x11S\x101\x111\x111\x11R\x111\x111\x102\x10S\x11R\x11\x00\x01\xb9\x00\x01&K\x11\x00\x06\x81\x00\x01'J\x10\x00\r\x05"
            device.send_data(onOffPacket)
            device.send_data(speedPacket)
            """;
            script = script.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Aggregate((x, y) => x + "; " + y);
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                UseShellExecute = true,
                CreateNoWindow = true,
                FileName = "python3",
                Arguments = $"-c script"
            };

            Process? p = Process.Start(psi);
            if (p is null)
            {
                return;
            }

            await p.WaitForExitAsync();
            return;
        }
    }
}
