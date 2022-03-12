using DV;
using DV.Logic.Job;
using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(Bogie), "ForceSleep")]
    internal class FixBogiesForceSleepNRE
    {
        private static bool Prefix(Bogie __instance)
        {
            return __instance.rb;
		}
    }
}
