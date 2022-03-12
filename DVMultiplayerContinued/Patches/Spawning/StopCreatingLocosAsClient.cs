using DV.Logic.Job;
using DVMultiplayer.Networking;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(StationLocoSpawner), "Update")]
    internal class StopCreatingLocosAsClient
    {
		private static bool Prefix(StationLocoSpawner __instance, GameObject ___spawnTrackMiddleAnchor, ref bool ___playerEnteredLocoSpawnRange, ref int ___nextLocoGroupSpawnIndex)
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
				if (!SaveLoadController.carsAndJobsLoadingFinished)
				{
					return false;
				}
				bool isHostInArea = (PlayerManager.PlayerTransform.position - ___spawnTrackMiddleAnchor.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack;
				if (!___playerEnteredLocoSpawnRange && isHostInArea)
				{
					___playerEnteredLocoSpawnRange = true;
					shouldSpawn = true;
				}
				else if (!___playerEnteredLocoSpawnRange && !isHostInArea)
				{
					if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers().Any(p => (p.transform.position - ___spawnTrackMiddleAnchor.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack))
                    {
						___playerEnteredLocoSpawnRange = true;
						shouldSpawn = true;
                    }
				}
				else if (___playerEnteredLocoSpawnRange)
				{
					if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers().All(p => !((p.transform.position - ___spawnTrackMiddleAnchor.transform.position).sqrMagnitude < __instance.spawnLocoPlayerSqrDistanceFromTrack)) && !isHostInArea)
						___playerEnteredLocoSpawnRange = false;
				}

                if (shouldSpawn)
                {
					List<Car> carsFullyOnTrack = __instance.locoSpawnTrack.logicTrack.GetCarsFullyOnTrack();
					if (carsFullyOnTrack.Count != 0)
					{
						if (carsFullyOnTrack.Any((Car car) => CarTypes.IsLocomotive(car.carType)))
						{
							return false;
						}
					}
					List<TrainCarType> trainCarTypes = new List<TrainCarType>(__instance.locoTypeGroupsToSpawn[___nextLocoGroupSpawnIndex].trainCarTypes);
					___nextLocoGroupSpawnIndex = UnityEngine.Random.Range(0, __instance.locoTypeGroupsToSpawn.Count);
					List<TrainCar> list = CarSpawner.SpawnCarTypesOnTrack(trainCarTypes, __instance.locoSpawnTrack, true, 0.0, __instance.spawnRotationFlipped, false);
					if (list != null)
					{
						SingletonBehaviour<UnusedTrainCarDeleter>.Instance.MarkForDelete(list);
						return false;
					}
				}
				return false;
			}
            return true;
        }
    }
}
