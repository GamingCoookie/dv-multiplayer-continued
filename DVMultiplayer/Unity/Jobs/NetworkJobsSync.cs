using DV.Logic.Job;
using DVMultiplayer;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class NetworkJobsSync : MonoBehaviour
{
    internal event Action<StationController, JobChainController[]> OnJobsGenerated;
    internal List<JobChainController> currentChains = new List<JobChainController>();
    StationController station;
    Coroutine sendNewJobsAfterGeneration = null;
    List<JobChainController> newChains = new List<JobChainController>();

    private void Awake()
    {
        station = GetComponent<StationController>();
        station.ProceduralJobsController.JobGenerationAttempt += OnChainsGenerated;
        currentChains.AddRange(station.ProceduralJobsController.GetCurrentJobChains());
    }

    private void OnDestroy()
    {
        station.ProceduralJobsController.JobGenerationAttempt -= OnChainsGenerated;
    }

    private void OnChainsGenerated()
    {
        List<JobChainController> newJobs = station.ProceduralJobsController.GetCurrentJobChains();
        foreach (JobChainController chain in currentChains)
        {
            newJobs.RemoveAll(j => j.currentJobInChain.ID == chain.currentJobInChain.ID);
        }

        currentChains.AddRange(newJobs);
        newChains.AddRange(newJobs);
        if (sendNewJobsAfterGeneration == null)
            sendNewJobsAfterGeneration = SingletonBehaviour<CoroutineManager>.Instance.Run(WaitTillGenerationFinished());
    }

    internal void OnSingleChainGeneratedWithExistingCars(JobChainController chain)
    {
        Main.Log("Single Chain with existing cars generated");
        currentChains.Add(chain);
        OnJobsGenerated?.Invoke(station, new JobChainController[] { chain });
    }

    internal void OnChainsGeneratedWithExistingCars(List<JobChainController> chains)
    {
        Main.Log("Multiple Chains with existing cars generated");
        currentChains.AddRange(chains);
        OnJobsGenerated?.Invoke(station, chains.ToArray());
    }

    internal void OnSingleChainGenerated(JobChainController chain)
    {
        Main.Log("Single Chain with existing cars generated");
        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewJobChainCars(chain.trainCarsForJobChain);
        currentChains.Add(chain);
        OnJobsGenerated?.Invoke(station, new JobChainController[] { chain });
    }

    private IEnumerator WaitTillGenerationFinished()
    {
        while (station.ProceduralJobsController.IsJobGenerationActive)
        {
            yield return new WaitUntil(() => !station.ProceduralJobsController.IsJobGenerationActive);
            yield return new WaitForSeconds(.25f);
        }

        Main.Log("Generation is finished Length = " + newChains.Count);
        List<TrainCar> newJobTrains = new List<TrainCar>();
        foreach (JobChainController job in newChains)
        {
            newJobTrains.AddRange(job.trainCarsForJobChain);
        }
        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewJobChainCars(newJobTrains);

        OnJobsGenerated?.Invoke(station, newChains.ToArray());
        newChains.Clear();
        sendNewJobsAfterGeneration = null;
    }
}
