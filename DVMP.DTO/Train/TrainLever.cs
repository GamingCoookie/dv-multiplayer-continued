using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public enum Levers
    {
        Bell,
        BlankValve,
        Blower,
        Brake,
        CabLightSwitch,
        Door1,
        Door2,
        Draft,
        DynamicBrake,
        EmergencyOff,
        EngineBayDoor1,
        EngineBayDoor2,
        EngineBayThrottle,
        EngineIgnition,
        FanSwitch,
        FusePanelDoor,
        FireDoor,
        FireOut,
        FusePowerStarter,
        Horn,
        IndependentBrake,
        Injector,
        LightLever,
        HeadlightSwitch,
        MainFuse,
        Reverser,
        Sander,
        SideFuse_1,
        SideFuse_2,
        SideFuse_3,
        SteamRelease,
        Throttle,
        WaterDump,
        Window1,
        Window2,
        Window3,
        Window4
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
