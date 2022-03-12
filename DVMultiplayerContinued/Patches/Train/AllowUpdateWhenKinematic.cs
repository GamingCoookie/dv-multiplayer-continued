using DV;
using DV.Logic.Job;
using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(TrainCar), "Update")]
    internal class AllowUpdateWhenKinematic
    {
        private static bool Prefix(TrainCar __instance, ref Vector3 ___prevAbsoluteWorldPosition, ref int ___stationaryFramesCounter, Action<bool> ___MovementStateChanged)
        {
            if (NetworkManager.IsClient())
            {
				if (AppUtil.IsPaused)
				{
					return true;
				}
				if (NumberUtil.AnyInfinityMinMaxNaN(__instance.transform.position) || __instance.transform.position.y < -500f)
				{
					Debug.LogWarning(string.Format("Car '{0}' fell through the ground (y: {1}) and will be deleted!", __instance.name, __instance.transform.position.y));
					Job jobOfCar = JobChainController.GetJobOfCar(__instance);
					if (jobOfCar != null)
					{
						JobState state = jobOfCar.State;
						if (state != JobState.Available)
						{
							if (state != JobState.InProgress)
							{
								Debug.LogError(string.Format("Unexpected state {0}, ignoring force abandon/expire!", jobOfCar.State));
							}
							else
							{
								PlayerJobs.Instance.AbandonJob(jobOfCar);
							}
						}
						else
						{
							jobOfCar.ExpireJob();
						}
					}
					CarSpawner.DeleteCar(__instance);
					SingletonBehaviour<UnusedTrainCarDeleter>.Instance.ClearInvalidCarReferencesAfterManualDelete();
				}
				Vector3 a = __instance.transform.position - WorldMover.currentMove;
				if ((a - ___prevAbsoluteWorldPosition).sqrMagnitude > 0.0001f)
				{
					___prevAbsoluteWorldPosition = a;
					___stationaryFramesCounter = 0;
					if (__instance.isStationary)
					{
						___MovementStateChanged?.Invoke(true);
					}
					__instance.isStationary = (__instance.isEligibleForSleep = false);
					return false;
				}
				if (!__instance.isEligibleForSleep || !__instance.isStationary)
				{
					if (___stationaryFramesCounter < 100)
					{
						___stationaryFramesCounter++;
						return false;
					}
					if (___stationaryFramesCounter >= 100)
					{
						___stationaryFramesCounter = 0;
						if (!__instance.isStationary)
						{
							___MovementStateChanged?.Invoke(false);
						}
						__instance.isEligibleForSleep = (__instance.isStationary = true);
					}
				}
			}
			return true;
		}
    }
}
