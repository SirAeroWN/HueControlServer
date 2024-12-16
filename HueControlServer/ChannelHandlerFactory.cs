using HueControlServer.HueControl;
using HueControlServer.HueSmartButton;
using MQTTnet;
using System;
using System.Threading.Channels;

namespace HueControlServer
{
    public static class ChannelHandlerFactory
    {
        public static ChannelHandlerBase Create<THandler>(CommandRunner runner, ChannelReader<MqttApplicationMessage> channelReader, Action<CommandRunner, string> set) where THandler : ChannelHandlerBase
        {
            Type t = typeof(THandler);
            return true switch
            {
                var _ when t.IsAssignableFrom(typeof(HueSmartButtonHandler)) => CreateHueSmartButtonHandler(runner, channelReader, set),
                var _ when t.IsAssignableFrom(typeof(HueRemoteHandler)) => CreateHueRemoteHandler(runner, channelReader, set),
                _ => throw new NotImplementedException("Unknown handler type"),
            };
        }

        private static HueSmartButtonHandler CreateHueSmartButtonHandler(CommandRunner runner, ChannelReader<MqttApplicationMessage> channelReader, Action<CommandRunner, string> set) => new HueSmartButtonHandler(runner, channelReader, set);

        private static HueRemoteHandler CreateHueRemoteHandler(CommandRunner runner, ChannelReader<MqttApplicationMessage> channelReader, Action<CommandRunner, string> set) => new HueRemoteHandler(runner, channelReader, set);
    }
}
