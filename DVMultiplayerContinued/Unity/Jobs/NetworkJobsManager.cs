using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV.Logic.Job;
using DVMultiplayer;
using DVMultiplayer.DTO.Job;
using DVMultiplayer.Networking;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Job = DVMultiplayer.DTO.Job.Job;

internal class NetworkJobsManager : SingletonBehaviour<NetworkJobsManager>
{
    private List<Chain> jobChains;
    private List<Job> jobs;
    public bool IsChangedByNetwork { get; set; }
    public bool IsSynced { get; internal set; }
    public List<JobChainController> newlyGeneratedJobChains = new List<JobChainController>();
    public StationController newlyGeneratedJobChainStation;

    protected override void Awake()
    {
        base.Awake();
        jobChains = new List<Chain>();
        jobs = new List<Job>();
        Main.Log($"NetworkJobsManager initialized");
        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (SingletonBehaviour<UnityClient>.Exists)
            SingletonBehaviour<UnityClient>.Instance.MessageReceived -= MessageReceived;

        if (NetworkManager.IsHost())
        {
            Main.Log("Destroying all NetworkJobsSync");
            foreach (StationController station in StationController.allStations)
            {
                if (station.GetComponent<NetworkJobsSync>())
                {
                    foreach (JobChainController chainController in station.GetComponent<NetworkJobsSync>().currentChains)
                    {
                        chainController.JobChainCompleted -= OnJobChainCompleted;
                        chainController.JobOfChainExpired -= OnJobInChainExpired;
                    }
                    DestroyImmediate(station.GetComponent<NetworkJobsSync>());
                }
            }
        }
        else
        {
            foreach (StationController station in StationController.allStations)
            {
                station.ExpireAllAvailableJobsInStation();
            }
            SingletonBehaviour<JobSaveManager>.Instance.DeleteAllNonActiveJobChains();
        }

        if (NetworkManager.IsHost())
        {
            Main.Log("Stop listening to Job events");
            foreach (Job data in jobs)
            {
                if (!data.Definition || data.Definition.job == null)
                    continue;

                if (data.Definition.job.State == JobState.Available)
                    data.Definition.job.JobTaken -= OnJobTaken;
                if (data.Definition.job.State == JobState.InProgress)
                    data.Definition.job.JobCompleted -= OnJobCompleted;
            }
        }

        jobs.Clear();
    }

    #region Events
    public void PlayerConnect()
    {
        Main.Log($"Expiring singleplayer jobs");
        foreach (StationController station in StationController.allStations)
        {
            station.ExpireAllAvailableJobsInStation();
        }
        SingletonBehaviour<JobSaveManager>.Instance.DeleteAllNonActiveJobChains();
    }

    public void OnFinishLoading()
    {
        if (NetworkManager.IsHost())
        {
            SendCurrentJobs();
            foreach (StationController station in StationController.allStations)
            {
                NetworkJobsSync jobSync = station.gameObject.AddComponent<NetworkJobsSync>();
                jobSync.OnJobsGenerated += SendJobCreatedMessage;
            }
            IsSynced = true;
        }

    }

    private void OnJobTaken(DV.Logic.Job.Job job, bool takenViaGame)
    {
        job.JobCompleted += OnJobCompleted;
        Job sJob = jobs.FirstOrDefault(j => j.GameId == job.ID);
        if (sJob != null && sJob.CanTakeJob)
        {
            SendJobTaken(sJob.Id);
            sJob.IsTakenByLocalPlayer = true;
            sJob.IsTaken = true;
        }
    }

    private void OnJobCompleted(DV.Logic.Job.Job job)
    {
        Job sJob = jobs.FirstOrDefault(j => j.GameId == job.ID);
        if (sJob != null && !sJob.IsCompleted && sJob.IsTakenByLocalPlayer)
        {
            SendJobCompleted(sJob.Id);
            sJob.IsCompleted = true;
            if (NetworkManager.IsHost())
                UpdateChainSaveData(sJob.ChainId);
        }
    }

    private void OnJobChainCompleted(JobChainController chainController)
    {
        Chain chain = jobChains.FirstOrDefault(c => c.Controller == chainController);
        chain.IsCompleted = true;
        SendChainCompleted(chain.Id);
    }

    private void OnJobInChainExpired(JobChainController chainController)
    {
        chainController.JobOfChainExpired -= OnJobInChainExpired;
        Chain chain = jobChains.FirstOrDefault(c => c.Controller == chainController);
        chain.IsExpired = true;
        SendChainExpired(chain.Id);
    }

    private void OnNextJobInChainGenerated(StaticJobDefinition jobDef, DV.Logic.Job.Job job)
    {
        if (NetworkManager.IsHost())
        {
            Job data = jobs.FirstOrDefault(j => j.Definition == jobDef);
            Job prevJob = jobs.FirstOrDefault(j => j.ChainId == data.ChainId && j.IsCurrentJob);
            prevJob.IsCurrentJob = false;
            data.GameId = job.ID;
            data.IsCurrentJob = true;
            SendNextJobGenerated(data);
        }

        Main.Log("Register job taken event");
        job.JobTaken += OnJobTaken;
        Main.Log("Job fully loaded");
    }

    private void OnJobExpired(DV.Logic.Job.Job job)
    {
        job.JobTaken -= OnJobTaken;
    }
    #endregion

    #region Messaging
    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.JOB_CREATED:
                    OnJobCreatedMessage(message);
                    break;

                case NetworkTags.JOB_SYNC:
                    OnJobSyncMessage(message);
                    break;

                case NetworkTags.JOB_TAKEN:
                    OnOtherPlayerTakenJobMessage(message);
                    break;

                case NetworkTags.JOB_COMPLETED:
                    OnJobCompletedMesssage(message);
                    break;

                case NetworkTags.JOB_NEXT_JOB:
                    OnNextJobGeneratedMessage(message);
                    break;

                case NetworkTags.JOB_CHAIN_COMPLETED:
                    OnJobChainFinishedMessage(message);
                    break;

                case NetworkTags.JOB_CHAIN_EXPIRED:
                    OnJobChainExpiredMessage(message);
                    break;

                case NetworkTags.JOB_STATION_EXPIRATION:
                    OnStationJobsExpire(message);
                    break;
            }
        }
    }
    #endregion

    #region Receiving Messages
    private void OnStationJobsExpire(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < JOB_STATION_EXPIRATION");

            while (reader.Position < reader.Length)
            {
                IsChangedByNetwork = true;
                string id = reader.ReadString();
                StationController station = StationController.allStations.FirstOrDefault(s => s.logicStation.ID == id);
                if (station != null)
                {
                    ExpireJobsUnTakenInStation(station);
                }
                IsChangedByNetwork = false;
            }
        }
    }

    private void OnJobChainFinishedMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < JOB_CHAIN_COMPLETED");

            while (reader.Position < reader.Length)
            {
                IsChangedByNetwork = true;
                string id = reader.ReadString();
                Chain chain = jobChains.FirstOrDefault(j => j.Id == id);
                if (chain != null)
                {
                    chain.IsCompleted = true;
                }
                IsChangedByNetwork = false;
            }
        }
    }

    private void OnJobChainExpiredMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < JOB_CHAIN_EXPIRED");

            while (reader.Position < reader.Length)
            {
                IsChangedByNetwork = true;
                string id = reader.ReadString();
                Chain chain = jobChains.FirstOrDefault(j => j.Id == id);
                if (chain != null)
                {
                    chain.IsExpired = true;
                }
                IsChangedByNetwork = false;
            }
        }
    }

    private void OnJobCompletedMesssage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < JOB_COMPLETED");

            while (reader.Position < reader.Length)
            {
                IsChangedByNetwork = true;
                string id = reader.ReadString();
                Job job = jobs.FirstOrDefault(j => j.Id == id);
                if (job != null && !job.IsCompleted && !job.IsTakenByLocalPlayer)
                {
                    job.IsCompleted = true;
                    job.Definition.job.CompleteJob();
                    if (NetworkManager.IsHost())
                        UpdateChainSaveData(job.ChainId);
                }
                IsChangedByNetwork = false;
            }
        }
    }

    private void OnOtherPlayerTakenJobMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < JOB_TAKEN");

            while (reader.Position < reader.Length)
            {
                IsChangedByNetwork = true;
                string id = reader.ReadString();
                Job job = jobs.FirstOrDefault(j => j.Id == id);
                if (!job.IsTaken)
                {
                    job.IsTaken = true;
                    SingletonBehaviour<CoroutineManager>.Instance.Run(ExpireJobAfterTime(job));
                }
                IsChangedByNetwork = false;
            }
        }
    }

    private void OnJobSyncMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < JOB_SYNC");

            while (reader.Position < reader.Length)
            {
                IsChangedByNetwork = true;
                Chain[] chains = reader.ReadSerializables<Chain>();
                Job[] cjobs = reader.ReadSerializables<Job>();
                foreach (Chain chain in chains)
                {
                    Job[] chainJobs = cjobs.Where(j => j.ChainId == chain.Id).ToArray();
                    Main.Log($"Job chain with ID {chain.Id} loading, amount of jobs in chain: {chainJobs.Length}");
                    JobChainSaveData jobSaveData = JsonConvert.DeserializeObject<JobChainSaveData>(chain.Data, JobSaveManager.serializeSettings);
                    GameObject chainGO = SingletonBehaviour<JobSaveManager>.Instance.LoadJobChain(jobSaveData);
                    if (chainGO)
                    {
                        Main.Log("Job chain succesfully loaded");
                        StaticJobDefinition[] chainJobDefinitions = chainGO.GetComponents<StaticJobDefinition>();
                        foreach (StaticJobDefinition definition in chainJobDefinitions)
                        {
                            Main.Log("Register job definition");
                            Job job = chainJobs.FirstOrDefault(j => j.Type == (definition.job != null ? definition.job.jobType : GetJobTypeFromDefinition(definition)));
                            job.Definition = definition;
                            Main.Log("Add to jobs list");
                            jobs.Add(job);

                            if (job.IsCurrentJob)
                            {

                                Main.Log("Job is current registering job taken event");
                                if (!job.IsTaken)
                                {
                                    job.Definition.job.JobExpired += OnJobExpired;
                                    job.Definition.job.JobTaken += OnJobTaken;
                                }
                                else
                                    job.CanTakeJob = false;
                            }
                            Main.Log("Job fully loaded");
                        }
                        Main.Log("Chain successfully loaded");
                    }
                }
                IsChangedByNetwork = false;
            }
        }
        IsSynced = true;
    }

    private void OnJobCreatedMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < JOB_CREATED");

            while (reader.Position < reader.Length)
            {
                Chain[] newChains = reader.ReadSerializables<Chain>();
                Job[] newJobs = reader.ReadSerializables<Job>();
                SingletonBehaviour<CoroutineManager>.Instance.Run(InitializeNewChainsAndJobs(newChains, newJobs));
            }
        }
    }

    private void OnNextJobGeneratedMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < JOB_NEXT_JOB");

            while (reader.Position < reader.Length)
            {
                Job generatedJob = reader.ReadSerializable<Job>();
                jobs.FirstOrDefault(j => j.ChainId == generatedJob.ChainId && j.IsCurrentJob).IsCurrentJob = false;
                Job referencedJob = jobs.FirstOrDefault(j => j.Id == generatedJob.Id);
                if (referencedJob != null)
                {
                    referencedJob.GameId = generatedJob.GameId;
                    referencedJob.IsCurrentJob = true;
                    if (referencedJob.Definition)
                    {
                        referencedJob.Definition.JobGenerated += OnNextJobInChainGenerated;
                        referencedJob.Definition.ForceJobId(referencedJob.GameId);
                        referencedJob.Definition.TryToGenerateJob();
                    }
                }
            }
        }
    }
    #endregion

    #region Sending Messages
    internal void SendJobCreatedMessage(StationController station, JobChainController[] chainControllers)
    {
        Main.Log($"[CLIENT] > JOB_CREATED");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            Tuple<List<Chain>, List<Job>> data = GenerateChainsAndJobs(chainControllers);

            writer.Write(data.Item1.ToArray());
            writer.Write(data.Item2.ToArray());

            using (Message message = Message.Create((ushort)NetworkTags.JOB_CREATED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendJobsRequest()
    {
        Main.Log($"[CLIENT] > JOB_SYNC");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(true);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendJobTaken(string id)
    {
        Main.Log($"[CLIENT] > JOB_TAKEN");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(id);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_TAKEN, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendNextJobGenerated(Job job)
    {
        Main.Log($"[CLIENT] > JOB_NEXT_JOB");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(job);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_NEXT_JOB, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendJobCompleted(string id)
    {
        Main.Log($"[CLIENT] > JOB_COMPLETED");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(id);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_COMPLETED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendCurrentJobs()
    {

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            List<Chain> chains = new List<Chain>();
            List<Job> jobs = new List<Job>();

            foreach (StationController station in StationController.allStations)
            {
                Tuple<List<Chain>, List<Job>> data = GenerateChainsAndJobs(station.ProceduralJobsController.GetCurrentJobChains().ToArray());
                chains.AddRange(data.Item1);
                jobs.AddRange(data.Item2);
            }
            Main.Log($"[CLIENT] > JOB_HOST_SYNC: chain amount: {chains.Count}, job amount: {jobs.Count}");

            writer.Write(chains.ToArray());
            writer.Write(jobs.ToArray());

            using (Message message = Message.Create((ushort)NetworkTags.JOB_HOST_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendChainCompleted(string id)
    {
        Main.Log($"[CLIENT] > JOB_CHAIN_COMPLETED");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(id);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_CHAIN_COMPLETED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendChainExpired(string id)
    {
        Main.Log($"[CLIENT] > JOB_CHAIN_COMPLETED");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(id);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_CHAIN_EXPIRED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendNewChainSaveData(Chain chain)
    {
        Main.Log($"[CLIENT] > JOB_CHAIN_CHANGED");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(chain);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_CHAIN_CHANGED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendJobsExpirationInStation(string stationId)
    {
        Main.Log($"[CLIENT] > JOB_STATION_EXPIRATION");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(stationId);

            using (Message message = Message.Create((ushort)NetworkTags.JOB_STATION_EXPIRATION, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }
    #endregion

    private IEnumerator ExpireJobAfterTime(Job job)
    {
        // Wait 2 minutes so the user can still accept the job
        yield return new WaitForSeconds(2 * 60);
        job.CanTakeJob = false;
    }

    internal void ExpireJobsUnTakenInStation(StationController station)
    {
        foreach (DV.Logic.Job.Job job in station.logicStation.availableJobs.ToList())
        {
            Job sjob = jobs.FirstOrDefault(j => j.GameId == job.ID);
            if (sjob != null && !sjob.IsTaken)
            {
                job.ExpireJob();
            }
        }
    }

    internal bool IsAllowedToTakeJob(string id)
    {
        Job job = jobs.FirstOrDefault(j => j.GameId == id);
        return job != null && job.CanTakeJob;
    }

    private void UpdateChainSaveData(string chainId)
    {
        Chain chain = jobChains.FirstOrDefault(c => c.Id == chainId);
        if (!chain.IsCompleted)
        {
            chain.Data = JsonConvert.SerializeObject(chain.Controller.GetJobChainSaveData(), JobSaveManager.serializeSettings);
            SendNewChainSaveData(chain);
        }
    }

    private IEnumerator InitializeNewChainsAndJobs(Chain[] chains, Job[] cjobs)
    {
        IsChangedByNetwork = true;
        foreach (Chain chain in chains)
        {
            Job[] chainJobs = cjobs.Where(j => j.ChainId == chain.Id).ToArray();
            Main.Log($"Job chain with ID {chain.Id} loading, amount of jobs in chain: {chainJobs.Length}");
            JobChainSaveData jobSaveData = JsonConvert.DeserializeObject<JobChainSaveData>(chain.Data, JobSaveManager.serializeSettings);
            yield return new WaitUntil(() =>
            {
                foreach (string guid in jobSaveData.trainCarGuids)
                {
                    if (!SingletonBehaviour<NetworkTrainManager>.Instance.localCars.Any(t => t.CarGUID == guid))
                        return false;
                }
                return true;
            });
            GameObject chainGO = SingletonBehaviour<JobSaveManager>.Instance.LoadJobChain(jobSaveData);
            if (chainGO)
            {
                Main.Log("Job chain succesfully loaded");
                StaticJobDefinition[] chainJobDefinitions = chainGO.GetComponents<StaticJobDefinition>();

                foreach (StaticJobDefinition definition in chainJobDefinitions)
                {
                    Main.Log("Register job definition");
                    Job job = chainJobs.FirstOrDefault(j => j.Type == (definition.job != null ? definition.job.jobType : GetJobTypeFromDefinition(definition)));
                    job.Definition = definition;
                    Main.Log("Add to jobs list");
                    jobs.Add(job);

                    if (job.IsCurrentJob)
                    {
                        Main.Log("Job is current registering job taken event");
                        if (!job.IsTaken)
                            job.Definition.job.JobTaken += OnJobTaken;
                        else
                            job.CanTakeJob = false;
                    }
                    Main.Log("Job fully loaded");
                }
                Main.Log("Chain successfully loaded");
            }
        }
        IsChangedByNetwork = false;
    }

    private Tuple<List<Chain>, List<Job>> GenerateChainsAndJobs(JobChainController[] chainControllers)
    {
        List<Chain> newChains = new List<Chain>();
        List<Job> newJobs = new List<Job>();
        foreach (JobChainController controller in chainControllers)
        {
            string chainId = Guid.NewGuid().ToString();
            while (jobChains.Any(c => c.Id == chainId))
            {
                chainId = Guid.NewGuid().ToString();
            }

            Chain chain = new Chain()
            {
                Id = chainId,
                Data = JsonConvert.SerializeObject(controller.GetJobChainSaveData(), JobSaveManager.serializeSettings),
                IsCompleted = false,
                Controller = controller
            };

            jobChains.Add(chain);
            newChains.Add(chain);

            controller.JobChainCompleted += OnJobChainCompleted;
            controller.JobOfChainExpired += OnJobInChainExpired;
            StaticJobDefinition[] definitions = controller.jobChainGO.GetComponents<StaticJobDefinition>();
            foreach (StaticJobDefinition definition in definitions)
            {
                string jobId = Guid.NewGuid().ToString();
                while (jobs.Any(c => c.Id == jobId))
                {
                    jobId = Guid.NewGuid().ToString();
                }

                Job job = new Job()
                {
                    Id = jobId,
                    ChainId = chainId,
                    GameId = definition.job != null ? definition.job.ID : "",
                    IsCurrentJob = definition.job != null,
                    Type = definition.job != null ? definition.job.jobType : GetJobTypeFromDefinition(definition),
                    Definition = definition,
                    CanTakeJob = definition.job != null ? definition.job.State != JobState.Completed && definition.job.State != JobState.InProgress : true,
                    IsCompleted = definition.job != null ? definition.job.State == JobState.Completed : false,
                    IsTaken = definition.job != null ? definition.job.State == JobState.InProgress : false,
                    IsTakenByLocalPlayer = definition.job != null ? definition.job.State == JobState.InProgress : false,
                };

                if (definition.job != null)
                    definition.job.JobTaken += OnJobTaken;
                else
                    definition.JobGenerated += OnNextJobInChainGenerated;

                jobs.Add(job);
                newJobs.Add(job);
            }
        }
        return Tuple.Create(newChains, newJobs);
    }

    private JobType GetJobTypeFromDefinition(StaticJobDefinition definition)
    {
        JobType jobType = JobType.Custom;

        if (definition is StaticEmptyHaulJobDefinition)
            jobType = JobType.EmptyHaul;
        else if (definition is StaticShuntingLoadJobDefinition)
            jobType = JobType.ShuntingLoad;
        else if (definition is StaticShuntingUnloadJobDefinition)
            jobType = JobType.ShuntingUnload;
        else if (definition is StaticTransportJobDefinition)
            jobType = JobType.Transport;

        return jobType;
    }

    internal StationController GetStationById(string id)
    {
        return StationController.allStations.FirstOrDefault(s => s.logicStation.ID == id);
    }
}