using DarkRift;
using DVMultiplayer.Darkrift;
using UnityEngine;

namespace DVMultiplayer.DTO.Player
{
    public class NPlayer : IDarkRiftSerializable
    {
        public ushort Id { get; set; }
        public string Username { get; set; }
        public string[] Mods { get; set; }
        public bool IsLoaded { get; set; }
        public string Color { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadUInt16();
            Username = e.Reader.ReadString();
            Mods = e.Reader.ReadStrings();
            IsLoaded = e.Reader.ReadBoolean();
            Color = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Username);
            e.Writer.Write(Mods);
            e.Writer.Write(IsLoaded);
            e.Writer.Write(Color);
        }
    }
}