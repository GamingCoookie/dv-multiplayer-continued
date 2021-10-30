using DarkRift;
using DarkRift.Server;
using DVMultiplayer.Darkrift;
using DVMultiplayer.DTO.Player;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;


namespace PlayerPlugin
{
    public class PlayerPlugin : Plugin
    {
        private readonly Dictionary<IClient, Player> players = new Dictionary<IClient, Player>();
        private SetSpawn playerSpawn;
        private long PlayerConnectLock = -1;
        private readonly BufferQueue buffer = new BufferQueue();
        System.Timers.Timer pingSendTimer;
        private ushort CurrentHostClientID = ushort.MaxValue;

        public IEnumerable<IClient> GetPlayers()
        {
            return players.Keys;
        }

        public override bool ThreadSafe => true;

        public override Version Version => new Version("2.6.20");

        public PlayerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;
            pingSendTimer = new System.Timers.Timer(250);
            pingSendTimer.Elapsed += PingSendMessage;
            pingSendTimer.AutoReset = true;
            pingSendTimer.Start();

        }

        private void PingSendMessage(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Read(ref PlayerConnectLock) >= 0)
                return;
            foreach (IClient client in players.Keys)
            {
                if (Interlocked.Read(ref PlayerConnectLock) >= 0)
                    return;

                using (Message ping = Message.CreateEmpty((ushort)NetworkTags.PING))
                {
                    ping.MakePingMessage();
                    client.SendMessage(ping, SendMode.Reliable);
                }
            }
            if (Interlocked.Read(ref PlayerConnectLock) < 0 && !buffer.IsEmpty)
            {
                Task.Run(() => buffer.RunNext());
            }
        }

        private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                if (players.Remove(e.Client)) // he was connected
                {
                    writer.Write<Disconnect>(new Disconnect()
                    {
                        PlayerId = e.Client.ID
                    });

                    using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_DISCONNECT, writer))
                        foreach (IClient client in ClientManager.GetAllClients().Where(client => client != e.Client))
                            client.SendMessage(outMessage, SendMode.Reliable);
                }
                if (Interlocked.Read(ref PlayerConnectLock) == e.Client.ID)
                    Interlocked.Exchange(ref PlayerConnectLock, -1);
                if (e.Client.ID == CurrentHostClientID)
                    SelectNewHostPlayer(); // Select a new Host
            }
        }

        private void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            e.Client.MessageReceived += MessageReceived;
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                NetworkTags tag = (NetworkTags)message.Tag;
                if (!tag.ToString().StartsWith("PLAYER_"))
                    return;

                if (tag != NetworkTags.PLAYER_LOCATION_UPDATE)
                    Logger.Trace($"[SERVER] < {tag.ToString()}");

                switch (tag)
                {
                    case NetworkTags.PLAYER_LOCATION_UPDATE:
                        LocationUpdateMessage(message, e.Client);
                        break;

                    case NetworkTags.PLAYER_INIT:
                        ServerPlayerInitializer(message, e.Client);
                        break;

                    case NetworkTags.PLAYER_SPAWN_SET:
                        SetSpawn(message);
                        break;

                    case NetworkTags.PLAYER_LOADED:
                        SetPlayerLoaded(message, e.Client);
                        break;
                }
            }
        }

        private void SetPlayerLoaded(Message message, IClient sender)
        {
            if (players.TryGetValue(sender, out Player player))
            {
                player.isLoaded = true;
                foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                    client.SendMessage(message, SendMode.Reliable);
            }
            else
                Logger.Error($"Client with ID {sender.ID} not found");
            if (Interlocked.Read(ref PlayerConnectLock) == sender.ID)
                Interlocked.Exchange(ref PlayerConnectLock, -1);
        }

        private void ServerPlayerInitializer(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                NPlayer player = reader.ReadSerializable<NPlayer>();
                if (Interlocked.Read(ref PlayerConnectLock) >= 0)
                {
                    Logger.Info($"Queueing {player.Username} {Interlocked.Read(ref PlayerConnectLock)}");
                    buffer.AddToBuffer(InitializePlayer, player, sender);
                    return;
                }
                Logger.Info($"Processing {player.Username}");
                InitializePlayer(player, sender);
            }
        }

        private void InitializePlayer(NPlayer player, IClient sender)
        {
            Interlocked.Exchange(ref PlayerConnectLock, sender.ID);
            bool newClientIsHost = true;
            if (players.Count > 0)
            {
                /* Disabled for now 
                Player host = players.Values.First();
                List<string> missingMods = GetMissingMods(host.mods, player.Mods);
                List<string> extraMods = GetMissingMods(player.Mods, host.mods);
                if (missingMods.Count != 0 || extraMods.Count != 0)
                {
                    succesfullyConnected = false;
                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(missingMods.ToArray());
                        writer.Write(extraMods.ToArray());

                        using (Message msg = Message.Create((ushort)NetworkTags.PLAYER_MODS_MISMATCH, writer))
                            sender.SendMessage(msg, SendMode.Reliable);
                    }
                    
                    IClient clientToDisconnect = sender;
                    Task.Delay(2000).ContinueWith((t) => {
                        if (clientToDisconnect.ConnectionState != ConnectionState.Disconnected)
                            clientToDisconnect.Disconnect();
                            });

                    return;
                }
                */

                // Annoucne Player to other clients
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(new NPlayer()
                    {
                        Id = player.Id,
                        Username = player.Username,
                        Mods = player.Mods
                    });

                    writer.Write(new Location()
                    {
                        Position = player.Position
                    });

                    using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SPAWN, writer))
                        foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                            client.SendMessage(outMessage, SendMode.Reliable);
                }

                // Anounce other Players to new Player
                foreach (Player p in players.Values)
                {
                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(new NPlayer()
                        {
                            Id = p.id,
                            Username = p.username,
                            Mods = p.mods,
                            IsLoaded = p.isLoaded
                        });

                        writer.Write(new Location()
                        {
                            Position = p.position,
                            Rotation = p.rotation
                        });

                        using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SPAWN, writer))
                            sender.SendMessage(outMessage, SendMode.Reliable);
                    }
                }
                newClientIsHost = false;
            }
            if (players.ContainsKey(sender))
                sender.Disconnect();
            else
            {
                players.Add(sender, new Player(player.Id, player.Username, player.Mods));
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(newClientIsHost);
                    using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SET_ROLE, writer))
                        sender.SendMessage(outMessage, SendMode.Reliable);
                }
                Logger.Info($"Client with ID {sender.ID} accepted {newClientIsHost}");
            }
        }

        private void SetSpawn(Message message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                playerSpawn = reader.ReadSerializable<SetSpawn>();
            }
        }

        private void LocationUpdateMessage(Message message, IClient sender)
        {
            if (players.TryGetValue(sender, out Player player))
            {
                Location newLocation;
                using (DarkRiftReader reader = message.GetReader())
                {
                    newLocation = reader.ReadSerializable<Location>();
                    player.position = newLocation.Position;
                    if (newLocation.Rotation.HasValue)
                        player.rotation = newLocation.Rotation.Value;
                }

                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    newLocation.AproxPing = (int)(sender.RoundTripTime.SmoothedRtt / 2 * 1000);
                    writer.Write(newLocation);

                    using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_LOCATION_UPDATE, writer))
                        foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                            client.SendMessage(outMessage, SendMode.Unreliable);
                }
                
            }
        }

        private List<string> GetMissingMods(string[] modList1, string[] modList2)
        {
            List<string> missingMods = new List<string>();
            foreach (string mod in modList1)
            {
                if (!modList2.Contains(mod))
                    missingMods.Add(mod);
            }
            return missingMods;
        }

        public void SelectNewHostPlayer()
        {
            if (players.Count == 0)
                return;
            var minValue = players.Where(x => x.Value.isLoaded).Min(x => x.Key.RoundTripTime.SmoothedRtt);
            var result = players.FirstOrDefault(x => x.Key.RoundTripTime.SmoothedRtt <= minValue);
            if (result.Key == null)
            {
                Logger.Error($"Could not find a Client as new host?!");
                return;
            }
            Logger.Info($"{result.Value.id} is the new HOST");
            result.Value.isHost = true;
            PlayerSetRole(result.Key, true);
        }

        public void PlayerSetRole(IClient client, bool isHost)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(isHost);
                using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SET_ROLE, writer))
                    client.SendMessage(outMessage, SendMode.Reliable);
            }
        }

    }

    internal class Player
    {
        public readonly ushort id;
        public readonly string username;
        public readonly string[] mods;
        public Vector3 position;
        public Quaternion rotation;
        internal bool isLoaded;
        internal bool isHost;

        public Player(ushort id, string username, string[] mods)
        {
            this.id = id;
            this.username = username;
            this.mods = mods;

            isLoaded = false;
            position = new Vector3();
            rotation = new Quaternion();
        }
    }
}
