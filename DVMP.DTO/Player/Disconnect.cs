using DarkRift;

namespace DVMultiplayer.DTO.Player
{
    public class Disconnect : IDarkRiftSerializable
    {
        public ushort PlayerId { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            PlayerId = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(PlayerId);
        }
    }
}
