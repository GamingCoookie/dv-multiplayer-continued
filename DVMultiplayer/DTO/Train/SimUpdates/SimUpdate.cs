using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Train.SimUpdates
{
    public class SimUpdate : IDarkRiftSerializable
    {
        public string Id { get; set; }
        public TrainCarType CarType { get; set; } = TrainCarType.NotSet;
        public LocoSimData SimData { get; set; } = null;

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadString();
            CarType = (TrainCarType)e.Reader.ReadUInt32();
            switch (CarType)
            {
                case TrainCarType.LocoShunter:
                    SimData = e.Reader.ReadSerializable<ShunterSimData>();
                    break;

                case TrainCarType.LocoSteamHeavy:
                case TrainCarType.LocoSteamHeavyBlue:
                    SimData = e.Reader.ReadSerializable<SteamerSimData>();
                    break;
            }
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write((uint)CarType);
            e.Writer.Write(SimData);
        }
    }
}
