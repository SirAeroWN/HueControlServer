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

        private static string livingRoomName = "Living Room";

        private static Guid readSceneGuid = Guid.Parse("027ee501-07bd-40a1-b47c-e13814a616f0");

        private static Guid winkWinkSceneGuid = Guid.Parse("56b4ef34-0d58-45ca-8d64-c835789022a8");

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
            HuePutResponse sceneResp = await this.localHueApi.RecallSceneAsync(readSceneGuid);
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
