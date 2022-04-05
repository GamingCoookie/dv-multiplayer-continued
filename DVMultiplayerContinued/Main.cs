using DVMultiplayer.Networking;
using DVMultiplayer.Patches.PassengerJobs;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;

namespace DVMultiplayer
{
    public class Main
    {
        public static ModEntry mod;
        public static event Action OnGameFixedGUI;
        public static event Action OnGameUpdate;
        public static bool isInitialized = false;
        private static bool enabled = true;

        private static string[] AllowedMods = new string[]
        {
            "UnencryptedSaveGameMod",
            "DVMouseSmoothing",
            "DVSuperGauges",
            "ZSounds",
            "SkinManagerMod",
            "CargoSwapMod",
            "DVDiscordPresenceMod",
            "LocoLightsMod",
            "DVFPSLimiter",
            "DVOptimizer"
        };

        private static Harmony harmony;

        private static bool Load(ModEntry entry)
        {
            isInitialized = false;
            harmony = new Harmony(entry.Info.Id);
            mod = entry;
            mod.OnFixedGUI = OnFixedGUI;
            mod.OnUpdate = OnUpdate;
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ModEntry passengerJobsModEntry = FindMod("PassengerJobs");
            if (passengerJobsModEntry != null && passengerJobsModEntry.Active)
                PassengerJobsModInitializer.Initialize(passengerJobsModEntry, harmony);
            return true;
        }

        public static string[] GetEnabledMods()
        {
            return modEntries.Where(m => m.Active && m.Loaded).Select(m => m.Info.Id).Except(AllowedMods).ToArray();
        }

        private static void OnUpdate(ModEntry entry, float time)
        {
            if (!isInitialized && enabled && PlayerManager.PlayerTransform && !LoadingScreenManager.IsLoading && SingletonBehaviour<CanvasSpawner>.Instance)
            {
                Initialize();
            }

            if (enabled && isInitialized)
            {
#if DEBUG
                DebugUI.Update();
#endif
                OnGameUpdate?.Invoke();
            }
        }

        private static void OnFixedGUI(ModEntry entry)
        {
            if (enabled && isInitialized)
            {
#if DEBUG
                DebugUI.OnGUI();
#endif
                OnGameFixedGUI?.Invoke();
            }
        }

        private static void Initialize()
        {
            Log("Initializing...");
            CustomUI.Initialize();
            FavoritesManager.CreateFavoritesFileIfNotExists();
            NetworkManager.Initialize();
            isInitialized = true;
        }

        public static void Log(string msg)
        {
            if (mod.Info.Version.StartsWith("dev-"))
                mod.Logger.Log($"[DEBUG] {msg}");
            else
                mod.Logger.NativeLog($"[DEBUG] {msg}");

            CommandTerminal.Terminal.Log($"[DEBUG] {msg}");
        }
    }
}
