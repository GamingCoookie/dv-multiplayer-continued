using DV.Simulation.Brake;
using DVMultiplayer.DTO.Train.SimUpdates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.Unity.Train.Locomotives
{
    class NetworkSteamerSync : NetworkLocomotive
    {
        private LocoControllerSteam locoController;
        private SteamLocoSimulation locoSimulation;
        private DamageController damageController;
        private BrakeSystem brakeSystem;
        private CabItemObiRope whistleRope;

#pragma warning disable IDE0051 // Remove unused private members
        private void Start()
        {
            locoController = GetComponent<LocoControllerSteam>();
            locoSimulation = GetComponent<SteamLocoSimulation>();
            damageController = GetComponent<DamageController>();
            brakeSystem = locoController.train.brakeSystem;
        }

        private void Update()
        {
            if (!whistleRope && locoController.train && locoController.train.IsInteriorLoaded && locoController.train.interior && locoController.train.interior.GetComponentInChildren<CabInputSteamExtra>() && locoController.train.interior.GetComponentInChildren<CabInputSteamExtra>().whistleCtrl)
            {
                whistleRope = locoController.train.interior.GetComponentInChildren<CabInputSteamExtra>().whistleCtrl;
            }
        }
#pragma warning restore IDE0051 // Remove unused private members

        public override void UpdateSimulation(LocoSimData locationSimValues)
        {
            if (!locoSimulation || !brakeSystem)
                return;

            SteamerSimData values = locationSimValues as SteamerSimData;
            locoSimulation.temperature.SetValue(values.Temperature);
            locoSimulation.boilerPressure.SetValue(values.Pressure);
            locoSimulation.boilerWater.SetValue(values.Water);
            locoSimulation.coalbox.SetValue(values.Coal);
            locoSimulation.tenderCoal.SetValue(values.TenderCoal);
            locoSimulation.tenderWater.SetValue(values.TenderWater);
            locoSimulation.safetyPressureValve.SetValue(values.SafetyPressure);
            locoSimulation.coalConsumptionRate = values.CoalConsumption;
            locoSimulation.maxCoalConsumptionRate = values.MaxCoalConsumption;
            locoSimulation.sand.SetValue(values.Sand);
            brakeSystem.brakePipePressure = values.BrakePressure;
            brakeSystem.mainReservoirPressure = values.MainBrakePressure;
        }

        public override void UpdateAuthorityPhysics(bool hasAuthority)
        {
            if(locoSimulation is object)
                locoSimulation.runSimulation = hasAuthority;

            if (whistleRope is object)
                whistleRope.enabled = hasAuthority;
        }

        public override LocoSimData GetSimulationValues()
        {
            if (!locoSimulation || !brakeSystem)
                return null;

            return new SteamerSimData()
            {
                Temperature = locoSimulation.temperature.value,
                Pressure = locoSimulation.boilerPressure.value,
                Water = locoSimulation.boilerWater.value,
                Coal = locoSimulation.coalbox.value,
                TenderCoal = locoSimulation.tenderCoal.value,
                TenderWater = locoSimulation.tenderWater.value,
                SafetyPressure = locoSimulation.safetyPressureValve.value,
                CoalConsumption = locoSimulation.coalConsumptionRate,
                MaxCoalConsumption = locoSimulation.maxCoalConsumptionRate,
                Sand = locoSimulation.sand.value,
                BrakePressure = brakeSystem.brakePipePressure,
                MainBrakePressure = brakeSystem.mainReservoirPressure
            };
        }
    }
}
