using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HueApi;
using HueApi.Models.Requests;
using HueApi.Models;
using MQTTnet.Client;
using MQTTnet;

namespace HueOffice
{
    public class Office
    {
        private LocalHueApi localHueApi { get; }

        private IMqttClient mqttClient { get; }

        private static string officeName = "Office";

        private static Guid brightSceneGuid = Guid.Parse("78b34277-e66b-4dee-9e4f-9d3331314a13");

        private static Guid coolBrightSceneGuid = Guid.Parse("09a74264-432b-40ec-880a-927660d013a0");

        private static Guid winkWinkSceneGuid = Guid.Parse("8a82eb62-5a98-48b6-886d-22fa1ebbaa7e");

        private static Guid restSceneGuid = Guid.Parse("de249c6e-306c-4b3d-a0eb-cc9b12c3f6a6");

        public Office(LocalHueApi localHueApi, IMqttClient mqttClient)
        {
            this.localHueApi = localHueApi;
            this.mqttClient = mqttClient;
        }

        public async Task TurnLightsOff()
        {
            UpdateGroupedLight req = new UpdateGroupedLight().TurnOff();
            GroupedLight? bedroom = await this.FindGroupInRoom(officeName);
            if (bedroom == null)
            {
                return;
            }

            HuePutResponse resp = await this.localHueApi.UpdateGroupedLightAsync(bedroom.Id, req);
            bool success = resp.HasErrors == false;
            if (success)
            {
                MqttClientPublishResult result = await SetPlugState(on: false);
                success &= result.IsSuccess;
            }

            await Console.Out.WriteLineAsync($"scene set {(success ? "failed" : "succeeded")}");
        }

        public async Task TurnLightsOn()
        {
            bool success = await this.SetScene("Bright");
            if (success)
            {
                MqttClientPublishResult result = await SetPlugState(on: true);
                success &= result.IsSuccess;
            }
        }

        private async Task<MqttClientPublishResult> SetPlugState(bool on)
        {
            var offMessage = new MqttApplicationMessageBuilder()
                                .WithTopic("zigbee2mqtt/OfficeLampPlug/set")
                                .WithPayload($"{{\"state\": \"{(on ? "ON" : "OFF")}\"}}")
                                .Build();
            var result = await this.mqttClient.PublishAsync(offMessage, CancellationToken.None);
            return result;
        }

        public async Task<bool> SetScene(string scene)
        {
            Guid sceneGuid = scene switch
            {
                "Bright" => brightSceneGuid,
                "Rest" => restSceneGuid,
                "winkwink" => winkWinkSceneGuid,
                _ => throw new NotImplementedException()
            };
            HuePutResponse sceneResp = await this.localHueApi.RecallSceneAsync(sceneGuid);

            bool success = sceneResp.HasErrors == false;
            await Console.Out.WriteLineAsync($"setting scene {scene} {(success ? "failed" : "succeeded")}");
            return success;
        }

        public async Task<bool> IsOn()
        {
            GroupedLight? bedroomGroup = await this.FindGroupInRoom(officeName);
            ArgumentNullException.ThrowIfNull(nameof(bedroomGroup));
            return bedroomGroup!.On?.IsOn ?? false;
        }

        private async Task<GroupedLight?> FindGroupInRoom(string roomName)
        {
            List<GroupedLight> groupedLights = (await this.localHueApi.GetGroupedLightsAsync()).Data;
            GroupedLight? bedroom = null;
            foreach (GroupedLight groupedLight in groupedLights)
            {
                if (groupedLight.Owner != null)
                {
                    List<Room> room = (await this.localHueApi.GetRoomAsync(groupedLight.Owner.Rid)).Data;
                    if (room.Any() && (room.FirstOrDefault()?.Metadata?.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase) ?? false))
                    {
                        bedroom = groupedLight;
                        break;
                    }
                }
            }

            return bedroom;
        }
    }
}