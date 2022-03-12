using DV;
using DV.Logic.Job;
using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using DV.PointSet;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(Bogie), "FixedUpdate")]
    internal class AllowUpdateWhenBogieKinematic
	{
        private static void Postfix(Bogie __instance)
        {
            if (NetworkManager.IsClient() && __instance.rb.isKinematic && !__instance.HasDerailed)
            {
                MethodInfo dynMethod = __instance.GetType().GetMethod("UpdatePointSetTraveller", BindingFlags.NonPublic | BindingFlags.Instance);
                dynMethod.Invoke(__instance, null);
                bool useMicrobumps = Traverse.Create(__instance).Field<bool>("useMicrobumps").Value;
                if (useMicrobumps)
                {
                    SimManager simManager = Traverse.Create(__instance).Field<SimManager>("simManager").Value;
                    __instance.transform.position = (Vector3)__instance.traveller.worldPosition + __instance.GetMicrobumpOffset() * simManager.microbumpScale + WorldMover.currentMove;
                }
                else
                {
                    __instance.transform.position = (Vector3)__instance.traveller.worldPosition + WorldMover.currentMove;
                }
            }
		}
    }
}
