using DarkRift;
using DVMultiplayer.Darkrift;
#if DVSERVER
using System.Numerics;
#endif
using UnityEngine;

namespace DVMultiplayer.DTO.Turntable
{
    public class Turntable : IDarkRiftSerializable
    {
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public ushort playerAuthId { get; set; } = 0;

        public void Deserialize(DeserializeEvent e)
        {
            Position = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadSingle();
            playerAuthId = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Position);
            e.Writer.Write(Rotation);
            e.Writer.Write(playerAuthId);
        }
    }
}
