using DVMultiplayer.Networking;
using HarmonyLib;
using UnityEngine;

namespace DVMultiplayer.Patches.Train.Steamer
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(LocoControllerSteam), "SetWhistle")]
    internal class LocoControllerSteam_SetWhistle_Patch
    {
        private static void Prefix(LocoControllerSteam __instance, float value)
        {
            float val = Mathf.Clamp01(value);
            if (NetworkManager.IsClient() && val != __instance.whistleRopeValue)
            {
                NetworkTrainSync trainSync = __instance.GetComponent<NetworkTrainSync>();
                trainSync.OnSteamerWhistleChanged(val);
            }
        }
    }
}
