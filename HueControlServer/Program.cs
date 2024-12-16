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
using HueControlServer.HueSmartButton;
using System.Text;

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
                // make a cancellation token
                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;

                List<(string topic, Func<CommandRunner, ChannelReader<MqttApplicationMessage>, Action<CommandRunner, string>, ChannelHandlerBase> create, Action<CommandRunner, string> action)> channelActions = new()
                {
                    ( "zigbee2mqtt/Bedroom", ChannelHandlerFactory.Create<HueRemoteHandler>, (commandRunner, command) => commandRunner.SetBedRoom(command) ),
                    ( "zigbee2mqtt/Office", ChannelHandlerFactory.Create<HueRemoteHandler>, (commandRunner, command) => commandRunner.SetOffice(command) ),
                    ( "zigbee2mqtt/LivingRoom", ChannelHandlerFactory.Create<HueRemoteHandler>, (commandRunner, command) => commandRunner.SetLivingRoom(command) ),
                    ( "zigbee2mqtt/BedRoomSmartButton", ChannelHandlerFactory.Create<HueSmartButtonHandler>, (commandRunner, command) => commandRunner.SetBedRoom(command) ),
                };

                (List<Task> tasks, Dictionary<string, ChannelWriter<MqttApplicationMessage>> commandWriters) = InitializeHandlers(commandRunner, channelActions, token);

                // start the mqtt client
                MQTTListener mQTTListener = new MQTTListener(isProd ? "mqtt" : "olympus-homelab.duckdns.org", mqttClient, commandWriters);
                await mQTTListener.Initialize();

                // start the app
                app.Urls.Add("http://hcs.olympus-homelab.duckdns.org:7160");
                app.Run();

                // stop the handler tasks
                source.Cancel();
                await Task.WhenAll(tasks);
            }
        }

        private static (List<Task> tasks, Dictionary<string, ChannelWriter<MqttApplicationMessage>> commandWriters) InitializeHandlers(CommandRunner commandRunner, Dictionary<string, Action<CommandRunner, string>> channelActions, CancellationToken token)
        {
            Dictionary<string, ChannelWriter<MqttApplicationMessage>> commandWriters = new();
            List<Task> tasks = new();
            foreach (KeyValuePair<string, Action<CommandRunner, string>> channelAction in channelActions)
            {
                Channel<MqttApplicationMessage> channel = Channel.CreateUnbounded<MqttApplicationMessage>();
                commandWriters.Add(channelAction.Key, channel.Writer);
                tasks.Add(new HueRemoteHandler(commandRunner, channel.Reader, channelAction.Value).Listen(token));
            }

            return (tasks, commandWriters);
        }

        private static (List<Task> tasks, Dictionary<string, ChannelWriter<MqttApplicationMessage>> commandWriters) InitializeHandlers(
            CommandRunner commandRunner,
            List<(string topic, Func<CommandRunner, ChannelReader<MqttApplicationMessage>, Action<CommandRunner, string>, ChannelHandlerBase> create, Action<CommandRunner, string> action)> channelActions,
            CancellationToken token
        )
        {
            Dictionary<string, ChannelWriter<MqttApplicationMessage>> commandWriters = new();
            List<Task> tasks = new();
            foreach (var channelAction in channelActions)
            {
                Channel<MqttApplicationMessage> channel = Channel.CreateUnbounded<MqttApplicationMessage>();
                commandWriters.Add(channelAction.topic, channel.Writer);
                tasks.Add(channelAction.create(commandRunner, channel.Reader, channelAction.action).Listen(token));
            }

            return (tasks, commandWriters);
        }
    }
}
