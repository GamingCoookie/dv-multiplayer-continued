using DarkRift;
using DarkRift.Client.Unity;
using DV.Logic.Job;
using DVMultiplayer.Networking;
using DVMultiplayer.DTO.Train;
using HarmonyLib;
using PassengerJobsMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;

namespace DVMultiplayer.Patches.PassengerJobs
{
    public static class PassengerJobsModInitializer
    {
        public static void Initialize(UnityModManager.ModEntry passengerJobsModEntry, Harmony harmony)
        {
            Main.Log("Patching passenger jobs...");
            try
            {
                // Passenger Jobs spawning
                Type passengerJobsGen = passengerJobsModEntry.Assembly.GetType("PassengerJobsMod.PassengerJobGenerator", true);

                // Patch StartGenerationAsync method
                Main.Log("Patching PassengerJobsMod.PassengerJobGenerator.StartGenerationAsync");
                MethodInfo StartGenerationAsync = AccessTools.Method(passengerJobsGen, "StartGenerationAsync");
                MethodInfo StartGenerationAsyncPrefix = AccessTools.Method(typeof(PassengerJobs_StartGenerationAsync_Patch), "Prefix");
                harmony.Patch(StartGenerationAsync, prefix: new HarmonyMethod(StartGenerationAsyncPrefix));

                // Patch GenerateNewTransportJob method
                Main.Log("Patching PassengerJobsMod.PassengerJobGenerator.GenerateNewTransportJob");
                MethodInfo GenerateNewTransportJob = AccessTools.Method(passengerJobsGen, "GenerateNewTransportJob");
                MethodInfo GenerateNewTransportJobPostfix = AccessTools.Method(typeof(PassengerJobs_GenerateNewTransportJob_Patch), "Postfix");
                harmony.Patch(GenerateNewTransportJob, postfix: new HarmonyMethod(GenerateNewTransportJobPostfix));

                // Patch GenerateNewCommuterRun method
                Main.Log("Patching PassengerJobsMod.PassengerJobGenerator.GenerateNewCommuterRun");
                MethodInfo GenerateNewCommuterRun = AccessTools.Method(passengerJobsGen, "GenerateNewCommuterRun");
                MethodInfo GenerateNewCommuterRunPostfix = AccessTools.Method(typeof(PassengerJobs_GenerateNewCommuterRun_Patch), "Postfix");
                harmony.Patch(GenerateNewCommuterRun, postfix: new HarmonyMethod(GenerateNewCommuterRunPostfix));

                // Patch GenerateCommuterReturnTrip method
                Main.Log("Patching PassengerJobsMod.PassengerJobGenerator.GenerateCommuterReturnTrip");
                MethodInfo GenerateCommuterReturnTrip = AccessTools.Method(passengerJobsGen, "GenerateCommuterReturnTrip");
                MethodInfo GenerateCommuterReturnTripPostfix = AccessTools.Method(typeof(PassengerJobs_GenerateCommuterReturnTrip_Patch), "Postfix");
                harmony.Patch(GenerateCommuterReturnTrip, postfix: new HarmonyMethod(GenerateCommuterReturnTripPostfix));

                // Patch GetJobTypeFromDefinition method
                Main.Log("Patching DVMultiplayer.NetworkJobsManager.GetJobTypeFromDefinition");
                MethodInfo GetJobTypeFromDefinition = AccessTools.Method(typeof(NetworkJobsManager), "GetJobTypeFromDefinition");
                MethodInfo GetJobTypeFromDefinitionPostfix = AccessTools.Method(typeof(PassengerJobs_GetJobTypeFromDefinition_Patch), "Postfix");
                harmony.Patch(GetJobTypeFromDefinition, postfix: new HarmonyMethod(GetJobTypeFromDefinitionPostfix));

                Main.Log("Patching DVMultiplayerContinued.NetworkTrainManager.OnCargoChangeMessage");
                MethodInfo OnCargoChangeMessage = AccessTools.Method(typeof(NetworkTrainManager), nameof(NetworkTrainManager.OnCargoChangeMessage));
                MethodInfo OnCargoChangeMessagePrefix = AccessTools.Method(typeof(DVMultiplayer_OnCargoChangeMessage_Patch), "Prefix");
                harmony.Patch(OnCargoChangeMessage, prefix: new HarmonyMethod(OnCargoChangeMessagePrefix));

                Main.Log("Patching DVMultiplayerContinued.NetworkTrainManager.CargoStateChanged");
                MethodInfo CargoStateChanged = AccessTools.Method(typeof(NetworkTrainManager), nameof(NetworkTrainManager.CargoStateChanged));
                MethodInfo CargoStateChangedPrefix = AccessTools.Method(typeof(DVMultiplayer_CargoStateChanged_Patch), "Prefix");
                harmony.Patch(CargoStateChanged, prefix: new HarmonyMethod(CargoStateChangedPrefix));
            }
            catch(Exception ex)
            {
                Main.Log($"Patching passenger jobs failed. Error: {ex.Message}");
            }
        }
    }

    class PassengerJobs_StartGenerationAsync_Patch
    {
        static bool Prefix()
        {
            return !NetworkManager.IsClient() || NetworkManager.IsHost();
        }
    }

    class PassengerJobs_GenerateNewTransportJob_Patch
    {
        static void Postfix(PassengerTransportChainController __result, PassengerJobGenerator __instance, TrainCarsPerLogicTrack consistInfo = null)
        {
            if (NetworkManager.IsHost())
            {
                if (__instance.Controller && __instance.Controller.GetComponent<NetworkJobsSync>())
                {
                    NetworkJobsSync jobSync = __instance.Controller.GetComponent<NetworkJobsSync>();
                    if(consistInfo != null)
                        jobSync.OnSingleChainGeneratedWithExistingCars(__result);
                    else
                        jobSync.OnSingleChainGenerated(__result);
                }
            }
        }
    }

    class PassengerJobs_GenerateNewCommuterRun_Patch
    {
        static void Postfix(CommuterChainController __result, PassengerJobGenerator __instance, TrainCarsPerLogicTrack consistInfo = null)
        {
            if (NetworkManager.IsHost())
            {
                if (__instance.Controller && __instance.Controller.GetComponent<NetworkJobsSync>())
                {
                    NetworkJobsSync jobSync = __instance.Controller.GetComponent<NetworkJobsSync>();
                    if (consistInfo != null)
                        jobSync.OnSingleChainGeneratedWithExistingCars(__result);
                    else
                        jobSync.OnSingleChainGenerated(__result);
                }
            }
        }
    }

    class PassengerJobs_GenerateCommuterReturnTrip_Patch
    {
        static void Postfix(CommuterChainController __result, StationController sourceStation)
        {
            if (NetworkManager.IsHost())
            {
                NetworkJobsSync jobSync = sourceStation.GetComponent<NetworkJobsSync>();
                if (jobSync != null)
                {
                    jobSync.OnSingleChainGeneratedWithExistingCars(__result);
                }
            }
        }
    }

    class PassengerJobs_GetJobTypeFromDefinition_Patch
    {
        static void Postfix(ref JobType __result, StaticJobDefinition definition)
        {
            if (__result == JobType.Custom)
            {
                if(definition is StaticPassengerJobDefinition)
                {
                    __result = (definition as StaticPassengerJobDefinition).subType;
                }
            }
        }
    }

    class DVMultiplayer_OnCargoChangeMessage_Patch
    {
        static bool Prefix(NetworkTrainManager __instance, Message message)
        {
            if (__instance.buffer.NotSyncedAddToBuffer(__instance.IsSynced, __instance.OnCargoChangeMessage, message))
                return false;

            using (DarkRiftReader reader = message.GetReader())
            {
                while (reader.Position < reader.Length)
                {
                    TrainCargoChanged data = reader.ReadSerializable<TrainCargoChanged>();
                    if (data.WarehouseId != "")
                        return true;
                    Main.Log($"[CLIENT] < TRAIN_CARGO_CHANGE: Car: {data.Id} {(data.IsLoading ? $"Loaded {data.Type.GetCargoName()}" : "Unloaded")}");
                    WorldTrain train = __instance.serverCarStates.FirstOrDefault(t => t.Guid == data.Id);
                    if (train != null)
                    {
                        train.CargoType = data.Type;
                        train.CargoAmount = data.Amount;
                    }

                    TrainCar car = __instance.localCars.FirstOrDefault(t => t.CarGUID == data.Id);
                    if (car)
                    {
                        __instance.IsChangeByNetwork = true;
                        Main.Log(data.YardID + " " + data.TrackID);
                        WarehouseMachine warehouse = PlatformManager.GetController(data.YardID, data.TrackID).LogicMachine;
                        if (warehouse != null)
                        {
                            if (data.IsLoading)
                                car.logicCar.LoadCargo(data.Amount, data.Type, warehouse);
                            else
                                car.logicCar.UnloadCargo(car.logicCar.LoadedCargoAmount, car.logicCar.CurrentCargoTypeInCar, warehouse);
                            __instance.IsChangeByNetwork = false;
                        }
                    }
                }
            }
            return false;
        }
    }

    class DVMultiplayer_CargoStateChanged_Patch
    {
        static bool Prefix(TrainCar trainCar, CargoType type, bool isLoaded)
        {
            if (trainCar.logicCar.CurrentTrack.ID.trackType == "LP")
            {
                string yardID = trainCar.logicCar.CurrentTrack.ID.yardId;
                string trackID = trainCar.logicCar.CurrentTrack.ID.FullDisplayID;
                SendCargoStateChange(trainCar.CarGUID, trainCar.LoadedCargoAmount, type, yardID, trackID, isLoaded);
                return false;
            }
            return true;
        }

        static void SendCargoStateChange(string carId, float loadedCargoAmount, CargoType loadedCargo, string yardID, string trackID, bool isLoaded)
        {
            Main.Log($"[CLIENT] > TRAIN_CARGO_CHANGE: Car: {carId} {(isLoaded ? $"Loaded {loadedCargo.GetCargoName()}" : "Unloaded")}");
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(new TrainCargoChanged() { Id = carId, Amount = loadedCargoAmount, Type = loadedCargo, YardID = yardID, TrackID = trackID, IsLoading = isLoaded });
                using (Message message = Message.Create((ushort)NetworkTags.TRAIN_CARGO_CHANGE, writer))
                    SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
            }
        }
    }
}
