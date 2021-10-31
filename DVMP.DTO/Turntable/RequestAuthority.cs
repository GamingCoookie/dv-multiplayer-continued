using DarkRift;
using DVMultiplayer.Darkrift;
#if DVSERVER
using System.Numerics;
#endif
using UnityEngine;

namespace DVMultiplayer.DTO.Turntable
{
    public class RequestAuthority : IDarkRiftSerializable
    {
        public Vector3 Position { get; set; }
        public ushort PlayerId { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Position = e.Reader.ReadVector3();
            PlayerId = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Position);
            e.Writer.Write(PlayerId);
        }
    }
}
