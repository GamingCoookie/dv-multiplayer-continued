using DV;
using DV.Logic.Job;
using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(LocoControllerBase), "GetSpeedKmH")]
    internal class ChangeSpeedCalculationWhenPhysicsAreOff
	{
        private static void Postfix(LocoControllerBase __instance, ref float __result)
        {
            if (NetworkManager.IsClient() && __instance.train.rb.isKinematic)
            {
                NetworkTrainPosSync networking = __instance.GetComponent<NetworkTrainPosSync>();
                if (networking)
                {
                    __result = networking.velocity.magnitude * 3.6f;
                }
			}
		}
    }
}
