using MQTTnet;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HueControlServer
{
    public abstract class ChannelHandlerBase
    {

        private ChannelReader<MqttApplicationMessage> _channelReader { get; }
        protected CommandRunner _commandRunner { get; }

        public ChannelHandlerBase(CommandRunner runner, ChannelReader<MqttApplicationMessage> channelReader)
        {
            this._commandRunner = runner;
            this._channelReader = channelReader;
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