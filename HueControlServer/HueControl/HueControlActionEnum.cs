using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace HueControlServer.HueControl
{
    [JsonConverter(typeof(JsonStringEnumConverter<HueControlActionEnum>))]
    public enum HueControlActionEnum
    {
        on_press,
        on_hold,
        on_press_release,
        on_hold_release,
        off_press,
        off_hold,
        off_press_release,
        off_hold_release,
        up_press,
        up_hold,
        up_press_release,
        up_hold_release,
        down_press,
        down_hold,
        down_press_release,
        down_hold_release,
        recall_0,
        recall_1
    }
}
