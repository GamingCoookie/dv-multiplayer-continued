using DV.Logic.Job;
using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    

    [HarmonyPatch(typeof(JobChainControllerWithEmptyHaulGeneration), "OnLastJobInChainCompleted")]
    internal class StopGeneratingJobsAsClientAfterChain
    { 

        private static bool Prefix(Job lastJobInChain)
        {
            if (NetworkManager.IsClient() && !NetworkManager.IsHost())
            {
                return false;
            }
            return true;
        }

        private static void Postfix(Job lastJobInChain)
        {
            if (NetworkManager.IsClient() && NetworkManager.IsHost() && SingletonBehaviour<NetworkJobsManager>.Exists && SingletonBehaviour<NetworkJobsManager>.Instance.newlyGeneratedJobChains.Count > 0)
            {
                NetworkJobsManager networkJobsManager = SingletonBehaviour<NetworkJobsManager>.Instance;
                networkJobsManager.newlyGeneratedJobChainStation.GetComponent<NetworkJobsSync>().OnChainsGeneratedWithExistingCars(networkJobsManager.newlyGeneratedJobChains);
                networkJobsManager.newlyGeneratedJobChains.Clear();
                networkJobsManager.newlyGeneratedJobChainStation = null;
            }
        }
    }

    [HarmonyPatch(typeof(EmptyHaulJobProceduralGenerator), "GenerateEmptyHaulJobWithExistingCars")]
    internal class SendNewEmptyHailChainAsHost
    {
        private static void Postfix(JobChainController __result, StationController startingStation, Track startingTrack, List<TrainCar> transportedTrainCars, System.Random rng)
        {
            if (NetworkManager.IsClient() && NetworkManager.IsHost() && SingletonBehaviour<NetworkJobsManager>.Instance)
            {
                SingletonBehaviour<NetworkJobsManager>.Instance.newlyGeneratedJobChains.Add(__result);
                SingletonBehaviour<NetworkJobsManager>.Instance.newlyGeneratedJobChainStation = startingStation;
            }
        }
    }
}
