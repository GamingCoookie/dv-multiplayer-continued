using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class CarsAuthChange : IDarkRiftSerializable
    {
        public string[] Guids { get; set; }
        public ushort PlayerId { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Guids = e.Reader.ReadStrings();
            PlayerId = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Guids);
            e.Writer.Write(PlayerId);
        }
    }
}
