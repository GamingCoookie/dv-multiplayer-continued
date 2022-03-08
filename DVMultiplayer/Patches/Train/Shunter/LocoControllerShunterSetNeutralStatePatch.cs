using DVMultiplayer.DTO.Train;
using DVMultiplayer.Networking;
using HarmonyLib;

namespace DVMultiplayer.Patches.Train
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(LocoControllerShunter), "SetNeutralState")]
    internal class LocoControllerShunterSetNeutralStatePatch
    {
        private static void Prefix(TrainCar ___train)
        {
            if(NetworkManager.IsClient() && SingletonBehaviour<NetworkTrainManager>.Exists)
            {
                NetworkTrainManager net = SingletonBehaviour<NetworkTrainManager>.Instance;
                //foreach(Bogie bogie in ___train.Bogies)
                //{
                //    bogie.RefreshBogiePoints();
                //}
                WorldTrain state = net.GetServerStateById(___train.CarGUID);
                if(state != null)
                {
                    DTO.Train.Locomotives.Shunter shunter = state.Locomotive as DTO.Train.Locomotives.Shunter;
                    shunter.IsEngineOn = false;
                    shunter.IsMainFuseOn = false;
                    shunter.IsSideFuse1On = false;
                    shunter.IsSideFuse2On = false;
                    state.Sander = 0;
                    state.Throttle = 0;
                    state.Reverser = 0;
                    state.Brake = 0;
                    state.IndepBrake = 1;
                }
            }
        }
    }
}
