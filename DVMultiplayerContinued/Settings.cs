using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityModManagerNet;

namespace DVMultiplayerContinued
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public string ColorString = "#FFFFFF";
        [Header("Each color value from 0 to 255.")]
        [Draw("Player Color")]
        public Color32 Color = new Color32(255, 255, 255, 255);

        public void OnChange()
        {
            ColorString = ColorUtility.ToHtmlStringRGB(Color);
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
