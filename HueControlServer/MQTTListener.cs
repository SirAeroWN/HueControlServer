
using MQTTnet.Client;
using MQTTnet;
using System.Threading;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet.Server;

namespace HueControlServer
{
    public class MQTTListener
    {
        private string _server { get; }

        private IMqttClient _mqttClient { get; }

        private CommandRunner _commandRunner { get; }

        public MQTTListener(string server, IMqttClient mqttClient, CommandRunner commandRunner)
        {
            this._server = server;
            this._mqttClient = mqttClient;
            this._commandRunner = commandRunner;
        }

        public async Task Initialize()
        {
            // Subscribe to MQTT to get device events
            MqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(this._server, port: 1883).Build();

            // Setup message handling before connecting so that queued messages
            // are also handled properly. When there is no event handler attached all
            // received messages get lost.
            this._mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                Console.WriteLine("Received application message.");
                string topic = e.ApplicationMessage.Topic;
                string payload = System.Text.Encoding.Default.GetString(e.ApplicationMessage.PayloadSegment);
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

                return Task.CompletedTask;
            };

            await this._mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            MqttClientSubscribeOptions mqttSubscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
                .WithTopicFilter("zigbee2mqtt/Button")
                .WithTopicFilter("zigbee2mqtt/Hue Remote")
            .Build();

            await this._mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

            Console.WriteLine("MQTT client subscribed to topic.");
        }
    }
}
