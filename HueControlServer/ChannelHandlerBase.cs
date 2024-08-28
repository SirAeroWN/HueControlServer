using MQTTnet;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HueControlServer
{
    public abstract class ChannelHandlerBase
    {

        private ChannelReader<MqttApplicationMessage> _channelReader { get; }
        protected CommandRunner _commandRunner { get; }
        protected Action<CommandRunner, string> _set { get; }

        public ChannelHandlerBase(CommandRunner runner, ChannelReader<MqttApplicationMessage> channelReader, Action<CommandRunner, string> set)
        {
            this._commandRunner = runner;
            this._channelReader = channelReader;
            this._set = set;
        }

        public async Task Listen(CancellationToken cancellationToken)
        {
            await foreach (MqttApplicationMessage message in this._channelReader.ReadAllAsync(cancellationToken))
            {
                this.HandleMessage(message);
            }
        }

        protected abstract void HandleMessage(MqttApplicationMessage applicationMessage);
    }
}