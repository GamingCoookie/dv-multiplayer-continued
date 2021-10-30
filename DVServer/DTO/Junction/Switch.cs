using DarkRift;

namespace DVMultiplayer.DTO.Junction
{
    public class Switch : IDarkRiftSerializable
    {
        public uint Id { get; set; }
        public SwitchMode Mode { get; set; }
        public bool SwitchToLeft { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadUInt32();
            Mode = (SwitchMode)e.Reader.ReadInt32();
            SwitchToLeft = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write((int)Mode);
            e.Writer.Write(SwitchToLeft);
        }
    }

    public enum SwitchMode
    {
        REGULAR,
        FORCED,
        NO_SOUND,
    }
}
