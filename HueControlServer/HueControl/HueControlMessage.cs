using System.Text.Json.Serialization;

namespace HueControlServer.HueControl
{
    public class HueControlMessage
    {
        public HueControlActionEnum? action { get; set; }
        public float? action_duration { get; set; }

        public override string ToString()
        {
            return $"action: {action}, action_duration: {action_duration}";
        }
    }
}
