using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.DTO.Savegame
{
    class OfflineSaveGame
    {
        public string SaveDataCars { get; set; }
        public JobsSaveGameData SaveDataJobs { get; set; }
        public string SaveDataSwitches { get; set; } = "";
        public string SaveDataTurntables { get; set; } = "";
        public string SaveDataDestroyedLocoDebt { get; internal set; }
        public string SaveDataStagedJobDebt { get; internal set; }
        public string SaveDataDeletedJoblessCarsDept { get; internal set; }
        public string SaveDataInsuranceDept { get; internal set; }
        public Vector3 SaveDataPosition { get; internal set; }
    }
}
