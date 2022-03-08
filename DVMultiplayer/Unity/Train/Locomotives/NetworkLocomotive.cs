using DVMultiplayer.DTO.Train.Positioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.Unity.Train.Locomotives
{
    public abstract class NetworkLocomotive : MonoBehaviour
    {
        public string Guid
        {
            get
            {
                return GetComponent<TrainCar>().CarGUID;
            }
        }

        public abstract void UpdateSimulation(DVMultiplayer.DTO.Train.SimUpdates.LocoSimData locationSimValues);

        public abstract void UpdateAuthorityPhysics(bool hasAuthority);

        public abstract DVMultiplayer.DTO.Train.SimUpdates.LocoSimData GetSimulationValues();
    }
}
