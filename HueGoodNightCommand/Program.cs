using HueApi;
using HueApi.Models.Requests;
using HueApi.Models;

namespace HueGoodNightCommand
{
    internal class Program
    {
        static async Task<int> Main(string bridgeIp, string key)
        {
            // create the api object
            var localHueApi = new LocalHueApi(bridgeIp, key);

            // find the grouplight for the bedroom
            List<GroupedLight> groupedLights = (await localHueApi.GetGroupedLightsAsync()).Data;
            GroupedLight? bedroom = null;
            foreach (GroupedLight groupedLight in groupedLights)
            {
                if (groupedLight.Owner != null)
                {
                    List<Room> room = (await localHueApi.GetRoomAsync(groupedLight.Owner.Rid)).Data;
                    if (room.Any() && (room.FirstOrDefault()?.Metadata?.Name.Equals("bedroom", StringComparison.OrdinalIgnoreCase) ?? false))
                    {
                        bedroom = groupedLight;
                        break;
                    }
                }
            }

            if (bedroom == null)
            {
                return 1;
            }

            int interval = 60;
            await Console.Out.WriteLineAsync($"waiting first {interval} seconds..");
            await Task.Delay(interval * 1000);

            HuePutResponse sceneResp = await localHueApi.RecallSceneAsync(Guid.Parse("1783fb18-e820-408f-a5fa-8e41a9189584"));
            await Console.Out.WriteLineAsync($"scene set {(sceneResp.HasErrors ? "failed" : "succeeded")}");

            await Console.Out.WriteLineAsync($"waiting next {interval} seconds..");
            await Task.Delay(interval * 1000);

            // update the light
            UpdateGroupedLight req = new UpdateGroupedLight().TurnOff();
            HuePutResponse resp = await localHueApi.UpdateGroupedLightAsync(bedroom.Id, req);
            if (resp.HasErrors)
            {
                await Console.Out.WriteLineAsync("Turning off lights failed");
                return 1;
            }

            await Console.Out.WriteLineAsync("done");
            return 0;
        }
    }
}
