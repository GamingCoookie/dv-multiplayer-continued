using DarkRift;
using DVMultiplayer.Darkrift;
using UnityEngine;

namespace DVMultiplayer.DTO.Train.Positioning
{
    public class TrainLocation : IDarkRiftSerializable
    {
        public string TrainId { get; set; }
        public Vector3 Forward { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public TrainBogie[] Bogies { get; set; }
        public bool IsStationary { get; set; }
        public Vector3 Velocity { get; internal set; }
        public float Temperature { get; internal set; }
        public float RPM { get; internal set; }
        public long Timestamp { get; internal set; }

        public void Deserialize(DeserializeEvent e)
        {
            TrainId = e.Reader.ReadString();
            Forward = e.Reader.ReadVector3();
            Position = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadQuaternion();
            Bogies = e.Reader.ReadSerializables<TrainBogie>();
            IsStationary = e.Reader.ReadBoolean();
            Velocity = e.Reader.ReadVector3();
            Temperature = e.Reader.ReadSingle();
            RPM = e.Reader.ReadSingle();
            Timestamp = e.Reader.ReadInt64();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainId);
            e.Writer.Write(Forward);
            e.Writer.Write(Position);
            e.Writer.Write(Rotation);
            e.Writer.Write(Bogies);
            e.Writer.Write(IsStationary);
            e.Writer.Write(Velocity);
            e.Writer.Write(Temperature);
            e.Writer.Write(RPM);
            e.Writer.Write(Timestamp);
        }
    }

    public class TrainBogie : IDarkRiftSerializable
    {
        public string TrackName { get; set; }
        public bool Derailed { get; set; } = false;
        public double PositionAlongTrack { get; set; } = 0;

        public void Deserialize(DeserializeEvent e)
        {
            TrackName = e.Reader.ReadString();
            Derailed = e.Reader.ReadBoolean();
            PositionAlongTrack = e.Reader.ReadDouble();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrackName);
            e.Writer.Write(Derailed);
            e.Writer.Write(PositionAlongTrack);
        }
    }
}
