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
using HueControlServer.SNZB_01;
using System.Threading.Channels;
using HueControlServer.HueControl;
using System.CommandLine;

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
                { "GoodNight", Path.Combine(builder.Environment.ContentRootPath, isProd ? "HueGoodNightCommand" : "../HueGoodNightCommand/bin/Debug/net8.0/HueGoodNightCommand.exe") }
                ,{ "SetLivingRoom", Path.Combine(builder.Environment.ContentRootPath, isProd ? "HueSetLivingRoom" : "../HueSetLivingRoom/bin/Debug/net8.0/HueSetLivingRoom.exe") }
                ,{ "SetBedroom", Path.Combine(builder.Environment.ContentRootPath, isProd ? "HueSetBedroom" : "../HueSetBedroom/bin/Debug/net8.0/HueSetBedroom.exe") }
                ,{ "SetOffice", Path.Combine(builder.Environment.ContentRootPath, isProd ? "HueSetOffice" : "../HueSetOffice/bin/Debug/net8.0/HueSetOffice.exe") }
            };

            CommandRunner commandRunner = new CommandRunner(ip, key, commands);

            app.MapGet("/", () => "Hello World!");

            app.MapGet("/gn", () =>
            {
                Process process = new Process();
                process.StartInfo.FileName = commands["GoodNight"];
                process.StartInfo.Arguments = $"--bridge-ip {ip} --key {key}";
                process.Start();
                return Results.Ok("Good Night Started");
            });

            app.MapGet("/cgn", () =>
            {
                Process.GetProcessesByName("HueGoodNightCommand").FirstOrDefault()?.Kill();
                commandRunner.SetBedRoom("on");
                return Results.Ok("Good Night Stopped, probably");
            });

            app.MapGet("/lr/t", () =>
            {
                return commandRunner.SetLivingRoom("toggle");
            });

            app.MapGet("/lr/on", () =>
            {
                return commandRunner.SetLivingRoom("on");
            });

            app.MapGet("/lr/off", () =>
            {
                return commandRunner.SetLivingRoom("off");
            });

            app.MapGet("/office/toggle", () =>
            {
                return commandRunner.SetOffice("toggle");
            });

            app.MapGet("/house/off", () =>
            {
                commandRunner.SetLivingRoom("off");
                commandRunner.SetBedRoom("off");
                return commandRunner.SetOffice("off");

            });

            using (IMqttClient mqttClient = new MqttFactory().CreateMqttClient())
            {
                // set up the channels for the different topic handlers
                Channel<MqttApplicationMessage> SNZB_01Channel = Channel.CreateUnbounded<MqttApplicationMessage>();
                Channel<MqttApplicationMessage> BedroomChannel = Channel.CreateUnbounded<MqttApplicationMessage>();
                Channel<MqttApplicationMessage> OfficeChannel = Channel.CreateUnbounded<MqttApplicationMessage>();
                Channel<MqttApplicationMessage> LivingRoomChannel = Channel.CreateUnbounded<MqttApplicationMessage>();
                // associate channels with topics
                Dictionary<string, ChannelWriter<MqttApplicationMessage>> commandWriters = new Dictionary<string, ChannelWriter<MqttApplicationMessage>>()
                {
                    { "zigbee2mqtt/Button", SNZB_01Channel.Writer },
                    { "zigbee2mqtt/Bedroom", BedroomChannel.Writer },
                    { "zigbee2mqtt/Office", OfficeChannel.Writer },
                    { "zigbee2mqtt/LivingRoom", LivingRoomChannel.Writer },
                };

                // create the channel handlers
                //SNZB_01Handler SNZB_01Handler = new SNZB_01Handler(commandRunner, SNZB_01Channel.Reader);
                HueRemoteHandler BedroomHandler = new HueRemoteHandler(commandRunner, BedroomChannel.Reader, (commandRunner, command) => commandRunner.SetBedRoom(command));
                HueRemoteHandler OfficeHandler = new HueRemoteHandler(commandRunner, OfficeChannel.Reader, (commandRunner, command) => commandRunner.SetOffice(command));
                HueRemoteHandler LivingRoomHandler = new HueRemoteHandler(commandRunner, LivingRoomChannel.Reader, (commandRunner, command) => commandRunner.SetLivingRoom(command));

                // make a cancellation token
                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;

                // start the handler tasks
                //Task SNZB_01Task = SNZB_01Handler.Listen(token);
                Task bedroomTask = BedroomHandler.Listen(token);
                Task officeTask = OfficeHandler.Listen(token);
                Task livingRoomTask = LivingRoomHandler.Listen(token);

                // start the mqtt client
                MQTTListener mQTTListener = new MQTTListener(isProd ? "mqtt" : "olympus-homelab.duckdns.org", mqttClient, commandWriters);
                await mQTTListener.Initialize();

                // start the app
                app.Urls.Add("http://hcs.olympus-homelab.duckdns.org:7160");
                app.Run();

                // stop the handler tasks
                source.Cancel();
                await bedroomTask;
                await officeTask;
                await livingRoomTask;
            }
        }
    }
}
