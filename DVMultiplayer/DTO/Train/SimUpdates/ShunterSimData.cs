using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Train.SimUpdates
{
    public class ShunterSimData : LocoSimData
    {
        public float Temperature { get; set; } = 0;
        public float RPM { get; set; } = 0;
        public float Sand { get; set; }
        public float Oil { get; set; }
        public float Fuel { get; set; }

        public override void Deserialize(DeserializeEvent e)
        {
            base.Deserialize(e);
            Temperature = e.Reader.ReadSingle();
            RPM = e.Reader.ReadSingle();
            Sand = e.Reader.ReadSingle();
            Oil = e.Reader.ReadSingle();
            Fuel = e.Reader.ReadSingle();
        }

        public override void Serialize(SerializeEvent e)
        {
            base.Serialize(e);
            e.Writer.Write(Temperature);
            e.Writer.Write(RPM);
            e.Writer.Write(Sand);
            e.Writer.Write(Oil);
            e.Writer.Write(Fuel);
        }
    }
}
