using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public enum Levers
    {
        Throttle,
        Brake,
        IndependentBrake,
        Reverser,
        Sander,
        SideFuse_1,
        SideFuse_2,
        SideFuse_3,
        MainFuse,
        FusePowerStarter,
        Horn
    }

    public class TrainLever : IDarkRiftSerializable
    {
        public string TrainId { get; set; }
        public Levers Lever { get; set; }
        public float Value { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            TrainId = e.Reader.ReadString();
            Lever = (Levers)e.Reader.ReadUInt32();
            Value = e.Reader.ReadSingle();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainId);
            e.Writer.Write((uint)Lever);
            e.Writer.Write(Value);
        }
    }
}
