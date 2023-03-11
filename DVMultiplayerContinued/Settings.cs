using System.Globalization;
using UnityEngine;
using UnityModManagerNet;

namespace DVMultiplayerContinued
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public Color32 Color = new Color32(255, 255, 255, 255);
        public string DefaultUsername = "";

        public void Draw(UnityModManager.ModEntry modEntry)
        {
            // Player color
            GUILayout.Label("Each color value from 0 to 255");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Player Color");
            byte[] values = { Color.r, Color.g, Color.b };
            string[] labels = { "Red", "Green", "Blue" };
            if (DrawMutiByte(ref values, labels))
                Color = new Color32(values[0], values[1], values[2], 255);
            GUILayout.EndHorizontal();

            // Default Username
            GUILayout.BeginHorizontal();
            GUILayout.Label("Default Username");
            DefaultUsername = GUILayout.TextField(DefaultUsername);
            GUILayout.EndHorizontal();
        }

        public void OnChange()
        { }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        private static bool DrawMutiByte(ref byte[] values, string[] labels)
        {
            bool modified = false;
            byte[] arr = new byte[values.Length];
            for (int index = 0; index < values.Length; ++index)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(labels[index], GUILayout.ExpandWidth(false));
                string s = GUILayout.TextField(values[index].ToString(), GUI.skin.textField);
                GUILayout.EndHorizontal();
                arr[index] = byte.TryParse(s, NumberStyles.Any, NumberFormatInfo.CurrentInfo, out byte result) ? result : (byte)0;
                if (arr[index] != values[index])
                    modified = true;
            }

            values = arr;
            return modified;
        }
    }
}
