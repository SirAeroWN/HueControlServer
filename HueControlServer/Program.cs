using HueApi.Models;
using HueApi;
using HueApi.Models.Requests;
using System.Diagnostics;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using System;
using System.Threading;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Builder;
using System.Linq;

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

            CommandRunner commandRunner = new CommandRunner(ip, key, commands);

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
                    commandRunner.SetBedRoom("on");
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
                    return commandRunner.SetLivingRoom("toggle");
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
                    return commandRunner.SetLivingRoom("on");
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
                    return commandRunner.SetLivingRoom("off");
                }
                else
                {
                    return Results.BadRequest("Too many attempts");
                }
            });

            using (IMqttClient mqttClient = new MqttFactory().CreateMqttClient())
            {
                // Subscribe to MQTT to get device events
                MQTTListener mQTTListener = new MQTTListener(isProd ? "mqtt" : "olympus-homelab.duckdns.org", mqttClient, commandRunner);
                await mQTTListener.Initialize();
                app.Urls.Add("http://hcs.olympus-homelab.duckdns.org:7160");
                app.Run();
            }
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
