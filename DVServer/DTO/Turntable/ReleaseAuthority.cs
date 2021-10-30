using DarkRift;
using DVMultiplayer.Darkrift;
using System.Numerics;
using UnityEngine;

namespace DVMultiplayer.DTO.Turntable
{
    public class ReleaseAuthority : IDarkRiftSerializable
    {
        public Vector3 Position { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Position = e.Reader.ReadVector3();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Position);
        }
    }
}
