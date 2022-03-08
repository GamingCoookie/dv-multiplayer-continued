using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class Shunter : IDarkRiftSerializable
    {
        public bool IsEngineOn { get; set; } = false;
        public bool IsSideFuse1On { get; set; } = false;
        public bool IsSideFuse2On { get; set; } = false;
        public bool IsMainFuseOn { get; set; } = false;
        
        public void Deserialize(DeserializeEvent e)
        {
            IsEngineOn = e.Reader.ReadBoolean();
            IsSideFuse1On = e.Reader.ReadBoolean();
            IsSideFuse2On = e.Reader.ReadBoolean();
            IsMainFuseOn = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(IsEngineOn);
            e.Writer.Write(IsSideFuse1On);
            e.Writer.Write(IsSideFuse2On);
            e.Writer.Write(IsMainFuseOn);
        }
    }
}
