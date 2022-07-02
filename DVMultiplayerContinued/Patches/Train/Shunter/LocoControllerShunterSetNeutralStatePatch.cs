using DVMultiplayer.DTO.Train;
using DVMP.DTO;
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
                    state.LocoStuff.EngineOn = false;
                    state.Controls[Ctrls.ShunterMainFuse] = 0;
                    state.Controls[Ctrls.ShunterSideFuse1] = 0;
                    state.Controls[Ctrls.ShunterSideFuse2] = 0;
                    state.Controls[Ctrls.DESand] = 0;
                    state.Controls[Ctrls.DEThrottle] = 0;
                    state.Controls[Ctrls.DEReverser] = 0;
                    state.Controls[Ctrls.DETrainBrake] = 0;
                    state.Controls[Ctrls.DEIndepBrake] = 1;
                }
            }
        }
    }
}
