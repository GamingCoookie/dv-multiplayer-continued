using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(GarageCarSpawner), "Update")]
    internal class StopCreatingCarsAsClient
    {
		private static bool Prefix(GarageCarSpawner __instance, bool ___spawnAllowed, ref bool ___playerEnteredLocoSpawnRange)
        {
			if (NetworkManager.IsClient() && !NetworkManager.IsHost())
            {
                return false;
            }
            else if (NetworkManager.IsHost())
            {
				if (!SingletonBehaviour<NetworkPlayerManager>.Exists || !SingletonBehaviour<NetworkPlayerManager>.Instance)
					return false;

				if (SingletonBehaviour<NetworkPlayerManager>.Instance.newPlayerConnecting)
					return false;

				bool shouldSpawn = false;
				if (!___spawnAllowed || !SaveLoadController.carsAndJobsLoadingFinished)
				{
					return false;
				}
				bool isHostInSpawnArea = (PlayerManager.PlayerTransform.position - __instance.locoSpawnPoint.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack;
				if (!___playerEnteredLocoSpawnRange && isHostInSpawnArea)
				{
					___playerEnteredLocoSpawnRange = true;
					shouldSpawn = true;
				}
				else if (!___playerEnteredLocoSpawnRange && !isHostInSpawnArea)
				{
					if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers().Any(p => (p.transform.position - __instance.locoSpawnPoint.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack))
                    {
						___playerEnteredLocoSpawnRange = true;
						shouldSpawn = true;
                    }
				}
				else if (___playerEnteredLocoSpawnRange)
				{
					if (!SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers().All(p => ((p.transform.position - __instance.locoSpawnPoint.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack)) && !isHostInSpawnArea)
						___playerEnteredLocoSpawnRange = false;
				}

                if (shouldSpawn)
                {
					if (__instance.garageSpawnedCar == null)
					{
						__instance.garageSpawnedCar = CarSpawner.SpawnCarOnClosestTrack(__instance.locoSpawnPoint.transform.position, __instance.locoType, __instance.flipSpawnLoco, true);
						if (__instance.garageSpawnedCar != null)
						{
							SingletonBehaviour<UnusedTrainCarDeleter>.Instance.MarkForDelete(__instance.garageSpawnedCar);
							__instance.garageSpawnedCar.OnDestroyCar += (TrainCar deletedCar) => {
								if (__instance.garageSpawnedCar != null && __instance.garageSpawnedCar == deletedCar)
								{
									__instance.garageSpawnedCar = null;
									return;
								}
								Main.Log("[ERROR] Unexpected state: garageSpawnedCar is either null, or not matching deletedCar!");
							};
							return false;
						}
						Main.Log("[ERROR] Couldn't spawn garage car!");
						return false;
					}
				}
				return false;
			}
            return true;
        }
    }
}
