using MQTTnet;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading.Channels;

namespace HueControlServer.HueSmartButton
{
    public class HueSmartButtonHandler : ChannelHandlerBase
    {
        private bool _held = false;

        public HueSmartButtonHandler(CommandRunner runner, ChannelReader<MqttApplicationMessage> channelReader, Action<CommandRunner, string> set) : base(runner, channelReader, set)
        {
        }

        protected override void HandleMessage(MqttApplicationMessage applicationMessage)
        {
            string payload = System.Text.Encoding.Default.GetString(applicationMessage.PayloadSegment);

            //Console.WriteLine(payload);
            HueSmartButtonMessage? message = JsonSerializer.Deserialize<HueSmartButtonMessage>(payload);
            if (message != null)
            {
                switch (message.action)
                {
                    case HueSmartButtonActionEnum.hold:
                        this._held = true;
                        this._set(this._commandRunner, "morning");
                        break;
                    case HueSmartButtonActionEnum.release:
                        if (!this._held)
                        {
                            this._set(this._commandRunner, "goodnight");
                        }

                        // reset held
                        this._held = false;
                        break;
                }
            }
            else
            {
                Console.WriteLine("Invalid hue message");
            }
        }
    }
}
