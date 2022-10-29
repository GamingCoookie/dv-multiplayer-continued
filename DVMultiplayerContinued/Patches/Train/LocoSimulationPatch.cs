using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.Patches.Train
{
    [HarmonyPatch(typeof(ShunterLocoSimulation), "SimulateTick")]
    class ShunterSimulationTickPatch
    {
        static bool Prefix(LocoSimulation __instance)
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

    [HarmonyPatch(typeof(DieselLocoSimulation), "SimulateTick")]
    class DieselSimulationTickPatch
    {
        static bool Prefix(LocoSimulation __instance)
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

    [HarmonyPatch(typeof(SteamLocoSimulation), "SimulateTick")]
    class SteamSimulationTickPatch
    {
        static bool Prefix(LocoSimulation __instance)
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
