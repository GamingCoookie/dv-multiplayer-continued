using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches.Jobs
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(StaticJobDefinition), "TryToGenerateJob")]
    internal class StaticJobDefinitionPatch_DisableIfClientAndNoJobId
    {
        private static bool Prefix(string ___forcedJobId)
        {
            if(NetworkManager.IsClient() && !NetworkManager.IsHost())
            {
                return !string.IsNullOrEmpty(___forcedJobId);
            }
            return true;
        }
    }
}
