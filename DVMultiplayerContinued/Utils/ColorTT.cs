using UnityEngine;

namespace DVMultiplayer.Utils
{
    public static class ColorTT
    {
        public static ushort Pack(this Color32 color)
        {
            ushort packed = 0;
            packed |= (ushort)(color.r << 8);
            packed |= color.g;
            packed |= (ushort)(color.b >> 8);
            return packed;
        }

        public static Color32 Unpack(ushort packedColor)
        {
            byte r = (byte)(packedColor >> 8);
            byte g = (byte)(packedColor & 0xFF);
            byte b = (byte)((packedColor << 8) >> 8);
            return new Color32(r, g, b, 255);
        }
    }
}
