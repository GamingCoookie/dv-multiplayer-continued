using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Train.SimUpdates
{
    public class SteamerSimData : LocoSimData
    {
        public float Temperature { get; set; } = 0;
        public float Water { get; set; } = 0;
        public float Pressure { get; set; } = 0;
        public float Coal { get; set; }
        public float TenderCoal { get; set; }
        public float TenderWater { get; set; }
        public float CoalConsumption { get; set; }
        public float MaxCoalConsumption { get; set; }
        public float SafetyPressure { get; set; }
        public float Sand { get; set; }

        public override void Deserialize(DeserializeEvent e)
        {
            base.Deserialize(e);
            Temperature = e.Reader.ReadSingle();
            Water = e.Reader.ReadSingle();
            Pressure = e.Reader.ReadSingle();
            Coal = e.Reader.ReadSingle();
            TenderCoal = e.Reader.ReadSingle();
            TenderWater = e.Reader.ReadSingle();
            CoalConsumption = e.Reader.ReadSingle();
            MaxCoalConsumption = e.Reader.ReadSingle();
            SafetyPressure = e.Reader.ReadSingle();
            Sand = e.Reader.ReadSingle();
        }

        public override void Serialize(SerializeEvent e)
        {
            base.Serialize(e);
            e.Writer.Write(Temperature);
            e.Writer.Write(Water);
            e.Writer.Write(Pressure);
            e.Writer.Write(Coal);
            e.Writer.Write(TenderCoal);
            e.Writer.Write(TenderWater);
            e.Writer.Write(CoalConsumption);
            e.Writer.Write(MaxCoalConsumption);
            e.Writer.Write(SafetyPressure);
            e.Writer.Write(Sand);
        }
    }
}
