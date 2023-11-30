using HueApi;
using HueApi.Models.Requests;
using HueApi.Models;

namespace HueToggleLivingRoom
{
    internal class Program
    {
        static async Task<int> Main(string bridgeIp, string key)
        {
            // create the api object
            var localHueApi = new LocalHueApi(bridgeIp, key);
            string room = "Living room";

            // find the grouplight for the bedroom
            GroupedLight? livingRoom = await FindGroupInRoom(localHueApi, room);
            if (livingRoom == null)
            {
                return 1;
            }

            bool isOn = livingRoom.On?.IsOn ?? false;
            if (isOn)
            {
                await TurnLightsOff(localHueApi, livingRoom);
            }
            else
            {
                await TurnLightsOn(localHueApi);
            }

            await Console.Out.WriteLineAsync("done");
            return 0;
        }

        private static async Task TurnLightsOff(LocalHueApi localHueApi, GroupedLight livingRoom)
        {
            UpdateGroupedLight req = new UpdateGroupedLight().TurnOff();
            HuePutResponse resp = await localHueApi.UpdateGroupedLightAsync(livingRoom.Id, req);
            await Console.Out.WriteLineAsync($"scene set {(resp.HasErrors ? "failed" : "succeeded")}");
        }

        private static async Task TurnLightsOn(LocalHueApi localHueApi)
        {
            HuePutResponse sceneResp = await localHueApi.RecallSceneAsync(Guid.Parse("09a74264-432b-40ec-880a-927660d013a0"));
            await Console.Out.WriteLineAsync($"scene set {(sceneResp.HasErrors ? "failed" : "succeeded")}");
        }

        private static async Task<GroupedLight?> FindGroupInRoom(LocalHueApi localHueApi, string roomName)
        {
            List<GroupedLight> groupedLights = (await localHueApi.GetGroupedLightsAsync()).Data;
            GroupedLight? livingRoom = null;
            foreach (GroupedLight groupedLight in groupedLights)
            {
                if (groupedLight.Owner != null)
                {
                    List<Room> room = (await localHueApi.GetRoomAsync(groupedLight.Owner.Rid)).Data;
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
