using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class TrainCouplingChange : IDarkRiftSerializable
    {
        public string TrainIdC1 { get; set; }
        public bool IsC1Front { get; set; }
        public string TrainIdC2 { get; set; }
        public bool IsC2Front { get; set; }
        public bool ViaChainInteraction { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            TrainIdC1 = e.Reader.ReadString();
            IsC1Front = e.Reader.ReadBoolean();
            TrainIdC2 = e.Reader.ReadString();
            IsC2Front = e.Reader.ReadBoolean();
            ViaChainInteraction = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainIdC1);
            e.Writer.Write(IsC1Front);
            e.Writer.Write(TrainIdC2);
            e.Writer.Write(IsC2Front);
            e.Writer.Write(ViaChainInteraction);
        }
    }
}
