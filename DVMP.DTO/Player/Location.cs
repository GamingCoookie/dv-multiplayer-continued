using DarkRift;
using DVMultiplayer.Darkrift;
#if DVSERVER
using System.Numerics;
#endif
using UnityEngine;

namespace DVMultiplayer.DTO.Player
{
    public class Location : IDarkRiftSerializable
    {
        public ushort Id { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion? Rotation { get; set; }
        public int AproxPing { get; set; } = 0;
        public long UpdatedAt { get; internal set; }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadUInt16();
            Position = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadNullableQuaternion();
            UpdatedAt = e.Reader.ReadInt64();
            AproxPing = e.Reader.ReadInt32();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Position);
            e.Writer.Write(Rotation);
            e.Writer.Write(UpdatedAt);
            e.Writer.Write(AproxPing);
        }
    }
}