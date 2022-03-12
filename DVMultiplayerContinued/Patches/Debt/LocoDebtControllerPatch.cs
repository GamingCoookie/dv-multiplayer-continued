using HarmonyLib;
using DV.ServicePenalty;
using DVMultiplayer.Networking;

namespace DVMultiplayer.Patches.Debt
{
    #pragma warning disable IDE0060 // Remove unused parameter
    #pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(LocoDebtController), "PayExistingLocoDebt")]
    internal class LocoDebtControllerExistingPatch
    {
        private static void Postfix(ExistingLocoDebt locoDebtToPay)
        {
            if(NetworkManager.IsClient() && SingletonBehaviour<NetworkTrainManager>.Exists && SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced && SingletonBehaviour<NetworkDebtManager>.Exists && !SingletonBehaviour<NetworkDebtManager>.Instance.IsChangeByNetwork)
            {
                SingletonBehaviour<NetworkDebtManager>.Instance.OnLocoDeptPaid(locoDebtToPay.ID, false);
            }
        }
    }

    [HarmonyPatch(typeof(LocoDebtController), "PayStagedLocoDebt")]
    internal class LocoDebtControllerDestroyedPatch
    {
        private static void Postfix(StagedLocoDebt locoDebtToPay)
        {
            if (NetworkManager.IsClient() && SingletonBehaviour<NetworkTrainManager>.Exists && SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced && SingletonBehaviour<NetworkDebtManager>.Exists && !SingletonBehaviour<NetworkDebtManager>.Instance.IsChangeByNetwork)
            {
                SingletonBehaviour<NetworkDebtManager>.Instance.OnLocoDeptPaid(locoDebtToPay.ID, true);
            }
        }
    }
}
