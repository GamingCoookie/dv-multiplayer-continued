using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV.CabControls;
using DV.TerrainSystem;
using DVMultiplayer;
using DVMultiplayer.Darkrift;
using DVMultiplayer.DTO.Turntable;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class NetworkTurntableManager : SingletonBehaviour<NetworkTurntableManager>
{
    private TurntableController[] turntables;

    public bool IsChangeByNetwork { get; internal set; }
    public bool IsSynced { get; set; }
    private readonly BufferQueue buffer = new BufferQueue();

    protected override void Awake()
    {
        base.Awake();

        turntables = GameObject.FindObjectsOfType<TurntableController>();
        foreach (TurntableController turntable in turntables)
        {
            turntable.gameObject.AddComponent<NetworkTurntableSync>();
        }

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;

        if (NetworkManager.IsHost())
            HostSyncTurntables();
        
    }

    internal void SyncTurntables()
    {
        IsSynced = false;
        Main.Log($"[CLIENT] > TURNTABLE_SYNC");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(true);

            using (Message message = Message.Create((ushort)NetworkTags.TURNTABLE_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    internal void HostSyncTurntables()
    {
        Main.Log($"[CLIENT] > TURNTABLE_HOST_SYNC");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            List<Turntable> serverTurntables = new List<Turntable>();
            foreach(TurntableController controller in turntables)
            {
                serverTurntables.Add(new Turntable()
                {
                    Position = controller.transform.position - WorldMover.currentMove,
                    Rotation = controller.turntable.currentYRotation
                });
            }
            writer.Write(serverTurntables.ToArray());

            using (Message message = Message.Create((ushort)NetworkTags.TURNTABLE_HOST_SYNC, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
        IsSynced = true;
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.TURNTABLE_ANGLE_CHANGED:
                    ReceiveNetworkTurntableChange(message);
                    break;

                case NetworkTags.TURNTABLE_SYNC:
                    ReceiveTurntableSync(message);
                    break;

                case NetworkTags.TURNTABLE_SNAP:
                    ReceiveTurntableOnSnap(message);
                    break;

                case NetworkTags.TURNTABLE_AUTH_RELEASE:
                    ReceiveAuthorityRelease(message);
                    break;

                case NetworkTags.TURNTABLE_AUTH_REQUEST:
                    ReceiveAuthorityRequest(message);
                    break;
            }
        }
    }

    private void ReceiveTurntableSync(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < TURNTABLE_SYNC");

            while (reader.Position < reader.Length)
            {
                Turntable[] turntableInfos = reader.ReadSerializables<Turntable>();

                foreach (Turntable turntable in turntableInfos)
                {
                    TurntableController turntableController = turntables.FirstOrDefault(j => j.transform.position == turntable.Position + WorldMover.currentMove);
                    if (turntableController)
                    {
                        turntableController.GetComponent<NetworkTurntableSync>().playerAuthId = turntable.playerAuthId;
                        SingletonBehaviour<CoroutineManager>.Instance.Run(RotateTurntableTowardsByNetwork(turntableController, turntable.Rotation));
                    }
                }
            }
        }
        IsSynced = true;
        buffer.RunBuffer();
    }

    internal void SendRequestAuthority(TurntableController turntable, ushort id)
    {
        if (!IsSynced)
            return;
        Main.Log($"[CLIENT] > TURNTABLE_AUTH_REQUEST: PlayerId: {id}");
        
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new RequestAuthority()
            {
                Position = turntable.transform.position - WorldMover.currentMove,
                PlayerId = id
            });


            using (Message message = Message.Create((ushort)NetworkTags.TURNTABLE_AUTH_REQUEST, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Unreliable);
        }
    }

    private IEnumerator RotateTurntableTowardsByNetwork(TurntableController turntableController, float angle, bool moveSlow = false)
    {
        IsChangeByNetwork = true;
        if (moveSlow)
        {
            bool addToAngle = angle > turntableController.turntable.currentYRotation;
            turntableController.turntable.targetYRotation += addToAngle ? .1f : -.1f;
            turntableController.turntable.RotateToTargetRotation();
            yield return new WaitUntil(() => turntableController.turntable.targetYRotation == turntableController.turntable.currentYRotation);
            if (Mathf.Abs(turntableController.turntable.currentYRotation - angle) > .25f)
            {
                yield return RotateTurntableTowardsByNetwork(turntableController, angle, moveSlow);
            }
        }
        else
        {
            turntableController.turntable.targetYRotation = angle;
            turntableController.turntable.RotateToTargetRotation();
            yield return new WaitUntil(() => Mathf.Abs(turntableController.turntable.currentYRotation - angle) < .1f);
        }
        IsChangeByNetwork = false;
    }

    internal void SendReleaseAuthority(TurntableController turntable)
    {
        if (!IsSynced)
            return;
        Main.Log($"[CLIENT] > TURNTABLE_AUTH_RELEASE");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new ReleaseAuthority()
            {
                Position = turntable.transform.position - WorldMover.currentMove
            });

            using (Message message = Message.Create((ushort)NetworkTags.TURNTABLE_AUTH_RELEASE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Unreliable);
        }
    }

    internal void OnTurntableRotationChanged(TurntableController turntable, float value)
    {
        if (!IsSynced)
            return;
        //Main.Log($"[CLIENT] > TURNTABLE_ANGLE_CHANGED");
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new Turntable()
            {
                Position = turntable.transform.position - WorldMover.currentMove,
                Rotation = value
            });

            using (Message message = Message.Create((ushort)NetworkTags.TURNTABLE_ANGLE_CHANGED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Unreliable);
        }
    }

    internal void OnTurntableSnap(TurntableController turntable, float value)
    {
        if (!IsSynced)
            return;

        Main.Log($"[CLIENT] > TURNTABLE_SNAP");
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new Turntable()
            {
                Position = turntable.transform.position - WorldMover.currentMove,
                Rotation = value
            });

            using (Message message = Message.Create((ushort)NetworkTags.TURNTABLE_SNAP, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if(SingletonBehaviour<UnityClient>.Instance)
            SingletonBehaviour<UnityClient>.Instance.MessageReceived -= MessageReceived;

        if (turntables == null)
            return;

        foreach (TurntableController turntable in turntables)
        {
            if (turntable.GetComponent<NetworkTurntableSync>())
                DestroyImmediate(turntable.GetComponent<NetworkTurntableSync>());
        }
    }

    public void ReceiveNetworkTurntableChange(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, ReceiveNetworkTurntableChange, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            //Main.Log($"[CLIENT] < TURNTABLE_ANGLE_CHANGED");

            while (reader.Position < reader.Length)
            {
                Turntable turntableInfo = reader.ReadSerializable<Turntable>();

                TurntableController turntable = turntables.FirstOrDefault(j => j.transform.position == turntableInfo.Position + WorldMover.currentMove);
                if (turntable)
                {
                    SingletonBehaviour<CoroutineManager>.Instance.Run(RotateTurntableTowardsByNetwork(turntable, turntableInfo.Rotation));
                }
            }
        }
    }

    public void ReceiveAuthorityRelease(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, ReceiveAuthorityRelease, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < TURNTABLE_AUTH_RELEASE");

            while (reader.Position < reader.Length)
            {
                ReleaseAuthority authReset = reader.ReadSerializable<ReleaseAuthority>();

                TurntableController turntable = turntables.FirstOrDefault(j => j.transform.position == authReset.Position + WorldMover.currentMove);
                if (turntable)
                {
                    NetworkTurntableSync sync = turntable.GetComponent<NetworkTurntableSync>();
                    sync.IsAnyoneInControlArea = false;
                    sync.playerAuthId = 0;
                }
            }
        }
    }

    public void ReceiveAuthorityRequest(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, ReceiveAuthorityRequest, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < TURNTABLE_AUTH_REQUEST");

            while (reader.Position < reader.Length)
            {
                RequestAuthority authRequest = reader.ReadSerializable<RequestAuthority>();

                TurntableController turntable = turntables.FirstOrDefault(j => j.transform.position == authRequest.Position + WorldMover.currentMove);
                if (turntable)
                {
                    NetworkTurntableSync sync = turntable.GetComponent<NetworkTurntableSync>();
                    sync.IsAnyoneInControlArea = true;
                    sync.playerAuthId = authRequest.PlayerId;
                }
            }
        }
    }

    public void ReceiveTurntableOnSnap(Message message)
    {
        if (buffer.NotSyncedAddToBuffer(IsSynced, ReceiveTurntableOnSnap, message))
            return;

        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < TURNTABLE_SNAP");

            while (reader.Position < reader.Length)
            {
                Turntable turntableInfo = reader.ReadSerializable<Turntable>();

                TurntableController turntable = turntables.FirstOrDefault(j => j.transform.position == turntableInfo.Position + WorldMover.currentMove);
                if (turntable)
                {
                    SingletonBehaviour<CoroutineManager>.Instance.Run(RotateTurntableTowardsByNetwork(turntable, turntableInfo.Rotation));
                }
            }
        }
    }
}