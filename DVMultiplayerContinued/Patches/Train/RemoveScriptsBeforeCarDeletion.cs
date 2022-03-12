using DV;
using DVMultiplayer.Networking;
using HarmonyLib;
using UnityEngine;

namespace DVMultiplayer.Patches
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(CarSpawner), "DeleteCar")]
    internal class RemoveScriptsBeforeCarDeletion
	{
        private static void Prefix(TrainCar trainCar)
        {
            if (NetworkManager.IsClient())
            {
                if (!trainCar)
                    return;

                if (trainCar.GetComponent<NetworkTrainPosSync>())
                    Object.DestroyImmediate(trainCar.GetComponent<NetworkTrainPosSync>());
                if (trainCar.GetComponent<NetworkTrainSync>())
                    Object.DestroyImmediate(trainCar.GetComponent<NetworkTrainSync>());
                if (trainCar.frontCoupler.GetComponent<NetworkTrainCouplerSync>())
                    Object.DestroyImmediate(trainCar.frontCoupler.GetComponent<NetworkTrainCouplerSync>());
                if (trainCar.rearCoupler.GetComponent<NetworkTrainCouplerSync>())
                    Object.DestroyImmediate(trainCar.rearCoupler.GetComponent<NetworkTrainCouplerSync>());
            }
        }
    }
}
