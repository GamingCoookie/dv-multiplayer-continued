using DarkRift;
using DarkRift.Server;
using DV.Logic.Job;
using DVMultiplayer.Darkrift;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using PlayerPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace TrainPlugin
{
    public class TrainPlugin : Plugin
    {
        public override bool ThreadSafe => false;

        public override Version Version => new Version("1.6.48");

        private readonly List<WorldTrain> worldTrains;
        private readonly List<IClient> playerHasInitializedTrain;
        private bool isLoadingTrain = false;
        private BufferQueue queue;

        public TrainPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            worldTrains = new List<WorldTrain>();
            queue = new BufferQueue();
            playerHasInitializedTrain = new List<IClient>();
            ClientManager.ClientConnected += OnClientConnected;
            ClientManager.ClientDisconnected += OnClientDisconnect;
        }

        private void OnClientDisconnect(object sender, ClientDisconnectedEventArgs e)
        {
            if (isLoadingTrain)
                CheckIfAllPlayersLoadedTrain();
        }

        private void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            e.Client.MessageReceived += OnMessageReceived;
            if (isLoadingTrain)
                playerHasInitializedTrain.Add(e.Client);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                NetworkTags tag = (NetworkTags)message.Tag;
                if (!tag.ToString().StartsWith("TRAIN"))
                    return;

                if (tag != NetworkTags.TRAIN_LOCATION_UPDATE)
                    Logger.Trace($"[SERVER] < {tag}");

                switch (tag)
                {
                    case NetworkTags.TRAIN_LEVER:
                        UpdateTrainLever(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_RERAIL:
                        UpdateTrainRerail(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_DERAIL:
                        UpdateTrainDerailed(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_SWITCH:
                        UpdateTrainSwitch(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_COUPLE:
                        UpdateCouplingState(message, e.Client, true);
                        break;

                    case NetworkTags.TRAIN_UNCOUPLE:
                        UpdateCouplingState(message, e.Client, false);
                        break;

                    case NetworkTags.TRAIN_COUPLE_HOSE:
                        UpdateCoupledHoseState(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_COUPLE_COCK:
                        UpdateCoupleCockState(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_SYNC_ALL:
                        SendWorldTrains(e.Client);
                        break;

                    case NetworkTags.TRAIN_HOST_SYNC:
                        SyncTrainDataFromHost(message);
                        break;

                    case NetworkTags.TRAIN_LOCATION_UPDATE:
                        UpdateTrainPosition(message, e.Client);
                        break;

                    case NetworkTags.TRAINS_INIT:
                        NewTrainsInitialized(message, e.Client);
                        break;

                    case NetworkTags.TRAINS_INIT_FINISHED:
                        TrainsFinishedInitilizing(e.Client);
                        break;

                    case NetworkTags.TRAIN_REMOVAL:
                        OnCarRemovalMessage(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_DAMAGE:
                        OnCarDamage(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_AUTH_CHANGE:
                        OnAuthChange(message);
                        break;

                    case NetworkTags.TRAIN_CARGO_CHANGE:
                        OnCargoChange(message, e.Client);
                        break;

                    case NetworkTags.TRAIN_MU_CHANGE:
                        OnCarMUChange(message, e.Client);
                        break;
                }
            }
        }

        private void OnCarMUChange(Message message, IClient client)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                CarMUChange data = reader.ReadSerializable<CarMUChange>();
                WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == data.TrainId1);
                if (!(train is null))
                {
                    string value = "";
                    if (data.IsConnected)
                        value = data.TrainId2;
                    switch (train.CarType)
                    {
                        case TrainCarType.LocoShunter:
                            if (data.Train1IsFront)
                                train.MultipleUnit.IsFrontMUConnectedTo = value;
                            else
                                train.MultipleUnit.IsRearMUConnectedTo = value;
                            break;
                    }
                }

                if (data.IsConnected)
                {
                    train = worldTrains.FirstOrDefault(t => t.Guid == data.TrainId2);
                    if (!(train is null))
                    {
                        string value = "";
                        if (data.IsConnected)
                            value = data.TrainId1;
                        switch (train.CarType)
                        {
                            case TrainCarType.LocoShunter:
                                if (data.Train2IsFront)
                                    train.MultipleUnit.IsFrontMUConnectedTo = value;
                                else
                                    train.MultipleUnit.IsRearMUConnectedTo = value;
                                break;
                        }
                    }
                }
            }

            ReliableSendToOthers(message, client);
        }

        private void OnCargoChange(Message message, IClient client)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                TrainCargoChanged data = reader.ReadSerializable<TrainCargoChanged>();
                WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == data.Id);
                train.CargoType = data.Type;
                train.CargoAmount = data.Amount;
            }

            Logger.Trace("[SERVER] > TRAIN_CARGO_CHANGE");
            ReliableSendToOthers(message, client);
        }

        private void OnAuthChange(Message message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                CarsAuthChange authChange = reader.ReadSerializable<CarsAuthChange>();
                List<IClient> sentTo = new List<IClient>();
                IEnumerable<IClient> players = PluginManager.GetPluginByType<PlayerPlugin.PlayerPlugin>().GetPlayers();
                foreach (string guid in authChange.Guids)
                {
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == guid);
                    if (train != null)
                    {
                        if (!sentTo.Any(c => c.ID == train.AuthorityPlayerId))
                        {
                            IClient ocl = players.FirstOrDefault(c => c.ID == train.AuthorityPlayerId);
                            if (ocl != null)
                            {
                                if (ocl.ID != 0)
                                    ocl.SendMessage(message, SendMode.Reliable);
                                sentTo.Add(ocl);
                            }
                        }
                        train.AuthorityPlayerId = authChange.PlayerId;
                    }
                }
                Logger.Trace("[SERVER] > TRAIN_AUTH_CHANGE");
                IClient cl = players.FirstOrDefault(c => c.ID == authChange.PlayerId);
                if(cl.ID != 0)
                    SendDelayedMessage(authChange, NetworkTags.TRAIN_AUTH_CHANGE, cl, (int)sentTo.OrderByDescending(c => c.RoundTripTime.SmoothedRtt).First().RoundTripTime.SmoothedRtt / 2 * 1000);
                sentTo.Add(cl);

                foreach(IClient client in players)
                {
                    if(!sentTo.Any(c => c.ID == client.ID))
                    {
                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write(authChange);
                            using (Message msg = Message.Create((ushort)NetworkTags.TRAIN_AUTH_CHANGE, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                    }
                }
            }
        }

        private void TrainsFinishedInitilizing(IClient sender)
        {
            if (!isLoadingTrain)
                return;

            Logger.Trace($"[SERVER] < TRAINS_INIT_FINISHED: {sender.ID}");
            playerHasInitializedTrain.Add(sender);

            CheckIfAllPlayersLoadedTrain();
        }

        private void CheckIfAllPlayersLoadedTrain()
        {
            bool allPlayersHaveLoadedTrains = true;
            IEnumerable<IClient> clients = PluginManager.GetPluginByType<PlayerPlugin.PlayerPlugin>().GetPlayers();
            foreach (IClient client in clients)
            {
                if (!playerHasInitializedTrain.Contains(client))
                {
                    allPlayersHaveLoadedTrains = false;
                    break;
                }
            }

            Logger.Trace($"[SERVER] TRAINS_INIT_FINISHED All players loaded Train? {allPlayersHaveLoadedTrains}");
            if (allPlayersHaveLoadedTrains)
            {
                Logger.Trace("[SERVER] > TRAINS_INIT_FINISHED");
                List<IClient> playersOrderedByPing = playerHasInitializedTrain.OrderBy(c => c.RoundTripTime.SmoothedRtt / 2).ToList();
                isLoadingTrain = false;
                for(int i = 0; i < playersOrderedByPing.Count; i++)
                {
                    if(i == playersOrderedByPing.Count - 1)
                        SendDelayedMessage(true, NetworkTags.TRAINS_INIT_FINISHED, playersOrderedByPing[i], (int)(playersOrderedByPing[0].RoundTripTime.SmoothedRtt / 2 - playersOrderedByPing[i].RoundTripTime.SmoothedRtt / 2) * 1000, queue.RunNext);
                    else
                        SendDelayedMessage(true, NetworkTags.TRAINS_INIT_FINISHED, playersOrderedByPing[i], (int)(playersOrderedByPing[0].RoundTripTime.SmoothedRtt / 2 - playersOrderedByPing[i].RoundTripTime.SmoothedRtt / 2) * 1000);
                }
                
            }
            else
            {
                foreach (IClient client in clients)
                {
                    if (!playerHasInitializedTrain.Contains(client))
                    {
                        Logger.Trace($"[SERVER] TRAINS_INIT_FINISHED Missing player {client.ID}");
                    }
                }
            }
        }

        private void SendDelayedMessage<T>(T item, NetworkTags tag, IClient client, int interval, Action callback = null) where T : IDarkRiftSerializable
        {
            if(interval == 0)
            {
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(item);
                    using (Message msg = Message.Create((ushort)tag, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
                callback?.Invoke();
            }
            else
            {
                Timer timer = new Timer();
                timer.Elapsed += (object sender, ElapsedEventArgs e) =>
                {
                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(item);
                        using (Message msg = Message.Create((ushort)tag, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                    callback?.Invoke();
                };
                timer.Interval = interval;
                timer.AutoReset = false;
                timer.Start();
            }
        }

        private void SendDelayedMessage(bool item, NetworkTags tag, IClient client, int interval, Action callback = null)
        {
            if (interval == 0)
            {
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(item);
                    using (Message msg = Message.Create((ushort)tag, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
                callback?.Invoke();
            }
            else
            {
                Timer timer = new Timer();
                timer.Elapsed += (object sender, ElapsedEventArgs e) =>
                {
                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(item);
                        using (Message msg = Message.Create((ushort)tag, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                    callback?.Invoke();
                };
                timer.Interval = interval;
                timer.AutoReset = false;
                timer.Start();
            }
        }

        private void OnCarDamage(Message message, IClient client)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                CarDamage damage = reader.ReadSerializable<CarDamage>();
                WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == damage.Guid);
                if (train == null)
                {
                    train = new WorldTrain()
                    {
                        Guid = damage.Guid,
                    };
                    worldTrains.Add(train);
                }

                switch (damage.DamageType)
                {
                    case DamageType.Car:
                        train.CarHealth = damage.NewHealth;
                        train.CarHealthData = damage.Data;
                        break;

                    case DamageType.Cargo:
                        train.CargoHealth = damage.NewHealth;
                        break;
                }    
            }

            Logger.Trace("[SERVER] > TRAIN_DAMAGE");
            ReliableSendToOthers(message, client);
        }

        private void OnCarRemovalMessage(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                CarRemoval carRemoval = reader.ReadSerializable<CarRemoval>();
                WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == carRemoval.Guid);
                if (train == null)
                {
                    train = new WorldTrain()
                    {
                        Guid = carRemoval.Guid,
                    };
                    worldTrains.Add(train);
                }
                train.IsRemoved = true;
            }

            Logger.Trace("[SERVER] > TRAIN_REMOVAL");
            ReliableSendToOthers(message, sender);
        }

        private void UpdateTrainSwitch(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                TrainCarChange carChange = reader.ReadSerializable<TrainCarChange>();
                if (carChange.TrainId != "")
                {
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == carChange.TrainId);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = carChange.TrainId
                        };
                        worldTrains.Add(train);
                    }
                }
            }

            Logger.Trace("[SERVER] > TRAIN_SWITCH");
            ReliableSendToOthers(message, sender);
        }
        
        private void UpdateCoupleCockState(Message message, IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainCouplerCockChange cockStateChanged = reader.ReadSerializable<TrainCouplerCockChange>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == cockStateChanged.TrainIdCoupler);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = cockStateChanged.TrainIdCoupler,
                        };
                        worldTrains.Add(train);
                    }

                    if (cockStateChanged.IsCouplerFront)
                        train.IsFrontCouplerCockOpen = cockStateChanged.IsOpen;
                    else
                        train.IsRearCouplerCockOpen = cockStateChanged.IsOpen;
                }
            }

            Logger.Trace("[SERVER] > TRAIN_COUPLE_COCK");
            ReliableSendToOthers(message, sender);
        }

        private void UpdateCoupledHoseState(Message message, IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainCouplerHoseChange hoseStateChanged = reader.ReadSerializable<TrainCouplerHoseChange>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == hoseStateChanged.TrainIdC1);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = hoseStateChanged.TrainIdC1,
                        };
                        worldTrains.Add(train);
                    }

                    if (hoseStateChanged.IsC1Front)
                        train.FrontCouplerHoseConnectedTo = hoseStateChanged.TrainIdC2;
                    else
                        train.RearCouplerHoseConnectedTo = hoseStateChanged.TrainIdC2;

                    if (hoseStateChanged.IsConnected)
                    {
                        train = worldTrains.FirstOrDefault(t => t.Guid == hoseStateChanged.TrainIdC2);
                        if (train == null)
                        {
                            train = new WorldTrain()
                            {
                                Guid = hoseStateChanged.TrainIdC2,
                            };
                            worldTrains.Add(train);
                        }

                        if (hoseStateChanged.IsC2Front)
                            train.FrontCouplerHoseConnectedTo = hoseStateChanged.TrainIdC1;
                        else
                            train.RearCouplerHoseConnectedTo = hoseStateChanged.TrainIdC1;
                    }
                }
            }

            Logger.Trace("[SERVER] > TRAIN_COUPLE_HOSE");
            ReliableSendToOthers(message, sender);
        }

        private void UpdateCouplingState(Message message, IClient sender, bool isCoupled)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainCouplingChange coupledChanged = reader.ReadSerializable<TrainCouplingChange>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == coupledChanged.TrainIdC1);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = coupledChanged.TrainIdC1,
                        };
                        worldTrains.Add(train);
                    }

                    if (isCoupled)
                    {
                        if (coupledChanged.IsC1Front)
                            train.FrontCouplerCoupledTo = coupledChanged.TrainIdC2;
                        else
                            train.RearCouplerCoupledTo = coupledChanged.TrainIdC2;
                    }
                    else
                    {
                        if (coupledChanged.IsC1Front)
                            train.FrontCouplerCoupledTo = "";
                        else
                            train.RearCouplerCoupledTo = "";
                    }

                    train = worldTrains.FirstOrDefault(t => t.Guid == coupledChanged.TrainIdC2);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = coupledChanged.TrainIdC2,
                        };
                        worldTrains.Add(train);
                    }

                    if (isCoupled)
                    {
                        if (coupledChanged.IsC2Front)
                            train.FrontCouplerCoupledTo = coupledChanged.TrainIdC1;
                        else
                            train.RearCouplerCoupledTo = coupledChanged.TrainIdC1;
                    }
                    else
                    {
                        if (coupledChanged.IsC2Front)
                            train.FrontCouplerCoupledTo = "";
                        else
                            train.RearCouplerCoupledTo = "";
                    }
                }
            }

            Logger.Trace($"[SERVER] > {(isCoupled ? "TRAIN_COUPLE" : "TRAIN_UNCOUPLE")}");
            ReliableSendToOthers(message, sender);
        }

        private void SyncTrainDataFromHost(Message message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                worldTrains.Clear();
                worldTrains.AddRange(reader.ReadSerializables<WorldTrain>());
            }
        }

        private void NewTrainsInitialized(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                WorldTrain[] trains = reader.ReadSerializables<WorldTrain>();
                worldTrains.AddRange(trains);
                if (isLoadingTrain)
                    queue.AddToBuffer(InitializeNewTrains, trains, sender);
                else
                    InitializeNewTrains(trains, sender);
            }
        }

        private void InitializeNewTrains(WorldTrain[] worldTrains, IClient sender)
        {
            isLoadingTrain = true;
            playerHasInitializedTrain.Clear();
            playerHasInitializedTrain.Add(sender);
            CheckIfAllPlayersLoadedTrain();
            Logger.Trace("[SERVER] > TRAINS_INIT");
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(worldTrains);
                using (Message msg = Message.Create((ushort)NetworkTags.TRAINS_INIT, writer))
                    ReliableSendToOthers(msg, sender);
            }
        }

        private void UpdateTrainDerailed(Message message, IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainDerail data = reader.ReadSerializable<TrainDerail>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == data.TrainId);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = data.TrainId,
                        };
                        worldTrains.Add(train);
                    }

                    train.Bogies[0] = new TrainBogie()
                    {
                        TrackName = data.Bogie1TrackName,
                        PositionAlongTrack = data.Bogie1PositionAlongTrack,
                        Derailed = data.IsBogie1Derailed
                    };
                    train.Bogies[train.Bogies.Length - 1] = new TrainBogie()
                    {
                        TrackName = data.Bogie2TrackName,
                        PositionAlongTrack = data.Bogie2PositionAlongTrack,
                        Derailed = data.IsBogie2Derailed
                    };
                    train.CarHealth = data.CarHealth;
                    train.CargoHealth = data.CargoHealth;
                }
            }

            Logger.Trace("[SERVER] > TRAIN_DERAIL");
            ReliableSendToOthers(message, sender);
        }

        private void UpdateTrainRerail(Message message, IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainRerail data = reader.ReadSerializable<TrainRerail>();
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == data.Guid);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = data.Guid,
                        };
                        worldTrains.Add(train);
                    }

                    train.Bogies[0] = new TrainBogie()
                    {
                        TrackName = data.Bogie1TrackName,
                        PositionAlongTrack = data.Bogie1PositionAlongTrack,
                        Derailed = false
                    };
                    train.Bogies[train.Bogies.Length - 1] = new TrainBogie()
                    {
                        TrackName = data.Bogie2TrackName,
                        PositionAlongTrack = data.Bogie2PositionAlongTrack,
                        Derailed = false
                    };
                    train.Position = data.Position;
                    train.Forward = data.Forward;
                    train.Rotation = data.Rotation;
                    train.CarHealth = data.CarHealth;
                    train.CargoHealth = data.CargoHealth;

                    if (train.IsLoco)
                    {
                        train.Throttle = 0;
                        train.Sander = 0;
                        train.Brake = 0;
                        train.IndepBrake = 1;
                        train.Reverser = 0f;
                        if (train.Shunter != null)
                        {
                            train.Shunter.IsEngineOn = false;
                            train.Shunter.IsMainFuseOn = false;
                            train.Shunter.IsSideFuse1On = false;
                            train.Shunter.IsSideFuse2On = false;
                        }
                    }
                }
            }
            Logger.Trace("[SERVER] > TRAIN_RERAIL");
            ReliableSendToOthers(message, sender);
        }

        private void SendWorldTrains(IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    Logger.Trace("[SERVER] > TRAIN_SYNC_ALL");

                    writer.Write(worldTrains.Where(t => !t.IsRemoved).ToArray());

                    using (Message msg = Message.Create((ushort)NetworkTags.TRAIN_SYNC_ALL, writer))
                        sender.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        private void UpdateTrainLever(Message message, IClient sender)
        {
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainLever lever = reader.ReadSerializable<TrainLever>();
                    //Logger.Trace($"Setting serverTrainState lever: [{lever.TrainId}] {lever.Lever}: {lever.Value}");
                    WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == lever.TrainId);
                    if (train == null)
                    {
                        train = new WorldTrain()
                        {
                            Guid = lever.TrainId,
                            IsLoco = true
                        };
                        worldTrains.Add(train);
                        Logger.Trace($"Train not found adding new one");
                    }

                    if (train.CarType == TrainCarType.LocoShunter)
                        switch (lever.Lever)
                        {
                            case Levers.Brake:
                            case Levers.IndependentBrake:
                            case Levers.Reverser:
                            case Levers.Sander:
                            case Levers.Throttle:
                                UpdateMULevers(train, lever);
                                break;

                            default:
                                UpdateLeverTrain(train, lever);
                                break;
                        }
                    else
                        UpdateLeverTrain(train, lever);
                }
            }
            Logger.Trace("[SERVER] > TRAIN_LEVER");
            ReliableSendToOthers(message, sender);
        }

        private void UpdateTrainPosition(Message message, IClient sender)
        {
            bool isReliable = false;
            if (worldTrains != null)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    TrainLocation[] datas = reader.ReadSerializables<TrainLocation>();
                    foreach(TrainLocation data in datas)
                    {
                        WorldTrain train = worldTrains.FirstOrDefault(t => t.Guid == data.TrainId);
                        if (data.Timestamp <= train.updatedAt)
                            continue;

                        train.Position = data.Position;
                        train.Rotation = data.Rotation;
                        train.Forward = data.Forward;
                        train.Bogies = data.Bogies;
                        train.IsStationary = data.IsStationary;
                        train.updatedAt = data.Timestamp;
                        isReliable = train.IsStationary;
                    }
                }
            }
            //Logger.Trace("[SERVER] > TRAIN_LOCATION_UPDATE");
            if (!isReliable)
                UnreliableSendToOthers(message, sender);
            else
                ReliableSendToOthers(message, sender);
        }

        private void UnreliableSendToOthers(Message message, IClient sender)
        {
            foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                client.SendMessage(message, SendMode.Unreliable);
        }

        private void ReliableSendToOthers(Message message, IClient sender)
        {
            foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                client.SendMessage(message, SendMode.Reliable);
        }

        private void UpdateMULevers(WorldTrain train, TrainLever lever, string prevGuid = "")
        {
            if (train == null)
                return;

            UpdateLeverTrain(train, lever);

            if(train.MultipleUnit.IsFrontMUConnectedTo != "" && train.MultipleUnit.IsFrontMUConnectedTo != prevGuid)
                UpdateMULevers(worldTrains.FirstOrDefault(t => t.Guid == train.MultipleUnit.IsFrontMUConnectedTo), lever, train.Guid);

            if(train.MultipleUnit.IsRearMUConnectedTo != "" && train.MultipleUnit.IsFrontMUConnectedTo != prevGuid)
                UpdateMULevers(worldTrains.FirstOrDefault(t => t.Guid == train.MultipleUnit.IsRearMUConnectedTo), lever, train.Guid);
        }

        private void UpdateLeverTrain(WorldTrain train, TrainLever lever)
        {
            switch (lever.Lever)
            {
                case Levers.Throttle:
                    train.Throttle = lever.Value;
                    break;

                case Levers.Brake:
                    train.Brake = lever.Value;
                    break;

                case Levers.IndependentBrake:
                    Logger.Trace($"Changed independent brake of {train.Guid}");
                    train.IndepBrake = lever.Value;
                    break;

                case Levers.Sander:
                    train.Sander = lever.Value;
                    break;

                case Levers.Reverser:
                    train.Reverser = lever.Value;
                    break;
            }

            switch (train.CarType)
            {
                case TrainCarType.LocoShunter:
                    if (train.Shunter == null)
                        train.Shunter = new Shunter();

                    Shunter shunter = train.Shunter;
                    switch (lever.Lever)
                    {
                        case Levers.MainFuse:
                            shunter.IsMainFuseOn = lever.Value == 1;
                            if (lever.Value == 0)
                                shunter.IsEngineOn = false;
                            break;

                        case Levers.SideFuse_1:
                            shunter.IsSideFuse1On = lever.Value == 1;
                            if (lever.Value == 0)
                                shunter.IsEngineOn = false;
                            break;

                        case Levers.SideFuse_2:
                            shunter.IsSideFuse2On = lever.Value == 1;
                            if (lever.Value == 0)
                                shunter.IsEngineOn = false;
                            break;

                        case Levers.FusePowerStarter:
                            if (shunter.IsSideFuse1On && shunter.IsSideFuse2On && shunter.IsMainFuseOn && lever.Value == 1)
                                shunter.IsEngineOn = true;
                            else if (lever.Value == 0)
                                shunter.IsEngineOn = false;
                            break;
                    }
                    break;
            }
        }
    }
}
