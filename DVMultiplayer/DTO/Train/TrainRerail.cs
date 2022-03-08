using DarkRift;
using DVMultiplayer.Darkrift;
using UnityEngine;

namespace DVMultiplayer.DTO.Train
{
    public class TrainRerail : IDarkRiftSerializable
    {
        public string Guid { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Forward { get; set; }
        public Quaternion Rotation { get; set; }
        public string Bogie1TrackName { get; set; }
        public string Bogie2TrackName { get; set; }
        public double Bogie1PositionAlongTrack { get; set; }
        public double Bogie2PositionAlongTrack { get; set; }
        public float CarHealth { get; set; }
        public float CargoHealth { get; set; } = 0;

        public void Deserialize(DeserializeEvent e)
        {
            Guid = e.Reader.ReadString();
            Forward = e.Reader.ReadVector3();
            Position = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadQuaternion();

            Bogie1TrackName = e.Reader.ReadString();
            Bogie2TrackName = e.Reader.ReadString();

            Bogie1PositionAlongTrack = e.Reader.ReadDouble();
            Bogie2PositionAlongTrack = e.Reader.ReadDouble();

            CarHealth = e.Reader.ReadSingle();
            CargoHealth = e.Reader.ReadSingle();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Guid);
            e.Writer.Write(Forward);
            e.Writer.Write(Position);
            e.Writer.Write(Rotation);

            e.Writer.Write(Bogie1TrackName);
            e.Writer.Write(Bogie2TrackName);

            e.Writer.Write(Bogie1PositionAlongTrack);
            e.Writer.Write(Bogie2PositionAlongTrack);

            e.Writer.Write(CarHealth);
            e.Writer.Write(CargoHealth);
        }
    }
}
