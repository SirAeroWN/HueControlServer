using HueApi.Models;
using HueApi;
using HueApi.Models.Requests;
using System.Diagnostics;

namespace HueControlServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            string? ip = builder.Configuration["LocalHueApi:ip"];
            string? key = builder.Configuration["LocalHueApi:key"];

            if (ip == null || key == null)
            {
                throw new ArgumentNullException("config values missing");
            }

            WebApplication app = builder.Build();

            // create the api object
            var localHueApi = new LocalHueApi(ip, key);

            // set command paths
            Dictionary<string, string> commands = new Dictionary<string, string>()
            {
                { "GoodNight", Path.Combine(builder.Environment.ContentRootPath, "../HueGoodNightCommand/bin/Debug/net7.0/HueGoodNightCommand.exe") }
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

            app.Urls.Add("https://*:7160");
            app.Run();
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