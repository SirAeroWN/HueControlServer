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

namespace HueBedroom
{
    public class Bedroom
    {
        private LocalHueApi localHueApi { get; }

        private IMqttClient mqttClient { get; }

        private static string BedroomName = "Bedroom";

        private static Guid readSceneGuid = Guid.Parse("e5740397-f180-4de2-9fef-a923c6323eab");

        private static Guid restSceneGuid = Guid.Parse("1783fb18-e820-408f-a5fa-8e41a9189584");

        private static Guid toBedSceneGuid = Guid.Parse("c7b1428b-bf4b-4dc0-b9bf-756a189a69b4");

        private static Guid winkWinkSceneGuid = Guid.Parse("36bcc559-8baa-41e1-8b89-46939ea5066c");

        private static Guid nightLightSceneGuid = Guid.Parse("f7445165-e440-4f76-8818-6ff3fa3ee12d");

        private static Guid morningSceneGuid = Guid.Parse("5dff25c6-252b-46a0-99fb-2368a15899c7");

        public Bedroom(LocalHueApi localHueApi, IMqttClient mqttClient)
        {
            this.localHueApi = localHueApi;
            this.mqttClient = mqttClient;
        }

        public async Task TurnLightsOff()
        {
            UpdateGroupedLight req = new UpdateGroupedLight().TurnOff();
            GroupedLight? bedroom = await this.FindGroupInRoom(BedroomName);
            if (bedroom == null)
            {
                return;
            }

            HuePutResponse resp = await this.localHueApi.UpdateGroupedLightAsync(bedroom.Id, req);
            await Console.Out.WriteLineAsync($"scene set {(resp.HasErrors ? "failed" : "succeeded")}");
        }

        public async Task TurnLightsOn()
        {
            await this.SetScene("Read");
        }

        public async Task SetScene(string scene)
        {
            Guid sceneGuid = scene switch
            {
                "Read" => readSceneGuid,
                "Rest" => restSceneGuid,
                "To Bed" => toBedSceneGuid,
                "winkwink" => winkWinkSceneGuid,
                "Night Light" => nightLightSceneGuid,
                "morning" => morningSceneGuid,
                _ => throw new NotImplementedException()
            };
            HuePutResponse sceneResp = await this.localHueApi.RecallSceneAsync(sceneGuid);
            await Console.Out.WriteLineAsync($"scene set {(sceneResp.HasErrors ? "failed" : "succeeded")}");
        }

        public async Task<MqttClientPublishResult> SetFanPlugState(bool on)
        {
            var offMessage = new MqttApplicationMessageBuilder()
                                .WithTopic("zigbee2mqtt/BedRoomFanPlug/set")
                                .WithPayload($"{{\"state\": \"{(on ? "ON" : "OFF")}\"}}")
                                .Build();
            var result = await this.mqttClient.PublishAsync(offMessage, CancellationToken.None);
            return result;
        }

        public async Task<bool> IsOn()
        {
            GroupedLight? bedroomGroup = await this.FindGroupInRoom(BedroomName);
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
