using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class TrainCarChange : IDarkRiftSerializable
    {
        public ushort PlayerId { get; set; }
        public string TrainId { get; set; } = "";


        public void Deserialize(DeserializeEvent e)
        {
            PlayerId = e.Reader.ReadUInt16();
            TrainId = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(PlayerId);
            e.Writer.Write(TrainId);
        }
    }
}
