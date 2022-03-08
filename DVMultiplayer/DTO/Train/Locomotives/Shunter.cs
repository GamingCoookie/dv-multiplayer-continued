using DarkRift;
using DVMultiplayer.DTO.Train.SimUpdates;

namespace DVMultiplayer.DTO.Train.Locomotives
{
    public class Shunter : Locomotive
    {
        public bool IsEngineOn { get; set; } = false;
        public bool IsSideFuse1On { get; set; } = false;
        public bool IsSideFuse2On { get; set; } = false;
        public bool IsMainFuseOn { get; set; } = false;
        public MultipleUnit MultipleUnit { get; set; } = new MultipleUnit();
        public ShunterSimData SimData { get; set; } = new ShunterSimData();

        public override void Deserialize(DeserializeEvent e)
        {
            IsEngineOn = e.Reader.ReadBoolean();
            IsSideFuse1On = e.Reader.ReadBoolean();
            IsSideFuse2On = e.Reader.ReadBoolean();
            IsMainFuseOn = e.Reader.ReadBoolean();
            MultipleUnit = e.Reader.ReadSerializable<MultipleUnit>();
            SimData = e.Reader.ReadSerializable<ShunterSimData>();
        }

        public override void Serialize(SerializeEvent e)
        {
            e.Writer.Write(IsEngineOn);
            e.Writer.Write(IsSideFuse1On);
            e.Writer.Write(IsSideFuse2On);
            e.Writer.Write(IsMainFuseOn);
            e.Writer.Write(MultipleUnit);
            e.Writer.Write(SimData);
        }
    }
}
