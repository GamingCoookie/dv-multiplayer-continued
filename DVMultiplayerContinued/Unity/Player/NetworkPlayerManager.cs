using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV;
using DV.TerrainSystem;
using DVMultiplayer;
using DVMultiplayer.DTO.Player;
using DVMP.DTO.Player;
using DVMultiplayer.Networking;
using DVMultiplayer.Utils;
using DVMultiplayer.Utils.Game;
using DVMultiplayerContinued.Unity.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal class NetworkPlayerManager : SingletonBehaviour<NetworkPlayerManager>
{
    public Dictionary<ushort, GameObject> localPlayers = new Dictionary<ushort, GameObject>();
    private readonly Dictionary<ushort, NPlayer> serverPlayers = new Dictionary<ushort, NPlayer>();
    public bool IsChangeByNetwork { get; internal set; }
    private SetSpawn spawnData;
    private Coroutine playersLoaded;
    private bool modMismatched = false;
    public bool newPlayerConnecting;
    public int CanBuyLicense = 0;
    private bool _RoleHasBeenSet = false;

    public bool IsSynced { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        localPlayers = new Dictionary<ushort, GameObject>();

        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
        SingletonBehaviour<Inventory>.Instance.MoneyChanged += OnLocalMoneyChanged;
    }


    private GameObject GetNewPlayerObject(Vector3 pos, Quaternion rotation, string username, string hexColor)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = username;
        player.transform.position = pos;
        player.transform.rotation = rotation;
        player.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
        player.GetComponent<CapsuleCollider>().enabled = false;
        ColorUtility.TryParseHtmlString(hexColor, out Color color);
        if (color != null)
            player.GetComponent<Renderer>().material.color = color;
        player.AddComponent<NetworkPlayerSync>();

        GameObject nametagCanvas = new GameObject("Nametag Canvas");
        nametagCanvas.transform.parent = player.transform;
        nametagCanvas.transform.localPosition = new Vector3(0, 1.5f, 0);
        nametagCanvas.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);
        nametagCanvas.AddComponent<Canvas>();
        nametagCanvas.AddComponent<RotateTowardsPlayer>();

        RectTransform rectTransform = nametagCanvas.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1920, 1080);
        rectTransform.localScale = new Vector3(.0018f, .0004f, 0);

        GameObject nametagBackground = new GameObject("Nametag BG");
        nametagBackground.transform.parent = nametagCanvas.transform;
        nametagBackground.transform.localPosition = new Vector3(0, 0, 0);

        RawImage bg = nametagBackground.AddComponent<RawImage>();
        bg.color = new Color(69 / 255, 69 / 255, 69 / 255, .45f);

        rectTransform = nametagBackground.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(1f, 1f, 0);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);

        GameObject nametag = new GameObject("Nametag");
        nametag.transform.parent = nametagCanvas.transform;
        nametag.transform.localPosition = new Vector3(775, 0, 0);

        Text tag = nametag.AddComponent<Text>();
        tag.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        tag.fontSize = 200;
        tag.alignment = TextAnchor.MiddleCenter;
        tag.resizeTextForBestFit = true;
        tag.text = username;

        rectTransform = nametag.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(1f, 3f, 0);
        rectTransform.anchorMin = new Vector2(0, .5f);
        rectTransform.anchorMax = new Vector2(0, .5f);
        rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, 350);
        rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -350);
        rectTransform.sizeDelta = new Vector2(1575, 350);

        GameObject ping = new GameObject("Ping");
        ping.transform.parent = nametagCanvas.transform;
        ping.transform.localPosition = new Vector3(25, 0, 0);

        Text pingTxt = ping.AddComponent<Text>();

        pingTxt.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        pingTxt.fontSize = 130;
        pingTxt.alignment = TextAnchor.MiddleRight;
        pingTxt.resizeTextForBestFit = true;
        pingTxt.text = "0ms";

        rectTransform = ping.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(1f, 3f, 0);
        rectTransform.anchorMin = new Vector2(1, .5f);
        rectTransform.anchorMax = new Vector2(1, .5f);
        rectTransform.pivot = new Vector2(1, .5f);
        rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, 350);
        rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -350);
        rectTransform.sizeDelta = new Vector2(350, 350);

        GameObject p = Instantiate(player);
        Destroy(player);
        return p;
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            if (message.IsPingMessage)
            {
                using (Message acknowledgementMessage = Message.Create((ushort)NetworkTags.PING, DarkRiftWriter.Create()))
                {
                    acknowledgementMessage.MakePingAcknowledgementMessage(message);
                    SingletonBehaviour<UnityClient>.Instance.SendMessage(acknowledgementMessage, SendMode.Reliable);
                }
            }

            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.PLAYER_BUY_LICENSE:
                    StartCoroutine(OnPlayerBuyLicense(message));
                    break;

                case NetworkTags.PLAYER_CHAT_MESSAGE:
                    ChatMessageReceived(message);
                    break;

                case NetworkTags.PLAYER_DISCONNECT:
                    OnPlayerDisconnect(message);
                    break;

                case NetworkTags.PLAYER_SPAWN:
                    SpawnNetworkPlayer(message);
                    break;

                case NetworkTags.PLAYER_MODS_MISMATCH:
                    OnModMismatch(message);
                    break;

                case NetworkTags.PLAYER_MONEY_UPDATE:
                    OnMoneyUpdate(message);
                    break;

                case NetworkTags.PLAYER_LOCATION_UPDATE:
                    UpdateNetworkPositionAndRotation(message);
                    break;

                case NetworkTags.PLAYER_SPAWN_SET:
                    SetSpawnPosition(message);
                    break;

                case NetworkTags.PLAYER_LOADED:
                    SetPlayerLoaded(message);
                    break;

                case NetworkTags.PLAYER_SET_ROLE:
                    SetPlayerRole(message);
                    break;
            }
        }
    }

    private void SetPlayerRole(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            NetworkManager.SetIsHost(reader.ReadBoolean());
                
        }
        _RoleHasBeenSet = true;
    }

    private void ChatMessageReceived(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            ChatMessage chatMessage = reader.ReadSerializable<ChatMessage>();
            GameChat.AppendNewMessage(chatMessage.Message);
        }
    }

    private void SetPlayerLoaded(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                PlayerLoaded player = reader.ReadSerializable<PlayerLoaded>();
                if (player.Id != SingletonBehaviour<UnityClient>.Instance.ID)
                {
                    if (localPlayers.TryGetValue(player.Id, out GameObject playerObject))
                    {
                        playerObject.GetComponent<NetworkPlayerSync>().IsLoaded = true;
                    }
                    else
                    {
                        Main.mod.Logger.Critical($"Player with ID: {player.Id} not found");
                    }
                }
            }
        }
    }

    private void SetSpawnPosition(Message message)
    {
        Main.Log("[CLIENT] < PLAYER_SPAWN_SET");
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                spawnData = reader.ReadSerializable<SetSpawn>();
            }
        }
    }

    private void OnModMismatch(Message message)
    {
        Main.Log("[CLIENT] Client disconnected due to mods mismatch");
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                string[] missingMods = reader.ReadStrings();
                string[] extraMods = reader.ReadStrings();

                List<string> mismatches = new List<string>();
                mismatches.AddRange(missingMods);
                mismatches.AddRange(extraMods);
                MenuScreen screen = CustomUI.ModMismatchScreen;
                screen.transform.Find("Label Mismatched").GetComponent<TextMeshProUGUI>().text = "Your mods and the mods of the host mismatched.\n";
                for (int i = 0; i < (mismatches.Count > 10 ? 10 : mismatches.Count); i++)
                {
                    screen.transform.Find("Label Mismatched").GetComponent<TextMeshProUGUI>().text += "[MISMATCH] " + mismatches[i] + "\n";
                }
                if (mismatches.Count > 10)
                    screen.transform.Find("Label Mismatched").GetComponent<TextMeshProUGUI>().text += $"And {mismatches.Count - 10} more mismatches.";

                if (missingMods.Length > 0)
                    Main.mod.Logger.Error($"[MOD MISMATCH] You are missing the following mods: {string.Join(", ", missingMods)}");

                if (extraMods.Length > 0)
                    Main.mod.Logger.Error($"[MOD MISMATCH] You installed mods the host doesn't have, these are: {string.Join(", ", extraMods)}");

                modMismatched = true;
            }
        }
    }

    private void OnMoneyUpdate(Message message)
    {
        Main.Log($"[CLIENT] < PLAYER_MONEY_UPDATE");
        IsChangeByNetwork = true;
        SingletonBehaviour<Inventory>.Instance.SetMoney(message.GetReader().ReadDouble());
        IsChangeByNetwork = false;
    }

    private void OnLocalMoneyChanged(double oldMoney, double newMoney)
    {
        if (IsChangeByNetwork || !IsSynced)
            return;
        Main.Log($"[CLIENT] > PLAYER_MONEY_UPDATE");
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(newMoney);

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_MONEY_UPDATE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private IEnumerator OnPlayerBuyLicense(Message message)
    {
        Main.Log($"[CLIENT] < PLAYER_BUY_LICENSE");
        using (DarkRiftReader reader = message.GetReader())
        {
            License license = reader.ReadSerializable<License>();
            if (NetworkManager.IsHost() && license.PurchaseAllowed == 0)
            {
                CustomUI.YesNoPopupUI.transform.Find("Button Yes").GetComponent<Button>().onClick.AddListener(() =>
                {
                    license.PurchaseAllowed = 1;
                });
                CustomUI.YesNoPopupUI.transform.Find("Button No").GetComponent<Button>().onClick.AddListener(() =>
                {
                    license.PurchaseAllowed = 2;
                });

                CustomUI.OpenYesNoPopup($"\"{serverPlayers[license.PlayerID].Username}\" wants to buy {license.LicenseName}", $"License cost: {license.Price}", $"Current wallet: {SingletonBehaviour<Inventory>.Instance.PlayerMoney + license.Price}");
                UUI.UnlockMouse(true);
                TutorialController.movementAllowed = false;
                AppUtil.Instance.PauseGame();
                yield return new WaitUntil(() => license.PurchaseAllowed != 0);
                Main.Log($"Purchase allowed: {license.PurchaseAllowed}");
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    License returnAnswer = new License
                    {
                        PlayerID = license.PlayerID,
                        PurchaseAllowed = license.PurchaseAllowed
                    };

                    writer.Write(returnAnswer);
                    Main.Log($"[CLIENT] > PLAYER_BUY_LICENSE");
                    using (Message returnMessage = Message.Create((ushort)NetworkTags.PLAYER_BUY_LICENSE, writer))
                        SingletonBehaviour<UnityClient>.Instance.SendMessage(returnMessage, SendMode.Reliable);
                }
                AppUtil.Instance.UnpauseGame();
                TutorialController.movementAllowed = true;
                UUI.UnlockMouse(false);
                CustomUI.Close();
            }
            else
            {
                if (license.PlayerID == NetworkManager.client.ID && license.PurchaseAllowed != 0)
                    CanBuyLicense = license.PurchaseAllowed; // 1 is basically true

                else if (license.PurchaseAllowed != 0)
                {
                    AppUtil.Instance.UnpauseGame();
                    TutorialController.movementAllowed = true;
                    UUI.UnlockMouse(false);
                    CustomUI.Close();
                }
                else
                {
                    CustomUI.OpenPopup($"Somebody wants a license", $"{serverPlayers[license.PlayerID].Username} wants to buy {license.LicenseName}");
                    UUI.UnlockMouse(true);
                    TutorialController.movementAllowed = false;
                    AppUtil.Instance.PauseGame();
                }
            }
        }
    }

    private void OnPlayerDisconnect(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                Disconnect disconnectedPlayer = reader.ReadSerializable<Disconnect>();

                if (disconnectedPlayer.PlayerId != SingletonBehaviour<UnityClient>.Instance.ID && localPlayers.ContainsKey(disconnectedPlayer.PlayerId))
                {
                    Main.Log($"[CLIENT] < PLAYER_DISCONNECT: Username: {localPlayers[disconnectedPlayer.PlayerId].GetComponent<NetworkPlayerSync>().Username}");
                    GameChat.PutSystemMessage($"{localPlayers[disconnectedPlayer.PlayerId].GetComponent<NetworkPlayerSync>().Username} disconnected from the server");
                    if (localPlayers[disconnectedPlayer.PlayerId])
                        Destroy(localPlayers[disconnectedPlayer.PlayerId]);

                    serverPlayers.Remove(disconnectedPlayer.PlayerId);
                    localPlayers.Remove(disconnectedPlayer.PlayerId);
                }
            }
        }
    }

    /// <summary>
    /// This method is called upon the player connects.
    /// </summary>
    public void PlayerConnect()
    {
        newPlayerConnecting = true;
        serverPlayers.Add(SingletonBehaviour<UnityClient>.Instance.ID, new NPlayer());
        if (NetworkManager.IsHost())
        {
            GameChat.PutSystemMessage($"The server has been launched!");
        }
        else
        {
            GameChat.PutSystemMessage($"You have connected to a server!");
        }
        

        Main.Log("[CLIENT] > PLAYER_INIT");
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<NPlayer>(new NPlayer()
            {
                Id = SingletonBehaviour<UnityClient>.Instance.ID,
                Username = PlayerManager.PlayerTransform.GetComponent<NetworkPlayerSync>().Username,
                Mods = Main.GetEnabledMods(),
                Color = Main.Settings.ColorString
            });

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_INIT, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }

        Main.Log($"Wait for connection initializiation is finished");
        SingletonBehaviour<CoroutineManager>.Instance.Run(WaitForInit());
    }

    private IEnumerator WaitForInit()
    {
        UUI.UnlockMouse(true);
        TutorialController.movementAllowed = false;

        GamePreferences.Set(Preferences.CommsRadioSpawnMode, false);
        GamePreferences.RegisterToPreferenceUpdated(Preferences.CommsRadioSpawnMode, DisableSpawnMode);

        CustomUI.OpenPopup("Connecting", "Syncing with Server");
        DateTime waitStart = DateTime.Now;

        yield return new WaitUntil(() => _RoleHasBeenSet || modMismatched || waitStart - DateTime.Now > TimeSpan.FromSeconds(30) );

        if (modMismatched)
        {
            UUI.UnlockMouse(false);
            TutorialController.movementAllowed = true;
            Main.Log($"Mods Mismatched so disconnecting player");
            CustomUI.Open(CustomUI.ModMismatchScreen, false, false);
            NetworkManager.Disconnect();
            yield break;
        }
        if (!_RoleHasBeenSet)
        {
            UUI.UnlockMouse(false);
            TutorialController.movementAllowed = true;
            Main.Log($"Timeout");
            CustomUI.Open(CustomUI.ModMismatchScreen, false, false);
            NetworkManager.Disconnect();
            yield break;
        }

        CustomUI.Close();

        // Create offline save
        Main.Log($"[CLIENT] Creating offline save");
        SingletonBehaviour<NetworkSaveGameManager>.Instance.CreateOfflineBackup();

        if (!NetworkManager.IsHost())
        {
            CustomUI.OpenPopup("Connecting", "Loading savegame");
            Main.Log($"[CLIENT] Receiving savegame");
            AppUtil.Instance.PauseGame();
            // Check if host is connected if so the savegame should be available to receive
            SingletonBehaviour<NetworkJobsManager>.Instance.PlayerConnect();
            yield return new WaitUntil(() => localPlayers.ContainsKey(0) || modMismatched);
            if (modMismatched)
            {
                UUI.UnlockMouse(false);
                TutorialController.movementAllowed = true;
                Main.Log($"Mods Mismatched so disconnecting player");
                CustomUI.Open(CustomUI.ModMismatchScreen, false, false);
                NetworkManager.Disconnect();
                yield break;
            }

            // Wait till spawn is set
            yield return new WaitUntil(() => spawnData != null);

            AppUtil.Instance.UnpauseGame();
            yield return new WaitUntil(() => !AppUtil.IsPaused);
            yield return new WaitForEndOfFrame();
            PlayerManager.TeleportPlayer(spawnData.Position + WorldMover.currentMove, PlayerManager.PlayerTransform.rotation, null, false);
            UUI.UnlockMouse(true);

            // Wait till world is loaded
            yield return new WaitUntil(() => SingletonBehaviour<TerrainGrid>.Instance.IsInLoadedRegion(PlayerManager.PlayerTransform.position));
            AppUtil.Instance.PauseGame();
            yield return new WaitUntil(() => AppUtil.IsPaused);

            // Remove all Cars
            SingletonBehaviour<CarsSaveManager>.Instance.DeleteAllExistingCars();

            // Load Junction data from server that changed since uptime
            Main.Log($"Syncing Junctions");
            SingletonBehaviour<NetworkJunctionManager>.Instance.SyncJunction();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkJunctionManager>.Instance.IsSynced);

            // Load Turntable data from server that changed since uptime
            Main.Log($"Syncing Turntables");
            SingletonBehaviour<NetworkTurntableManager>.Instance.SyncTurntables();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkTurntableManager>.Instance.IsSynced);

            // Load Train data from server that changed since uptime
            Main.Log($"Syncing traincars");
            SingletonBehaviour<NetworkTrainManager>.Instance.SendInitCarsRequest();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced);
            SingletonBehaviour<NetworkSaveGameManager>.Instance.ResetDebts();

            // Load Job data from server that changed since uptime
            Main.Log($"Syncing jobs");
            SingletonBehaviour<NetworkJobsManager>.Instance.SendJobsRequest();
            yield return new WaitUntil(() => SingletonBehaviour<NetworkJobsManager>.Instance.IsSynced);

            AppUtil.Instance.UnpauseGame();
            yield return new WaitUntil(() => !AppUtil.IsPaused);
            yield return new WaitForEndOfFrame();
            CustomUI.Close();
        }
        else
        {
            if (spawnData != null)
            {
                CustomUI.OpenPopup("Connecting", "Loading savegame");
                Main.Log($"[CLIENT] Receiving savegame");
                AppUtil.Instance.PauseGame();
                // Expire single player jobs
                SingletonBehaviour<NetworkJobsManager>.Instance.PlayerConnect();

                // Teleport player
                AppUtil.Instance.UnpauseGame();
                yield return new WaitUntil(() => !AppUtil.IsPaused);
                yield return new WaitForEndOfFrame();
                PlayerManager.TeleportPlayer(spawnData.Position + WorldMover.currentMove, PlayerManager.PlayerTransform.rotation, null, false);
                UUI.UnlockMouse(true);

                // Wait till world is loaded
                yield return new WaitUntil(() => SingletonBehaviour<TerrainGrid>.Instance.IsInLoadedRegion(PlayerManager.PlayerTransform.position));
                AppUtil.Instance.PauseGame();
                yield return new WaitUntil(() => AppUtil.IsPaused);

                // Remove all Cars
                SingletonBehaviour<CarsSaveManager>.Instance.DeleteAllExistingCars();

                // Load Junction data from server that changed since uptime
                Main.Log($"Syncing Junctions");
                SingletonBehaviour<NetworkJunctionManager>.Instance.SyncJunction();
                yield return new WaitUntil(() => SingletonBehaviour<NetworkJunctionManager>.Instance.IsSynced);

                // Load Turntable data from server that changed since uptime
                Main.Log($"Syncing Turntables");
                SingletonBehaviour<NetworkTurntableManager>.Instance.SyncTurntables();
                yield return new WaitUntil(() => SingletonBehaviour<NetworkTurntableManager>.Instance.IsSynced);

                // Load Train data from server that changed since uptime
                Main.Log($"Syncing traincars");
                SingletonBehaviour<NetworkTrainManager>.Instance.SendInitCarsRequest();
                yield return new WaitUntil(() => SingletonBehaviour<NetworkTrainManager>.Instance.IsSynced);
                SingletonBehaviour<NetworkSaveGameManager>.Instance.ResetDebts();

                // Load Job data from server that changed since uptime
                Main.Log($"Syncing jobs");
                SingletonBehaviour<NetworkJobsManager>.Instance.SendJobsRequest();
                yield return new WaitUntil(() => SingletonBehaviour<NetworkJobsManager>.Instance.IsSynced);

                AppUtil.Instance.UnpauseGame();
                yield return new WaitUntil(() => !AppUtil.IsPaused);
                yield return new WaitForEndOfFrame();
                CustomUI.Close();
            }
            else
            {
                Main.Log("[CLIENT] > PLAYER_SPAWN_SET");
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    Vector3 pos = PlayerManager.PlayerTransform.position - WorldMover.currentMove;
                    KeyValuePair<string, Vector3> closestStation = SavedPositions.Stations.Where(pair => pair.Value == SavedPositions.Stations.Values.OrderBy(x => Vector3.Distance(x, pos)).First()).FirstOrDefault();
                    Main.Log($"Requesting spawn at: {closestStation.Key}");
                    writer.Write(new SetSpawn()
                    {
                        Position = closestStation.Value
                    });

                    writer.Write(SingletonBehaviour<Inventory>.Instance.PlayerMoney);

                    using (Message message = Message.Create((ushort)NetworkTags.PLAYER_SPAWN_SET, writer))
                        SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
                }

                Main.Log($"Save should be loaded. Run OnFinishedLoading in NetworkTrainManager");
                SingletonBehaviour<NetworkTrainManager>.Instance.OnFinishedLoading();
                yield return new WaitUntil(() => SingletonBehaviour<NetworkTrainManager>.Instance.SaveCarsLoaded);

                Main.Log($"Run OnFinishedLoading in NetworkJobsManager");
                SingletonBehaviour<NetworkJobsManager>.Instance.OnFinishLoading();
            }
        }
        SendIsLoaded();
        Main.Log($"Finished loading everything. Unlocking mouse and allow movement");
        UUI.UnlockMouse(false);
        TutorialController.movementAllowed = true;
        newPlayerConnecting = false;
        IsSynced = true;
        // Move to spawn
    }

    private void DisableSpawnMode()
    {
        GamePreferences.Set(Preferences.CommsRadioSpawnMode, false);
    }

    private void SendIsLoaded()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<PlayerLoaded>(new PlayerLoaded()
            {
                Id = SingletonBehaviour<UnityClient>.Instance.ID
            });

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_LOADED, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    /// <summary>
    /// This method is called upon the player disconnects.
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (SingletonBehaviour<UnityClient>.Instance)
            SingletonBehaviour<UnityClient>.Instance.MessageReceived -= MessageReceived;
        GamePreferences.UnregisterFromPreferenceUpdated(Preferences.CommsRadioSpawnMode, DisableSpawnMode);
        foreach (GameObject player in localPlayers.Values)
        {
            DestroyImmediate(player);
        }
        localPlayers.Clear();
        spawnData = null;
    }

    private void SpawnNetworkPlayer(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                newPlayerConnecting = true;
                NPlayer player = reader.ReadSerializable<NPlayer>();

                if (player.Id != SingletonBehaviour<UnityClient>.Instance.ID)
                {
                    Location playerPos = reader.ReadSerializable<Location>();
                    Main.Log($"[CLIENT] < PLAYER_SPAWN: Username: {player.Username} ");
                    if (IsSynced)
                        GameChat.PutSystemMessage($"{player.Username} has connected!");
                    else
                        GameChat.PutSystemMessage($"{player.Username} is already here!");
                    Vector3 pos = playerPos.Position + WorldMover.currentMove;
                    pos = new Vector3(pos.x, pos.y + 1, pos.z);
                    Quaternion rotation = Quaternion.identity;
                    if (playerPos.Rotation.HasValue)
                        rotation = playerPos.Rotation.Value;
                    GameObject playerObject = GetNewPlayerObject(pos, rotation, player.Username, player.Color);
                    WorldMover.Instance.AddObjectToMove(playerObject.transform);

                    NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
                    playerSync.Id = player.Id;
                    playerSync.Username = player.Username;
                    playerSync.Mods = player.Mods;
                    playerSync.IsLoaded = player.IsLoaded;
                    playerSync.Color = player.Color;

                    localPlayers.Add(player.Id, playerObject);
                    serverPlayers.Add(player.Id, player);
                    if (!player.IsLoaded)
                        WaitForPlayerLoaded();
                }
            }
        }
    }

    private void WaitForPlayerLoaded()
    {
        if (playersLoaded == null)
        {
            playersLoaded = SingletonBehaviour<CoroutineManager>.Instance.Run(WaitForAllPlayersLoaded());
        }
    }

    private IEnumerator WaitForAllPlayersLoaded()
    {
        CustomUI.OpenPopup("Incoming connection", "A new player is connecting");
        AppUtil.Instance.PauseGame();
        yield return new WaitUntil(() => localPlayers.All(p => p.Value.GetComponent<NetworkPlayerSync>().IsLoaded));
        AppUtil.Instance.UnpauseGame();
        playersLoaded = null;
        CustomUI.Close();
        newPlayerConnecting = false;
    }

    /// <summary>
    /// This method is called to send a position update to the server
    /// </summary>
    /// <param name="position">The players position</param>
    /// <param name="rotation">The players rotation</param>
    public void UpdateLocalPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        if (AppUtil.IsPaused)
            return;
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<Location>(new Location()
            {
                Id = SingletonBehaviour<UnityClient>.Instance.ID,
                Position = position - WorldMover.currentMove,
                Rotation = rotation,
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_LOCATION_UPDATE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Unreliable);
        }
    }

    internal void SendChatMessage(string chatMessage)
    {
        chatMessage = chatMessage.Insert(0, $"{GetLocalPlayerSync().Username}> ");
        GameChat.AppendNewMessage(chatMessage);
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write<ChatMessage>(new ChatMessage()
            {
                Message = chatMessage
            });

            using (Message message = Message.Create((ushort)NetworkTags.PLAYER_CHAT_MESSAGE, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void UpdateNetworkPositionAndRotation(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                Location location = reader.ReadSerializable<Location>();

                if (location.Id != SingletonBehaviour<UnityClient>.Instance.ID && localPlayers.TryGetValue(location.Id, out GameObject playerObject))
                {
                    Vector3 pos = location.Position;
                    pos = new Vector3(pos.x, pos.y + 1, pos.z);
                    NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
                    playerSync.UpdateLocation(pos, location.AproxPing, location.UpdatedAt, location.Rotation);
                }
            }
        }
    }

    /// <summary>
    /// Gets any player with the specified ID
    /// </summary>
    /// <param name="playerId">The network ID assigned to the player</param>
    /// <returns>GameObject of player</returns>
    internal GameObject GetPlayerById(ushort playerId)
    {
        if (localPlayers.ContainsKey(playerId))
            return localPlayers[playerId];
        else
            return null;
    }

    internal NPlayer GetNPlayerById(ushort playerId)
    {
        if (serverPlayers.ContainsKey(playerId))
            return serverPlayers[playerId];
        else
            return null;
    }

    /// <summary>
    /// Gets all player objects
    /// </summary>
    /// <returns>GameObjects of all player</returns>
    internal IEnumerable<GameObject> GetPlayers()
    {
        return localPlayers.Values;
    }

    /// <summary>
    /// Gets the local player gameobject
    /// </summary>
    /// <returns>Local Player GameObject</returns>
    internal GameObject GetLocalPlayer()
    {
        return PlayerManager.PlayerTransform.gameObject;
    }

    /// <summary>
    /// Gets the NetworkPlayerSync script of the local player
    /// </summary>
    /// <returns>NetworkPlayerSync of local player</returns>
    internal NetworkPlayerSync GetLocalPlayerSync()
    {
        return GetLocalPlayer().GetComponent<NetworkPlayerSync>();
    }

    /// <summary>
    /// Gets the NetworkPlayerSync script of the player with the specified ID
    /// </summary>
    /// <param name="playerId">The network ID assigned to the player</param>
    /// <returns>NetworkPlayerSync of player with the specified ID</returns>
    internal NetworkPlayerSync GetPlayerSyncById(ushort playerId)
    {
        if (localPlayers.ContainsKey(playerId))
            return localPlayers[playerId].GetComponent<NetworkPlayerSync>();
        else
            return null;
    }

    /// <summary>
    /// Gets the NetworkPlayerSync script of all non local players.
    /// </summary>
    /// <returns>Readonly list containing the NetworkPlayerSync script of all non local players</returns>
    internal IReadOnlyList<NetworkPlayerSync> GetAllNonLocalPlayerSync()
    {
        List<NetworkPlayerSync> networkPlayerSyncs = new List<NetworkPlayerSync>();
        foreach (GameObject playerObject in localPlayers.Values)
        {
            NetworkPlayerSync playerSync = playerObject.GetComponent<NetworkPlayerSync>();
            if (playerSync != null)
                networkPlayerSyncs.Add(playerSync);
        }
        return networkPlayerSyncs;
    }

    /// <summary>
    /// Gets the amount of players with the local player NOT included.
    /// </summary>
    /// <returns>The amount of players with the local player NOT included</returns>
    internal int GetPlayerCount()
    {
        return localPlayers.Count;
    }

    /// <summary>
    /// Gets all the player game objects that are in/on a given traincar.
    /// </summary>
    /// <param name="train">The requested traincar</param>
    /// <returns>An array containing all the players gameobjects in/on the given traincar</returns>
    internal GameObject[] GetPlayersInTrain(TrainCar train)
    {
        return localPlayers.Values.Where(p => p.GetComponent<NetworkPlayerSync>().Train?.CarGUID == train.CarGUID).ToArray();
    }

    internal GameObject[] GetPlayersInTrainSet(Trainset trainset)
    {
        List<GameObject> players = new List<GameObject>();
        foreach (TrainCar car in trainset.cars)
        {
            if (car)
                players.AddRange(GetPlayersInTrain(car));
        }
        return players.ToArray();
    }

    internal bool IsAnyoneInLocalPlayerRegion()
    {
        foreach (GameObject playerObject in localPlayers.Values)
        {
            if (SingletonBehaviour<TerrainGrid>.Instance.IsInLoadedRegion(playerObject.transform.position - WorldMover.currentMove))
            {
                return true;
            }
        }
        return false;
    }

    internal bool IsPlayerCloseToStation(GameObject player, StationController station)
    {
        StationJobGenerationRange stationRange = station.GetComponent<StationJobGenerationRange>();
        float playerSqrDistanceFromStationCenter = (player.transform.position - stationRange.stationCenterAnchor.position).sqrMagnitude;
        return stationRange.IsPlayerInJobGenerationZone(playerSqrDistanceFromStationCenter);
    }
}