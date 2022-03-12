using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(TrainCar), "ReturnCarToPool")]
    internal class StopTrainsToPoolWhenClient
    {
        private static bool Prefix()
        {
            if (NetworkManager.IsClient())
                return false;

            return true;
        }
    }
}
