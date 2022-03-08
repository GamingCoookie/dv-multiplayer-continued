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
    class NetworkShunterSync : NetworkLocomotive
    {
        private LocoControllerShunter locoController;
        private ShunterLocoSimulation locoSimulation;
        private DamageControllerShunter damageController;
        private BrakeSystem brakeSystem;
        private ParticleSystem exhaustParticles;

#pragma warning disable IDE0051 // Remove unused private members
        private void Start()
        {
            locoController = GetComponent<LocoControllerShunter>();
            locoSimulation = GetComponent<ShunterLocoSimulation>();
            damageController = GetComponent<DamageControllerShunter>();
            brakeSystem = locoController.train.brakeSystem;
            exhaustParticles = transform.Find("[particles]").Find("ExhaustEngineSmoke").GetComponent<ParticleSystem>();
        }
#pragma warning restore IDE0051 // Remove unused private members

        public override void UpdateSimulation(LocoSimData locationSimValues)
        {
            if (!locoSimulation || !brakeSystem)
                return;

            ShunterSimData shunter = locationSimValues as ShunterSimData;
            locoSimulation.engineTemp.SetValue(shunter.Temperature);
            locoSimulation.engineRPM.SetValue(shunter.RPM);
            locoSimulation.sand.SetValue(shunter.Sand);
            locoSimulation.oil.SetValue(shunter.Oil);
            locoSimulation.fuel.SetValue(shunter.Fuel);
            brakeSystem.brakePipePressure = shunter.BrakePressure;
            brakeSystem.mainReservoirPressure = shunter.MainBrakePressure;
        }

        public override void UpdateAuthorityPhysics(bool hasAuthority)
        {
            locoSimulation.runSimulation = hasAuthority;

            var shunterExhaust = exhaustParticles.main;
            shunterExhaust.emitterVelocityMode = hasAuthority ? ParticleSystemEmitterVelocityMode.Rigidbody : ParticleSystemEmitterVelocityMode.Transform;
        }

        public override LocoSimData GetSimulationValues()
        {
            if (!locoSimulation || !brakeSystem)
                return null;

            return new ShunterSimData()
            {
                Temperature = locoSimulation.engineTemp.value,
                RPM = locoSimulation.engineRPM.value,
                Sand = locoSimulation.sand.value,
                Oil = locoSimulation.oil.value,
                Fuel = locoSimulation.fuel.value,
                BrakePressure = brakeSystem.brakePipePressure,
                MainBrakePressure = brakeSystem.mainReservoirPressure
            };
        }
    }
}
