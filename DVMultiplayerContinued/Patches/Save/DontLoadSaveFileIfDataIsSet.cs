using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches
{
    #pragma warning disable IDE0060 // Remove unused parameter
    #pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(SaveGameManager), "Load")]
    internal class DontLoadSaveFileIfDataIsSet
    {
        private static bool Prefix(bool loadBackup = false)
        {
            if (SaveGameManager.data != null && NetworkManager.IsClient())
                return false;

            return true;
        }
    }
}
