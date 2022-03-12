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
    [HarmonyPatch(typeof(CarDamageModel), "IgnoreDamage")]
    internal class CarDamageModelPatch
    {
        private static void Prefix(CarDamageModel __instance, bool set)
        {
            if (NetworkManager.IsClient())
            {
                NetworkTrainPosSync posSync = __instance.trainCar.GetComponent<NetworkTrainPosSync>();
                if (posSync)
                {
                    posSync.IsCarDamageEnabled = !set;
                }
            }
		}
    }
}
