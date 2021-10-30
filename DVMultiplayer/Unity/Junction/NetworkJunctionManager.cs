using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DVMultiplayer;
using DVMultiplayer.Darkrift;
using DVMultiplayer.DTO.Junction;
using DVMultiplayer.Networking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class NetworkJunctionManager : SingletonBehaviour<NetworkJunctionManager>
{
    public bool IsChangeByNetwork { get; internal set; }
    public bool IsSynced { get; internal set; }

    private Junction[] junctions;
    private readonly BufferQueue buffer = new BufferQueue();

    protected override void Awake()
    {
        Main.Log("NetworkJunctionManager initialized");
        base.Awake();

        junctions = SingletonBehaviour<CarsSaveManager>.Instance.TrackRootParent.GetComponentsInChildren<Junction>();
        Main.Log($"NetworkJunctionManager found {junctions.Length} switches in world");
        for(uint i = 0; i < junctions.Length; i++)
        {
            Junction junction = junctions[i];
            junction.gameObject.AddComponent<NetworkJunctionSync>().Id = i;
        }


        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
        
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (SingletonBehaviour<UnityClient>.Exists)
            SingletonBehaviour<UnityClient>.Instance.MessageReceived -= MessageReceived;

        if (junctions == null)
            return;

        foreach (Junction junction in junctions)
        {
            if (junction.GetComponent<NetworkJunctionSync>())
                Destroy(junction.GetComponent<NetworkJunctionSync>());
        }
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.SWITCH_CHANGED:
                    ReceiveNetworkJunctionSwitch(message);
                    break;

                case NetworkTags.SWITCH_SYNC:
                    ReceiveServerSwitches(message);
                    break;
            }
        }
    }

    private void ReceiveServerSwitches(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < SWITCH_SYNC");

            while (reader.Position < reader.Length)
            {
                Switch[] switchesServer = reader.ReadSerializables<Switch>();

                foreach (Switch switchInfo in switchesServer)
                {
                    if(switchInfo.Id >= junctions.Length)
                    {
                        Main.Log($"Unidentified junction received. Skipping (ID: {switchInfo.Id})");
                        continue;
                    }

                    Junction junction = junctions[switchInfo.Id];
                    if (switchInfo.SwitchToLeft && junction.selectedBranch == 0 || !switchInfo.SwitchToLeft && junction.selectedBranch == 1)
                    {
                        Main.Log($"Junction with ID {switchInfo.Id} already set to correct branch.");
                        continue;
                    }

                    IsChangeByNetwork = true;
                    junction.Switch(Junction.SwitchMode.NO_SOUND);
                    IsChangeByNetwork = false;
                }
            }
        }
        IsSynced = true;
        buffer.RunBuffer();
    }

    public void OnJunctionSwitched(uint id, Junction.SwitchMode mode, bool switchedToLeft)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > SWITCH_CHANGED");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<Switch>(new Switch()
            {
                Id = id,
                Mode = (SwitchMode)mode,
                SwitchToLeft = switchedToLeft
            });

            using (Message message = Message.Create((ushort)NetworkTags.SWITCH_CHANGED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    public void ReceiveNetworkJunctionSwitch(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, ReceiveNetworkJunctionSwitch, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < SWITCH_CHANGED");

            while (reader.Position < reader.Length)
            {
                Switch switchInfo = reader.ReadSerializable<Switch>();

                if (switchInfo.Id >= junctions.Length)
                {
                    Main.Log($"Unidentified junction received. Skipping (ID: {switchInfo.Id})");
                    return;
                }

                Junction junction = junctions[switchInfo.Id];
                if (switchInfo.SwitchToLeft && junction.selectedBranch == 0 || !switchInfo.SwitchToLeft && junction.selectedBranch == 1)
                {
                    Main.Log($"Junction with ID {switchInfo.Id} already set to correct branch.");
                    return;
                }

                IsChangeByNetwork = true;
                junction.Switch((Junction.SwitchMode)switchInfo.Mode);
                IsChangeByNetwork = false;
            }
        }
    }

    internal void SyncJunction()
    {
        IsSynced = false;
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            Main.Log($"[CLIENT] > SWITCH_SYNC");
            writer.Write(true);

            using (Message message = Message.Create((ushort)NetworkTags.SWITCH_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void HostSentJunctions()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            List<Switch> serverSwitches = new List<Switch>();
            for(uint i = 0; i < junctions.Length; i++)
            {
                Junction junction = junctions[i];
                serverSwitches.Add(new Switch()
                {
                    Id = i,
                    Mode = SwitchMode.NO_SOUND,
                    SwitchToLeft = junction.selectedBranch == 0
                });
            }
            Main.Log($"[CLIENT] > SWITCH_HOST_SYNC");
            writer.Write(serverSwitches.ToArray());

            using (Message message = Message.Create((ushort)NetworkTags.SWITCH_HOST_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
        IsSynced = true;
    }
}