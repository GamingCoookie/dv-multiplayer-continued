using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(CanvasSpawner), "Close")]
    internal class EnableMovementOutsidePauseMenus
    {
        private static void Postfix()
        {
            if (!TutorialController.movementAllowed && NetworkManager.IsClient())
                TutorialController.movementAllowed = true;
        }
    }
}
