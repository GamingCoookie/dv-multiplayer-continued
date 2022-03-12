using DarkRift;
using DarkRift.Server;
using DVMultiplayer.DTO.Turntable;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TurntablePlugin
{
    public class TurntablePlugin : Plugin
    {
        public override bool ThreadSafe => true;

        public override Version Version => new Version("1.0.11");

        private readonly List<Turntable> turntableStates = new List<Turntable>();

        public TurntablePlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += OnClientConnected;
        }

        private void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                NetworkTags tag = (NetworkTags)message.Tag;
                if (!tag.ToString().StartsWith("TURNTABLE_"))
                    return;

                Logger.Trace($"[SERVER] < {tag.ToString()}");

                switch (tag)
                {
                    case NetworkTags.TURNTABLE_ANGLE_CHANGED:
                        OnTurntableChanged(message, e.Client);
                        break;

                    case NetworkTags.TURNTABLE_SYNC:
                        SendAllTurntableStates(e.Client);
                        break;

                    case NetworkTags.TURNTABLE_SNAP:
                        OnTurntableSnap(message, e.Client);
                        break;

                    case NetworkTags.TURNTABLE_AUTH_RELEASE:
                        OnTurntableReleaseAuth(message, e.Client);
                        break;

                    case NetworkTags.TURNTABLE_AUTH_REQUEST:
                        OnTurntableRequestAuth(message, e.Client);
                        break;

                    case NetworkTags.TURNTABLE_HOST_SYNC:
                        SyncHostTurntables(message);
                        break;
                }
            }
        }

        private void OnTurntableRequestAuth(Message message, IClient client)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                RequestAuthority info = reader.ReadSerializable<RequestAuthority>();
                Turntable turntable = turntableStates.FirstOrDefault(t => t.Position == info.Position);
                if (turntable != null)
                {
                    turntable.playerAuthId = info.PlayerId;
                }
            }

            Logger.Trace("[SERVER] > TURNTABLE_AUTH_REQUEST");
            ReliableSendToOthers(message, client);
        }

        private void OnTurntableReleaseAuth(Message message, IClient client)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                ReleaseAuthority info = reader.ReadSerializable<ReleaseAuthority>();
                Turntable turntable = turntableStates.FirstOrDefault(t => t.Position == info.Position);
                if (turntable != null)
                {
                    turntable.playerAuthId = 0;
                }
            }

            Logger.Trace("[SERVER] > TURNTABLE_AUTH_RELEASE");
            ReliableSendToOthers(message, client);
        }

        private void SendAllTurntableStates(IClient sender)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(turntableStates.ToArray());

                using (Message msg = Message.Create((ushort)NetworkTags.TURNTABLE_SYNC, writer))
                    sender.SendMessage(msg, SendMode.Reliable);
            }
        }

        private void OnTurntableChanged(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                Turntable turntableInfo = reader.ReadSerializable<Turntable>();
                Turntable turntable = turntableStates.FirstOrDefault(t => t.Position == turntableInfo.Position);
                if (turntable != null)
                {
                    turntable.Rotation = turntableInfo.Rotation;
                }
                else
                {
                    turntableStates.Add(turntableInfo);
                }
            }

            //Logger.Trace("[SERVER] > TURNTABLE_ANGLE_CHANGED");
            ReliableSendToOthers(message, sender);
        }

        private void SyncHostTurntables(Message message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                Turntable[] turntables = reader.ReadSerializables<Turntable>();
                turntableStates.AddRange(turntables);
            }
        }

        private void OnTurntableSnap(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                Turntable turntableInfo = reader.ReadSerializable<Turntable>();
                Turntable turntable = turntableStates.FirstOrDefault(t => t.Position == turntableInfo.Position);
                if (turntable != null)
                {
                    turntable.Rotation = turntableInfo.Rotation;
                }
                else
                {
                    turntableStates.Add(turntableInfo);
                }
            }

            Logger.Trace("[SERVER] > TURNTABLE_SNAP");
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
    }
}
