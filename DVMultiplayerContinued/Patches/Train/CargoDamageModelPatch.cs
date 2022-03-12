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
    [HarmonyPatch(typeof(CargoDamageModel), "ApplyDamageToCargo")]
    internal class CargoDamageModelPatch
    {
        private static bool Prefix(CargoDamageModel __instance, TrainCar ___trainCar, float damage)
        {
            if (NetworkManager.IsClient())
            {
                NetworkTrainPosSync posSync = ___trainCar.GetComponent<NetworkTrainPosSync>();
                if (posSync && !posSync.IsCarDamageEnabled)
                {
                    return false;
                }
            }
            return true;
		}
    }
}
