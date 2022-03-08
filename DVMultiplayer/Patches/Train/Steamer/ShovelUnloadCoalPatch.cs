using DVMultiplayer.Networking;
using HarmonyLib;
using UnityEngine;

namespace DVMultiplayer.Patches.Train.Steamer
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(ShovelNonPhysicalCoal), "UnloadCoal")]
    internal class ShovelUnloadCoalPatch
    {
        private static void Postfix(bool __result, ShovelNonPhysicalCoal __instance, GameObject target)
        {
            if(__result && NetworkManager.IsClient())
            {
                TrainCar car = TrainCar.Resolve(target);
                NetworkTrainSync trainSync = car.GetComponent<NetworkTrainSync>();
                LocoControllerSteam steamer = car.GetComponent<LocoControllerSteam>();

                trainSync.OnSteamerCoalShoveled(__instance.shovelChunksCapacity);
            }
        }
    }
}
