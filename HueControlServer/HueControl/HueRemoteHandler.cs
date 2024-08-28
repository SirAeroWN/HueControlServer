using MQTTnet;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HueControlServer.HueControl
{
    /// <summary>
    /// Handles the Hue Remote 929002398602
    /// </summary>
    public class HueRemoteHandler : ChannelHandlerBase
    {
        private Dictionary<HueControlActionEnum, float> _holdDuration { get; }

        public HueRemoteHandler(CommandRunner runner, ChannelReader<MqttApplicationMessage> channelReader, Action<CommandRunner, string> set) : base(runner, channelReader, set)
        {
            this._holdDuration = new Dictionary<HueControlActionEnum, float>()
            {
                { HueControlActionEnum.on_hold, 0 },
                { HueControlActionEnum.off_hold, 0 },
                { HueControlActionEnum.up_hold, 0 },
                { HueControlActionEnum.down_hold, 0 },
            };
        }

        protected override void HandleMessage(MqttApplicationMessage applicationMessage)
        {
            string payload = System.Text.Encoding.Default.GetString(applicationMessage.PayloadSegment);

            //Console.WriteLine(payload);
            HueControlMessage? message = JsonSerializer.Deserialize<HueControlMessage>(payload);
            if (message != null)
            {
                switch (message.action)
                {
                    #region on button
                    case HueControlActionEnum.on_press:
                        break;
                    case HueControlActionEnum.on_hold:
                        this._holdDuration[HueControlActionEnum.on_hold] = message.action_duration ?? 0;
                        break;
                    case HueControlActionEnum.on_press_release:
                        this._set(this._commandRunner, "toggle");
                        break;
                    case HueControlActionEnum.on_hold_release:
                        if (this._holdDuration[HueControlActionEnum.on_hold] > 1)
                        {
                            this._set(this._commandRunner, "on");
                            this._holdDuration[HueControlActionEnum.on_hold] = 0;
                        }
                        else
                        {
                            this._set(this._commandRunner, "toggle");
                        }
                        break;
                    #endregion
                    #region up button
                    case HueControlActionEnum.up_press:
                        break;
                    case HueControlActionEnum.up_hold:
                        this._holdDuration[HueControlActionEnum.up_hold] = message.action_duration ?? 0;
                        break;
                    case HueControlActionEnum.up_press_release:
                    case HueControlActionEnum.up_hold_release:
                        this._set(this._commandRunner, "winkwink");
                        this._holdDuration[HueControlActionEnum.up_hold] = 0;
                        break;
                    #endregion
                    #region down button
                    case HueControlActionEnum.down_press:
                        break;
                    case HueControlActionEnum.down_hold:
                        this._holdDuration[HueControlActionEnum.down_hold] = message.action_duration ?? 0;
                        break;
                    case HueControlActionEnum.down_press_release:
                    case HueControlActionEnum.down_hold_release:
                        this._set(this._commandRunner, "goodnight");
                        this._holdDuration[HueControlActionEnum.down_hold] = 0;
                        break;
                    #endregion
                    #region off button
                    case HueControlActionEnum.off_press:
                        break;
                    case HueControlActionEnum.off_hold:
                        this._holdDuration[HueControlActionEnum.off_hold] = message.action_duration ?? 0;
                        break;
                    case HueControlActionEnum.off_press_release:
                    case HueControlActionEnum.off_hold_release:
                        this._holdDuration[HueControlActionEnum.off_hold] = 0;
                        break;
                    #endregion
                    // no idea what recall is
                    case HueControlActionEnum.recall_0:
                        Console.WriteLine("recall_0");
                        break;
                    case HueControlActionEnum.recall_1:
                        Console.WriteLine("recall_1");
                        break;
                    case null:
                        Console.WriteLine(payload);
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
