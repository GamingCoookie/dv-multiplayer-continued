using DarkRift;
using DVMP.DTO;

namespace DVMultiplayer.DTO.Train
{
    public class TrainLever : IDarkRiftSerializable
    {
        public string TrainId { get; set; }
        public float Value { get; set; }
        public string Name { get; set; } = "";

        public void Deserialize(DeserializeEvent e)
        {
            TrainId = e.Reader.ReadString();
            Value = e.Reader.ReadSingle();
            Name = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainId);
            e.Writer.Write(Value);
            e.Writer.Write(Name);
        }
    }
}
