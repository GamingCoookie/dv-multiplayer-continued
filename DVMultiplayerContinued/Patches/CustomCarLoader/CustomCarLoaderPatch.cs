using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using DarkRift;
using DarkRift.Client.Unity;
using DV.CabControls;
using DVCustomCarLoader;
using DVMultiplayer.Networking;
using DVMultiplayer.DTO.Train;
using DVCustomCarLoader.LocoComponents.Steam;
using DVCustomCarLoader.LocoComponents.DieselElectric;
using HarmonyLib;
using UnityEngine;
using static UnityModManagerNet.UnityModManager;
using Main = DVMultiplayer.Main;

namespace DVMultiplayerContinued.Patches.CustomCarLoader
{
    public static class CustomCarLoaderInitializer
    {

        public static string[] Cars { get; private set; }
        
        public static void Initialize(ModEntry customCarLoaderEntry, Harmony harmony)
        {
            Main.Log("Patching for CCL compatibility...");
            try
            {
                Cars = CustomCarManager.CustomCarTypes.Select(car => car.identifier).ToArray();
                Main.Log($"Patching NetworkTrainSync.Awake");
                MethodInfo NetworkTrainSyncAwake = AccessTools.Method(typeof(NetworkTrainSync), nameof(NetworkTrainSync.Awake));
                MethodInfo NetworkTrainSyncAwakePostfix = AccessTools.Method(typeof(NetworkTrainSync_Awake_Patch), nameof(NetworkTrainSync_Awake_Patch.Postfix));
                harmony.Patch(NetworkTrainSyncAwake, postfix: new HarmonyMethod(NetworkTrainSyncAwakePostfix));
                Main.Log($"Patching NetworkTrainManager.SendNewLocoValue");
                MethodInfo NetworkTrainManagerSendNewLocoValue = AccessTools.Method(typeof(NetworkTrainManager), nameof(NetworkTrainManager.SendNewLocoValue));
                MethodInfo NetworkTrainManagerSendNewLocoValuePostfix = AccessTools.Method(typeof(NetworkTrainManager_SendNewLocoValue_Patch), nameof(NetworkTrainManager_SendNewLocoValue_Patch.Postfix));
                harmony.Patch(NetworkTrainManagerSendNewLocoValue, postfix: new HarmonyMethod(NetworkTrainManagerSendNewLocoValuePostfix));
                Main.Log($"Patching NetworkTrainManager.UpdateLocoValue");
                MethodInfo NetworkTrainManagerUpdateLocoValue = AccessTools.Method(typeof(NetworkTrainManager), nameof(NetworkTrainManager.UpdateLocoValue));
                MethodInfo NetworkTrainManagerUpdateLocoValuePatch = AccessTools.Method(typeof(NetworkTrainManager_UpdateLocoValue_Patch), nameof(NetworkTrainManager_UpdateLocoValue_Patch.Postfix));
                harmony.Patch(NetworkTrainManagerUpdateLocoValue, postfix: new HarmonyMethod(NetworkTrainManagerUpdateLocoValuePatch));
                Main.Log($"Patching NetworkTrainManager.SyncLocomotiveWithServerState");
                MethodInfo NetworkTrainManagerSyncLocomotive = AccessTools.Method(typeof(NetworkTrainManager), nameof(NetworkTrainManager.SyncLocomotiveWithServerState));
                MethodInfo NetworkTrainManagerSyncLocomotivePatch = AccessTools.Method(typeof(NetworkTrainManager_SyncLocomotiveWithServerState_Patch), nameof(NetworkTrainManager_SyncLocomotiveWithServerState_Patch.Postfix));
                harmony.Patch(NetworkTrainManagerSyncLocomotive, postfix: new HarmonyMethod(NetworkTrainManagerSyncLocomotivePatch));
                Main.Log($"Patching NetworkTrainManager.GenerateServerCarsData");
                MethodInfo NTMGSCD = AccessTools.Method(typeof(NetworkTrainManager), nameof(NetworkTrainManager.GenerateServerCarsData));
                MethodInfo NTMGSCDPatch = AccessTools.Method(typeof(NetworkTrainManager_GenerateServerCarsData_Patch), nameof(NetworkTrainManager_GenerateServerCarsData_Patch.Postfix));
                harmony.Patch(NTMGSCD, postfix: new HarmonyMethod(NTMGSCDPatch));
                Main.Log($"Patching NetworkTrainManager.SendNewCarsSpawned");
                MethodInfo SendNewCarsSpawned = AccessTools.Method(typeof(NetworkTrainManager), nameof(NetworkTrainManager.SendNewCarsSpawned));
                MethodInfo SendNewCarsSpawnedPrefix = AccessTools.Method(typeof(DVMultiplayerContinued_SendNewCarsSpawned_Patch), "Prefix");
                harmony.Patch(SendNewCarsSpawned, prefix: new HarmonyMethod(SendNewCarsSpawnedPrefix));
                Main.Log($"Patching NetworkTrainPosSync.CoroUpdateLocation");
                MethodInfo CoroUpdateLocation = AccessTools.Method(typeof(NetworkTrainPosSync), nameof(NetworkTrainPosSync.CoroUpdateLocation));
                MethodInfo CoroUpdateLocationPostfix = AccessTools.Method(typeof(DVMultiplayerContinued_CoroUpdateLocation_Patch), "Postfix");
                harmony.Patch(CoroUpdateLocation, postfix: new HarmonyMethod(CoroUpdateLocationPostfix));
                Main.Log($"Patching NetworkTrainManager.SendCarLocationUpdate");
                MethodInfo SendCarLocationUpdate = AccessTools.Method(typeof(NetworkTrainManager), nameof(NetworkTrainManager.SendCarLocationUpdate));
                MethodInfo SendCarLocationUpdatePrefix = AccessTools.Method(typeof(DVMultiplayerContinued_SendCarLocationUpdate_Patch), "Prefix");
                harmony.Patch(SendCarLocationUpdate, prefix: new HarmonyMethod(SendCarLocationUpdatePrefix));
                Main.Log($"Patching CustomLocoSimDiesel&Steam.SimulateTick");
                MethodInfo DieselSimulateTick = AccessTools.Method(typeof(CustomLocoSimDiesel), "SimulateTick");
                MethodInfo DieselSimulateTickPrefix = AccessTools.Method(typeof(SimulationPatches), "DieselSimTickPatch");
                MethodInfo SteamSimulateTick = AccessTools.Method(typeof(CustomLocoSimSteam), "SimulateTick");
                MethodInfo SteamSimulateTickPrefix = AccessTools.Method(typeof(SimulationPatches), "SteamSimTickPatch");
                harmony.Patch(DieselSimulateTick, prefix: new HarmonyMethod(DieselSimulateTickPrefix));
                harmony.Patch(SteamSimulateTick, prefix: new HarmonyMethod(SteamSimulateTickPrefix));
                Main.Log($"Patching NetworkTrainPosSync.LoadLocoDamage");
                MethodInfo LoadLocoDamage = AccessTools.Method(typeof(NetworkTrainPosSync), "LoadLocoDamage");
                MethodInfo LoadLocoDamagePostFix = AccessTools.Method(typeof(NetworkTrainPosSync_LoadLocoDamage_Patch), "Postfix");
                harmony.Patch(LoadLocoDamage, postfix: new HarmonyMethod(LoadLocoDamagePostFix));
                Main.Log($"Patching NetworkTrainManager.SendInitializedCars");
                MethodInfo SendInitCars = AccessTools.Method(typeof(NetworkTrainManager), "SendInitializedCars");
                MethodInfo SendInitCarsPrefix = AccessTools.Method(typeof(NetworkTrainManager_SendInitCars_Patch), "Prefix");
                harmony.Patch(SendInitCars, prefix: new HarmonyMethod(SendInitCarsPrefix));
                MethodInfo RaiseCarSpawned = AccessTools.Method(typeof(CarSpawner_Patches), "RaiseCarSpawned");
                MethodInfo RaiseCarSpawnedPostfix = AccessTools.Method(typeof(CarSpawner_Patches_RaiseCarSpawned), "Postfix");
                harmony.Patch(RaiseCarSpawned, prefix: new HarmonyMethod(RaiseCarSpawnedPostfix));
            }
            catch (Exception ex)
            {
                Main.Log($"Patching methods for CCL compatability. Error: {ex.Message}");
            }
        }
    }

    class NetworkTrainSync_Awake_Patch
    {
        public static void Postfix(NetworkTrainSync __instance)
        {
            if (__instance.loco.GetComponent<CustomLocoSimSteam>() != null)
            {
                SingletonBehaviour<CoroutineManager>.Instance.Run(CheckCustomLocoValues(__instance.loco));
            }
        }

        public static IEnumerator CheckCustomLocoValues(TrainCar loco)
        {
            CustomLocoSimSteam customSteamSim = loco.GetComponent<CustomLocoSimSteam>();
            while (true)
            {
                if (customSteamSim != null)
                {
                    var fireOn = customSteamSim.fireOn.value;
                    var fireboxCoal = customSteamSim.fireboxFuel.value;
                    var tenderCoal = customSteamSim.tenderFuel.value;
                    yield return new WaitUntil(() => customSteamSim.fireOn.value != fireOn || customSteamSim.fireboxFuel.value > fireboxCoal || customSteamSim.tenderFuel.value < tenderCoal);
                }
                else
                    yield break;

                if (!SingletonBehaviour<NetworkTrainManager>.Instance || !loco || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork)
                    continue;

                SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoValue(loco);
            }
        }
    }
    class NetworkTrainManager_SendNewLocoValue_Patch
    {
        public static void Postfix(NetworkTrainManager __instance, TrainCar loco) 
        {
            bool send = false;
            WorldTrain serverState = __instance.serverCarStates.FirstOrDefault(t => t.Guid == loco.CarGUID);
            LocoStuff locoStuff = serverState.LocoStuff;
            if (loco.GetComponent<CustomLocoSimSteam>() != null)
            {
                CustomLocoSimSteam customSteamSimulation = loco.GetComponent<CustomLocoSimSteam>();
                float fireOn = customSteamSimulation.fireOn.value;
                float coalInFireBox = customSteamSimulation.fireboxFuel.value;
                float tenderCoal = customSteamSimulation.tenderFuel.value;

                if (!(fireOn == 1f && locoStuff.FireOn) && (fireOn != 0f && !locoStuff.FireOn))
                {
                    //Main.Log($"Fire state is now {fireOn}");
                    send = true;
                }
                if (coalInFireBox > locoStuff.FireboxCoalLevel)
                {
                    //Main.Log($"Coal in Firebox is now {coalInFireBox}");
                    send = true;
                }
                if (tenderCoal < locoStuff.TenderCoalLevel)
                {
                    //Main.Log($"Coal in Tender is now {tenderCoal}");
                    send = true;
                }
                locoStuff.FireboxCoalLevel = coalInFireBox;
                locoStuff.TenderCoalLevel = tenderCoal;
                locoStuff.FireOn = fireOn == 1f;
            }

            if (send)
            {
                Main.Log($"[CLIENT] > TRAIN_SYNC - for {loco.ID} - on demand - CCL loco");
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write<WorldTrain>(serverState);

                    using (Message message = Message.Create((ushort)NetworkTags.TRAIN_SYNC, writer))
                        SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
                }
            }
        }
    }

    class NetworkTrainManager_UpdateLocoValue_Patch
    {
        public static void Postfix(NetworkTrainManager __instance, TrainCar loco)
        {
            bool send = false;
            WorldTrain serverState = __instance.serverCarStates.FirstOrDefault(t => t.Guid == loco.CarGUID);

            if (loco.GetComponent<CustomLocoSimSteam>() != null)
            {
                CustomLocoSimSteam customSteamSim = loco.GetComponent<CustomLocoSimSteam>();
                serverState.LocoStuff = new LocoStuff()
                {
                    BoilerPressure = customSteamSim.boilerPressure.value,
                    BoilerWaterLevel = customSteamSim.boilerWater.value,
                    FireboxCoalLevel = customSteamSim.fireboxFuel.value,
                    FireOn = customSteamSim.fireOn.value == 1,
                    SandLevel = customSteamSim.sand.value,
                    Temp = customSteamSim.temperature.value,
                    TenderCoalLevel = customSteamSim.tenderFuel.value,
                    TenderWaterLevel = customSteamSim.tenderWater.value
                };
                send = true;
            }
            else if (loco.GetComponent<CustomLocoSimDiesel>() != null)
            {
                CustomLocoSimDiesel customDieselSim = loco.GetComponent<CustomLocoSimDiesel>();
                serverState.LocoStuff = new LocoStuff()
                {
                    FuelLevel = customDieselSim.fuel.value,
                    OilLevel = customDieselSim.oil.value,
                    SandLevel = customDieselSim.sand.value,
                    Temp = customDieselSim.engineTemp.value
                };
                send = true;
            }
            if (send)
            {
                Main.Log($"[CLIENT] > TRAIN_SYNC - for {loco.ID} - periodic - CCL loco");
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write<WorldTrain>(serverState);

                    using (Message message = Message.Create((ushort)NetworkTags.TRAIN_SYNC, writer))
                        SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Unreliable);
                }
            }
        }
    }

    class NetworkTrainManager_SyncLocomotiveWithServerState_Patch
    {
        public static void Postfix(TrainCar train, WorldTrain serverState)
        {
            if (train.GetComponent<CustomLocoSimDiesel>() != null)
            {
                Main.Log($"CCL Diesel engine found");
                CustomLocoSimDiesel customDieselSim = train.GetComponent<CustomLocoSimDiesel>();
                customDieselSim.fuel.SetValue(serverState.LocoStuff.FuelLevel);
                customDieselSim.oil.SetValue(serverState.LocoStuff.OilLevel);
                customDieselSim.engineRPM.SetValue(serverState.LocoStuff.RPM);
                customDieselSim.sand.SetValue(serverState.LocoStuff.SandLevel);
                customDieselSim.engineTemp.SetValue(serverState.LocoStuff.Temp);
            }
            else if (train.GetComponent<CustomLocoSimSteam>() != null)
            {
                Main.Log($"CCL Steam engine found");
                CustomLocoSimSteam customSteamSim = train.GetComponent<CustomLocoSimSteam>();
                customSteamSim.boilerPressure.SetValue(serverState.LocoStuff.BoilerPressure);
                customSteamSim.boilerWater.SetValue(serverState.LocoStuff.BoilerWaterLevel);
                customSteamSim.fireboxFuel.SetValue(serverState.LocoStuff.FireboxCoalLevel);
                customSteamSim.fireOn.SetValue(serverState.LocoStuff.FireOn ? 1 : 0);
                customSteamSim.sand.SetValue(serverState.LocoStuff.SandLevel);
                customSteamSim.temperature.SetValue(serverState.LocoStuff.Temp);
                customSteamSim.tenderFuel.SetValue(serverState.LocoStuff.TenderCoalLevel);
                customSteamSim.tenderWater.SetValue(serverState.LocoStuff.TenderWaterLevel);
            }
        }
    }
    class NetworkTrainManager_GenerateServerCarsData_Patch
    {
        public static void Postfix(NetworkTrainManager __instance, ref List<WorldTrain> __result, IEnumerable<TrainCar> cars)
        {
            List<WorldTrain> data = __result;
            foreach (TrainCar car in cars)
            {
                WorldTrain train = data.FirstOrDefault(t => t.Guid == car.CarGUID);
                if (car.IsLoco)
                {
                    if (car.GetComponent<CustomLocoSimSteam>() != null)
                    {
                        Main.Log($"Found CustomLocoSimSteam");
                        CustomLocoSimSteam customSteamSim = car.GetComponent<CustomLocoSimSteam>();
                        train.CarHealthData = car.GetComponent<CustomDamageControllerSteam>().GetDamageSaveData().ToString(Newtonsoft.Json.Formatting.None);
                        train.LocoStuff = new LocoStuff()
                        {
                            BoilerPressure = customSteamSim.boilerPressure.value,
                            BoilerWaterLevel = customSteamSim.boilerWater.value,
                            FireboxCoalLevel = customSteamSim.fireboxFuel.value,
                            FireOn = customSteamSim.fireOn.value == 1,
                            SandLevel = customSteamSim.sand.value,
                            Temp = customSteamSim.temperature.value,
                            TenderCoalLevel = customSteamSim.tenderFuel.value,
                            TenderWaterLevel = customSteamSim.tenderWater.value
                        };
                        //Main.Log($"Get {car.ID} ControlImplBases");
                        ControlImplBase[] ctrls = car.interior.GetComponentsInChildren<ControlImplBase>();
                        //Main.Log($"Found {ctrls.Length} ControlImplBases");
                        foreach (ControlImplBase control in ctrls)
                        {
                            train.Controls[control.name] = control.Value;
                        }
                    }
                    else if (car.GetComponent<CustomLocoSimDiesel>() != null)
                    {
                        Main.Log($"Found CustomLocoSimDiesel");
                        CustomLocoSimDiesel customDieselSim = car.GetComponent<CustomLocoSimDiesel>();
                        train.CarHealthData = car.GetComponent<DamageControllerCustomDiesel>().GetDamageSaveData().ToString(Newtonsoft.Json.Formatting.None);
                        train.LocoStuff = new LocoStuff()
                        {
                            FuelLevel = customDieselSim.fuel.value,
                            OilLevel = customDieselSim.oil.value,
                            RPM = customDieselSim.engineRPM.value,
                            SandLevel = customDieselSim.sand.value,
                            Temp = customDieselSim.engineTemp.value
                        };
                        //Main.Log($"Get {car.ID} ControlImplBases");
                        ControlImplBase[] ctrls = car.interior.GetComponentsInChildren<ControlImplBase>();
                        //Main.Log($"Found {ctrls.Length} ControlImplBases");
                        foreach (ControlImplBase control in ctrls)
                        {
                            train.Controls[control.name] = control.Value;
                        }
                    }
                }
            }
            __result = data;
        }
    }

    class DVMultiplayerContinued_SendNewCarsSpawned_Patch
    {
        static bool Prefix(NetworkTrainManager __instance, IEnumerable<TrainCar> cars)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                foreach (TrainCar car in cars)
                {
                    __instance.AddNetworkingScripts(car, null);
                }

                WorldTrain[] newServerTrains = __instance.GenerateServerCarsData(cars).ToArray();
                foreach (WorldTrain newServerTrain in newServerTrains)
                {
                    if (DVCustomCarLoader.CarTypeInjector.TryGetCustomCarByType(newServerTrain.CarType, out DVCustomCarLoader.CustomCar customCar))
                    {
                        Main.Log($"custom car identifier: {customCar.identifier}");
                        newServerTrain.CCLCarId = customCar.identifier;
                        newServerTrain.CarType = TrainCarType.NotSet;
                    }
                    else
                        Main.Log("What the fuck there's no custom car identifier!");
                }
                __instance.serverCarStates.AddRange(newServerTrains);
                __instance.localCars.AddRange(cars);
                writer.Write(newServerTrains);
                Main.Log($"[CLIENT] > TRAINS_INIT: {newServerTrains.Length}");

                using (Message message = Message.Create((ushort)NetworkTags.TRAINS_INIT, writer))
                    SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
            }
            return false;
        }
    }

    class DVMultiplayerContinued_CoroUpdateLocation_Patch
    {
        static void Postfix(NetworkTrainPosSync __instance, TrainLocation location)
        {
            if (__instance.hasLocalPlayerAuthority)
                return;

            CustomLocoSimDiesel customDiesel = __instance.trainCar.GetComponent<CustomLocoSimDiesel>();
            if (customDiesel)
            {
                customDiesel.engineRPM.SetValue(location.RPM);
                customDiesel.engineTemp.SetValue(location.Temperature);
            }
        }
    }

    class DVMultiplayerContinued_SendCarLocationUpdate_Patch
    {
        static bool Prefix(NetworkTrainManager __instance, TrainCar trainCar, bool reliable = false)
        {
            if (!__instance.IsSynced)
                return false;

            //Main.Log($"[CLIENT] > TRAIN_LOCATION_UPDATE: TrainID: {trainCar.ID}");

            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                List<TrainLocation> locations = new List<TrainLocation>();
                foreach (TrainCar car in trainCar.trainset.cars)
                {
                    List<TrainBogie> bogies = new List<TrainBogie>();
                    foreach (Bogie bogie in car.Bogies)
                    {
                        bogies.Add(new TrainBogie()
                        {
                            TrackName = bogie.HasDerailed ? "" : bogie.track.name,
                            Derailed = bogie.HasDerailed,
                            PositionAlongTrack = bogie.HasDerailed ? 0 : bogie.traveller.pointRelativeSpan + bogie.traveller.curPoint.span,
                        });
                    }

                    TrainLocation loc = new TrainLocation()
                    {
                        TrainId = car.CarGUID,
                        Forward = car.transform.forward,
                        Position = car.transform.position - WorldMover.currentMove,
                        Rotation = car.transform.rotation,
                        Bogies = bogies.ToArray(),
                        IsStationary = car.isStationary,
                        Velocity = car.rb.velocity,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    LocoControllerShunter controllerShunter = car.GetComponent<LocoControllerShunter>();
                    LocoControllerDiesel controllerDiesel = car.GetComponent<LocoControllerDiesel>();
                    CustomLocoSimDiesel customDiesel = car.GetComponent<CustomLocoSimDiesel>();

                    if (controllerShunter)
                    {
                        loc.RPM = controllerShunter.GetEngineRPM();
                        loc.Temperature = controllerShunter.GetEngineTemp();
                    }
                    else if (controllerDiesel)
                    {
                        loc.RPM = controllerDiesel.GetEngineRPM();
                        loc.Temperature = controllerDiesel.GetEngineTemp();
                    }
                    else if (customDiesel)
                    {
                        loc.RPM = customDiesel.engineRPM.value;
                        loc.Temperature = customDiesel.engineTemp.value;
                    }

                    locations.Add(loc);
                }

                writer.Write(locations.ToArray());

                using (Message message = Message.Create((ushort)NetworkTags.TRAIN_LOCATION_UPDATE, writer))
                    SingletonBehaviour<UnityClient>.Instance.SendMessage(message, reliable ? SendMode.Reliable : SendMode.Unreliable);
            }
            return false;
        }
    }

    class SimulationPatches
    {
        static bool DieselSimTickPatch(CustomLocoSimDiesel __instance)
        {
            if (NetworkManager.IsClient())
            {
                NetworkTrainPosSync networking = __instance.GetComponent<NetworkTrainPosSync>();
                if (networking)
                {
                    return networking.hasLocalPlayerAuthority;
                }
            }
            return true;
        }

        static bool SteamSimTickPatch(CustomLocoSimSteam __instance)
        {
            if (NetworkManager.IsClient())
            {
                NetworkTrainPosSync networking = __instance.GetComponent<NetworkTrainPosSync>();
                if (networking)
                {
                    return networking.hasLocalPlayerAuthority;
                }
            }
            return true;
        }
    }

    class NetworkTrainPosSync_LoadLocoDamage_Patch
    {
        static void Postfix(NetworkTrainPosSync __instance, string carHealthData)
        {
            DamageControllerCustomDiesel customDamageDiesel = __instance.trainCar.GetComponent<DamageControllerCustomDiesel>();
            CustomDamageControllerSteam customDamageSteam = __instance.trainCar.GetComponent<CustomDamageControllerSteam>();
            if (customDamageDiesel != null)
                customDamageDiesel.LoadDamagesState(JObject.Parse(carHealthData));
            else if (customDamageSteam != null)
                customDamageSteam.LoadDamagesState(JObject.Parse(carHealthData));
        }
    }

    class NetworkTrainManager_SendInitCars_Patch
    {
        static bool Prefix(NetworkTrainManager __instance)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                __instance.serverCarStates.Clear();
                Main.Log($"Host synching trains with server. Train amount: {__instance.localCars.Count}");
                __instance.serverCarStates.AddRange(__instance.GenerateServerCarsData(__instance.localCars));

                Main.Log($"[CLIENT] > TRAIN_HOSTSYNC: AmountOfTrains: {__instance.serverCarStates.Count}");

                foreach (WorldTrain serverCarState in __instance.serverCarStates)
                {
                    if (DVCustomCarLoader.CarTypeInjector.TryGetCustomCarByType(serverCarState.CarType, out DVCustomCarLoader.CustomCar customCar))
                    {
                        Main.Log($"custom car identifier: {customCar.identifier}");
                        serverCarState.CCLCarId = customCar.identifier;
                        serverCarState.CarType = TrainCarType.NotSet;
                    }
                    else
                        Main.Log("What the fuck there's no custom car identifier!");
                }

                writer.Write(__instance.serverCarStates.ToArray());

                using (Message message = Message.Create((ushort)NetworkTags.TRAIN_HOST_SYNC, writer))
                    SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
            }
            __instance.IsSynced = true;
            return false;
        }
    }

    class CarSpawner_Patches_RaiseCarSpawned
    {
        static void Postfix(TrainCar car)
        {
            NetworkTrainManager ntm = SingletonBehaviour<NetworkTrainManager>.Instance;
            if (ntm == null) return;
            ntm.OnCarSpawned(car);
        }
    }
}
