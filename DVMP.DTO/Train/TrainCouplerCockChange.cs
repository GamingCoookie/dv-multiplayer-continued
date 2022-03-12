using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class TrainCouplerCockChange : IDarkRiftSerializable
    {
        public string TrainIdCoupler { get; set; }
        public bool IsCouplerFront { get; set; }
        public bool IsOpen { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            TrainIdCoupler = e.Reader.ReadString();
            IsCouplerFront = e.Reader.ReadBoolean();
            IsOpen = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainIdCoupler);
            e.Writer.Write(IsCouplerFront);
            e.Writer.Write(IsOpen);
        }
    }
}
