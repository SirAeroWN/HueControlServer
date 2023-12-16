using HueApi.Models;
using HueApi;
using HueApi.Models.Requests;
using System.Diagnostics;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

namespace HueControlServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            string? ip = builder.Configuration["LocalHueApi:ip"];
            string? key = builder.Configuration["LocalHueApi:key"];
            string? env = builder.Configuration["Runtime:Environment"];

            if (ip == null || key == null || env == null)
            {
                throw new ArgumentNullException("config values missing");
            }

            bool isProd = env.Equals("Production", StringComparison.OrdinalIgnoreCase);

            WebApplication app = builder.Build();

            // create the api object
            var localHueApi = new LocalHueApi(ip, key);

            // set command paths
            Dictionary<string, string> commands = new Dictionary<string, string>()
            {
                { "GoodNight", Path.Combine(builder.Environment.ContentRootPath, isProd ? "HueGoodNightCommand" : "../HueGoodNightCommand/bin/Debug/net7.0/HueGoodNightCommand.exe") }
                ,{ "SetLivingRoom", Path.Combine(builder.Environment.ContentRootPath, isProd ? "HueSetLivingRoom" : "../HueSetLivingRoom/bin/Debug/net7.0/HueSetLivingRoom.exe") }
                ,{ "SetBedroom", Path.Combine(builder.Environment.ContentRootPath, isProd ? "HueSetBedroom" : "../HueSetBedroom/bin/Debug/net7.0/HueSetBedroom.exe") }
            };

            GateKeeper gateKeeper = new GateKeeper(1000);

            app.MapGet("/", () => "Hello World!");

            app.MapGet("/gn", () =>
            {
                if (gateKeeper.TryRun("GoodNight"))
                {
                    Process process = new Process();
                    process.StartInfo.FileName = commands["GoodNight"];
                    process.StartInfo.Arguments = $"--bridge-ip {ip} --key {key}";
                    process.Start();
                    return Results.Ok("Good Night Started");
                }
                else
                {
                    return Results.BadRequest("Too many attempts");
                }
            });

            app.MapGet("/cgn", () =>
            {
                if (gateKeeper.TryRun("CancelGoodNight"))
                {
                    Process.GetProcessesByName("HueGoodNightCommand").FirstOrDefault()?.Kill();
                    SetBedRoom(ip, key, commands, "on");
                    return Results.Ok("Good Night Stopped, probably");
                }
                else
                {
                    return Results.BadRequest("Too many attempts");
                }
            });

            app.MapGet("/lr/t", () =>
            {
                if (gateKeeper.TryRun("SetLivingRoom"))
                {
                    return SetLivingRoom(ip, key, commands, "toggle");
                }
                else
                {
                    return Results.BadRequest("Too many attempts");
                }
            });

            app.MapGet("/lr/on", () =>
            {
                if (gateKeeper.TryRun("SetLivingRoom"))
                {
                    return SetLivingRoom(ip, key, commands, "on");
                }
                else
                {
                    return Results.BadRequest("Too many attempts");
                }
            });

            app.MapGet("/lr/off", () =>
            {
                if (gateKeeper.TryRun("SetLivingRoom"))
                {
                    return SetLivingRoom(ip, key, commands, "off");
                }
                else
                {
                    return Results.BadRequest("Too many attempts");
                }
            });

            // Subscribe to MQTT to get device events
            var mqttFactory = new MqttFactory();

            using (IMqttClient mqttClient = mqttFactory.CreateMqttClient())
            {
                MqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(isProd ? "mqtt" : "olympus-homelab.duckdns.org", port: 1883).Build();

                // Setup message handling before connecting so that queued messages
                // are also handled properly. When there is no event handler attached all
                // received messages get lost.
                mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    Console.WriteLine("Received application message.");
                    string payload = System.Text.Encoding.Default.GetString(e.ApplicationMessage.PayloadSegment);
                    SNZB_01Message? message = JsonSerializer.Deserialize<SNZB_01Message>(payload);
                    switch (message?.action)
                    {
                        case "":
                            break;
                        case "single":
                            SetBedRoom(ip, key, commands, "toggle");
                            break;
                        case "double":
                            SetBedRoom(ip, key, commands, "goodnight");
                            break;
                        case "long":
                            SetBedRoom(ip, key, commands, "winkwink");
                            break;
                    }

                    return Task.CompletedTask;
                };

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                MqttClientSubscribeOptions mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(
                        f =>
                        {
                            f.WithTopic("zigbee2mqtt/0x00124b0029114714");
                        })
                    .Build();

                await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

                Console.WriteLine("MQTT client subscribed to topic.");

                app.Urls.Add("http://hcs.olympus-homelab.duckdns.org:7160");
                app.Run();
            }
        }

        private static void SetBedRoom(string ip, string key, Dictionary<string, string> commands, string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = commands["SetBedroom"];
            process.StartInfo.Arguments = $"--bridge-ip {ip} --key {key} --command {command}";
            process.Start();
        }

        private static IResult SetLivingRoom(string ip, string key, Dictionary<string, string> commands, string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = commands["SetLivingRoom"];
            process.StartInfo.Arguments = $"--bridge-ip {ip} --key {key} --command {command}";
            process.Start();
            return Results.Ok("Toggle Living Room Started");
        }
    }

    class GateKeeper
    {
        private Dictionary<string, long> _lastCommandRuntime = new();

        private long _ageThreshold { get; }

        public GateKeeper(long ageThreshold)
        {
            this._ageThreshold = ageThreshold;
        }

        public bool TryRun(string command)
        {
            if (!_lastCommandRuntime.ContainsKey(command))
            {
                this._lastCommandRuntime[command] = DateTime.Now.Ticks;
                return true;
            }

            if ((DateTime.Now.Ticks - _lastCommandRuntime[command]) > this._ageThreshold)
            {
                this._lastCommandRuntime[command] = DateTime.Now.Ticks;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
