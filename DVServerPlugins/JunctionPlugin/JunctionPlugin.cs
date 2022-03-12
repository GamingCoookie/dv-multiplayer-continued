using DarkRift;
using DarkRift.Server;
using DVMultiplayer.DTO.Junction;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JunctionPlugin
{
    public class JunctionPlugin : Plugin
    {
        public override bool ThreadSafe => true;

        public override Version Version => new Version("1.0.6");

        private readonly List<Switch> switchStates;

        public JunctionPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            switchStates = new List<Switch>();
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
                if (!tag.ToString().StartsWith("SWITCH_"))
                    return;

                Logger.Trace($"[SERVER] < {tag}");

                switch (tag)
                {
                    case NetworkTags.SWITCH_CHANGED:
                        UpdateSwitch(message, e.Client);
                        break;

                    case NetworkTags.SWITCH_SYNC:
                        SyncSwitchStatesWithClient(e.Client);
                        break;

                    case NetworkTags.SWITCH_HOST_SYNC:
                        SyncHostSwitches(message);
                        break;
                }
            }
        }

        private void UpdateSwitch(Message message, IClient client)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                Switch switchInfo = reader.ReadSerializable<Switch>();
                Switch s = switchStates.FirstOrDefault(t => t.Id == switchInfo.Id);
                if (s != null)
                {
                    s.SwitchToLeft = switchInfo.SwitchToLeft;
                }
                else
                {
                    switchStates.Add(switchInfo);
                }
            }

            ReliableSendToOthers(message, client);
        }

        private void SyncHostSwitches(Message message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                Switch[] switches = reader.ReadSerializables<Switch>();
                switchStates.AddRange(switches);
            }
        }

        private void SyncSwitchStatesWithClient(IClient sender)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(switchStates.ToArray());

                using (Message msg = Message.Create((ushort)NetworkTags.SWITCH_SYNC, writer))
                    sender.SendMessage(msg, SendMode.Reliable);
            }
        }

        private void ReliableSendToOthers(Message message, IClient sender)
        {
            foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                client.SendMessage(message, SendMode.Reliable);
        }
    }
}
