using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Diagnostics;

namespace HueControlServer
{
    public class CommandRunner
    {
        private string _ip { get; }
        private string _key { get; }
        private Dictionary<string, string> _commands { get; }

        public CommandRunner(string ip, string key, Dictionary<string, string> commands)
        {
            this._ip = ip;
            this._key = key;
            this._commands = commands;
        }

        public void SetBedRoom(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = this._commands["SetBedroom"];
            process.StartInfo.Arguments = $"--bridge-ip {this._ip} --key {this._key} --command {command}";
            process.Start();
        }

        public IResult SetLivingRoom(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = this._commands["SetLivingRoom"];
            process.StartInfo.Arguments = $"--bridge-ip {this._ip} --key {this._key} --command {command}";
            process.Start();
            return Results.Ok("Toggle Living Room Started");
        }
    }
}
