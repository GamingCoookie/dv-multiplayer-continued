using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DarkRift;
using DarkRift.Client.Unity;
using DV.CabControls;
using DVMultiplayer;
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
        public static void Initialize(ModEntry customCarLoaderEntry, Harmony harmony)
        {
            Main.Log("Patching own methods for CCL compatability...");
            try
            {
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
            }
            catch (Exception ex)
            {
                Main.Log($"Patching own methods for CCL compatability failed. Error: {ex.Message}");
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
                        Main.Log($"Get {car.ID} ControlImplBases");
                        ControlImplBase[] ctrls = car.interior.GetComponentsInChildren<ControlImplBase>();
                        Main.Log($"Found {ctrls.Length} ControlImplBases");
                        foreach (ControlImplBase control in ctrls)
                        {
                            train.Controls[control.name] = control.Value;
                        }
                    }
                    else if (car.GetComponent<CustomLocoSimDiesel>() != null)
                    {
                        Main.Log($"Found CustomLocoSimDiesel");
                        CustomLocoSimDiesel customDieselSim = car.GetComponent<CustomLocoSimDiesel>();
                        train.LocoStuff = new LocoStuff()
                        {
                            FuelLevel = customDieselSim.fuel.value,
                            OilLevel = customDieselSim.oil.value,
                            SandLevel = customDieselSim.sand.value,
                            Temp = customDieselSim.engineTemp.value
                        };
                        Main.Log($"Get {car.ID} ControlImplBases");
                        ControlImplBase[] ctrls = car.interior.GetComponentsInChildren<ControlImplBase>();
                        Main.Log($"Found {ctrls.Length} ControlImplBases");
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
}
