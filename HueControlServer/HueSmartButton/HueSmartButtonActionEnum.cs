using HueControlServer.HueControl;
using System.Text.Json.Serialization;

namespace HueControlServer.HueSmartButton
{
    [JsonConverter(typeof(JsonStringEnumConverter<HueSmartButtonActionEnum>))]
    public enum HueSmartButtonActionEnum
    {
        on,
        off,
        skip_backward,
        skip_forward,
        press,
        hold,
        release,
        brightness_step_down,
        brightness_step_up
    }
}
