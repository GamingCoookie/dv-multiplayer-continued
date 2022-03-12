using DV;
using DVMultiplayer.Networking;
using HarmonyLib;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(UnusedTrainCarDeleter), "AreDeleteConditionsFulfilled")]
    internal class StopRemovingTrainsWhenClient
    {
        private static void Postfix(TrainCar trainCar, ref bool __result)
        {
            if (NetworkManager.IsClient())
            {
				if (!SingletonBehaviour<NetworkTrainManager>.Exists)
					return;

				if (!NetworkManager.IsHost())
				{
					__result = false;
				}
				else if (NetworkManager.IsHost())
				{
					bool isPlayerNotNearTrain = true;
					float distCheck;
					if (CarTypes.IsAnyLocomotiveOrTender(trainCar.carType))
					{
						distCheck = 16000000f;
					}
					else if (!trainCar.playerSpawnedCar)
					{
						distCheck = 360000f;
					}
					else
					{
						distCheck = 9000000f;
					}

					foreach (GameObject player in SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayers())
					{
						isPlayerNotNearTrain = (trainCar.transform.position - player.transform.position).sqrMagnitude > distCheck;
						if (!isPlayerNotNearTrain)
							break;
					}

					if (isPlayerNotNearTrain)
					{
						isPlayerNotNearTrain = (trainCar.transform.position - PlayerManager.PlayerTransform.position).sqrMagnitude > distCheck;
					}

					__result = isPlayerNotNearTrain;
				}
			}
        }
    }
}
