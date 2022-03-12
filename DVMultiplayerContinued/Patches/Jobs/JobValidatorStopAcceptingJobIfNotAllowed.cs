using DV.Printers;
using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches
{
    #pragma warning disable IDE0060 // Remove unused parameter
    #pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(JobValidator), "ProcessJobOverview")]
    internal class JobValidatorStopAcceptingJobIfNotAllowed
    {
        private static bool Prefix(JobOverview jobOverview, PrinterController ___bookletPrinter)
        {
            if (NetworkManager.IsClient())
            {
                if(jobOverview.job.State == DV.Logic.Job.JobState.Available && !SingletonBehaviour<NetworkJobsManager>.Instance.IsAllowedToTakeJob(jobOverview.job.ID))
                {
                    ___bookletPrinter.PlayErrorSound();
                    jobOverview.DestroyJobOverview();
                    return false;
                }
            }
            return true;
        }
    }
}
