using DVMultiplayer.Networking;
using HarmonyLib;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(TurntableController), "GetPushingInput")]
    internal class StopTurntablePushWhenClient
    {
        private static bool Prefix(Transform handle)
        {
            if (NetworkManager.IsClient())
                return false;

            return true;
        }
    }
}
