using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HueApi;
using HueApi.Models.Requests;
using HueApi.Models;

namespace HueLivingRoom
{
    public class LivingRoom
    {
        private LocalHueApi localHueApi { get; }

        private static string livingRoomName = "Living room";

        private static Guid brightSceneGuid = Guid.Parse("78b34277-e66b-4dee-9e4f-9d3331314a13");

        public LivingRoom(LocalHueApi localHueApi)
        {
            this.localHueApi = localHueApi;
        }

        public async Task TurnLightsOff()
        {
            UpdateGroupedLight req = new UpdateGroupedLight().TurnOff();
            GroupedLight? livingRoom = await this.FindGroupInRoom(livingRoomName);
            if (livingRoom == null)
            {
                return;
            }

            HuePutResponse resp = await this.localHueApi.UpdateGroupedLightAsync(livingRoom.Id, req);
            await Console.Out.WriteLineAsync($"scene set {(resp.HasErrors ? "failed" : "succeeded")}");
        }

        public async Task TurnLightsOn()
        {
            HuePutResponse sceneResp = await this.localHueApi.RecallSceneAsync(brightSceneGuid);
            await Console.Out.WriteLineAsync($"scene set {(sceneResp.HasErrors ? "failed" : "succeeded")}");
        }

        public async Task<bool> IsOn()
        {
            GroupedLight? livingRoomGroup = await this.FindGroupInRoom(livingRoomName);
            ArgumentNullException.ThrowIfNull(nameof(livingRoomGroup));
            return livingRoomGroup!.On?.IsOn ?? false;
        }

        private async Task<GroupedLight?> FindGroupInRoom(string roomName)
        {
            List<GroupedLight> groupedLights = (await this.localHueApi.GetGroupedLightsAsync()).Data;
            GroupedLight? livingRoom = null;
            foreach (GroupedLight groupedLight in groupedLights)
            {
                if (groupedLight.Owner != null)
                {
                    List<Room> room = (await this.localHueApi.GetRoomAsync(groupedLight.Owner.Rid)).Data;
                    if (room.Any() && (room.FirstOrDefault()?.Metadata?.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase) ?? false))
                    {
                        livingRoom = groupedLight;
                        break;
                    }
                }
            }

            return livingRoom;
        }
    }
}
