using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(CanvasSpawner), "Update")]
    internal class StopClosingUIIfNotAllowed
    {
        private static bool Prefix()
        {
            return CustomUI.isAllowedToBeClosedByPlayer;
        }
    }
}
