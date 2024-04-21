using MQTTnet.Client;
using MQTTnet;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Channels;

namespace HueControlServer
{
    public class MQTTListener
    {
        private string _server { get; }

        private IMqttClient _mqttClient { get; }

        private Dictionary<string, ChannelWriter<MqttApplicationMessage>> _channelWriters { get; }

        public MQTTListener(string server, IMqttClient mqttClient, Dictionary<string, ChannelWriter<MqttApplicationMessage>> channelWriters)
        {
            this._server = server;
            this._mqttClient = mqttClient;
            this._channelWriters = channelWriters;
        }

        public async Task Initialize()
        {
            // Subscribe to MQTT to get device events
            MqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(this._server, port: 1883).Build();

            // Setup message handling before connecting so that queued messages
            // are also handled properly. When there is no event handler attached all
            // received messages get lost.
            this._mqttClient.ApplicationMessageReceivedAsync += this.HandleMessage;

            await this._mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            // subscribe to all topics that we have channels for
            MqttClientSubscribeOptionsBuilder subscriptionOptionsBuilder = new MqttFactory().CreateSubscribeOptionsBuilder();
            foreach (string topic in this._channelWriters.Keys)
            {
                subscriptionOptionsBuilder = subscriptionOptionsBuilder.WithTopicFilter(topic);
            }

            MqttClientSubscribeOptions mqttSubscribeOptions = subscriptionOptionsBuilder.Build();
            await this._mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

            Console.WriteLine($"MQTT client subscribed to {this._channelWriters.Count} topic{(this._channelWriters.Count == 1 ? "" : "s")}.");
        }

        private async Task HandleMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            Console.WriteLine("Received application message.");
            string topic = eventArgs.ApplicationMessage.Topic;
            await this._channelWriters[topic].WriteAsync(eventArgs.ApplicationMessage);
        }
    }
}
