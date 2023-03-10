﻿using DarkRift;
using DarkRift.Server;
using DVMultiplayer.Darkrift;
using DVMultiplayer.DTO.Player;
using DVMultiplayer.Networking;
using DVMP.DTO.ServerSave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using UnityEngine;


namespace PlayerPlugin
{
    public class PlayerPlugin : Plugin, IPluginSave
    {
        new public string Name { get; } = "PlayerPlugin";
        private readonly Dictionary<IClient, Player> players = new Dictionary<IClient, Player>();
        public SetSpawn playerSpawn { get; private set; }
        private double money;
        private IClient playerConnecting = null;
        private readonly BufferQueue buffer = new BufferQueue();
        Timer pingSendTimer;

        public IEnumerable<IClient> GetPlayers()
        {
            return players.Keys;
        }

        public override bool ThreadSafe => true;

        public override Version Version => new Version("1.4.1");

        public PlayerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;
            pingSendTimer = new Timer(1000);
            pingSendTimer.Elapsed += PingSendMessage;
            pingSendTimer.AutoReset = true;
            pingSendTimer.Start();
        }

        public string SaveData()
        {
            return JsonConvert.SerializeObject(playerSpawn.Position) + ";" + JsonConvert.SerializeObject(money);
        }

        public void LoadData(string json)
        {
            string[] jsons = json.Split(';');
            playerSpawn.Position = (Vector3)JsonConvert.DeserializeObject(jsons[0]);
            money = (double)JsonConvert.DeserializeObject(jsons[1]);
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
            players.Remove(e.Client);
            if (players.Count > 0)
            {
                // Tell other players that someone left
                Logger.Trace("[SERVER] > PLAYER_DISCONNECT");
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
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
                        ReliableSendToOthers(outMessage, e.Client);
                }
                // Make next player in line the new host
                Logger.Trace("[SERVER] > PLAYER_SET_ROLE");
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(true);

                    using (Message message = Message.Create((ushort)NetworkTags.PLAYER_SET_ROLE, writer))
                        players.Keys.First().SendMessage(message, SendMode.Reliable);
                }
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
                    case NetworkTags.PLAYER_BUY_LICENSE:
                        ProcessPacket(message, e.Client);
                        break;

                    case NetworkTags.PLAYER_CHAT_MESSAGE:
                        ProcessPacket(message, e.Client);
                        break;

                    case NetworkTags.PLAYER_LOCATION_UPDATE:
                        LocationUpdateMessage(message, e.Client);
                        break;

                    case NetworkTags.PLAYER_INIT:
                        ServerPlayerInitializer(message, e.Client);
                        break;

                    case NetworkTags.PLAYER_SPAWN_SET:
                        SetSpawn(message, e.Client);
                        break;

                    case NetworkTags.PLAYER_LOADED:
                        SetPlayerLoaded(message, e.Client);
                        break;

                    case NetworkTags.PLAYER_MONEY_UPDATE:
                        MoneyUpdate(message, e.Client);
                        break;
                }
            }
        }

        private void SetPlayerLoaded(Message message, IClient sender)
        {
            if (players.TryGetValue(sender, out Player player))
            {
                player.isLoaded = true;
                ReliableSendToOthers(message, sender);
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
                    Logger.Trace("[SERVER] > PLAYER_MODS_MISMATCH");
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
                        Logger.Trace("[SERVER] > PLAYER_SPAWN_SET");
                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write(playerSpawn);
                            using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SPAWN_SET, writer))
                                sender.SendMessage(outMessage, SendMode.Reliable);
                        }

                        Logger.Trace("[SERVER] > PLAYER_SPAWN");
                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write(new NPlayer()
                            {
                                Id = player.Id,
                                Username = player.Username,
                                Mods = player.Mods,
                                Color = player.Color
                            });

                            writer.Write(new Location()
                            {
                                Position = playerSpawn.Position
                            });
                            using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SPAWN, writer))
                                ReliableSendToOthers(outMessage, sender);
                        }
                    }
                    
                    // Announce other players to new player
                    foreach (Player p in players.Values)
                    {
                        Logger.Trace("[SERVER] > PLAYER_SPAWN");
                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write(new NPlayer()
                            {
                                Id = p.id,
                                Username = p.username,
                                Mods = p.mods,
                                IsLoaded = p.isLoaded,
                                Color = p.color,
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

                    // Send money info
                    Logger.Trace("[SERVER] > PLAYER_MONEY_UPDATE");
                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(money);

                        using (Message message = Message.Create((ushort)NetworkTags.PLAYER_MONEY_UPDATE, writer))
                            sender.SendMessage(message, SendMode.Reliable);
                    }

                    // Tell client they are just client
                    Logger.Trace("[SERVER] > PLAYER_SET_ROLE");
                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(false);

                        using (Message message = Message.Create((ushort)NetworkTags.PLAYER_SET_ROLE, writer))
                            sender.SendMessage(message, SendMode.Reliable);
                    }
                }
            }
            else
            {
                // New player is host
                if (playerSpawn != null)
                {
                    Logger.Trace("[SERVER] > PLAYER_SPAWN_SET");
                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(playerSpawn);
                        using (Message outMessage = Message.Create((ushort)NetworkTags.PLAYER_SPAWN_SET, writer))
                            sender.SendMessage(outMessage, SendMode.Reliable);
                    }

                    Logger.Trace("[SERVER] > PLAYER_SET_ROLE");
                }

                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(true);
                    if (playerSpawn != null)
                        writer.Write(true);

                    using (Message message = Message.Create((ushort)NetworkTags.PLAYER_SET_ROLE, writer))
                        sender.SendMessage(message, SendMode.Reliable);
                }
            }
            if (succesfullyConnected)
            {
                if (players.ContainsKey(sender))
                    sender.Disconnect();
                else
                {
                    players.Add(sender, new Player(player.Id, player.Username, player.Mods, player.Color));
                }
            }
        }

        private void SetSpawn(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                playerSpawn = reader.ReadSerializable<SetSpawn>();
                money = reader.ReadDouble();
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
                        UnreliableSendToOthers(outMessage, sender);
                }
                
            }
        }

        private void MoneyUpdate(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                money = reader.ReadDouble();
            }

            ReliableSendToOthers(message, sender);
        }

        private void ProcessPacket(Message message, IClient sender)
        {
            ReliableSendToOthers(message, sender);
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
    }

    internal class Player
    {
        public readonly ushort id;
        public readonly string username;
        public readonly string[] mods;
        public Vector3 position;
        public Quaternion rotation;
        internal bool isLoaded;
        public readonly uint color;

        public Player(ushort id, string username, string[] mods, uint color)
        {
            this.id = id;
            this.username = username;
            this.mods = mods;
            this.color = color;

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
