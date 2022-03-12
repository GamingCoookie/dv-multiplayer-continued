using DV.MultipleUnit;
using DV.Simulation.Brake;
using DVMultiplayer.Networking;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace DVMultiplayer.Patches.Train
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0051 // Remove unused private members
    [HarmonyPatch(typeof(HoseSeparationChecker), "CheckDistances")]
    internal class HoseSeperationCheckPatch
    {
        private static bool Prefix(HoseSeparationChecker __instance, List<HoseSeparationChecker.BrakeHoseCheckPair> ___brakeHosesToCheck, List<HoseSeparationChecker.MUCableCheckPair> ___muCablesToCheck)
        {
            if (NetworkManager.IsClient())
            {
				if (Trainset.allSets.Count == 0 || Brakeset.allSets.Count == 0)
				{
					return false;
				}
				for (int i = ___brakeHosesToCheck.Count - 1; i >= 0; i--)
				{
					HoseSeparationChecker.BrakeHoseCheckPair brakeHoseCheckPair = ___brakeHosesToCheck[i];
					BrakeSystem parentSystem = brakeHoseCheckPair.a.parentSystem;
					BrakeSystem parentSystem2 = brakeHoseCheckPair.b.parentSystem;
					if (parentSystem == null || parentSystem2 == null)
					{
						___brakeHosesToCheck.RemoveAt(i);
					}
					else
					{
						TrainCar component = parentSystem.GetComponent<TrainCar>();
						TrainCar component2 = parentSystem2.GetComponent<TrainCar>();
						if(!component.GetComponent<NetworkTrainPosSync>().hasLocalPlayerAuthority && !component2.GetComponent<NetworkTrainPosSync>().hasLocalPlayerAuthority)
                        {
							continue;
                        }

						Coupler coupler = brakeHoseCheckPair.a.isFront ? component.frontCoupler : component.rearCoupler;
						Coupler c = brakeHoseCheckPair.b.isFront ? component2.frontCoupler : component2.rearCoupler;
						CouplingHoseRig rig = CouplingHoseRig.GetRig(coupler);
						CouplingHoseRig rig2 = CouplingHoseRig.GetRig(c);
						if (rig && rig2 && Vector3.SqrMagnitude(rig.ropeAnchor.position - rig2.ropeAnchor.position) > 1.9599999f)
						{
							Debug.Log("Breaking hose connection due to separation", __instance);
							coupler.DisconnectAirHose(true);
						}
					}
				}
				for (int j = ___muCablesToCheck.Count - 1; j >= 0; j--)
				{
					HoseSeparationChecker.MUCableCheckPair mucableCheckPair = ___muCablesToCheck[j];
					MultipleUnitModule muModule = mucableCheckPair.a.muModule;
					MultipleUnitModule muModule2 = mucableCheckPair.b.muModule;
					if (muModule == null || muModule2 == null)
					{
						Debug.LogError(string.Format("Encountered null for {0} a: {1} b: {2}, removing pair", "MultipleUnitModule", muModule == null, muModule2 == null));
						___muCablesToCheck.RemoveAt(j);
					}
					else
					{
						if (!muModule.GetComponent<NetworkTrainPosSync>().hasLocalPlayerAuthority && !muModule2.GetComponent<NetworkTrainPosSync>().hasLocalPlayerAuthority)
						{
							continue;
						}

						CouplingHoseMultipleUnitAdapter hoseAdapter = mucableCheckPair.a.HoseAdapter;
						CouplingHoseRig couplingHoseRig = (hoseAdapter != null) ? hoseAdapter.rig : null;
						CouplingHoseMultipleUnitAdapter hoseAdapter2 = mucableCheckPair.b.HoseAdapter;
						CouplingHoseRig couplingHoseRig2 = (hoseAdapter2 != null) ? hoseAdapter2.rig : null;
						if (couplingHoseRig && couplingHoseRig2 && Vector3.SqrMagnitude(couplingHoseRig.ropeAnchor.position - couplingHoseRig2.ropeAnchor.position) > 1.9599999f)
						{
							Debug.Log("Breaking MU hose connection due to separation", __instance);
							mucableCheckPair.a.Disconnect(true);
						}
					}
				}
				return false;
            }
            return true;
        }
    }
}
