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
    [HarmonyPatch(typeof(DrivingForce), "UpdateWheelslip")]
    internal class DrivingForcePatch
    {
        private static bool Prefix(float inputForce, Bogie bogie, float maxTractionForcePossible)
        {
            return bogie && bogie.Car && bogie.rb;
		}
    }
}
