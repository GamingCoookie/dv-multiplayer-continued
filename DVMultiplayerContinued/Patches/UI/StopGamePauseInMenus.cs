using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(CanvasSpawner), "Open", new[] { typeof(MenuScreen), typeof(bool) })]
    internal class StopGamePauseInMenus
    {
        private static void Prefix(MenuScreen screenToOpen, ref bool pauseGame)
        {
            if (NetworkManager.IsClient())
            {
                pauseGame = false;
                TutorialController.movementAllowed = false;
            }
        }
    }
}
