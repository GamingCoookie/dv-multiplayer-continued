﻿using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DVMultiplayer;
using DVMultiplayer.DTO;
using DVMultiplayer.DTO.Player;
using DVMultiplayer.DTO.Savegame;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkPlayerManager : SingletonBehaviour<NetworkPlayerManager>
{
    GameObject networkedPlayer;
    Dictionary<ushort, GameObject> networkPlayers = new Dictionary<ushort, GameObject>();

    protected override void Awake()
    {
        base.Awake();
        networkedPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        networkedPlayer.GetComponent<CapsuleCollider>().enabled = false;
        networkedPlayer.AddComponent<NetworkPlayerSync>();
        networkedPlayer.tag = "NPlayer";
        networkPlayers = new Dictionary<ushort, GameObject>();

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.PLAYER_DISCONNECT:
                    OnPlayerDisconnect(message);
                    break;

                case NetworkTags.PLAYER_SPAWN:
                    SpawnNetworkPlayer(message);
                    break;

                case NetworkTags.PLAYER_LOCATION_UPDATE:
                    UpdateNetworkPositionAndRotation(message);
                    break;
            }
        }
    }

    private void OnPlayerDisconnect(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] PLAYER_DISCONNECT received | Packet size: {reader.Length}");
            //if (reader.Length % 44 != 0 && reader.Length % 34 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed spawn packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                Disconnect disconnectedPlayer = reader.ReadSerializable<Disconnect>();

                if (disconnectedPlayer.PlayerId != SingletonBehaviour<UnityClient>.Instance.ID)
                {
                    Destroy(networkPlayers[disconnectedPlayer.PlayerId]);
                    networkPlayers.Remove(disconnectedPlayer.PlayerId);
                }
            }
        }
    }

    public void PlayerConnect()
    {
        Main.DebugLog("[CLIENT] > PLAYER_SPAWN");
        Vector3 pos = PlayerManager.PlayerTransform.position;
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<NPlayer>(new NPlayer()
            {
                Id = SingletonBehaviour<UnityClient>.Instance.ID,
                Position = pos - WorldMover.currentMove,
                Rotation = PlayerManager.PlayerTransform.rotation,
            });

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_SPAWN, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
        SingletonBehaviour<WorldMover>.Instance.WorldMoved += WorldMoved;
        SingletonBehaviour<CoroutineManager>.Instance.Run(WaitForHost());
    }

    IEnumerator WaitForHost()
    {
        if (!NetworkManager.IsHost())
        {
            SingletonBehaviour<NetworkSaveGameManager>.Instance.isLoadingSave = true;
            SingletonBehaviour<NetworkJobsManager>.Instance.PlayerConnect();
            yield return new WaitUntil(() => networkPlayers.ContainsKey(0));
            SingletonBehaviour<NetworkSaveGameManager>.Instance.SyncSave();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkSaveGameManager>.Instance.IsHostSaveReceived);
            SingletonBehaviour<WorldMover>.Instance.movingEnabled = false;

            Vector3 vector3_1 = SaveGameManager.data.GetVector3("Player_position").Value;
            Vector3 vector3_2 = SaveGameManager.data.GetVector3("Player_rotation").Value;
            PlayerManager.PlayerTransform.position = vector3_1 + WorldMover.currentMove;
            PlayerManager.PlayerTransform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Quaternion.Euler(vector3_2) * Vector3.forward, Vector3.up).normalized);
            SingletonBehaviour<NetworkSaveGameManager>.Instance.isLoadingSave = false;
        }
        else
        {
            SingletonBehaviour<WorldMover>.Instance.movingEnabled = false;
            SingletonBehaviour<NetworkSaveGameManager>.Instance.SyncSave();
        }
    }

    public void PlayerDisconnect()
    {
        base.OnDestroy();
        foreach(GameObject player in networkPlayers.Values)
        {
            Destroy(player);
        }
        networkPlayers.Clear();
        SingletonBehaviour<WorldMover>.Instance.WorldMoved -= WorldMoved;
        SingletonBehaviour<WorldMover>.Instance.movingEnabled = true;
    }

    private void SpawnNetworkPlayer(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] PLAYER_SPAWN received | Packet size: {reader.Length}");
            //if (reader.Length % 44 != 0 && reader.Length % 34 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed spawn packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                NPlayer player = reader.ReadSerializable<NPlayer>();

                if (player.Id != SingletonBehaviour<UnityClient>.Instance.ID)
                {
                    Vector3 pos = player.Position + WorldMover.currentMove;
                    pos = new Vector3(pos.x, pos.y + 1, pos.z);
                    GameObject playerObject = Instantiate(networkedPlayer, pos, player.Rotation);
                    NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
                    playerSync.Id = player.Id;
                    networkPlayers.Add(player.Id, playerObject);
                    WorldMover.Instance.AddObjectToMove(playerObject.transform);
                }
            }
        }
    }

    public void UpdateLocalPositionAndRotation(Vector3 position, Vector3 prevPosition, Quaternion rotation)
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<Location>(new Location()
            {
                Id = SingletonBehaviour<UnityClient>.Instance.ID,
                AbsPosition = position,
                NewRotation = rotation
            });

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_LOCATION_UPDATE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Unreliable);
        }
    }

    private void UpdateNetworkPositionAndRotation(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            //Main.DebugLog($"[CLIENT] PLAYER_LOCATION_UPDATE received | Packet size: {reader.Length}"); //Commented due to console spam in debug
            //if (reader.Length % 30 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed location update packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                Location location = reader.ReadSerializable<Location>();

                GameObject playerObject = null;
                if (location.Id != SingletonBehaviour<UnityClient>.Instance.ID && networkPlayers.TryGetValue(location.Id, out playerObject))
                {

                    Vector3 pos = location.AbsPosition + WorldMover.currentMove;
                    pos = new Vector3(pos.x, pos.y + 1, pos.z);
                    playerObject.GetComponent<NetworkPlayerSync>().UpdateLocation(pos, location.NewRotation);
                }
            }
        }
    }

    private void WorldMoved(WorldMover mover, Vector3 newPos)
    {
        Main.DebugLog("[CLIENT] > PLAYER_WORLDMOVED");
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<WorldMove>(new WorldMove()
            {
                WorldPosition = newPos
            });
            Main.DebugLog($"[CLIENT] > PLAYER_WORLDMOVED {writer.Length}");

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_WORLDMOVED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void OnWorldMoved(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.DebugLog($"[CLIENT] SAVEGAME_WORLDMOVED received | Packet size: {reader.Length}");
            //if (reader.Length % 44 != 0 && reader.Length % 34 != 0)
            //{
            //    Main.mod.Logger.Warning("Received malformed spawn packet.");
            //    return;
            //}

            while (reader.Position < reader.Length)
            {
                WorldMove move = reader.ReadSerializable<WorldMove>();
                if (move.PlayerId != SingletonBehaviour<UnityClient>.Instance.ID && networkPlayers.TryGetValue(move.PlayerId, out GameObject playerObject))
                {
                    playerObject.GetComponent<NetworkPlayerSync>().currentWorldMove = move.WorldPosition;
                }
            }
        }
    }

    internal GameObject GetPlayerById(ushort playerId)
    {
        return networkPlayers[playerId];
    }

    internal GameObject GetLocalPlayer()
    {
        return PlayerManager.PlayerTransform.gameObject;
    }

    internal NetworkPlayerSync GetLocalPlayerSync()
    {
        return GetLocalPlayer().GetComponent<NetworkPlayerSync>();
    }

    internal NetworkPlayerSync GetPlayerSyncById(ushort playerId)
    {
        return networkPlayers[playerId].GetComponent<NetworkPlayerSync>();
    }

    internal IReadOnlyList<NetworkPlayerSync> GetAllNonLocalPlayerSync()
    {
        List<NetworkPlayerSync> networkPlayerSyncs = new List<NetworkPlayerSync>();
        foreach(GameObject playerObject in networkPlayers.Values)
        {
            NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
            if(playerSync != null)
                networkPlayerSyncs.Add(playerSync);
        }
        return networkPlayerSyncs;
    }

    internal int GetPlayerCount()
    {
        return networkPlayers.Count;
    }

    internal GameObject[] GetPlayersInTrain(TrainCar train)
    {
        return networkPlayers.Values.Where(p => p.GetComponent<NetworkPlayerSync>().train?.CarGUID == train.CarGUID).ToArray();
    }
}