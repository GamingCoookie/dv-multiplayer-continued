using DV.Logic.Job;
using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(StationController), "Update")]
    internal class StopGeneratingJobsAsClient
    {
        private static bool Prefix(StationController __instance, StationJobGenerationRange ___stationRange, Station ___logicStation, ref bool ___attemptJobOverviewGeneration, ref bool ___playerEnteredJobGenerationZone, ref HashSet<Job> ___processedNewJobs, ref List<JobOverview> ___spawnedJobOverviews, PointOnPlane ___jobBookletSpawnSurface)
        {
            if (NetworkManager.IsClient())
            {
				if (___logicStation == null || !SaveLoadController.carsAndJobsLoadingFinished)
				{
					return false;
				}
				if (___stationRange.IsPlayerInRangeForBookletGeneration(___stationRange.PlayerSqrDistanceFromStationOffice) && ___attemptJobOverviewGeneration)
				{
					for (int i = 0; i < ___logicStation.availableJobs.Count; i++)
					{
						Job job = ___logicStation.availableJobs[i];
						if (!___processedNewJobs.Contains(job))
						{
							PointOnPlane pointOnPlane = ___jobBookletSpawnSurface;
							ValueTuple<Vector3, Quaternion> valueTuple = (pointOnPlane != null) ? pointOnPlane.GetRandomPointWithRotationOnPlane() : new ValueTuple<Vector3, Quaternion>(__instance.transform.position, __instance.transform.rotation);
							Transform parent = SingletonBehaviour<WorldMover>.Exists ? SingletonBehaviour<WorldMover>.Instance.originShiftParent : null;
							JobOverview item = BookletCreator.CreateJobOverview(job, valueTuple.Item1, valueTuple.Item2, parent);
							___spawnedJobOverviews.Add(item);
							___processedNewJobs.Add(job);
						}
					}
					___attemptJobOverviewGeneration = false;
				}


                if (NetworkManager.IsHost() && SingletonBehaviour<NetworkPlayerManager>.Exists && SingletonBehaviour<NetworkPlayerManager>.Instance.IsSynced && SingletonBehaviour<NetworkJobsManager>.Exists)
                {
					if (SingletonBehaviour<NetworkPlayerManager>.Instance.newPlayerConnecting)
						return false;

					float playerSqrDistanceFromStationCenter = ___stationRange.PlayerSqrDistanceFromStationCenter;
					bool isHostInGenerationZone = ___stationRange.IsPlayerInJobGenerationZone(playerSqrDistanceFromStationCenter);
					if (isHostInGenerationZone && !___playerEnteredJobGenerationZone)
					{
						Main.Log("Generating jobs because host is in area");
						__instance.ProceduralJobsController.TryToGenerateJobs();
						___playerEnteredJobGenerationZone = true;
					}
					else if(!isHostInGenerationZone && !___playerEnteredJobGenerationZone)
                    {
						if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers().Any(p => ___stationRange.IsPlayerInJobGenerationZone((p.transform.position - ___stationRange.stationCenterAnchor.position).sqrMagnitude)))
                        {
							Main.Log("Generating jobs because a client is in area");
							__instance.ProceduralJobsController.TryToGenerateJobs();
							___playerEnteredJobGenerationZone = true;
                        }
					}
					else if(___playerEnteredJobGenerationZone)
                    {
						if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers().All(p => !___stationRange.IsPlayerInJobGenerationZone((p.transform.position - ___stationRange.stationCenterAnchor.position).sqrMagnitude)) && !isHostInGenerationZone)
                        {
							___playerEnteredJobGenerationZone = false;
							Main.Log("No one in area reseting generation flag");
							SingletonBehaviour<NetworkJobsManager>.Instance.ExpireJobsUnTakenInStation(__instance);
							SingletonBehaviour<NetworkJobsManager>.Instance.SendJobsExpirationInStation(__instance.logicStation.ID);
                        }
					}
				}
				return false;
            }
            return true;
        }
    }
}
