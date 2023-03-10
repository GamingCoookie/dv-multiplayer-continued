using DVMultiplayer.Networking;
using DVMultiplayer.Patches.PassengerJobs;
using DVMultiplayerContinued.Unity.Player;
using DVMultiplayerContinued.Patches.CustomCarLoader;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using static UnityModManagerNet.UnityModManager;
using DVMultiplayerContinued;
using UnityModManagerNet;

namespace DVMultiplayer
{
    public class Main
    {
        public static ModEntry mod;
        public static Settings Settings;
        private static Harmony harmony;
        public static event Action OnGameFixedGUI;
        public static event Action OnGameUpdate;
        public static bool isInitialized = false;
        private static bool enabled = true;
        private static readonly ModEntry CCLMod = FindMod("DVCustomCarLoader");
        private static bool IsCCLEnabled => CCLMod != null && CCLMod.Enabled;


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

        private static bool Load(ModEntry entry)
        {
            isInitialized = false;
            harmony = new Harmony(entry.Info.Id);
            mod = entry;
            Settings = UnityModManager.ModSettings.Load<Settings>(mod);
            mod.OnFixedGUI = OnFixedGUI;
            mod.OnUpdate = OnUpdate;
            mod.OnGUI = OnGUI;
            mod.OnSaveGUI = OnSaveGUI;
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ModEntry passengerJobsModEntry = FindMod("PassengerJobs");
            if (passengerJobsModEntry != null && passengerJobsModEntry.Enabled)
                PassengerJobsModInitializer.Initialize(passengerJobsModEntry, harmony);
            if (IsCCLEnabled)
                CustomCarLoaderInitializer.Initialize(CCLMod, harmony);
            return true;
        }

        public static string[] GetEnabledMods()
        {
            return modEntries.Where(m => m.Active && m.Loaded).Select(m => m.Info.Id).Except(AllowedMods).ToArray();
        }

        public static string[] GetCCLCars()
        {
            return !IsCCLEnabled ? Array.Empty<string>() : CustomCarLoaderInitializer.Cars;
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

        private static void OnGUI(ModEntry mod)
        {
            Settings.Draw(mod);
        }

        private static void OnSaveGUI(ModEntry mod)
        {
            Settings.Save(mod);
        }

        private static void Initialize()
        {
            Log("Initializing...");
            CustomUI.Initialize();
            FavoritesManager.CreateFavoritesFileIfNotExists();
            NetworkManager.Initialize();
            GameChat.Setup();
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
