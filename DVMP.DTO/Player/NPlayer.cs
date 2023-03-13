using DarkRift;

namespace DVMultiplayer.DTO.Player
{
    public class NPlayer : IDarkRiftSerializable
    {
        public ushort Id { get; set; }
        public string Username { get; set; }
        public Location Location { get; set; }
        public Mod[] Mods { get; set; }
        public string[] CustomCars { get; set; }
        public bool IsLoaded { get; set; }
        public uint Color { get; set; }

        public NPlayer()
        { }
        
        public NPlayer(NPlayer player)
        {
            Id = player.Id;
            Username = player.Username;
            Location = player.Location;
            Mods = player.Mods;
            CustomCars = player.CustomCars;
            IsLoaded = player.IsLoaded;
            Color = player.Color;
        }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadUInt16();
            Username = e.Reader.ReadString();
            Location = e.Reader.ReadSerializable<Location>();
            Mods = e.Reader.ReadSerializables<Mod>();
            CustomCars = e.Reader.ReadStrings();
            IsLoaded = e.Reader.ReadBoolean();
            Color = e.Reader.ReadUInt32();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Username);
            e.Writer.Write(Location);
            e.Writer.Write(Mods);
            e.Writer.Write(CustomCars);
            e.Writer.Write(IsLoaded);
            e.Writer.Write(Color);
        }
    }
}
