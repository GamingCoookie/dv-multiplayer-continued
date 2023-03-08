using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DarkRift.Server.Unity;
using DVMultiplayer.Utils;
using DVMultiplayerContinued.Unity.Player;
using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DVMultiplayer.Networking
{
    public static class NetworkManager
    {
        public static UnityClient client;
        public static XmlUnityServer server;
        private static NetworkingUI UI;
        private static GameObject networkManager;
        internal static bool isHost;
        private static bool isTrueHost;
        private static bool isClient;
        private static bool isConnecting;
        private static string host;
        private static int port;
        internal static string username;
        private static bool scriptsInitialized = false;
        private static int tries = 1;
        private static PlayerDistanceMultipleGameObjectsOptimizer[] objectDisablers;

        /// <summary>
        /// Initializes the NetworkManager by:
        /// Spawning all the components needed.
        /// Listening to events.
        /// </summary>
        public static void Initialize()
        {
            Main.Log("Initializing NetworkManager");
            isHost = false;
            isClient = false;
            if (!UGameObject.Exists("NetworkManager"))
            {
                networkManager = Object.Instantiate(new GameObject(), Vector3.zero, Quaternion.identity);
                networkManager.name = "NetworkManager";
                server = networkManager.AddComponent<XmlUnityServer>();
                client = networkManager.AddComponent<UnityClient>();

                client.Disconnected += OnClientDisconnected;

                Object.DontDestroyOnLoad(networkManager);

                server.configuration = new TextAsset(File.ReadAllText("./Mods/DVMultiplayerContinued/Resources/config.xml"));
            }

            if (UI == null)
            {
                UI = new NetworkingUI();
                UI.Setup();
            }
        }

        private static void OnClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            SingletonBehaviour<CoroutineManager>.Instance.Run(DisconnectCoroutine());
        }

        private static IEnumerator DisconnectCoroutine()
        {
            yield return new WaitUntil(() => !CustomUI.currentScreen);
            yield return new WaitForEndOfFrame();
            if (scriptsInitialized)
            {
                yield return DeInitializeUnityScripts();
                scriptsInitialized = false;
            }
            foreach (PlayerDistanceMultipleGameObjectsOptimizer disabler in objectDisablers)
            {
                disabler.enabled = true;
            }
            isHost = false;
            isTrueHost = false;
            isClient = false;
            client.Close();
        }

        /// <summary>
        /// Deinitializing by destroying this gameobject.
        /// </summary>
        public static void Deinitialize()
        {
            Main.Log("Deinitializing NetworkManager");

            if (UGameObject.Exists("NetworkManager"))
            {
                GameObject.Destroy(GameObject.Find("NetworkManager"));
            }
        }

        /// <summary>
        /// Connects to the server with a given host and port
        /// </summary>
        /// <param name="host">The hostname to connect to</param>
        /// <param name="port">The port of the server</param>
        public static void Connect(string host, int port, string username)
        {
            NetworkManager.username = username;
            NetworkManager.host = host;
            NetworkManager.port = port;
            ClientConnect();
        }

        private static void ClientConnect()
        {
            if (isClient || isConnecting)
                return;

            isConnecting = true;
            Main.Log("[CLIENT] Connecting to server");
            client.ConnectInBackground(host, port, true, OnConnected);
        }

        /// <summary>
        /// Disconnects the client from the server.
        /// </summary>
        public static void Disconnect()
        {
            if (!isClient)
                return;

            Main.Log($"Disconnecting client");

            try
            {
                client.Disconnect();
                GameChat.PutSystemMessage("You've disconnected from a server");
            }
            catch (Exception ex)
            {
                Main.Log($"[ERROR] {ex.InnerException}");
            }
        }

        /// <summary>
        /// Starts up the game server and connects to it automatically
        /// </summary>
        public static void StartServer(string username, ushort port)
        {
            if (isHost)
                return;

            NetworkManager.username = username;
            Main.Log("Start hosting server");
            server.port = port;
            NetworkManager.port = port;
            try
            {
                SingletonBehaviour<CoroutineManager>.Instance.Run(StartHosting());
            }
            catch (Exception ex)
            {
                Main.mod.Logger.Error(ex.Message);
                server.Close();
            }
        }

        private static IEnumerator StartHosting()
        {
            server.Create();
            yield return new WaitUntil(() => server.CheckTCPSocketReady());
            Main.Log($"Server should be started connecting client now");
            isHost = true;
            isTrueHost = true;
            host = "127.0.0.1";
            ClientConnect();
        }

        /// <summary>
        /// Stops the hosted server and disconnects from it
        /// </summary>
        public static void StopServer()
        {
            if (!isHost)
                return;

            Main.Log("Stop hosting server");
            SingletonBehaviour<CoroutineManager>.Instance.Run(StopHosting());
        }

        private static IEnumerator StopHosting()
        {
            Disconnect();
            yield return new WaitUntil(() => !isClient);
            try
            {
                server.Close();
                isHost = false;
                TutorialController.movementAllowed = true;
            }
            catch (Exception ex)
            {
                Main.Log($"[ERROR] {ex.Message}");
            }
        }

        private static void OnConnected(Exception ex)
        {
            isConnecting = false;
            if (ex != null && !string.IsNullOrEmpty(ex.Message))
            {
                isClient = false;
                Main.Log($"[ERROR] {ex.Message}");
                Main.mod.Logger.Log($"[CLIENT] Connecting failed retrying..., tries: {tries}/5");
                if (tries < 5)
                {
                    tries++;
                    ClientConnect();
                }
                else
                {
                    Main.mod.Logger.Log($"[CLIENT] Connecting failed stopping retries.");
                    tries = 1;
                }
            }
            else
            {
                isClient = true;
                UI.HideUI();
                Main.Log($"Disabling autosave");
                SingletonBehaviour<SaveGameManager>.Instance.disableAutosave = true;
                CarSpawner.useCarPooling = false;
                if (!scriptsInitialized)
                {
                    Main.Log($"Client connected loading required unity scripts");
                    InitializeUnityScripts();
                    scriptsInitialized = true;
                }

                objectDisablers = GameObject.FindObjectsOfType<PlayerDistanceMultipleGameObjectsOptimizer>();
                foreach(PlayerDistanceMultipleGameObjectsOptimizer disabler in objectDisablers)
                {
                    disabler.enabled = false;
                }

                Main.Log($"Everything should be initialized running PlayerConnect method");
                SingletonBehaviour<NetworkPlayerManager>.Instance.PlayerConnect();
                Main.Log($"Connecting finished");
            }
        }

        internal static void SetIsHost(bool isHost)
        {
            Main.Log("[CLIENT] Set Role "+(isHost ? "HOST":"CLIENT"));
            NetworkManager.isHost = isHost;
        }
        
        private static void InitializeUnityScripts()
        {
            Main.Log($"[CLIENT] Initializing Player");
            NetworkPlayerSync playerSync = PlayerManager.PlayerTransform.gameObject.AddComponent<NetworkPlayerSync>();
            playerSync.IsLocal = true;
            playerSync.Username = username;
            playerSync.Id = client.ID;

            Main.Log($"[CLIENT] Initializing NetworkPlayerManager");
            networkManager.AddComponent<NetworkPlayerManager>();
            Main.Log($"[CLIENT] Initializing NetworkTrainManager");
            networkManager.AddComponent<NetworkTrainManager>();
            Main.Log($"[CLIENT] Initializing NetworkJunctionManager");
            networkManager.AddComponent<NetworkJunctionManager>();
            Main.Log($"[CLIENT] Initializing NetworkSaveGameManager");
            networkManager.AddComponent<NetworkSaveGameManager>();
            Main.Log($"[CLIENT] Initializing NetworkJobsManager");
            networkManager.AddComponent<NetworkJobsManager>();
            Main.Log($"[CLIENT] Initializing NetworkTurntableManager");
            networkManager.AddComponent<NetworkTurntableManager>();
            Main.Log($"[CLIENT] Initializing NetworkDebtManager");
            networkManager.AddComponent<NetworkDebtManager>();
        }

        private static IEnumerator DeInitializeUnityScripts()
        {
            Main.Log($"[DISCONNECTING] NetworkPlayerSync Deinitializing");
            Object.DestroyImmediate(PlayerManager.PlayerTransform.GetComponent<NetworkPlayerSync>());
            Main.Log($"[DISCONNECTING] NetworkPlayerManager Deinitializing");
            Object.DestroyImmediate(networkManager.GetComponent<NetworkPlayerManager>());
            Main.Log($"[DISCONNECTING] NetworkJobsManager Deinitializing");
            Object.DestroyImmediate(networkManager.GetComponent<NetworkJobsManager>());
            Main.Log($"[DISCONNECTING] NetworkTrainManager Deinitializing");
            Object.DestroyImmediate(networkManager.GetComponent<NetworkTrainManager>());
            Main.Log($"[DISCONNECTING] NetworkJunctionManager Deinitializing");
            Object.DestroyImmediate(networkManager.GetComponent<NetworkJunctionManager>());
            Main.Log($"[DISCONNECTING] NetworkTurntableManager Deinitializing");
            Object.DestroyImmediate(networkManager.GetComponent<NetworkTurntableManager>());
            Main.Log($"[DISCONNECTING] NetworkDebtManager Deinitializing");
            Object.DestroyImmediate(networkManager.GetComponent<NetworkDebtManager>());
            Main.Log($"[DISCONNECTING] NetworkSaveGameManager Deinitializing");
            networkManager.GetComponent<NetworkSaveGameManager>().PlayerDisconnect();
            yield return new WaitUntil(() => networkManager.GetComponent<NetworkSaveGameManager>().IsOfflineSaveLoaded);
            Main.Log("Offline Save Loaded");
            Object.DestroyImmediate(networkManager.GetComponent<NetworkSaveGameManager>());
        }

        /// <summary>
        /// Gets the value if the current local user is connected with a client.
        /// </summary>
        /// <returns>If the user is connected to a server as client</returns>
        public static bool IsClient()
        {
            return isClient;
        }

        /// <summary>
        /// Gets the value if the current local user has controlling authority over a server.
        /// </summary>
        /// <returns>If the user has lordship of a server</returns>
        public static bool IsHost()
        {
            return isHost;
        }

        /// <summary>
        /// Gets the value if the current local user is hosting a server.
        /// </summary>
        /// <returns>If the user is hosting a server</returns>
        public static bool IsTrueHost()
        {
            return isTrueHost;
        }
    }
}
