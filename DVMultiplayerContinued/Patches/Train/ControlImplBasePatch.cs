using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using DV.MultipleUnit;
using DV.CabControls;
using System.Reflection.Emit;
using UnityEngine;
using Object = UnityEngine.Object;
using DVMultiplayer;
using DVMultiplayer.Networking;
using DVMultiplayer.DTO.Train;
using DVMP.DTO;

namespace DVMultiplayerContinued.Patches.Train
{
    internal static class ControlImplBasePatch
    {
        [HarmonyPatch(typeof(ControlImplBase), nameof(ControlImplBase.RequestValueUpdate))]
        internal static class RequestValueUpdatePatch
        {
            public static void Prefix(ControlImplBase __instance, float newValue)
            {
                if (__instance.GetComponentInParent<TurntableController>() != null)
                    return;
                Transform transform = __instance.GetComponentsInParent<Transform>().FirstOrDefault(s => s.name.Contains("[interior]"));
                if (transform == null)
                    return;
                TrainCar train = transform.gameObject.GetComponent<TrainCarInteriorObject>().actualTrainCar;
                NetworkTrainManager ntm = null;
                if (SingletonBehaviour<NetworkTrainManager>.Exists)
                    ntm = SingletonBehaviour<NetworkTrainManager>.Instance;
                else
                    return;
                MultipleUnitModule mu = train.GetComponent<MultipleUnitModule>();
                MultipleUnitModule MUWithSmallestLocoNumber = mu;
                if (!train || !NetworkManager.IsClient() || ntm.IsChangeByNetwork)
                    return;
                if (mu)
                {
                    List<MultipleUnitModule> otherMUs = GetAllMU(mu);
                    if (otherMUs.Count > 0)
                    {
                        foreach (MultipleUnitModule otherMU in otherMUs)
                        {
                            if (ushort.Parse(otherMU.loco.train.ID.TrimStart('L', '-', '0')) < ushort.Parse(MUWithSmallestLocoNumber.loco.train.ID.TrimStart('L', '-', '0')))
                                MUWithSmallestLocoNumber = otherMU;
                        }
                    }
                }
                if (ntm.IsChangeByNetwork2 && !ntm.IsChangeByNetwork)
                {
                    ntm.IsChangeByNetwork2 = false;
                    return;
                }
                if (MUWithSmallestLocoNumber != mu && Ctrls.IsMUControl(__instance.name))
                    return;
                ntm.SendNewLocoLeverValue(train, newValue, __instance.name);
            }
        }

        public static List<MultipleUnitModule> GetAllMU(MultipleUnitModule origin, MultipleUnitModule previous = null)
        {
            List<MultipleUnitModule> MUs = new List<MultipleUnitModule>();
            if (origin.frontCable.connectedTo != null)
            {
                if (previous != null && origin.frontCable.connectedTo.muModule != previous)
                    MUs.Add(origin.frontCable.connectedTo.muModule);
                else if (previous == null)
                    MUs.Add(origin.frontCable.connectedTo.muModule);
            }
            if (origin.rearCable.connectedTo != null)
            {
                if (previous != null && origin.rearCable.connectedTo.muModule != previous)
                    MUs.Add(origin.rearCable.connectedTo.muModule);
                else if (previous == null)
                    MUs.Add(origin.rearCable.connectedTo.muModule);
            }
            if (MUs.Count > 0)
            {
                List<MultipleUnitModule> newMUs = new List<MultipleUnitModule>();
                foreach (MultipleUnitModule MU in MUs)
                    newMUs.AddRange(GetAllMU(MU, origin));
                MUs.AddRange(newMUs);
            }
            return MUs;
        }
    }
}
