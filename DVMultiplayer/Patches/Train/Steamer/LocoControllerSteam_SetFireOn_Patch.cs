using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches.Train.Steamer
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(LocoControllerSteam), "SetFireOn")]
    internal class LocoControllerSteam_SetFireOn_Patch
    {
        private static void Prefix(LocoControllerSteam __instance, float percentage)
        {
            if (NetworkManager.IsClient())
            {
                NetworkTrainSync trainSync = __instance.GetComponent<NetworkTrainSync>();
                if(__instance.GetFireOn() != percentage)
                    trainSync.OnSteamerFireOnChanged(percentage);
            }
        }
    }
}
