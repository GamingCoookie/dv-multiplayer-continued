using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV;
using DV.CabControls;
using DV.Logic.Job;
using DV.MultipleUnit;
using DV.PointSet;
using DVMultiplayer;
using DVMultiplayer.Darkrift;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using DVMultiplayer.Utils.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class NetworkTrainManager : SingletonBehaviour<NetworkTrainManager>
{
    public List<TrainCar> localCars = new List<TrainCar>();
    public List<WorldTrain> serverCarStates = new List<WorldTrain>();
    public bool IsChangeByNetwork { get; internal set; }
    public bool IsSynced { get; private set; }
    public bool SaveCarsLoaded { get; internal set; }
    public bool IsSpawningTrains { get; set; } = false;
    public bool IsDisconnecting { get; set; } = false;
    private readonly BufferQueue buffer = new BufferQueue();

    protected override void Awake()
    {
        base.Awake();
        IsChangeByNetwork = false;
        localCars = new List<TrainCar>();
        SingletonBehaviour<UnityClient>.Instance.MessageReceived += OnMessageReceived;

        Main.Log($"Listening to CarChanged event");
        PlayerManager.CarChanged += OnPlayerSwitchTrainCarEvent;
        CarSpawner.CarSpawned += OnCarSpawned;
        CarSpawner.CarAboutToBeDeleted += OnCarAboutToBeDeleted;
    }

#pragma warning disable IDE0051 // Remove unused private members
    private void Update()
    {
        if (IsSpawningTrains || !IsSynced)
            return;

        foreach(TrainCar car in localCars.ToList())
        {
            if(car.logicCar == null)
            {
                localCars.Remove(car);
            }
        }
    }
#pragma warning restore IDE0051 // Restore unused private members

    #region Events
    public void OnFinishedLoading()
    {
        SaveCarsLoaded = false;
        localCars = GameObject.FindObjectsOfType<TrainCar>().ToList();
        Main.Log($"{localCars.Count} traincars found, {localCars.Where(car => car.IsLoco).Count()} are locomotives");

        if (NetworkManager.IsHost())
        {
            foreach (TrainCar trainCar in localCars)
            {
                AddNetworkingScripts(trainCar, null);
            }
        }

        SendInitializedCars();
        SaveCarsLoaded = true;
    }

    private void OnCarAboutToBeDeleted(TrainCar car)
    {
        if (IsChangeByNetwork || !IsSynced)
            return;

        localCars.Remove(car);
        SendCarBeingRemoved(car);
    }

    private void OnCarSpawned(TrainCar car)
    {
        if (IsChangeByNetwork || !IsSynced || IsSpawningTrains)
            return;

        if (car.IsLoco || car.playerSpawnedCar || car.carType == TrainCarType.Tender || car.carType == TrainCarType.TenderBlue)
        {
            AddNetworkingScripts(car, null);

            SendNewCarSpawned(car);
            AppUtil.Instance.PauseGame();
            CustomUI.OpenPopup("Streaming", "New Area being loaded");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        IsDisconnecting = true;
        if (SingletonBehaviour<UnityClient>.Exists)
            SingletonBehaviour<UnityClient>.Instance.MessageReceived -= OnMessageReceived;
        PlayerManager.CarChanged -= OnPlayerSwitchTrainCarEvent;
        CarSpawner.CarSpawned -= OnCarSpawned;
        CarSpawner.CarAboutToBeDeleted -= OnCarAboutToBeDeleted;
        if (localCars == null)
            return;

        foreach (TrainCar trainCar in localCars)
        {
            if (!trainCar)
                continue;

            if (NetworkManager.IsHost())
            {
                if (trainCar.GetComponent<NetworkTrainPosSync>())
                    DestroyImmediate(trainCar.GetComponent<NetworkTrainPosSync>());
                if (trainCar.GetComponent<NetworkTrainSync>())
                    DestroyImmediate(trainCar.GetComponent<NetworkTrainSync>());
                if (trainCar.frontCoupler.GetComponent<NetworkTrainCouplerSync>())
                    DestroyImmediate(trainCar.frontCoupler.GetComponent<NetworkTrainCouplerSync>());
                if (trainCar.rearCoupler.GetComponent<NetworkTrainCouplerSync>())
                    DestroyImmediate(trainCar.rearCoupler.GetComponent<NetworkTrainCouplerSync>());
                if (trainCar.GetComponent<NetworkTrainMUSync>())
                    DestroyImmediate(trainCar.GetComponent<NetworkTrainMUSync>());
            }
        }

        localCars.Clear();
    }

    internal void CargoStateChanged(TrainCar trainCar, CargoType type, bool isLoaded)
    {
        WarehouseMachine warehouse = null;
        if (trainCar.IsCargoLoadedUnloadedByMachine)
            warehouse = trainCar.logicCar.CargoOriginWarehouse;

        SendCargoStateChange(trainCar.CarGUID, trainCar.LoadedCargoAmount, type, warehouse != null ? warehouse.ID : "", isLoaded);
    }

    private void OnPlayerSwitchTrainCarEvent(TrainCar trainCar)
    {
        if (trainCar)
        {
            AddNetworkingScripts(trainCar, null);
        }

        NetworkPlayerSync playerSync = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync();
        if (playerSync.Train && playerSync.Train.IsLoco)
        {
            playerSync.Train.GetComponent<NetworkTrainSync>().listenToLocalPlayerInputs = false;
            if (NetworkManager.IsHost()) playerSync.Train.GetComponent<NetworkTrainPosSync>().CheckAuthorityChange();
        }

        playerSync.Train = trainCar;
        if (NetworkManager.IsHost() && trainCar) playerSync.Train.GetComponent<NetworkTrainPosSync>().CheckAuthorityChange();
        SendPlayerCarChange(trainCar);

        if (trainCar && trainCar.IsLoco)
        {
            StartCoroutine(ListenToTrainInputEvents(trainCar));
            StartCoroutine(ResyncCarWithInteriorLoaded(trainCar));
        }
    }

    private IEnumerator ResyncCarWithInteriorLoaded(TrainCar car)
    {
        yield return new WaitUntil(() => car.IsInteriorLoaded);
        yield return new WaitForSeconds(.1f);
        ResyncCar(car);
    }

    private void NetworkTrainManager_OnTrainCarInitialized(TrainCar train)
    {
        WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == train.CarGUID);
        if (!train.IsLoco && serverState.CargoType != CargoType.None)
        {
            train.logicCar.LoadCargo(serverState.CargoAmount, serverState.CargoType);
        }
    }
    #endregion

    #region Messaging
    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.TRAIN_LEVER:
                    OnLocoLeverMessage(message);
                    break;

                case NetworkTags.TRAIN_LOCATION_UPDATE:
                    OnCarLocationMessage(message);
                    break;

                case NetworkTags.TRAIN_SWITCH:
                    OnPlayerCarChangeMessage(message);
                    break;

                case NetworkTags.TRAIN_DERAIL:
                    OnCarDerailmentMessage(message);
                    break;

                case NetworkTags.TRAIN_COUPLE:
                    OnCarCoupleChangeMessage(message, true);
                    break;

                case NetworkTags.TRAIN_UNCOUPLE:
                    OnCarCoupleChangeMessage(message, false);
                    break;

                case NetworkTags.TRAIN_COUPLE_HOSE:
                    OnCarCouplerHoseChangeMessage(message);
                    break;

                case NetworkTags.TRAIN_COUPLE_COCK:
                    OnCarCouplerCockChangeMessage(message);
                    break;

                case NetworkTags.TRAIN_SYNC_ALL:
                    OnCarSyncAllMessage(message);
                    break;

                case NetworkTags.TRAIN_RERAIL:
                    OnCarRerailMessage(message);
                    break;

                case NetworkTags.TRAINS_INIT:
                    OnCarInitMessage(message);
                    break;

                case NetworkTags.TRAINS_INIT_FINISHED:
                    OnAllClientsNewTrainsLoaded();
                    break;

                case NetworkTags.TRAIN_REMOVAL:
                    OnCarRemovalMessage(message);
                    break;

                case NetworkTags.TRAIN_DAMAGE:
                    OnCarDamageMessage(message);
                    break;

                case NetworkTags.TRAIN_AUTH_CHANGE:
                    OnAuthorityChangeMessage(message);
                    break;

                case NetworkTags.TRAIN_CARGO_CHANGE:
                    OnCargoChangeMessage(message);
                    break;

                case NetworkTags.TRAIN_MU_CHANGE:
                    OnCarMUChangeMessage(message);
                    break;
            }
        }
    }
    #endregion

    #region Receiving Messages
    private void OnAllClientsNewTrainsLoaded()
    {
        Main.Log("[CLIENT] < TRAINS_INIT_FINISHED");
        CustomUI.Close();
        AppUtil.Instance.UnpauseGame();
        IsSpawningTrains = false;
    }

    private void OnCarDamageMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarDamageMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                Main.Log($"[CLIENT] < TRAIN_DAMAGE");
                CarDamage damage = reader.ReadSerializable<CarDamage>();
                TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == damage.Guid);
                if (train)
                {
                    IsChangeByNetwork = true;
                    WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == damage.Guid);
                    UpdateServerStateDamage(serverState, damage.DamageType, damage.NewHealth, damage.Data);
                    switch (damage.DamageType)
                    {
                        case DamageType.Car:
                            if (train.IsLoco)
                                train.GetComponent<NetworkTrainPosSync>().LoadLocoDamage(damage.Data);
                            else
                                train.CarDamage.LoadCarDamageState(damage.NewHealth);
                            break;

                        case DamageType.Cargo:
                            train.CargoDamage.LoadCargoDamageState(damage.NewHealth);
                            break;
                    }
                    SyncLocomotiveWithServerState(train, serverState);
                    IsChangeByNetwork = false;
                }
            }
        }
    }

    private void OnCarRemovalMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarRemovalMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                CarRemoval carRemoval = reader.ReadSerializable<CarRemoval>();
                TrainCar train = localCars.ToList().FirstOrDefault(t => t.CarGUID == carRemoval.Guid);
                if (train)
                {
                    IsChangeByNetwork = true;
                    localCars.Remove(train);
                    CarSpawner.DeleteCar(train);
                    for(int i = localCars.Count - 1; i >= 0 ; i--)
                    {
                        if (!localCars[i] || localCars[i].logicCar == null)
                            localCars.RemoveAt(i);
                    }
                    IsChangeByNetwork = false;
                }
            }
        }
    }

    private void OnCarInitMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarInitMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                IsSpawningTrains = true;
                Main.Log($"[CLIENT] < TRAINS_INIT");
                IsChangeByNetwork = true;
                WorldTrain[] trains = reader.ReadSerializables<WorldTrain>();
                SingletonBehaviour<CoroutineManager>.Instance.Run(SpawnSendedTrains(trains));
                IsChangeByNetwork = false;
            }
        }
    }

    private void OnCarRerailMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarRerailMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainRerail data = reader.ReadSerializable<TrainRerail>();
                Main.Log($"[CLIENT] < TRAIN_RERAIL: ID: {data.Guid}");
                TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == data.Guid);
                if (train)
                {
                    WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == data.Guid);
                    if (serverState != null)
                    {
                        serverState.Position = data.Position;
                        serverState.Rotation = data.Rotation;
                        serverState.Forward = data.Forward;
                        serverState.Bogies[0] = new TrainBogie()
                        {
                            TrackName = data.Bogie1TrackName,
                            PositionAlongTrack = data.Bogie1PositionAlongTrack,
                            Derailed = false
                        };
                        serverState.Bogies[serverState.Bogies.Length - 1] = new TrainBogie()
                        {
                            TrackName = data.Bogie2TrackName,
                            PositionAlongTrack = data.Bogie2PositionAlongTrack,
                            Derailed = false
                        };
                        serverState.CarHealth = data.CarHealth;
                        if (!serverState.IsLoco)
                            serverState.CargoHealth = data.CargoHealth;
                        else
                        {
                            serverState.Throttle = 0;
                            serverState.Sander = 0;
                            serverState.Brake = 0;
                            serverState.IndepBrake = 1;
                            serverState.Reverser = 0f;
                            if (serverState.Shunter != null)
                            {
                                serverState.Shunter.IsEngineOn = false;
                                serverState.Shunter.IsMainFuseOn = false;
                                serverState.Shunter.IsSideFuse1On = false;
                                serverState.Shunter.IsSideFuse2On = false;
                            }
                            if (serverState.Diesel != null)
                            {
                                serverState.Diesel.IsEngineOn = false;
                                serverState.Diesel.IsMainFuseOn = false;
                                serverState.Diesel.IsSideFuse1On = false;
                                serverState.Diesel.IsSideFuse2On = false;
                                serverState.Diesel.IsSideFuse3On = false;
                            }
                        }
                    }
                    train.GetComponent<NetworkTrainPosSync>().isDerailed = false;
                    SingletonBehaviour<CoroutineManager>.Instance.Run(RerailDesynced(train, data.Position, data.Forward));
                }
            }
        }
    }

    private void OnCarSyncAllMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                Main.Log($"[CLIENT] < TRAIN_SYNC_ALL");
                serverCarStates = reader.ReadSerializables<WorldTrain>().ToList();
                SingletonBehaviour<CoroutineManager>.Instance.Run(SyncCarsFromServerState());
            }
        }
    }

    private void OnPlayerCarChangeMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnPlayerCarChangeMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainCarChange changedCar = reader.ReadSerializable<TrainCarChange>();
                NetworkPlayerSync targetPlayerSync = SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayerSyncById(changedCar.PlayerId);
                
                if (changedCar.TrainId == "")
                {
                    Main.Log($"[CLIENT] < TRAIN_SWITCH: Player left train");
                    if (NetworkManager.IsHost()) targetPlayerSync.Train.GetComponent<NetworkTrainPosSync>().CheckAuthorityChange();
                    targetPlayerSync.Train = null;
                }
                else
                {
                    TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == changedCar.TrainId);
                    if (train)
                    {
                        AddNetworkingScripts(train, null);
                        ResyncCar(train);
                        Main.Log($"[CLIENT] < TRAIN_SWITCH: Train found: {train}, ID: {train.ID}, GUID: {train.CarGUID}");
                        if (NetworkManager.IsHost() && targetPlayerSync.Train) targetPlayerSync.Train.GetComponent<NetworkTrainPosSync>().CheckAuthorityChange();
                        targetPlayerSync.Train = train;
                        if (NetworkManager.IsHost()) targetPlayerSync.Train.GetComponent<NetworkTrainPosSync>().CheckAuthorityChange();
                    }
                    else
                    {
                        Main.Log($"[CLIENT] < TRAIN_SWITCH: Train not found, GUID: {changedCar.TrainId}");
                    }
                }
            }
        }
    }

    private void OnCarDerailmentMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarDerailmentMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainDerail data = reader.ReadSerializable<TrainDerail>();
                TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == data.TrainId);

                if (train)
                {
                    IsChangeByNetwork = true;
                    Main.Log($"[CLIENT] < TRAIN_DERAIL: Packet size: {reader.Length}, TrainId: {train.ID}");
                    WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == train.CarGUID);
                    if (serverState == null)
                    {
                        serverState = new WorldTrain()
                        {
                            Guid = train.CarGUID,
                        };
                        switch (train.carType)
                        {
                            case TrainCarType.LocoShunter:
                                serverState.Shunter = new Shunter();
                                break;
                            case TrainCarType.LocoDiesel:
                                serverState.Diesel = new Diesel();
                                break;
                            case TrainCarType.LocoSteamHeavy:
                            case TrainCarType.LocoSteamHeavyBlue:
                                serverState.Steamer = new Steamer();
                                break;
                        }
                                
                        serverCarStates.Add(serverState);
                    }
                    serverState.Bogies[0] = new TrainBogie()
                    {
                        TrackName = data.Bogie1TrackName,
                        PositionAlongTrack = data.Bogie1PositionAlongTrack,
                        Derailed = data.IsBogie1Derailed
                    };
                    serverState.Bogies[serverState.Bogies.Length - 1] = new TrainBogie()
                    {
                        TrackName = data.Bogie2TrackName,
                        PositionAlongTrack = data.Bogie2PositionAlongTrack,
                        Derailed = data.IsBogie1Derailed
                    };
                    serverState.CarHealth = data.CarHealth;
                    if (!serverState.IsLoco)
                        serverState.CargoHealth = data.CargoHealth;

                    train.GetComponent<NetworkTrainPosSync>().isDerailed = true;
                    train.Derail();
                    SyncDamageWithServerState(train, serverState);
                    IsChangeByNetwork = false;
                }
                else
                {
                    Main.Log($"[CLIENT] < TRAIN_SWITCH: Train not found, GUID: {data.TrainId}");
                }
            }
        }
    }

    private void OnCarLocationMessage(Message message)
    {
        if (!IsSynced)
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainLocation[] locations = reader.ReadSerializables<TrainLocation>();
                if(locations.Length == 0)
                {
                    Main.Log("Train positions data empty");
                }
                foreach(TrainLocation location in locations)
                {
                    TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == location.TrainId);
                    if (train)
                    {
                        WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == train.CarGUID);

                        if(serverState == null)
                        {
                            Main.Log("Server state not found");
                            continue;
                        }

                        if(serverState.updatedAt <= location.Timestamp)
                        {
                            serverState.Position = location.Position;
                            serverState.Rotation = location.Rotation;
                            serverState.Forward = location.Forward;
                            serverState.Bogies = location.Bogies;
                            serverState.IsStationary = location.IsStationary;
                            serverState.updatedAt = location.Timestamp;

                            //Main.Log($"[CLIENT] < TRAIN_LOCATION_UPDATE: TrainID: {train.ID}");
                            if (train.GetComponent<NetworkTrainPosSync>())
                                train.GetComponent<NetworkTrainPosSync>().UpdateLocation(location);
                            else
                                Main.Log("NetworkTrainPosSync not found");
                        }
                    }
                    else
                    {
                        Main.Log("Train not found");
                    }
                }
            }
        }
    }

    private void OnLocoLeverMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnLocoLeverMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainLever lever = reader.ReadSerializable<TrainLever>();

                TrainCar train = localCars.FirstOrDefault(t => t.IsLoco && t.CarGUID == lever.TrainId);
                if (train && train.IsLoco)
                {                    
                    WorldTrain serverTrainState = serverCarStates.FirstOrDefault(t => t.Guid == train.CarGUID);
                    if (train.GetComponent<MultipleUnitModule>())
                    {
                        switch (lever.Lever)
                        {
                            case Levers.Brake:
                            case Levers.IndependentBrake:
                            case Levers.Reverser:
                            case Levers.Sander:
                            case Levers.Throttle:
                                UpdateMUServerStateLeverChange(serverTrainState, lever.Lever, lever.Value);
                                break;

                            default:
                                if (serverTrainState != null)
                                {
                                    UpdateServerStateLeverChange(serverTrainState, lever.Lever, lever.Value);
                                }
                                break;

                        }
                    }
                    else
                    {
                        if (serverTrainState != null)
                        {
                            UpdateServerStateLeverChange(serverTrainState, lever.Lever, lever.Value);
                        }
                    }

                    //Main.Log($"[CLIENT] < TRAIN_LEVER: Packet size: {reader.Length}, TrainID: {train.ID}, Lever: {lever.Lever}, Value: {lever.Value}");
                    IsChangeByNetwork = true;
                    LocoControllerBase baseController = train.GetComponent<LocoControllerBase>();
                    switch (lever.Lever)
                    {
                        case Levers.Throttle:
                            baseController.SetThrottle(lever.Value);
                            break;

                        case Levers.Brake:
                            baseController.SetBrake(lever.Value);
                            break;

                        case Levers.IndependentBrake:
                            baseController.SetIndependentBrake(lever.Value);
                            break;

                        case Levers.Reverser:
                            baseController.SetReverser(lever.Value);
                            break;

                        case Levers.Sander:
                            baseController.SetSanders(lever.Value);
                            break;

                        case Levers.SideFuse_1:
                            if (train.carType == TrainCarType.LocoShunter && train.IsInteriorLoaded)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Use();
                                if (train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 1 && lever.Value == 0)
                                    train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Use();
                            }
                            else if (train.carType == TrainCarType.LocoDiesel && train.IsInteriorLoaded)
                            {
                                train.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Use();
                                if (train.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 1 && lever.Value == 0)
                                    train.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel.mainFuseObj.GetComponent<ToggleSwitchBase>().Use();
                            }
                            break;

                        case Levers.SideFuse_2:
                            if (train.carType == TrainCarType.LocoShunter && train.IsInteriorLoaded)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.sideFusesObj[1].GetComponent<ToggleSwitchBase>().Use();
                                if (train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 1 && lever.Value == 0)
                                    train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Use();
                            }
                            else if (train.carType == TrainCarType.LocoDiesel && train.IsInteriorLoaded)
                            {
                                train.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel.sideFusesObj[1].GetComponent<ToggleSwitchBase>().Use();
                                if (train.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 1 && lever.Value == 0)
                                    train.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel.mainFuseObj.GetComponent<ToggleSwitchBase>().Use();
                            }
                            break;

                        case Levers.SideFuse_3:
                            if (train.carType == TrainCarType.LocoDiesel && train.IsInteriorLoaded)
                            {
                                train.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel.sideFusesObj[2].GetComponent<ToggleSwitchBase>().Use();
                                if (train.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 1 && lever.Value == 0)
                                    train.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel.mainFuseObj.GetComponent<ToggleSwitchBase>().Use();
                            }
                            break;

                        case Levers.MainFuse:
                            if (train.carType == TrainCarType.LocoShunter && train.IsInteriorLoaded)
                            {
                                train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Use();
                            }
                            else if (train.carType == TrainCarType.LocoDiesel && train.IsInteriorLoaded)
                            {
                                train.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel.mainFuseObj.GetComponent<ToggleSwitchBase>().Use();
                            }
                            break;

                        case Levers.FusePowerStarter:
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                if (train.IsInteriorLoaded)
                                {
                                    train.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController.powerRotaryObj.GetComponent<RotaryBase>().SetValue(lever.Value);
                                }
                                else
                                {
                                    if (lever.Value == 0)
                                        (baseController as LocoControllerShunter).SetEngineRunning(false);
                                    else if (serverTrainState != null && serverTrainState.Shunter.IsEngineOn)
                                        (baseController as LocoControllerShunter).SetEngineRunning(true);
                                }
                            }
                            else if (train.carType == TrainCarType.LocoDiesel)
                            {
                                if (train.IsInteriorLoaded)
                                {
                                    train.interior.GetComponentInChildren<DieselDashboardControls>().fuseBoxPowerControllerDiesel.powerRotaryObj.GetComponent<RotaryBase>().SetValue(lever.Value);
                                }
                                else
                                {
                                    if (lever.Value == 0)
                                        (baseController as LocoControllerDiesel).SetEngineRunning(false);
                                    else if (serverTrainState != null && serverTrainState.Diesel.IsEngineOn)
                                        (baseController as LocoControllerDiesel).SetEngineRunning(true);
                                }
                            }
                            break;

                        case Levers.Horn:
                            float valHorn = lever.Value;
                            if (train.carType == TrainCarType.LocoShunter)
                            {
                                if(train.IsInteriorLoaded)
                                    train.interior.GetComponentInChildren<ShunterDashboardControls>().hornObj.GetComponent<LeverBase>().SetValue(valHorn);
                                if (valHorn < 0.5)
                                    valHorn *= 2;
                                else
                                    valHorn = (valHorn - 0.5f) * 2;
                            }
                            else if (train.carType == TrainCarType.LocoDiesel)
                            {
                                if (train.IsInteriorLoaded)
                                    train.interior.GetComponentInChildren<DieselDashboardControls>().hornObj.GetComponent<LeverBase>().SetValue(valHorn);
                                if (valHorn < 0.5)
                                    valHorn *= 2;
                                else
                                    valHorn = (valHorn - 0.5f) * 2;
                            }
                            baseController.UpdateHorn(valHorn);
                            break;
                    }
                    IsChangeByNetwork = false;
                }
            }
        }
    }

    private void UpdateMUServerStateLeverChange(WorldTrain serverState, Levers lever, float value, WorldTrain previousServerState = null)
    {
        //Main.Log($"Train Multiple unit lever changed Guid: {serverState.Guid}");
        if (serverState != null)
            UpdateServerStateLeverChange(serverState, lever, value);

        MultipleUnit multipleUnit = null;
        switch (serverState.CarType)
        {
            case TrainCarType.LocoShunter:
                multipleUnit = serverState.MultipleUnit;
                break;
            case TrainCarType.LocoDiesel:
                multipleUnit = serverState.MultipleUnit;
                break;
        }

        if (multipleUnit == null)
            return;

        if (multipleUnit.IsFrontMUConnectedTo != "" && (previousServerState == null || multipleUnit.IsFrontMUConnectedTo != previousServerState.Guid))
        {
            UpdateMUServerStateLeverChange(serverCarStates.FirstOrDefault(t => t.Guid == multipleUnit.IsFrontMUConnectedTo), lever, value, serverState);
        }

        if (multipleUnit.IsRearMUConnectedTo != "" && (previousServerState == null || multipleUnit.IsRearMUConnectedTo != previousServerState.Guid))
        {
            UpdateMUServerStateLeverChange(serverCarStates.FirstOrDefault(t => t.Guid == multipleUnit.IsRearMUConnectedTo), lever, value, serverState);
        }
    }

    private void OnCarCoupleChangeMessage(Message message, bool isCoupled)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarCoupleChangeMessage, message, isCoupled))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            //if (reader.Length % 30 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed lever update packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                TrainCouplingChange coupled = reader.ReadSerializable<TrainCouplingChange>();
                TrainCar trainCoupler1 = localCars.FirstOrDefault(t => t.CarGUID == coupled.TrainIdC1);
                TrainCar trainCoupler2 = localCars.FirstOrDefault(t => t.CarGUID == coupled.TrainIdC2);
                if (trainCoupler1 && trainCoupler2)
                {
                    WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == trainCoupler1.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler1.CarGUID,
                        };
                        switch (trainCoupler1.carType)
                        {
                            case TrainCarType.LocoShunter:
                                train.Shunter = new Shunter();
                                break;
                            case TrainCarType.LocoDiesel:
                                train.Diesel = new Diesel();
                                break;
                            case TrainCarType.LocoSteamHeavy:
                            case TrainCarType.LocoSteamHeavyBlue:
                                train.Steamer = new Steamer();
                                break;
                        }
                        serverCarStates.Add(train);
                    }
                    if (train != null)
                    {
                        if (isCoupled)
                        {
                            if (coupled.IsC1Front)
                                train.FrontCouplerCoupledTo = coupled.TrainIdC2;
                            else
                                train.RearCouplerCoupledTo = coupled.TrainIdC2;
                        }
                        else
                        {
                            if (coupled.IsC1Front)
                                train.FrontCouplerCoupledTo = "";
                            else
                                train.RearCouplerCoupledTo = "";
                        }
                    }
                    train = serverCarStates.FirstOrDefault(t => t.Guid == trainCoupler2.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler2.CarGUID,
                        };
                        switch (trainCoupler2.carType)
                        {
                            case TrainCarType.LocoShunter:
                                train.Shunter = new Shunter();
                                break;
                            case TrainCarType.LocoDiesel:
                                train.Diesel = new Diesel();
                                break;
                            case TrainCarType.LocoSteamHeavy:
                            case TrainCarType.LocoSteamHeavyBlue:
                                train.Steamer = new Steamer();
                                break;
                        }
                    }
                    if (train != null)
                    {
                        if (isCoupled)
                        {
                            if (coupled.IsC2Front)
                                train.FrontCouplerCoupledTo = coupled.TrainIdC1;
                            else
                                train.RearCouplerCoupledTo = coupled.TrainIdC1;
                        }
                        else
                        {
                            if (coupled.IsC2Front)
                                train.FrontCouplerCoupledTo = "";
                            else
                                train.RearCouplerCoupledTo = "";
                        }
                    }

                    Main.Log($"[CLIENT] < TRAIN_COUPLE: Packet size: {reader.Length}, TrainID_C1: {trainCoupler1.ID} (isFront: {coupled.IsC1Front}), TrainID_C2: {trainCoupler2.ID} (isFront: {coupled.IsC2Front})");
                    Coupler C1 = coupled.IsC1Front ? trainCoupler1.frontCoupler : trainCoupler1.rearCoupler;
                    Coupler C2 = coupled.IsC2Front ? trainCoupler2.frontCoupler : trainCoupler2.rearCoupler;

                    if (C1.GetFirstCouplerInRange() == C2 && isCoupled)
                    {
                        IsChangeByNetwork = true;
                        C1.TryCouple(viaChainInteraction: coupled.ViaChainInteraction);
                        IsChangeByNetwork = false;
                    }
                    else if (C1.coupledTo == C2 && !isCoupled)
                    {
                        IsChangeByNetwork = true;
                        C1.Uncouple(viaChainInteraction: coupled.ViaChainInteraction);
                        IsChangeByNetwork = false;
                    }
                    else if (C1.coupledTo != C2 && !isCoupled)
                    {
                        Main.Log($"[CLIENT] < TRAIN_COUPLE: Couplers were already uncoupled");
                    }
                }
                else
                {
                    Main.Log($"[CLIENT] < TRAIN_COUPLE: Trains not found, TrainID_C1: {coupled.TrainIdC1}, TrainID_C2: {coupled.TrainIdC2}");
                }
            }
        }
    }

    private void OnCarCouplerCockChangeMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarCouplerCockChangeMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainCouplerCockChange cockChange = reader.ReadSerializable<TrainCouplerCockChange>();
                TrainCar trainCoupler = localCars.FirstOrDefault(t => t.CarGUID == cockChange.TrainIdCoupler);

                if (trainCoupler)
                {
                    WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == cockChange.TrainIdCoupler);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler.CarGUID,
                        };
                        switch (trainCoupler.carType)
                        {
                            case TrainCarType.LocoShunter:
                                train.Shunter = new Shunter();
                                break;
                            case TrainCarType.LocoDiesel:
                                train.Diesel = new Diesel();
                                break;
                            case TrainCarType.LocoSteamHeavy:
                            case TrainCarType.LocoSteamHeavyBlue:
                                train.Steamer = new Steamer();
                                break;
                        }
                        serverCarStates.Add(train);
                    }

                    if (cockChange.IsCouplerFront)
                        train.IsFrontCouplerCockOpen = cockChange.IsOpen;
                    else
                        train.IsRearCouplerCockOpen = cockChange.IsOpen;
                    IsChangeByNetwork = true;
                    Main.Log($"[CLIENT] < TRAIN_COUPLE_COCK: Packet size: {reader.Length}, TrainID: {trainCoupler.ID} (isFront: {cockChange.IsCouplerFront}), isOpen: {cockChange.IsOpen}");
                    Coupler coupler = cockChange.IsCouplerFront ? trainCoupler.frontCoupler : trainCoupler.rearCoupler;
                    coupler.IsCockOpen = cockChange.IsOpen;
                    IsChangeByNetwork = false;
                }
                else
                {
                    Main.Log($"[CLIENT] < TRAIN_COUPLE_COCK: Trains not found, TrainID: {cockChange.TrainIdCoupler}, isOpen: {cockChange.IsOpen}");
                }
            }
        }
    }

    private void OnCarCouplerHoseChangeMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarCouplerHoseChangeMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainCouplerHoseChange hoseChange = reader.ReadSerializable<TrainCouplerHoseChange>();
                TrainCar trainCoupler1 = localCars.FirstOrDefault(t => t.CarGUID == hoseChange.TrainIdC1);
                TrainCar trainCoupler2 = null;
                if (hoseChange.IsConnected)
                    trainCoupler2 = localCars.FirstOrDefault(t => t.CarGUID == hoseChange.TrainIdC2);

                if (trainCoupler1 && trainCoupler2)
                {
                    WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == trainCoupler1.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler1.CarGUID,
                        };
                        switch (trainCoupler1.carType)
                        {
                            case TrainCarType.LocoShunter:
                                train.Shunter = new Shunter();
                                break;
                            case TrainCarType.LocoDiesel:
                                train.Diesel = new Diesel();
                                break;
                            case TrainCarType.LocoSteamHeavy:
                            case TrainCarType.LocoSteamHeavyBlue:
                                train.Steamer = new Steamer();
                                break;
                        }
                        serverCarStates.Add(train);
                    }
                    if (hoseChange.IsC1Front)
                        train.FrontCouplerHoseConnectedTo = hoseChange.TrainIdC2;
                    else
                        train.RearCouplerHoseConnectedTo = hoseChange.TrainIdC2;
                    train = serverCarStates.FirstOrDefault(t => t.Guid == trainCoupler2.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler2.CarGUID,
                        };
                        switch (trainCoupler2.carType)
                        {
                            case TrainCarType.LocoShunter:
                                train.Shunter = new Shunter();
                                break;
                            case TrainCarType.LocoDiesel:
                                train.Diesel = new Diesel();
                                break;
                            case TrainCarType.LocoSteamHeavy:
                            case TrainCarType.LocoSteamHeavyBlue:
                                train.Steamer = new Steamer();
                                break;
                        }
                        serverCarStates.Add(train);
                    }
                    if (hoseChange.IsC2Front)
                        train.FrontCouplerHoseConnectedTo = hoseChange.TrainIdC1;
                    else
                        train.RearCouplerHoseConnectedTo = hoseChange.TrainIdC1;
                    Main.Log($"[CLIENT] < TRAIN_COUPLE_HOSE: Packet size: {reader.Length}, TrainID_C1: {trainCoupler1.ID} (isFront: {hoseChange.IsC1Front}), TrainID_C2: {trainCoupler2.ID} (isFront: {hoseChange.IsC2Front}), HoseConnected: {hoseChange.IsConnected}");
                    Coupler C1 = hoseChange.IsC1Front ? trainCoupler1.frontCoupler : trainCoupler1.rearCoupler;
                    Coupler C2 = hoseChange.IsC2Front ? trainCoupler2.frontCoupler : trainCoupler2.rearCoupler;

                    if ((C1.IsCoupled() && C1.coupledTo == C2) || C1.GetFirstCouplerInRange() == C2)
                    {
                        IsChangeByNetwork = true;
                        C1.ConnectAirHose(C2, true);
                        IsChangeByNetwork = false;
                    }
                }
                else if (trainCoupler1 && !hoseChange.IsConnected)
                {
                    WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == trainCoupler1.CarGUID);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = trainCoupler1.CarGUID,
                        };
                        switch (trainCoupler1.carType)
                        {
                            case TrainCarType.LocoShunter:
                                train.Shunter = new Shunter();
                                break;
                            case TrainCarType.LocoDiesel:
                                train.Diesel = new Diesel();
                                break;
                            case TrainCarType.LocoSteamHeavy:
                            case TrainCarType.LocoSteamHeavyBlue:
                                train.Steamer = new Steamer();
                                break;
                        }
                        serverCarStates.Add(train);
                    }
                    if (hoseChange.IsC1Front)
                        train.FrontCouplerHoseConnectedTo = "";
                    else
                        train.RearCouplerHoseConnectedTo = "";

                    Main.Log($"[CLIENT] < TRAIN_COUPLE_HOSE: TrainID: {trainCoupler1.ID} (isFront: {hoseChange.IsC1Front}), HoseConnected: {hoseChange.IsConnected}");
                    Coupler C1 = hoseChange.IsC1Front ? trainCoupler1.frontCoupler : trainCoupler1.rearCoupler;
                    C1.DisconnectAirHose(true);
                }
                else
                {
                    Main.Log($"[CLIENT] < TRAIN_COUPLE: Trains not found, TrainID_C1: {hoseChange.TrainIdC1}, TrainID_C2: {hoseChange.TrainIdC2}, IsConnected: {hoseChange.IsConnected}");
                }
            }
        }
    }

    private void OnAuthorityChangeMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnAuthorityChangeMessage, message))
            return;
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                CarsAuthChange authChange = reader.ReadSerializable<CarsAuthChange>();
                Main.Log($"[CLIENT] < TRAIN_AUTH_CHANGE: Train: {authChange.Guids[0]}, PlayerId: {authChange.PlayerId}");
                foreach(string guid in authChange.Guids)
                {
                    WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == guid);
                    if (train != null)
                    {
                        train.AuthorityPlayerId = authChange.PlayerId;
                    }
                }
            }
        }
    }

    private void OnCargoChangeMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCargoChangeMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                TrainCargoChanged data = reader.ReadSerializable<TrainCargoChanged>();
                Main.Log($"[CLIENT] < TRAIN_CARGO_CHANGE: Car: {data.Id} {(data.IsLoading ? $"Loaded {data.Type.GetCargoName()}" : "Unloaded")}");
                WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == data.Id);
                if(train != null)
                {
                    train.CargoType = data.Type;
                    train.CargoAmount = data.Amount;
                }

                TrainCar car = localCars.FirstOrDefault(t => t.CarGUID == data.Id);
                if (car)
                {

                    IsChangeByNetwork = true;
                    WarehouseMachineController warehouse = WarehouseMachineController.allControllers.FirstOrDefault(w => w.warehouseMachine.ID == data.WarehouseId);
                    if (data.IsLoading)
                        car.logicCar.LoadCargo(data.Amount, data.Type, warehouse.warehouseMachine);
                    else
                        car.logicCar.UnloadCargo(car.logicCar.LoadedCargoAmount, car.logicCar.CurrentCargoTypeInCar, warehouse.warehouseMachine);
                    IsChangeByNetwork = false;
                }
            }
        }
    }

    private void OnCarMUChangeMessage(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, OnCarMUChangeMessage, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                CarMUChange data = reader.ReadSerializable<CarMUChange>();
                Main.Log($"[CLIENT] < TRAIN_MU_CHANGE: Car: {data.TrainId1} {(data.Train1IsFront ? "Front" : "Back")} MU {(data.IsConnected ? "Connected" : "Disconnected")}");
                UpdateMUServerState(data);

                IsChangeByNetwork = true;
                TrainCar car1 = localCars.FirstOrDefault(t => t.CarGUID == data.TrainId1);
                if (data.IsConnected)
                {
                    TrainCar car2 = localCars.FirstOrDefault(t => t.CarGUID == data.TrainId2);
                    MultipleUnitCable carCable1;
                    MultipleUnitCable carCable2;
                    if (data.Train1IsFront)
                        carCable1 = car1.GetComponent<MultipleUnitModule>().frontCableAdapter.muCable;
                    else
                        carCable1 = car1.GetComponent<MultipleUnitModule>().rearCableAdapter.muCable;

                    if (data.Train2IsFront)
                        carCable2 = car2.GetComponent<MultipleUnitModule>().frontCableAdapter.muCable;
                    else
                        carCable2 = car2.GetComponent<MultipleUnitModule>().rearCableAdapter.muCable;

                    carCable1.Connect(carCable2, data.AudioPlayed);
                }
                else
                {
                    if (data.Train1IsFront)
                        car1.GetComponent<MultipleUnitModule>().frontCableAdapter.muCable.Disconnect(data.AudioPlayed);
                    else
                        car1.GetComponent<MultipleUnitModule>().rearCableAdapter.muCable.Disconnect(data.AudioPlayed);
                }
                IsChangeByNetwork = false;
            }
        }
    }

    private void UpdateMUServerState(CarMUChange data)
    {
        WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == data.TrainId1);
        if (!(train is null))
        {
            string value = "";
            if (data.IsConnected)
                value = data.TrainId2;

            if (train.CarType == TrainCarType.LocoShunter)
            {
                if (data.Train1IsFront)
                    train.MultipleUnit.IsFrontMUConnectedTo = value;
                else
                    train.MultipleUnit.IsRearMUConnectedTo = value;
            }
            else if (train.CarType == TrainCarType.LocoDiesel)
            {
                if (data.Train1IsFront)
                    train.MultipleUnit.IsFrontMUConnectedTo = value;
                else
                    train.MultipleUnit.IsRearMUConnectedTo = value;
            }
        }

        if (data.IsConnected)
        {
            train = serverCarStates.FirstOrDefault(t => t.Guid == data.TrainId2);
            if (!(train is null))
            {
                string value = "";
                if (data.IsConnected)
                    value = data.TrainId1;

                if (train.CarType == TrainCarType.LocoShunter)
                {
                    if (data.Train1IsFront)
                        train.MultipleUnit.IsFrontMUConnectedTo = value;
                    else
                        train.MultipleUnit.IsRearMUConnectedTo = value;
                }
                else if (train.CarType == TrainCarType.LocoDiesel)
                {
                    if (data.Train1IsFront)
                        train.MultipleUnit.IsFrontMUConnectedTo = value;
                    else
                        train.MultipleUnit.IsRearMUConnectedTo = value;
                }
            }
        }
    }
    #endregion

    #region Sending Messages
    private void SendNewTrainsInitializationFinished()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(true);
            Main.Log("[CLIENT] > TRAINS_INIT_FINISHED");
            using (Message message = Message.Create((ushort)NetworkTags.TRAINS_INIT_FINISHED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendCarDamaged(string carGUID, DamageType type, float amount, string data)
    {
        if (!IsSynced)
            return;

        WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == carGUID);
        UpdateServerStateDamage(serverState, type, amount, data);

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new CarDamage()
            {
                Guid = carGUID,
                DamageType = type,
                NewHealth = amount,
                Data = data
            });
            Main.Log($"[CLIENT] > TRAIN_DAMAGE");
            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_DAMAGE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendCarBeingRemoved(TrainCar car)
    {
        if (!IsSynced)
            return;

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new CarRemoval()
            {
                Guid = car.CarGUID
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_REMOVAL, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendNewCarSpawned(TrainCar car)
    {
        SendNewCarsSpawned(new TrainCar[] { car });
    }

    private void SendNewCarsSpawned(IEnumerable<TrainCar> cars)
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            foreach (TrainCar car in cars)
            {
                AddNetworkingScripts(car, null);
            }

            WorldTrain[] newServerTrains = GenerateServerCarsData(cars);
            serverCarStates.AddRange(newServerTrains);
            localCars.AddRange(cars);
            writer.Write(newServerTrains);
            Main.Log($"[CLIENT] > TRAINS_INIT: {newServerTrains.Length}");

            using (Message message = Message.Create((ushort)NetworkTags.TRAINS_INIT, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendInitializedCars()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            serverCarStates.Clear();
            Main.Log($"Host synching trains with server. Train amount: {localCars.Count}");
            serverCarStates.AddRange(GenerateServerCarsData(localCars));

            Main.Log($"[CLIENT] > TRAIN_HOSTSYNC: AmountOfTrains: {serverCarStates.Count}");
            writer.Write(serverCarStates.ToArray());

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_HOST_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
        IsSynced = true;
    }

    internal void SendRerailCarUpdate(TrainCar trainCar)
    {
        if (!IsSynced)
            return;

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            Bogie bogie1 = trainCar.Bogies[0];
            Bogie bogie2 = trainCar.Bogies[trainCar.Bogies.Length - 1];
            float cargoDmg = 0;
            if (!trainCar.IsLoco)
                cargoDmg = trainCar.CargoDamage.currentHealth;

            TrainRerail data = new TrainRerail()
            {
                Guid = trainCar.CarGUID,
                Position = trainCar.transform.position - WorldMover.currentMove,
                Forward = trainCar.transform.forward,
                Rotation = trainCar.transform.rotation,
                Bogie1TrackName = (bogie1.track.name == "Turntable Track" ? "" : bogie1.track.name),
                Bogie2TrackName = (bogie2.track.name == "Turntable Track" ? "" : bogie2.track.name),
                Bogie1PositionAlongTrack = bogie1.traveller.pointRelativeSpan + bogie1.traveller.curPoint.span,
                Bogie2PositionAlongTrack = bogie2.traveller.pointRelativeSpan + bogie2.traveller.curPoint.span,
                CarHealth = trainCar.CarDamage.currentHealth,
                CargoHealth = cargoDmg
            };

            IsChangeByNetwork = true;
            WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == trainCar.CarGUID);
            if (serverState != null)
            {
                serverState.Position = data.Position;
                serverState.Rotation = data.Rotation;
                serverState.Forward = data.Forward;
                serverState.Bogies[0] = new TrainBogie()
                {
                    TrackName = data.Bogie1TrackName,
                    PositionAlongTrack = data.Bogie1PositionAlongTrack,
                    Derailed = false
                };
                serverState.Bogies[serverState.Bogies.Length - 1] = new TrainBogie()
                {
                    TrackName = data.Bogie2TrackName,
                    PositionAlongTrack = data.Bogie2PositionAlongTrack,
                    Derailed = false
                };
                serverState.CarHealth = data.CarHealth;
                if (!serverState.IsLoco)
                    serverState.CargoHealth = data.CargoHealth;
                else
                {
                    serverState.Throttle = 0;
                    serverState.Sander = 0;
                    serverState.Brake = 0;
                    serverState.IndepBrake = 1;
                    serverState.Reverser = 0f;
                    if (serverState.Shunter != null)
                    {
                        serverState.Shunter.IsEngineOn = false;
                        serverState.Shunter.IsMainFuseOn = false;
                        serverState.Shunter.IsSideFuse1On = false;
                        serverState.Shunter.IsSideFuse2On = false;
                    }
                    SyncLocomotiveWithServerState(trainCar, serverState);
                }
            }
            trainCar.GetComponent<NetworkTrainPosSync>().isDerailed = true;
            IsChangeByNetwork = false;

            writer.Write(data);

            Main.Log($"[CLIENT] > TRAIN_RERAIL: ID: {trainCar.CarGUID}");

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_RERAIL, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendDerailCarUpdate(TrainCar trainCar)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TRAIN_DERAIL: ID: {trainCar.ID}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            Bogie bogie1 = trainCar.Bogies[0];
            Bogie bogie2 = trainCar.Bogies[trainCar.Bogies.Length - 1];
            float cargoDmg = 0;
            if (!trainCar.IsLoco)
                cargoDmg = trainCar.CargoDamage.currentHealth;

            trainCar.GetComponent<NetworkTrainPosSync>().isDerailed = true;
            writer.Write(new TrainDerail()
            {
                TrainId = trainCar.CarGUID,
                IsBogie1Derailed = bogie1.HasDerailed,
                IsBogie2Derailed = bogie2.HasDerailed,
                Bogie1TrackName = bogie1.HasDerailed ? "" : bogie1.track.name,
                Bogie2TrackName = bogie2.HasDerailed ? "" : bogie2.track.name,
                Bogie1PositionAlongTrack = bogie1.HasDerailed ? 0 : bogie1.traveller.pointRelativeSpan + bogie1.traveller.curPoint.span,
                Bogie2PositionAlongTrack = bogie2.HasDerailed ? 0 : bogie2.traveller.pointRelativeSpan + bogie2.traveller.curPoint.span,
                CarHealth = trainCar.CarDamage.currentHealth,
                CargoHealth = cargoDmg
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_DERAIL, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendCarLocationUpdate(TrainCar trainCar, bool reliable = false)
    {
        if (!IsSynced)
            return;

        //Main.Log($"[CLIENT] > TRAIN_LOCATION_UPDATE: TrainID: {trainCar.ID}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            List<TrainLocation> locations = new List<TrainLocation>();
            foreach(TrainCar car in trainCar.trainset.cars)
            {
                List<TrainBogie> bogies = new List<TrainBogie>();
                foreach(Bogie bogie in car.Bogies)
                {
                    bogies.Add(new TrainBogie()
                    {
                        TrackName = bogie.HasDerailed ? "" : bogie.track.name,
                        Derailed = bogie.HasDerailed,
                        PositionAlongTrack = bogie.HasDerailed ? 0 : bogie.traveller.pointRelativeSpan + bogie.traveller.curPoint.span,
                    });
                }

                TrainLocation loc = new TrainLocation()
                {
                    TrainId = car.CarGUID,
                    Forward = car.transform.forward,
                    Position = car.transform.position - WorldMover.currentMove,
                    Rotation = car.transform.rotation,
                    Bogies = bogies.ToArray(),
                    IsStationary = car.isStationary,
                    Velocity = car.rb.velocity,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                if (car.IsLoco)
                {
                    switch (car.carType)
                    {
                        case TrainCarType.LocoShunter:
                            LocoControllerShunter shunter = car.GetComponent<LocoControllerShunter>();
                            loc.Temperature = shunter.GetEngineTemp();
                            loc.RPM = shunter.GetEngineRPM();
                            break;
                    }
                }

                locations.Add(loc);
            }

            writer.Write(locations.ToArray());

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_LOCATION_UPDATE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, reliable ? SendMode.Reliable : SendMode.Unreliable);
        }
    }

    internal void SendNewLocoLeverValue(TrainCar train, Levers lever, float value)
    {
        if (!IsSynced)
            return;

        if (train.GetComponent<MultipleUnitModule>())
        {
            switch (lever)
            {
                case Levers.Brake:
                case Levers.IndependentBrake:
                case Levers.Reverser:
                case Levers.Sander:
                case Levers.Throttle:
                    UpdateMUServerStateLeverChange(serverCarStates.FirstOrDefault(t => t.Guid == train.CarGUID), lever, value);
                    break;

                default:
                    UpdateServerStateLeverChange(serverCarStates.FirstOrDefault(t => t.Guid == train.CarGUID), lever, value);
                    break;

            }
        }
        else
        {
            UpdateServerStateLeverChange(serverCarStates.FirstOrDefault(t => t.Guid == train.CarGUID), lever, value);
        }
        //Main.Log($"[CLIENT] > TRAIN_LEVER: TrainID: {train.ID}, Lever: {lever}, value: {value}");
        if (!train.IsLoco)
            return;

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<TrainLever>(new TrainLever()
            {
                TrainId = train.CarGUID,
                Lever = lever,
                Value = value
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_LEVER, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendPlayerCarChange(TrainCar train)
    {
        if (!IsSynced)
            return;

        if (train)
            Main.Log($"[CLIENT] > TRAIN_SWITCH: ID: {train.CarGUID}");
        else
            Main.Log($"[CLIENT] > TRAIN_SWITCH: Player left train");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            TrainCarChange val = null;
            if (train)
            {
                Bogie bogie1 = train.Bogies[0];
                Bogie bogie2 = train.Bogies[train.Bogies.Length - 1];
                val = new TrainCarChange()
                {
                    PlayerId = SingletonBehaviour<UnityClient>.Instance.ID,
                    TrainId = train.CarGUID
                };
            }
            else
            {
                val = new TrainCarChange()
                {
                    PlayerId = SingletonBehaviour<UnityClient>.Instance.ID,
                    TrainId = ""
                };
            }

            writer.Write<TrainCarChange>(val);

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_SWITCH, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendCarCoupledChange(Coupler thisCoupler, Coupler otherCoupler, bool viaChainInteraction, bool isCoupled)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TRAIN_COUPLE: Coupler_1: {thisCoupler.train.ID}, Coupler_2: {otherCoupler.train.ID}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<TrainCouplingChange>(new TrainCouplingChange()
            {
                TrainIdC1 = thisCoupler.train.CarGUID,
                IsC1Front = thisCoupler.isFrontCoupler,
                TrainIdC2 = otherCoupler.train.CarGUID,
                IsC2Front = otherCoupler.isFrontCoupler,
                ViaChainInteraction = viaChainInteraction
            });

            using (Message message = Message.Create(isCoupled ? (ushort)NetworkTags.TRAIN_COUPLE : (ushort)NetworkTags.TRAIN_UNCOUPLE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendInitCarsRequest()
    {
        IsSynced = false;
        Main.Log($"[CLIENT] > TRAIN_SYNC_ALL");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(true);

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_SYNC_ALL, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendCarCouplerCockChanged(Coupler coupler, bool isCockOpen)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TRAIN_COUPLE_COCK: Coupler: {coupler.train.ID}, isOpen: {isCockOpen}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<TrainCouplerCockChange>(new TrainCouplerCockChange()
            {
                TrainIdCoupler = coupler.train.CarGUID,
                IsCouplerFront = coupler.isFrontCoupler,
                IsOpen = isCockOpen
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_COUPLE_COCK, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendCarCouplerHoseConChanged(Coupler coupler, bool isConnected)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TRAIN_COUPLE_HOSE: Coupler: {coupler.train.ID}, IsConnected: {isConnected}");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            Coupler C2 = null;
            if (isConnected)
                C2 = coupler.GetAirHoseConnectedTo();

            writer.Write<TrainCouplerHoseChange>(new TrainCouplerHoseChange()
            {
                TrainIdC1 = coupler.train.CarGUID,
                IsC1Front = coupler.isFrontCoupler,
                TrainIdC2 = C2 != null ? C2.train.CarGUID : "",
                IsC2Front = C2 != null && C2.isFrontCoupler,
                IsConnected = isConnected
            });

            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_COUPLE_HOSE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void SendNewJobChainCars(List<TrainCar> trainCarsForJobChain)
    {
        
        SendNewCarsSpawned(trainCarsForJobChain);
        AppUtil.Instance.PauseGame();
        CustomUI.OpenPopup("Streaming", "New Area being loaded");
    }

    internal void SendAuthorityChange(Trainset set, ushort id)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TRAIN_AUTH_CHANGE: Train: {set.firstCar.CarGUID}, PlayerId: {id}");

        string[] carGuids = new string[set.cars.Count];
        for(int i = 0; i < set.cars.Count; i++)
        {
            WorldTrain train = serverCarStates.FirstOrDefault(t => t.Guid == set.cars[i].CarGUID);
            train.AuthorityPlayerId = id;
            carGuids[i] = set.cars[i].CarGUID;
        }

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new CarsAuthChange() { Guids = carGuids, PlayerId = id });
            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_AUTH_CHANGE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void SendCargoStateChange(string carId, float loadedCargoAmount, CargoType loadedCargo, string warehouseId, bool isLoaded)
    {
        Main.Log($"[CLIENT] > TRAIN_CARGO_CHANGE: Car: {carId} {(isLoaded ? $"Loaded {loadedCargo.GetCargoName()}" : "Unloaded")}");
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            if (warehouseId == null)
                warehouseId = "";

            writer.Write(new TrainCargoChanged() { Id = carId, Amount = loadedCargoAmount, Type = loadedCargo, WarehouseId = warehouseId, IsLoading = isLoaded });
            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_CARGO_CHANGE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void OnMUConnectionChanged(string carId, bool isFront, string otherCarId, bool isOtherFront, bool isConnected, bool isAudioPlayed)
    {
        Main.Log($"[CLIENT] > TRAIN_MU_CHANGE: Car: {carId} {(isFront ? "Front" : "Back")} MU {(isConnected ? "Connected" : "Disconnected")}");
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            CarMUChange data = new CarMUChange() { TrainId1 = carId, TrainId2 = otherCarId, Train1IsFront = isFront, Train2IsFront = isOtherFront, IsConnected = isConnected, AudioPlayed = isAudioPlayed };
            UpdateMUServerState(data);
            writer.Write(data);
            using (Message message = Message.Create((ushort)NetworkTags.TRAIN_MU_CHANGE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }
    #endregion

    #region Car Functions
    internal Vector3 CalculateWorldPosition(Vector3 position, Vector3 forward, float zBounds)
    {
        return position + forward * zBounds;
    }

    private IEnumerator ListenToTrainInputEvents(TrainCar car)
    {
        yield return new WaitUntil(() => car.IsInteriorLoaded);
        NetworkTrainSync trainSync = car.GetComponent<NetworkTrainSync>();
        trainSync.ListenToTrainInputEvents();
        trainSync.listenToLocalPlayerInputs = true;
    }

    private void ResyncCoupling(TrainCar train, WorldTrain serverState)
    {
        // Front coupler check
        if (serverState.FrontCouplerCoupledTo != "" && !train.frontCoupler.coupledTo && serverState.FrontCouplerHoseConnectedTo == serverState.FrontCouplerCoupledTo && serverState.IsFrontCouplerCockOpen)
        {
            var otherTrainServerState = serverCarStates.FirstOrDefault(t => t.Guid == serverState.FrontCouplerCoupledTo);
            var otherTrain = localCars.FirstOrDefault(t => t.CarGUID == serverState.FrontCouplerCoupledTo);
            Coupler coupler = null;
            if (otherTrain && otherTrainServerState != null && otherTrainServerState.FrontCouplerCoupledTo == serverState.Guid)
                coupler = otherTrain.frontCoupler;

            if (otherTrain && otherTrainServerState != null && otherTrainServerState.RearCouplerCoupledTo == serverState.Guid)
                coupler = otherTrain.rearCoupler;

            if (coupler)
                train.frontCoupler.CoupleTo(coupler, false, false);
            else
                Main.Log($"Coupler not found of train {serverState.FrontCouplerCoupledTo}. Found serverState = {otherTrainServerState != null}. Found train = {otherTrain != null}");
        }
        else if(serverState.FrontCouplerCoupledTo != "" && !train.frontCoupler.coupledTo && (serverState.FrontCouplerHoseConnectedTo != serverState.FrontCouplerCoupledTo || !serverState.IsFrontCouplerCockOpen))
        {
            var otherTrainServerState = serverCarStates.FirstOrDefault(t => t.Guid == serverState.FrontCouplerCoupledTo);
            var otherTrain = localCars.FirstOrDefault(t => t.CarGUID == serverState.FrontCouplerCoupledTo);
            Coupler coupler = null;
            if (otherTrain && otherTrainServerState != null && otherTrainServerState.FrontCouplerCoupledTo == serverState.Guid)
                coupler = otherTrain.frontCoupler;

            if (otherTrain && otherTrainServerState != null && otherTrainServerState.RearCouplerCoupledTo == serverState.Guid)
                coupler = otherTrain.rearCoupler;

            if (coupler)
                train.frontCoupler.CoupleTo(coupler, false, true);
            else
                Main.Log($"Coupler not found of train {serverState.FrontCouplerCoupledTo}. Found serverState = {otherTrainServerState != null}. Found train = {otherTrain != null}");
        }
        
        if(serverState.FrontCouplerHoseConnectedTo != "" || serverState.IsFrontCouplerCockOpen)
        {
            if (serverState.IsFrontCouplerCockOpen && !train.frontCoupler.IsCockOpen)
                train.frontCoupler.IsCockOpen = true;

            if (serverState.FrontCouplerHoseConnectedTo != "" && !train.frontCoupler.GetAirHoseConnectedTo())
            {
                var otherTrainServerState = serverCarStates.FirstOrDefault(t => t.Guid == serverState.FrontCouplerHoseConnectedTo);
                var otherTrain = localCars.FirstOrDefault(t => t.CarGUID == serverState.FrontCouplerHoseConnectedTo);
                Coupler coupler = null;
                if (otherTrain && otherTrainServerState != null && otherTrainServerState.FrontCouplerHoseConnectedTo == serverState.Guid)
                    coupler = otherTrain.frontCoupler;

                if (otherTrain && otherTrainServerState != null && otherTrainServerState.RearCouplerHoseConnectedTo == serverState.Guid)
                    coupler = otherTrain.rearCoupler;

                if (coupler)
                    train.frontCoupler.ConnectAirHose(coupler, false);
                else
                    Main.Log($"Coupler not found of train {serverState.FrontCouplerHoseConnectedTo}. Found serverState = {otherTrainServerState != null}. Found train = {otherTrain != null}");
            }
        }

        // Rear coupler check
        if (serverState.RearCouplerCoupledTo != "" && !train.rearCoupler.coupledTo && serverState.RearCouplerHoseConnectedTo == serverState.RearCouplerCoupledTo && serverState.IsRearCouplerCockOpen)
        {
            var otherTrainServerState = serverCarStates.FirstOrDefault(t => t.Guid == serverState.RearCouplerCoupledTo);
            var otherTrain = localCars.FirstOrDefault(t => t.CarGUID == serverState.RearCouplerCoupledTo);
            Coupler coupler = null;
            if (otherTrain && otherTrainServerState != null && otherTrainServerState.FrontCouplerCoupledTo == serverState.Guid)
                coupler = otherTrain.frontCoupler;

            if (otherTrain && otherTrainServerState != null && otherTrainServerState.RearCouplerCoupledTo == serverState.Guid)
                coupler = otherTrain.rearCoupler;

            if (coupler)
                train.rearCoupler.CoupleTo(coupler, false, false);
            else
                Main.Log($"Coupler not found of train {serverState.RearCouplerCoupledTo}. Found serverState = {otherTrainServerState != null}. Found train = {otherTrain != null}");
        }
        else if (serverState.RearCouplerCoupledTo != "" && !train.rearCoupler.coupledTo && (serverState.RearCouplerHoseConnectedTo != serverState.RearCouplerCoupledTo || !serverState.IsRearCouplerCockOpen))
        {
            var otherTrainServerState = serverCarStates.FirstOrDefault(t => t.Guid == serverState.RearCouplerCoupledTo);
            var otherTrain = localCars.FirstOrDefault(t => t.CarGUID == serverState.RearCouplerCoupledTo);
            Coupler coupler = null;
            if (otherTrain && otherTrainServerState != null && otherTrainServerState.FrontCouplerCoupledTo == serverState.Guid)
                coupler = otherTrain.frontCoupler;

            if (otherTrain && otherTrainServerState != null && otherTrainServerState.RearCouplerCoupledTo == serverState.Guid)
                coupler = otherTrain.rearCoupler;

            if (coupler)
                train.rearCoupler.CoupleTo(coupler, false, true);
            else
                Main.Log($"Coupler not found of train {serverState.RearCouplerCoupledTo}. Found serverState = {otherTrainServerState != null}. Found train = {otherTrain != null}");
        }

        if (serverState.RearCouplerHoseConnectedTo != "" || serverState.IsRearCouplerCockOpen)
        {
            if (serverState.IsRearCouplerCockOpen && !train.rearCoupler.IsCockOpen)
                train.rearCoupler.IsCockOpen = true;

            if (serverState.RearCouplerHoseConnectedTo != "" && !train.rearCoupler.GetAirHoseConnectedTo())
            {
                var otherTrainServerState = serverCarStates.FirstOrDefault(t => t.Guid == serverState.RearCouplerHoseConnectedTo);
                var otherTrain = localCars.FirstOrDefault(t => t.CarGUID == serverState.RearCouplerHoseConnectedTo);
                Coupler coupler = null;
                if (otherTrain && otherTrainServerState != null && otherTrainServerState.FrontCouplerHoseConnectedTo == serverState.Guid)
                    coupler = otherTrain.frontCoupler;

                if (otherTrain && otherTrainServerState != null && otherTrainServerState.RearCouplerHoseConnectedTo == serverState.Guid)
                    coupler = otherTrain.rearCoupler;

                if (coupler)
                    train.rearCoupler.ConnectAirHose(coupler, false);
                else
                    Main.Log($"Coupler not found of train {serverState.RearCouplerHoseConnectedTo}. Found serverState = {otherTrainServerState != null}. Found train = {otherTrain != null}");
            }
        }

        // MU check
        if (serverState.CarType == TrainCarType.LocoShunter)
        {
            if(serverState.MultipleUnit.IsFrontMUConnectedTo != "")
            {
                TrainCar car2 = localCars.FirstOrDefault(t => t.CarGUID == serverState.MultipleUnit.IsFrontMUConnectedTo);
                WorldTrain worldTrain = serverCarStates.FirstOrDefault(t => t.Guid == serverState.MultipleUnit.IsFrontMUConnectedTo);
                if(worldTrain.CarType == TrainCarType.LocoShunter || worldTrain.CarType == TrainCarType.LocoDiesel)
                {
                    if(worldTrain.MultipleUnit.IsFrontMUConnectedTo == serverState.MultipleUnit.IsFrontMUConnectedTo)
                        train.GetComponent<MultipleUnitModule>().frontCableAdapter.muCable.Connect(car2.GetComponent<MultipleUnitModule>().frontCableAdapter.muCable, false);
                    else if(worldTrain.MultipleUnit.IsRearMUConnectedTo == serverState.MultipleUnit.IsFrontMUConnectedTo)
                        train.GetComponent<MultipleUnitModule>().frontCableAdapter.muCable.Connect(car2.GetComponent<MultipleUnitModule>().rearCableAdapter.muCable, false);
                }
            }

            if (serverState.MultipleUnit.IsRearMUConnectedTo != "")
            {
                TrainCar car2 = localCars.FirstOrDefault(t => t.CarGUID == serverState.MultipleUnit.IsRearMUConnectedTo);
                WorldTrain worldTrain = serverCarStates.FirstOrDefault(t => t.Guid == serverState.MultipleUnit.IsRearMUConnectedTo);
                if (worldTrain.CarType == TrainCarType.LocoShunter || worldTrain.CarType == TrainCarType.LocoDiesel)
                {
                    if (worldTrain.MultipleUnit.IsFrontMUConnectedTo == serverState.MultipleUnit.IsRearMUConnectedTo)
                        train.GetComponent<MultipleUnitModule>().rearCableAdapter.muCable.Connect(car2.GetComponent<MultipleUnitModule>().frontCableAdapter.muCable, false);
                    else if (worldTrain.MultipleUnit.IsRearMUConnectedTo == serverState.MultipleUnit.IsRearMUConnectedTo)
                        train.GetComponent<MultipleUnitModule>().rearCableAdapter.muCable.Connect(car2.GetComponent<MultipleUnitModule>().rearCableAdapter.muCable, false);
                }
            }
        }
        else if (serverState.CarType == TrainCarType.LocoDiesel)
        {
            if (serverState.MultipleUnit.IsFrontMUConnectedTo != "")
            {
                TrainCar car2 = localCars.FirstOrDefault(t => t.CarGUID == serverState.MultipleUnit.IsFrontMUConnectedTo);
                WorldTrain worldTrain = serverCarStates.FirstOrDefault(t => t.Guid == serverState.MultipleUnit.IsFrontMUConnectedTo);
                if (worldTrain.CarType == TrainCarType.LocoShunter || worldTrain.CarType == TrainCarType.LocoDiesel)
                {
                    if (worldTrain.MultipleUnit.IsFrontMUConnectedTo == serverState.MultipleUnit.IsFrontMUConnectedTo)
                        train.GetComponent<MultipleUnitModule>().frontCableAdapter.muCable.Connect(car2.GetComponent<MultipleUnitModule>().frontCableAdapter.muCable, false);
                    else if (worldTrain.MultipleUnit.IsRearMUConnectedTo == serverState.MultipleUnit.IsFrontMUConnectedTo)
                        train.GetComponent<MultipleUnitModule>().frontCableAdapter.muCable.Connect(car2.GetComponent<MultipleUnitModule>().rearCableAdapter.muCable, false);
                }
            }

            if (serverState.MultipleUnit.IsRearMUConnectedTo != "")
            {
                TrainCar car2 = localCars.FirstOrDefault(t => t.CarGUID == serverState.MultipleUnit.IsRearMUConnectedTo);
                WorldTrain worldTrain = serverCarStates.FirstOrDefault(t => t.Guid == serverState.MultipleUnit.IsRearMUConnectedTo);
                if (worldTrain.CarType == TrainCarType.LocoShunter || worldTrain.CarType == TrainCarType.LocoDiesel)
                {
                    if (worldTrain.MultipleUnit.IsFrontMUConnectedTo == serverState.MultipleUnit.IsRearMUConnectedTo)
                        train.GetComponent<MultipleUnitModule>().rearCableAdapter.muCable.Connect(car2.GetComponent<MultipleUnitModule>().frontCableAdapter.muCable, false);
                    else if (worldTrain.MultipleUnit.IsRearMUConnectedTo == serverState.MultipleUnit.IsRearMUConnectedTo)
                        train.GetComponent<MultipleUnitModule>().rearCableAdapter.muCable.Connect(car2.GetComponent<MultipleUnitModule>().rearCableAdapter.muCable, false);
                }
            }
        }
    }

    private IEnumerator FullResyncCar(TrainCar train, WorldTrain serverState)
    {
        Main.Log($"Train set derailed");
        bool isDerailed = train.derailed;
        Main.Log($"Train is derailed: {isDerailed}");
        if (train.Bogies != null && train.Bogies.Length >= 2 && serverState.Position != Vector3.zero)
        {
            Main.Log($"Train Bogies synching");
            Bogie bogie1 = train.Bogies[0];
            Bogie bogie2 = train.Bogies[train.Bogies.Length - 1];
            Main.Log($"Train bogies are set {bogie1 != null && bogie2 != null}");

            isDerailed = serverState.Bogies[0].Derailed || serverState.Bogies[serverState.Bogies.Length - 1].Derailed;
            Main.Log($"Train is derailed by bogies {isDerailed}");
            if (serverState.Bogies[0].Derailed && !bogie1.HasDerailed)
            {
                bogie1.Derail();
            }

            if (serverState.Bogies[serverState.Bogies.Length - 1].Derailed && !bogie2.HasDerailed)
            {
                bogie2.Derail();
            }
            Main.Log($"Train bogies synced");

            if (bogie1.HasDerailed || bogie2.HasDerailed)
            {
                Main.Log("Teleport train to derailed position");
                train.transform.position = serverState.Position + WorldMover.currentMove;
                train.transform.rotation = serverState.Rotation;
                Main.Log("Stop syncing rest of train since values will be reset at rerail");
                yield break;
            }
        }

        Main.Log($"Train repositioning sync: Pos: {serverState.Position.ToString("G3")}");
        if (serverState.Position != Vector3.zero && train.derailed && serverState.AuthorityPlayerId == SingletonBehaviour<UnityClient>.Instance.ID)
            yield return RerailDesynced(train, serverState.Position, serverState.Forward);
        else if(serverState.AuthorityPlayerId != SingletonBehaviour<UnityClient>.Instance.ID)
        {
            train.rb.isKinematic = true;
            train.rb.MovePosition(serverState.Position + WorldMover.currentMove);
            train.rb.MoveRotation(serverState.Rotation);
            foreach (Bogie bogie in train.Bogies)
                bogie.ResetBogiesToStartPosition();
        }
        else
        {
            SyncDamageWithServerState(train, serverState);
            SyncLocomotiveWithServerState(train, serverState);
        }

        Main.Log($"Train should be synced");
    }

    private IEnumerator SyncCarsFromServerState()
    {
        Main.Log($"Synching trains. Train amount: {serverCarStates.Count}");
        foreach (WorldTrain selectedTrain in serverCarStates)
        {
            IsChangeByNetwork = true;
            Main.Log($"Synching train: {selectedTrain.Guid}.");

            TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == selectedTrain.Guid);
            if (train == null)
            {
                train = InitializeNewTrainCar(selectedTrain);
                AddNetworkingScripts(train, selectedTrain);
            }

            if (train != null)
            {
                try
                {
                    if (train.frontCoupler.coupledTo)
                        train.frontCoupler.Uncouple(false);
                    if (train.rearCoupler.coupledTo)
                        train.rearCoupler.Uncouple(false);
                    
                }
                catch (Exception) { }
                yield return new WaitUntil(() => !train.frontCoupler.coupledTo && !train.rearCoupler.coupledTo);
                yield return FullResyncCar(train, selectedTrain);
            }
            IsChangeByNetwork = false;
        }

        foreach (WorldTrain selectedTrain in serverCarStates)
        {
            IsChangeByNetwork = true;
            Main.Log($"Synching train coupling: {selectedTrain.Guid}.");
            TrainCar train = localCars.FirstOrDefault(t => t.CarGUID == selectedTrain.Guid);
            ResyncCoupling(train, selectedTrain);
            IsChangeByNetwork = false;
        }
        IsSynced = true;
    }

    internal void RunBuffer()
    {
        buffer.RunBuffer();
    }

    private void SyncLocomotiveWithServerState(TrainCar train, WorldTrain serverState)
    {
        if (!train.IsLoco)
            return;

        Main.Log($"Train Loco generic sync");
        LocoControllerBase controller = train.GetComponent<LocoControllerBase>();
        Main.Log($"Train Loco controller found {controller != null}");
        if (controller != null)
        {
            controller.SetBrake(serverState.Brake);
            controller.SetIndependentBrake(serverState.IndepBrake);
            controller.SetSanders(serverState.Sander);
            controller.SetReverser(serverState.Reverser);
            controller.SetThrottle(serverState.Throttle);
        }

        Main.Log($"Train Loco specific sync");
        switch (serverState.CarType)
        {
            case TrainCarType.LocoShunter:
                Main.Log($"Train Loco is shunter");
                LocoControllerShunter controllerShunter = train.GetComponent<LocoControllerShunter>();
                Main.Log($"Train controller found {controllerShunter != null}");
                Shunter shunter = serverState.Shunter;
                Main.Log($"Train Loco Server data found {shunter != null}");
                if (shunter != null)
                {
                    Main.Log($"Sync engine on");
                    controllerShunter.SetEngineRunning(shunter.IsEngineOn);
                    if (train.IsInteriorLoaded && !shunter.IsEngineOn)
                    {
                        ShunterDashboardControls shunterDashboard = train.interior.GetComponentInChildren<ShunterDashboardControls>();
                        Main.Log($"Shunter dashboard found {shunterDashboard != null}");
                        Main.Log($"Sync engine fuse 1");
                        shunterDashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().SetValue(shunter.IsSideFuse1On ? 1 : 0);
                        Main.Log($"Sync engine fuse 2");
                        shunterDashboard.fuseBoxPowerController.sideFusesObj[1].GetComponent<ToggleSwitchBase>().SetValue(shunter.IsSideFuse2On ? 1 : 0);
                        Main.Log($"Sync engine main fuse");
                        shunterDashboard.fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().SetValue(shunter.IsMainFuseOn ? 1 : 0);
                    }
                }
                else
                {
                    serverState.Shunter = new Shunter();
                }
                break;
            case TrainCarType.LocoDiesel:
                Main.Log($"Train Loco is diesel");
                LocoControllerDiesel controllerDiesel = train.GetComponent<LocoControllerDiesel>();
                Main.Log($"Train controller found {controllerDiesel != null}");
                Diesel diesel = serverState.Diesel;
                Main.Log($"Train Loco Server data found {diesel != null}");
                if (diesel != null)
                {
                    Main.Log($"Sync engine on");
                    controllerDiesel.SetEngineRunning(diesel.IsEngineOn);
                    if (train.IsInteriorLoaded && !diesel.IsEngineOn)
                    {
                        DieselDashboardControls dieselDashboard = train.interior.GetComponentInChildren<DieselDashboardControls>();
                        Main.Log($"Shunter dashboard found {dieselDashboard != null}");
                        Main.Log($"Sync engine fuse 1");
                        dieselDashboard.fuseBoxPowerControllerDiesel.sideFusesObj[0].GetComponent<ToggleSwitchBase>().SetValue(diesel.IsSideFuse1On ? 1 : 0);
                        Main.Log($"Sync engine fuse 2");
                        dieselDashboard.fuseBoxPowerControllerDiesel.sideFusesObj[1].GetComponent<ToggleSwitchBase>().SetValue(diesel.IsSideFuse2On ? 1 : 0);
                        Main.Log($"Sync engine fuse 3");
                        dieselDashboard.fuseBoxPowerControllerDiesel.sideFusesObj[2].GetComponent<ToggleSwitchBase>().SetValue(diesel.IsSideFuse3On ? 1 : 0);
                        Main.Log($"Sync engine main fuse");
                        dieselDashboard.fuseBoxPowerControllerDiesel.mainFuseObj.GetComponent<ToggleSwitchBase>().SetValue(diesel.IsMainFuseOn ? 1 : 0);
                    }
                }
                else
                {
                    serverState.Diesel = new Diesel();
                }
                break;
            case TrainCarType.LocoSteamHeavy:
            case TrainCarType.LocoSteamHeavyBlue:
                Main.Log($"Train Loco is steamer");
                LocoControllerSteam controllerSteam = train.GetComponent<LocoControllerSteam>();
                Main.Log($"Train controller found {controllerSteam != null}");
                Steamer steamer = serverState.Steamer;
                if (steamer != null)
                {
                    Main.Log($"Sync fire on");
                    controllerSteam.SetFireOn(steamer.GetFireOn);
                }
                else
                {
                    serverState.Steamer = new Steamer();
                }
                break;
        }
    }

    private TrainCar InitializeNewTrainCar(WorldTrain serverState)
    {
        GameObject carPrefab = CarTypes.GetCarPrefab(serverState.CarType);
        TrainCar newTrain;
        TrainBogie bogie1 = serverState.Bogies[0];
        TrainBogie bogie2 = serverState.Bogies[serverState.Bogies.Length - 1];
        RailTrack track = RailTrack.GetClosest(serverState.Position + WorldMover.currentMove).track;
        newTrain = CarSpawner.SpawnLoadedCar(carPrefab, serverState.Id, serverState.Guid, serverState.IsPlayerSpawned, serverState.Position + WorldMover.currentMove, serverState.Rotation,
        bogie1.Derailed, track, bogie1.PositionAlongTrack,
        bogie2.Derailed, track, bogie2.PositionAlongTrack,
        false, false);
        foreach (Bogie bogie in newTrain.Bogies)
            bogie.ResetBogiesToStartPosition();
        newTrain.CarDamage.IgnoreDamage(true);
        newTrain.stress.EnableStress(false);
        newTrain.rb.isKinematic = true;
        newTrain.rb.MovePosition(serverState.Position + WorldMover.currentMove);
        newTrain.rb.MoveRotation(serverState.Rotation);
        foreach (Bogie bogie in newTrain.Bogies)
            bogie.ResetBogiesToStartPosition();

        if(newTrain.logicCar != null && !newTrain.IsLoco && serverState.CargoType != CargoType.None)
            newTrain.logicCar.LoadCargo(serverState.CargoAmount, serverState.CargoType);
        localCars.Add(newTrain);

        return newTrain;
    }

    internal IEnumerator RerailDesynced(TrainCar trainCar, WorldTrain train, bool resyncCoupling)
    {
        yield return RerailDesynced(trainCar, train.Position, train.Forward);
        if (resyncCoupling)
            try
            {
                ResyncCoupling(trainCar, train);
            }
            catch (Exception) { }
    }

    internal IEnumerator RerailDesynced(TrainCar trainCar, Vector3 pos, Vector3 fwd)
    {
        Main.Log("Train desynced and derailed");
        IsChangeByNetwork = true;
        RailTrack track = null;
        WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == trainCar.CarGUID);
        if (serverState != null && serverState.Bogies[0].TrackName != "")
            track = RailTrackRegistry.GetTrackWithName(serverState.Bogies[0].TrackName);
        else
            track = RailTrack.GetClosest(pos + WorldMover.currentMove).track;

        if(Vector3.Distance(track.transform.position - WorldMover.currentMove, pos) > 100)
        {
            track = RailTrack.GetClosest(pos + WorldMover.currentMove).track;
        }

        if (track)
        {
            if (!trainCar.derailed)
            {
                yield return trainCar.MoveToTrackWithCarUncoupleCoro(track, CalculateWorldPosition(pos + WorldMover.currentMove, fwd, trainCar.Bounds.center.z), fwd);
            }
            else
            {
                yield return trainCar.RerailCoro(track, CalculateWorldPosition(pos + WorldMover.currentMove, fwd, trainCar.Bounds.center.z), fwd);
                yield return new WaitUntil(() => !trainCar.derailed);
            }

            if (serverState != null)
            {
                SyncDamageWithServerState(trainCar, serverState);
                SyncLocomotiveWithServerState(trainCar, serverState);
            }
        }
        IsChangeByNetwork = false;
    }

    private void SyncDamageWithServerState(TrainCar trainCar, WorldTrain serverState)
    {   
        if (trainCar.IsLoco && trainCar.GetComponent<NetworkTrainPosSync>())
            if(serverState.CarHealthData != "")
                trainCar.GetComponent<NetworkTrainPosSync>().LoadLocoDamage(serverState.CarHealthData);
        else
            trainCar.CarDamage.LoadCarDamageState(serverState.CarHealth);
    }

    internal WorldTrain[] GenerateServerCarsData(IEnumerable<TrainCar> cars)
    {
        List<WorldTrain> data = new List<WorldTrain>();
        foreach(TrainCar car in cars)
        {
            Main.Log($"Get train bogies");
            Bogie bogie1 = car.Bogies[0];
            Bogie bogie2 = car.Bogies[car.Bogies.Length - 1];
            Main.Log($"Train bogies found: {bogie1 != null && bogie2 != null}");

            Main.Log($"Set train defaults");
            List<TrainBogie> bogies = new List<TrainBogie>();
            foreach (Bogie bogie in car.Bogies)
            {
                bogies.Add(new TrainBogie()
                {
                    TrackName = bogie.track.name,
                    Derailed = bogie.HasDerailed,
                    PositionAlongTrack = bogie.HasDerailed ? 0 : bogie.traveller.pointRelativeSpan + bogie.traveller.curPoint.span
                });
            }

            string carHealthData = "";
            if (car.IsLoco)
            {
                switch (car.carType)
                {
                    case TrainCarType.LocoShunter:
                        carHealthData = car.GetComponent<DamageControllerShunter>().GetDamageSaveData().ToString(Newtonsoft.Json.Formatting.None);
                        break;
                    case TrainCarType.LocoDiesel:
                        carHealthData = car.GetComponent<DamageControllerDiesel>().GetDamageSaveData().ToString(Newtonsoft.Json.Formatting.None);
                        break;
                    case TrainCarType.LocoSteamHeavy:
                    case TrainCarType.LocoSteamHeavyBlue:
                        carHealthData = car.GetComponent<DamageController>().GetDamageSaveData().ToString(Newtonsoft.Json.Formatting.None);
                        break;
                }
            }

            WorldTrain train = new WorldTrain()
            {
                Guid = car.CarGUID,
                Id = car.ID,
                CarType = car.carType,
                IsLoco = car.IsLoco,
                Position = car.transform.position - WorldMover.currentMove,
                Rotation = car.transform.rotation,
                Forward = car.transform.forward,
                Bogies = bogies.ToArray(),
                FrontCouplerCoupledTo = car.frontCoupler.coupledTo ? car.frontCoupler.coupledTo.train.CarGUID : "",
                IsFrontCouplerCockOpen = car.frontCoupler.IsCockOpen,
                FrontCouplerHoseConnectedTo = car.frontCoupler.GetAirHoseConnectedTo() ? car.frontCoupler.GetAirHoseConnectedTo().train.CarGUID : "",
                RearCouplerCoupledTo = car.rearCoupler.coupledTo ? car.rearCoupler.coupledTo.train.CarGUID : "",
                IsRearCouplerCockOpen = car.rearCoupler.IsCockOpen,
                RearCouplerHoseConnectedTo = car.rearCoupler.GetAirHoseConnectedTo() ? car.rearCoupler.GetAirHoseConnectedTo().train.CarGUID : "",
                IsPlayerSpawned = car.playerSpawnedCar,
                IsRemoved = false,
                IsStationary = true,
                CarHealth = car.CarDamage.currentHealth,
                CarHealthData = carHealthData
            };

            if (car.IsLoco && car.carType != TrainCarType.HandCar)
            {
                Main.Log($"Set locomotive defaults");
                LocoControllerBase loco = car.GetComponent<LocoControllerBase>();
                Main.Log($"Loco controller found: {loco != null}");
                train.Throttle = loco.throttle;
                Main.Log($"Throttle set: {train.Throttle}");
                train.Brake = loco.brake;
                Main.Log($"Brake set: {train.Brake}");
                train.IndepBrake = loco.independentBrake;
                Main.Log($"IndepBrake set: {train.IndepBrake}");
                train.Reverser = loco.reverser;
                Main.Log($"Reverser set: {train.Reverser}");
                train.Sander = loco.IsSandOn() ? 1 : 0;
                Main.Log($"Sander set: {train.Sander}");
            }
            else if(car.carType != TrainCarType.HandCar)
            {
                train.CargoType = car.LoadedCargo;
                train.CargoAmount = car.LoadedCargoAmount;
                train.CargoHealth = car.CargoDamage.currentHealth;
            }

            switch (car.carType)
            {
                case TrainCarType.LocoShunter:
                    Main.Log($"Set shunter defaults");
                    LocoControllerShunter shunter = car.GetComponent<LocoControllerShunter>();
                    Main.Log($"Shunter controller found: {shunter != null}");
                    if (car.IsInteriorLoaded)
                    {
                        ShunterDashboardControls dashboard = car.interior.GetComponentInChildren<ShunterDashboardControls>();
                        Main.Log($"Shunter dashboard found: {dashboard != null}");
                        train.Shunter = new Shunter()
                        {
                            IsMainFuseOn = dashboard.fuseBoxPowerController.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 1,
                            IsSideFuse1On = dashboard.fuseBoxPowerController.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Value == 1,
                            IsSideFuse2On = dashboard.fuseBoxPowerController.sideFusesObj[1].GetComponent<ToggleSwitchBase>().Value == 1
                        };
                    }
                    train.Shunter.IsEngineOn = shunter.GetEngineRunning();
                    Main.Log($"Shunter set: IsEngineOn: {train.Shunter.IsEngineOn}, IsMainFuseOn: {train.Shunter.IsMainFuseOn}, " +
                        $"IsSideFuse1On: {train.Shunter.IsSideFuse1On}, IsSideFuse2On: {train.Shunter.IsSideFuse2On}");
                    break;

                case TrainCarType.LocoDiesel:
                    Main.Log($"Set diesel defaults");
                    LocoControllerDiesel diesel = car.GetComponent<LocoControllerDiesel>();
                    Main.Log($"Diesel controller found: {diesel != null}");
                    if (car.IsInteriorLoaded)
                    {
                        DieselDashboardControls dashboard = car.interior.GetComponentInChildren<DieselDashboardControls>();
                        Main.Log($"Diesel dashboard found: {dashboard != null}");
                        train.Diesel = new Diesel()
                        {
                            IsMainFuseOn = dashboard.fuseBoxPowerControllerDiesel.mainFuseObj.GetComponent<ToggleSwitchBase>().Value == 1,
                            IsSideFuse1On = dashboard.fuseBoxPowerControllerDiesel.sideFusesObj[0].GetComponent<ToggleSwitchBase>().Value == 1,
                            IsSideFuse2On = dashboard.fuseBoxPowerControllerDiesel.sideFusesObj[1].GetComponent<ToggleSwitchBase>().Value == 1,
                            IsSideFuse3On = dashboard.fuseBoxPowerControllerDiesel.sideFusesObj[2].GetComponent<ToggleSwitchBase>().Value == 1
                        };
                    }
                    train.Diesel.IsEngineOn = diesel.GetEngineRunning();
                    Main.Log($"Diesel set: IsEngineOn: {train.Diesel.IsEngineOn}, IsMainFuseOn: {train.Diesel.IsMainFuseOn}, " +
                        $"IsSideFuse1On: {train.Diesel.IsSideFuse1On}, IsSideFuse2On: {train.Diesel.IsSideFuse2On}, " +
                        $"IsSideFuse3On: {train.Diesel.IsSideFuse3On}");
                    break;
                case TrainCarType.LocoSteamHeavy:
                case TrainCarType.LocoSteamHeavyBlue:
                    Main.Log($"Set steam defaults");
                    LocoControllerSteam steam = car.GetComponent<LocoControllerSteam>();
                    Main.Log($"Steam controller found: {steam != null}");
                    break;

            }

            data.Add(train);
        }
        return data.ToArray();
    }

    internal WorldTrain GetServerStateById(string guid)
    {
        return serverCarStates.FirstOrDefault(t => t.Guid == guid);
    }

    private IEnumerator SpawnSendedTrains(WorldTrain[] trains)
    {
        AppUtil.Instance.PauseGame();
        CustomUI.OpenPopup("Streaming", "New Area being loaded");
        yield return new WaitUntil(() => SingletonBehaviour<CanvasSpawner>.Instance.IsOpen);
        yield return new WaitForFixedUpdate();
        foreach (WorldTrain train in trains)
        {
            IsSpawningTrains = true;
            Main.Log($"Initializing: {train.Guid} in area");
            serverCarStates.Add(train);
            TrainCar car = InitializeNewTrainCar(train);
            yield return new WaitUntil(() => car.AreBogiesFullyInitialized());
            AddNetworkingScripts(car, train);
            Main.Log($"Initializing: {train.Guid} in area [DONE]");
        }
        yield return new WaitUntil(() =>
        {
            foreach (WorldTrain train in trains)
            {
                if (localCars.Any(t => t.logicCar == null))
                    return false;

                if (!localCars.Any(t => t.CarGUID == train.Guid && t.AreBogiesFullyInitialized()))
                    return false;
            }
            return true;
        });
        yield return SingletonBehaviour<FpsStabilityMeasurer>.Instance.WaitForStableFps();
        SendNewTrainsInitializationFinished();
    }

    private void UpdateServerStateLeverChange(WorldTrain serverState, Levers lever, float value)
    {
        switch (lever)
        {
            case Levers.Throttle:
                serverState.Throttle = value;
                break;

            case Levers.Brake:
                serverState.Brake = value;
                break;

            case Levers.IndependentBrake:
                serverState.IndepBrake = value;
                break;

            case Levers.Reverser:
                serverState.Reverser = value;
                break;

            case Levers.Sander:
                serverState.Sander = value;
                break;

            case Levers.SideFuse_1:
                if (serverState.CarType == TrainCarType.LocoShunter)
                {
                    serverState.Shunter.IsSideFuse1On = value == 1;
                    if (value == 0)
                    {
                        serverState.Shunter.IsMainFuseOn = false;
                        serverState.Shunter.IsEngineOn = false;
                    }
                }
                else if (serverState.CarType == TrainCarType.LocoDiesel)
                {
                    serverState.Diesel.IsSideFuse1On = value == 1;
                    if (value == 0)
                    {
                        serverState.Diesel.IsMainFuseOn = false;
                        serverState.Diesel.IsEngineOn = false;
                    }
                }
                break;

            case Levers.SideFuse_2:
                if (serverState.CarType == TrainCarType.LocoShunter)
                {
                    serverState.Shunter.IsSideFuse2On = value == 1;
                    if (value == 0)
                    {
                        serverState.Shunter.IsMainFuseOn = false;
                        serverState.Shunter.IsEngineOn = false;
                    }
                }
                else if (serverState.CarType == TrainCarType.LocoDiesel)
                {
                    serverState.Diesel.IsSideFuse2On = value == 1;
                    if (value == 0)
                    {
                        serverState.Diesel.IsMainFuseOn = false;
                        serverState.Diesel.IsEngineOn = false;
                    }
                }
                break;

            case Levers.SideFuse_3:
                if (serverState.CarType == TrainCarType.LocoDiesel)
                {
                    serverState.Diesel.IsSideFuse3On = value == 1;
                    if (value == 0)
                    {
                        serverState.Diesel.IsMainFuseOn = false;
                        serverState.Diesel.IsEngineOn = false;
                    }
                }
                break;

            case Levers.MainFuse:
                if (serverState.CarType == TrainCarType.LocoShunter)
                {
                    serverState.Shunter.IsMainFuseOn = value == 1;
                    if (value == 0)
                        serverState.Shunter.IsEngineOn = false;
                }
                else if(serverState.CarType == TrainCarType.LocoDiesel)
                {
                    serverState.Diesel.IsMainFuseOn = value == 1;
                    if (value == 0)
                        serverState.Diesel.IsEngineOn = false;
                }
                break;

            case Levers.FusePowerStarter:
                if (serverState.CarType == TrainCarType.LocoShunter)
                {
                    if (serverState.Shunter.IsSideFuse1On && serverState.Shunter.IsSideFuse2On && serverState.Shunter.IsMainFuseOn && value == 1)
                        serverState.Shunter.IsEngineOn = true;
                    else if (value == 0)
                        serverState.Shunter.IsEngineOn = false;
                }
                else if (serverState.CarType == TrainCarType.LocoDiesel)
                    {
                        if (serverState.Diesel.IsSideFuse1On && serverState.Diesel.IsSideFuse2On && serverState.Diesel.IsSideFuse3On 
                        && serverState.Diesel.IsMainFuseOn && value == 1)
                            serverState.Diesel.IsEngineOn = true;
                        else if (value == 0)
                            serverState.Diesel.IsEngineOn = false;
                    }
                break;
        }
    }

    private void UpdateServerStateDamage(WorldTrain serverState, DamageType type, float value, string data)
    {
        switch (type)
        {
            case DamageType.Car:
                serverState.CargoHealth = value;
                serverState.CarHealthData = data;
                break;

            case DamageType.Cargo:
                serverState.CargoHealth = value;
                break;
        }

        switch (serverState.CarType)
        {
            case TrainCarType.LocoShunter:
                serverState.Shunter.IsEngineOn = false;
                break;
        }
    }

    internal void ResyncCar(TrainCar trainCar)
    {
        IsChangeByNetwork = true;
        WorldTrain serverState = serverCarStates.FirstOrDefault(t => t.Guid == trainCar.CarGUID);
        if (serverState != null)
        {
            SyncDamageWithServerState(trainCar, serverState);
            SyncLocomotiveWithServerState(trainCar, serverState);
        }
        IsChangeByNetwork = false;
    }

    internal TrainCar GetAuthorityCar()
    {
        if (localCars != null && localCars.Count > 0)
            return localCars.FirstOrDefault(t => t.GetComponent<NetworkTrainPosSync>() && t.GetComponent<NetworkTrainPosSync>().hasLocalPlayerAuthority);
        else
            return null;
    }

    private void AddNetworkingScripts(TrainCar car, WorldTrain selectedTrain)
    {
        if (!car.GetComponent<NetworkTrainSync>() && car.IsLoco)
            car.gameObject.AddComponent<NetworkTrainSync>();

        if (!car.GetComponent<NetworkTrainMUSync>() && car.IsLoco && car.GetComponent<MultipleUnitModule>())
            car.gameObject.AddComponent<NetworkTrainMUSync>();

        if (!car.GetComponent<NetworkTrainPosSync>())
        {
            NetworkTrainPosSync s = car.gameObject.AddComponent<NetworkTrainPosSync>();
            s.serverState = selectedTrain;
        }

        if (!car.frontCoupler.GetComponent<NetworkTrainCouplerSync>())
            car.frontCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();

        if (!car.rearCoupler.GetComponent<NetworkTrainCouplerSync>())
            car.rearCoupler.gameObject.AddComponent<NetworkTrainCouplerSync>();
    }
    #endregion
}
