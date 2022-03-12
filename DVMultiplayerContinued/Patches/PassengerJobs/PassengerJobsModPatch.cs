using DV.Logic.Job;
using DVMultiplayer.Networking;
using HarmonyLib;
using PassengerJobsMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;

namespace DVMultiplayer.Patches.PassengerJobs
{
    public static class PassengerJobsModInitializer
    {
        public static void Initialize(UnityModManager.ModEntry passengerJobsModEntry, Harmony harmony)
        {
            Main.Log("Patching passenger jobs...");
            try
            {
                // Passenger Jobs spawning
                Type passengerJobsGen = passengerJobsModEntry.Assembly.GetType("PassengerJobsMod.PassengerJobGenerator", true);

                // Patch StartGenerationAsync method
                Main.Log("Patching PassengerJobsMod.PassengerJobGenerator.StartGenerationAsync");
                MethodInfo StartGenerationAsync = AccessTools.Method(passengerJobsGen, "StartGenerationAsync");
                MethodInfo StartGenerationAsyncPrefix = AccessTools.Method(typeof(PassengerJobs_StartGenerationAsync_Patch), "Prefix");
                harmony.Patch(StartGenerationAsync, prefix: new HarmonyMethod(StartGenerationAsyncPrefix));

                // Patch GenerateNewTransportJob method
                Main.Log("Patching PassengerJobsMod.PassengerJobGenerator.GenerateNewTransportJob");
                MethodInfo GenerateNewTransportJob = AccessTools.Method(passengerJobsGen, "GenerateNewTransportJob");
                MethodInfo GenerateNewTransportJobPostfix = AccessTools.Method(typeof(PassengerJobs_GenerateNewTransportJob_Patch), "Postfix");
                harmony.Patch(GenerateNewTransportJob, postfix: new HarmonyMethod(GenerateNewTransportJobPostfix));

                // Patch GenerateNewCommuterRun method
                Main.Log("Patching PassengerJobsMod.PassengerJobGenerator.GenerateNewCommuterRun");
                MethodInfo GenerateNewCommuterRun = AccessTools.Method(passengerJobsGen, "GenerateNewCommuterRun");
                MethodInfo GenerateNewCommuterRunPostfix = AccessTools.Method(typeof(PassengerJobs_GenerateNewCommuterRun_Patch), "Postfix");
                harmony.Patch(GenerateNewCommuterRun, postfix: new HarmonyMethod(GenerateNewCommuterRunPostfix));

                // Patch GenerateCommuterReturnTrip method
                Main.Log("Patching PassengerJobsMod.PassengerJobGenerator.GenerateCommuterReturnTrip");
                MethodInfo GenerateCommuterReturnTrip = AccessTools.Method(passengerJobsGen, "GenerateCommuterReturnTrip");
                MethodInfo GenerateCommuterReturnTripPostfix = AccessTools.Method(typeof(PassengerJobs_GenerateCommuterReturnTrip_Patch), "Postfix");
                harmony.Patch(GenerateCommuterReturnTrip, postfix: new HarmonyMethod(GenerateCommuterReturnTripPostfix));

                // Patch GetJobTypeFromDefinition method
                Main.Log("Patching DVMultiplayer.NetworkJobsManager.GetJobTypeFromDefinition");
                MethodInfo GetJobTypeFromDefinition = AccessTools.Method(typeof(NetworkJobsManager), "GetJobTypeFromDefinition");
                MethodInfo GetJobTypeFromDefinitionPostfix = AccessTools.Method(typeof(PassengerJobs_GetJobTypeFromDefinition_Patch), "Postfix");
                harmony.Patch(GetJobTypeFromDefinition, postfix: new HarmonyMethod(GetJobTypeFromDefinitionPostfix));
            }
            catch(Exception ex)
            {
                Main.Log($"Patching passenger jobs failed. Error: {ex.Message}");
            }
        }
    }

    class PassengerJobs_StartGenerationAsync_Patch
    {
        static bool Prefix()
        {
            return !NetworkManager.IsClient() || NetworkManager.IsHost();
        }
    }

    class PassengerJobs_GenerateNewTransportJob_Patch
    {
        static void Postfix(PassengerTransportChainController __result, PassengerJobGenerator __instance, TrainCarsPerLogicTrack consistInfo = null)
        {
            if (NetworkManager.IsHost())
            {
                if (__instance.Controller && __instance.Controller.GetComponent<NetworkJobsSync>())
                {
                    NetworkJobsSync jobSync = __instance.Controller.GetComponent<NetworkJobsSync>();
                    if(consistInfo != null)
                        jobSync.OnSingleChainGeneratedWithExistingCars(__result);
                    else
                        jobSync.OnSingleChainGenerated(__result);
                }
            }
        }
    }

    class PassengerJobs_GenerateNewCommuterRun_Patch
    {
        static void Postfix(CommuterChainController __result, PassengerJobGenerator __instance, TrainCarsPerLogicTrack consistInfo = null)
        {
            if (NetworkManager.IsHost())
            {
                if (__instance.Controller && __instance.Controller.GetComponent<NetworkJobsSync>())
                {
                    NetworkJobsSync jobSync = __instance.Controller.GetComponent<NetworkJobsSync>();
                    if (consistInfo != null)
                        jobSync.OnSingleChainGeneratedWithExistingCars(__result);
                    else
                        jobSync.OnSingleChainGenerated(__result);
                }
            }
        }
    }

    class PassengerJobs_GenerateCommuterReturnTrip_Patch
    {
        static void Postfix(CommuterChainController __result, StationController sourceStation)
        {
            if (NetworkManager.IsHost())
            {
                NetworkJobsSync jobSync = sourceStation.GetComponent<NetworkJobsSync>();
                if (jobSync != null)
                {
                    jobSync.OnSingleChainGeneratedWithExistingCars(__result);
                }
            }
        }
    }

    class PassengerJobs_GetJobTypeFromDefinition_Patch
    {
        static void Postfix(ref JobType __result, StaticJobDefinition definition)
        {
            if (__result == JobType.Custom)
            {
                if(definition is StaticPassengerJobDefinition)
                {
                    __result = (definition as StaticPassengerJobDefinition).subType;
                }
            }
        }
    }
}
