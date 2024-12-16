using System;

namespace HueControlServer.HueSmartButton
{
    public class HueSmartButtonMessage
    {
        public HueSmartButtonActionEnum? action { get; set; }

        public override string ToString()
        {
            return $"action: {action}";
        }
    }
}
