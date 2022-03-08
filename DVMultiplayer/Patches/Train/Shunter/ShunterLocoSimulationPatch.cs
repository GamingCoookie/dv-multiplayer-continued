using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.Patches.Train.Shunter
{
    [HarmonyPatch(typeof(ShunterLocoSimulation), "SimulateEngineTemp")]
    class ShunterLocoSimulationTempPatch
    {
        private static bool Prefix(ShunterLocoSimulation __instance)
        {
            if (NetworkManager.IsClient())
            {
                NetworkTrainPosSync networking = __instance.GetComponent<NetworkTrainPosSync>();
                if(networking)
                {
                    return networking.hasLocalPlayerAuthority;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ShunterLocoSimulation), "SimulateEngineRPM")]
    class ShunterLocoSimulationRPMPatch
    {
        private static bool Prefix(ShunterLocoSimulation __instance)
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
}
