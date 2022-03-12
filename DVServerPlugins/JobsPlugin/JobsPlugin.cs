using DarkRift;
using DarkRift.Server;
using DVMultiplayer.DTO.Job;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JobsPlugin
{
    public class JobsPlugin : Plugin
    {
        public override bool ThreadSafe => true;
        public override Version Version => new Version("1.0.10");
        private readonly List<Chain> chains;
        private readonly List<Job> jobs;

        public JobsPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            chains = new List<Chain>();
            jobs = new List<Job>();
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
                if (!tag.ToString().StartsWith("JOB_"))
                    return;

                Logger.Trace($"[SERVER] < {tag}");

                switch (tag)
                {
                    case NetworkTags.JOB_CREATED:
                        OnJobCreated(message, e.Client);
                        break;

                    case NetworkTags.JOB_SYNC:
                        SendAllServerJobs(e.Client);
                        break;

                    case NetworkTags.JOB_HOST_SYNC:
                        UpdateServerJobs(message);
                        break;

                    case NetworkTags.JOB_TAKEN:
                        OnJobTaken(message, e.Client);
                        break;

                    case NetworkTags.JOB_COMPLETED:
                        OnJobCompleted(message, e.Client);
                        break;

                    case NetworkTags.JOB_CHAIN_COMPLETED:
                        OnJobChainCompleted(message, e.Client);
                        break;

                    case NetworkTags.JOB_CHAIN_EXPIRED:
                        OnJobChainExpired(message, e.Client);
                        break;

                    case NetworkTags.JOB_NEXT_JOB:
                        OnNextJobInChainGenerated(message, e.Client);
                        break;

                    case NetworkTags.JOB_CHAIN_CHANGED:
                        OnChainDataChanged(message);
                        break;

                    case NetworkTags.JOB_STATION_EXPIRATION:
                        Logger.Trace("[SERVER] > JOB_STATION_EXPIRATION");
                        ReliableSendToOthers(message, e.Client);
                        break;
                }
            }
        }

        private void OnJobChainExpired(Message message, IClient client)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                string id = reader.ReadString();
                Chain chain = chains.FirstOrDefault(j => j.Id == id);
                chain.IsExpired = true;
            }

            Logger.Trace("[SERVER] > JOB_CHAIN_EXPIRED");
            ReliableSendToOthers(message, client);
        }

        private void OnChainDataChanged(Message message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                Chain data = reader.ReadSerializable<Chain>();
                Chain chain = chains.FirstOrDefault(c => c.Id == data.Id);
                chain.Data = data.Data;
            }
        }

        private void OnNextJobInChainGenerated(Message message, IClient client)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                Job data = reader.ReadSerializable<Job>();
                Job prevJob = jobs.FirstOrDefault(j => j.ChainId == data.ChainId && j.IsCurrentJob);
                Job job = jobs.FirstOrDefault(j => j.Id == data.Id);
                prevJob.IsCurrentJob = false;
                job.GameId = data.GameId;
                job.IsCurrentJob = true;
                job.IsTaken = false;
                job.IsCompleted = false;
            }

            Logger.Trace("[SERVER] > JOB_NEXT_JOB");
            ReliableSendToOthers(message, client);
        }

        private void OnJobChainCompleted(Message message, IClient client)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                string id = reader.ReadString();
                Chain chain = chains.FirstOrDefault(j => j.Id == id);
                chain.IsCompleted = true;
            }

            Logger.Trace("[SERVER] > JOB_CHAIN_COMPLETED");
            ReliableSendToOthers(message, client);
        }

        private void OnJobCompleted(Message message, IClient client)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                string id = reader.ReadString();
                Job job = jobs.FirstOrDefault(j => j.Id == id);
                job.IsCompleted = true;
            }

            Logger.Trace("[SERVER] > JOB_COMPLETED");
            ReliableSendToOthers(message, client);
        }

        private void OnJobTaken(Message message, IClient client)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                string id = reader.ReadString();
                Job job = jobs.FirstOrDefault(j => j.Id == id);
                job.IsTaken = true;
            }

            Logger.Trace("[SERVER] > JOB_TAKEN");
            ReliableSendToOthers(message, client);
        }

        private void UpdateServerJobs(Message message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                chains.Clear();
                chains.AddRange(reader.ReadSerializables<Chain>());
                jobs.Clear();
                jobs.AddRange(reader.ReadSerializables<Job>());
                Logger.Trace($"[SERVER] Registered {chains.Count} chains with a total of {jobs.Count} jobs.");
            }
        }

        private void SendAllServerJobs(IClient sender)
        {
            Chain[] chainsToSend = chains.Where(c => !c.IsCompleted && !c.IsExpired).ToArray();

            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                List<Job> jobsToSend = new List<Job>();
                foreach(Chain chain in chainsToSend)
                {
                    jobsToSend.AddRange(jobs.Where(j => j.ChainId == chain.Id && !j.IsCompleted));
                }
                writer.Write(chainsToSend);
                writer.Write(jobsToSend.ToArray());

                Logger.Trace("[SERVER] > JOB_SYNC");
                using (Message msg = Message.Create((ushort)NetworkTags.JOB_SYNC, writer))
                    sender.SendMessage(msg, SendMode.Reliable);
            }
        }

        private void OnJobCreated(Message message, IClient sender)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                chains.AddRange(reader.ReadSerializables<Chain>());
                jobs.AddRange(reader.ReadSerializables<Job>());
            }

            Logger.Trace("[SERVER] > JOB_CREATED");
            ReliableSendToOthers(message, sender);
        }

        private void ReliableSendToOthers(Message message, IClient sender)
        {
            foreach (IClient client in ClientManager.GetAllClients().Where(client => client != sender))
                client.SendMessage(message, SendMode.Reliable);
        }
    }
}