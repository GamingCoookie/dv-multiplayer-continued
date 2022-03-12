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
    [HarmonyPatch(typeof(CommsRadioCarDeleter), "OnUse")]
    internal class DisallowDeleteIfPlayerInTrain
    {
        private static bool Prefix(CommsRadioCarDeleter __instance, TrainCar ___carToDelete)
        {
            if (NetworkManager.IsClient())
            {
                FieldInfo state = __instance.GetType().GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);

                if ((int)state.GetValue(__instance)== 1 && SingletonBehaviour<NetworkPlayerManager>.Instance && SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayersInTrain(___carToDelete).Length > 0)
                {
                    return false;
                }
            }
            return true;
		}
    }
}
