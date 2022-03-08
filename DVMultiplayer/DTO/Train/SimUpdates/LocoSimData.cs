using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Train.SimUpdates
{
    public abstract class LocoSimData : IDarkRiftSerializable
    {
        public float BrakePressure { get; set; } = 0;
        public float MainBrakePressure { get; set; } = 0;

        public virtual void Deserialize(DeserializeEvent e) 
        {
            BrakePressure = e.Reader.ReadSingle();
            MainBrakePressure = e.Reader.ReadSingle();
        }

        public virtual void Serialize(SerializeEvent e)
        {
            e.Writer.Write(BrakePressure);
            e.Writer.Write(MainBrakePressure);
        }
    }
}
