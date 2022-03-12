using DarkRift;
using DarkRift.Server;
using DVMultiplayer.Darkrift;
using DVMultiplayer.DTO.Player;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using UnityEngine;


namespace PlayerPlugin
{
    public class PlayerPlugin : Plugin
    {
        private readonly Dictionary<IClient, Player> players = new Dictionary<IClient, Player>();
        private SetSpawn playerSpawn;
        private IClient playerConnecting = null;
        private readonly BufferQueue buffer = new BufferQueue();
        Timer pingSendTimer;

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
            pingSendTimer = new Timer(1000);
            pingSendTimer.Elapsed += PingSendMessage;
            pingSendTimer.AutoReset = true;
            pingSendTimer.Start();

        }

        private void PingSendMessage(object sender, ElapsedEventArgs e)
        {
            foreach (IClient client in players.Keys)
            {
                if (playerConnecting != null)
                    break;
                using (Message ping = Message.CreateEmpty((ushort)NetworkTags.PING))
                {
                    ping.MakePingMessage();
                    client.SendMessage(ping, SendMode.Reliable);
                }
            }
        }

        private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                players.Remove(e.Client);
                if (e.Client == playerConnecting)
                {
                    playerConnecting = null;
                    buffer.RunNext();
                }

                writer.Write(new Disconnect()
                {
                    PlayerId = e.Client.ID
                });

                using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_DISCONNECT, writer))
                    foreach (IClient client in ClientManager.GetAllClients().Where(client => client != e.Client))
                        client.SendMessage(outMessage, SendMode.Reliable);
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

            pingSendTimer.Start();
            playerConnecting = null;
            buffer.RunNext();
        }

        private void ServerPlayerInitializer(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                NPlayer player = reader.ReadSerializable<NPlayer>();
                if (playerConnecting != null)
                {
                    Logger.Info($"Queueing {player.Username}");
                    buffer.AddToBuffer(InitializePlayer, player, sender);
                    return;
                }
                Logger.Info($"Processing {player.Username}");
                InitializePlayer(player, sender);
            }
        }

        private void InitializePlayer(NPlayer player, IClient sender)
        {
            playerConnecting = sender;
            bool succesfullyConnected = true;
            if (players.Count > 0)
            {
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
                }
                else
                {   
                    // Announce new player to other players
                    if (playerSpawn != null)
                    {
                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write(playerSpawn);

                            using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SPAWN_SET, writer))
                                sender.SendMessage(outMessage, SendMode.Reliable);
                        }

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
                                Position = playerSpawn.Position
                            });

                            using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SPAWN, writer))
                                foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                                    client.SendMessage(outMessage, SendMode.Reliable);
                        }
                    }
                    
                    // Announce other players to new player
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
                }
            }
            if (succesfullyConnected)
            {
                if (players.ContainsKey(sender))
                    sender.Disconnect();
                else
                {
                    players.Add(sender, new Player(player.Id, player.Username, player.Mods));
                }
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
    }

    internal class Player
    {
        public readonly ushort id;
        public readonly string username;
        public readonly string[] mods;
        public Vector3 position;
        public Quaternion rotation;
        internal bool isLoaded;
        public Player(ushort id, string username, string[] mods)
        {
            this.id = id;
            this.username = username;
            this.mods = mods;

            isLoaded = false;
            position = new Vector3();
            rotation = new Quaternion();
        }

        public override string ToString()
        {
            return $"Player '{username}'/{id}";
        }
    }
}
