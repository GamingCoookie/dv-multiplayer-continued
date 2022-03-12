using HarmonyLib;
using DV.ServicePenalty;
using DVMultiplayer.Networking;

namespace DVMultiplayer.Patches.Debt
{
    #pragma warning disable IDE0060 // Remove unused parameter
    #pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(JobDebtController), "PayExistingJobDebt")]
    internal class JobDebtControllerExistingPatch
    {
        private static void Postfix(ExistingJobDebt jobDebt)
        {
            if(NetworkManager.IsClient() && SingletonBehaviour<NetworkTrainManager>.Exists && SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced && SingletonBehaviour<NetworkDebtManager>.Exists && !SingletonBehaviour<NetworkDebtManager>.Instance.IsChangeByNetwork)
            {
                SingletonBehaviour<NetworkDebtManager>.Instance.OnJobDeptPaid(jobDebt.ID, false);
            }
        }
    }

    [HarmonyPatch(typeof(JobDebtController), "PayStagedJobDebt")]
    internal class JobDebtControllerDestroyedPatch
    {
        private static void Postfix(StagedJobDebt jobDebt)
        {
            if (NetworkManager.IsClient() && SingletonBehaviour<NetworkTrainManager>.Exists && SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced && SingletonBehaviour<NetworkDebtManager>.Exists && !SingletonBehaviour<NetworkDebtManager>.Instance.IsChangeByNetwork)
            {
                SingletonBehaviour<NetworkDebtManager>.Instance.OnJobDeptPaid(jobDebt.ID, true);
            }
        }
    }

    [HarmonyPatch(typeof(JobDebtController), "PayExistingJoblessCarsDebt")]
    internal class JobDebtControllerOtherExistingPatch
    {
        private static void Postfix(JobDebtController __instance)
        {
            if (NetworkManager.IsClient() && SingletonBehaviour<NetworkTrainManager>.Exists && SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced && SingletonBehaviour<NetworkDebtManager>.Exists && !SingletonBehaviour<NetworkDebtManager>.Instance.IsChangeByNetwork)
            {
                SingletonBehaviour<NetworkDebtManager>.Instance.OnOtherDeptPaid(__instance.existingJoblessCarDebts.ID, false);
            }
        }
    }

    [HarmonyPatch(typeof(JobDebtController), "PayStagedJoblessCarsDebt")]
    internal class JobDebtControllerOtherDestroyedPatch
    {
        private static void Postfix(JobDebtController __instance)
        {
            if (NetworkManager.IsClient() && SingletonBehaviour<NetworkTrainManager>.Exists && SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced && SingletonBehaviour<NetworkDebtManager>.Exists && !SingletonBehaviour<NetworkDebtManager>.Instance.IsChangeByNetwork)
            {
                SingletonBehaviour<NetworkDebtManager>.Instance.OnOtherDeptPaid(__instance.deletedJoblessCarDebts.ID, true);
            }
        }
    }
}
