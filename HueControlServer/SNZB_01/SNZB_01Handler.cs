using MQTTnet;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HueControlServer.SNZB_01
{
    public class SNZB_01Handler : ChannelHandlerBase
    {
        public SNZB_01Handler(CommandRunner runner, ChannelReader<MqttApplicationMessage> channelReader) : base(runner, channelReader)
        {
        }

        protected override void HandleMessage(MqttApplicationMessage applicationMessage)
        {
            string payload = System.Text.Encoding.Default.GetString(applicationMessage.PayloadSegment);
            SNZB_01Message? message = JsonSerializer.Deserialize<SNZB_01Message>(payload);
            switch (message?.action)
            {
                case "":
                    break;
                case "single":
                    this._commandRunner.SetBedRoom("toggle");
                    break;
                case "double":
                    this._commandRunner.SetBedRoom("goodnight");
                    break;
                case "long":
                    this._commandRunner.SetBedRoom("winkwink");
                    break;
            }
        }
    }
}
