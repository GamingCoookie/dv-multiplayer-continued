using DarkRift;

namespace DVMultiplayer.DTO.Player
{
    public class Mod : IDarkRiftSerializable
    {
        public string Name;
        public string Version;

        public Mod()
        { }

        public Mod(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public void Deserialize(DeserializeEvent e)
        {
            Name = e.Reader.ReadString();
            Version = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Name);
            e.Writer.Write(Version);
        }

        public override string ToString()
        {
            return $"{Name} v{Version}";
        }

        public override bool Equals(object obj)
        {
            return obj is Mod m && Name == m.Name && Version == m.Version;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() * Version.GetHashCode();
        }
    }
}
