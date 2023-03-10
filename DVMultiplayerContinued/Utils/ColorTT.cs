using UnityEngine;

namespace DVMultiplayer.Utils
{
    public static class ColorTT
    {
        public static uint Pack(this Color32 color)
        {
            return ((uint)color.r << 24) | ((uint)color.g << 16) | ((uint)color.b << 8) | color.a;
        }

        public static Color32 Unpack(uint packedColor)
        {
            byte r = (byte)(packedColor >> 24);
            byte g = (byte)(packedColor >> 16);
            byte b = (byte)(packedColor >> 8);
            byte a = (byte)packedColor;
            return new Color32(r, g, b, a);
        }
    }
}
