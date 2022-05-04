using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class Diesel : IDarkRiftSerializable
    {
        public float Bell { get; set; } = 0f;
        public float CabLight { get; set; } = 0f;
        public float Door1 { get; set; } = 0f;
        public float Door2 { get; set; } = 0f;
        public float DynamicBrake { get; set; } = 0f;
        public float EmergencyOff { get; set; } = 0f;
        public float EngineBayDoor1 { get; set; } = 0f;
        public float EngineBayDoor2 { get; set; } = 0f;
        public float EngineBayThrottle { get; set; } = 0f;
        public float EngineIgnition { get; set; } = 0f;
        public float FanSwitch { get; set; } = 0f;
        public float Fuel { get; set; } = 0f;
        public float FusePanelDoor { get; set; } = 0f;
        public float HeadlightSwitch { get; set; } = 0f;
        public float Horn { get; set; } = 0f;
        public bool IsEngineOn { get; set; } = false;
        public bool IsMainFuseOn { get; set; } = false;
        public bool IsSideFuse1On { get; set; } = false;
        public bool IsSideFuse2On { get; set; } = false;
        public bool IsSideFuse3On { get; set; } = false;
        public float Oil { get; set; } = 0f;
        public float RPM { get; set; } = 0f;
        public float Sand { get; set; } = 0f;
        public float Temp { get; set; } = 0f;
        public float Window1 { get; set; } = 0f;
        public float Window2 { get; set; } = 0f;
        public float Window3 { get; set; } = 0f;
        public float Window4 { get; set; } = 0f;

        public void Deserialize(DeserializeEvent e)
        {
            Bell = e.Reader.ReadSingle();
            CabLight = e.Reader.ReadSingle();
            Door1 = e.Reader.ReadSingle();
            Door2 = e.Reader.ReadSingle();
            DynamicBrake = e.Reader.ReadSingle();
            EmergencyOff = e.Reader.ReadSingle();
            EngineBayDoor1 = e.Reader.ReadSingle();
            EngineBayDoor2 = e.Reader.ReadSingle();
            EngineBayThrottle = e.Reader.ReadSingle();
            EngineIgnition = e.Reader.ReadSingle();
            FanSwitch = e.Reader.ReadSingle();
            Fuel = e.Reader.ReadSingle();
            FusePanelDoor = e.Reader.ReadSingle();
            HeadlightSwitch = e.Reader.ReadSingle();
            Horn = e.Reader.ReadSingle();
            IsEngineOn = e.Reader.ReadBoolean();
            IsMainFuseOn = e.Reader.ReadBoolean();
            IsSideFuse1On = e.Reader.ReadBoolean();
            IsSideFuse2On = e.Reader.ReadBoolean();
            IsSideFuse3On = e.Reader.ReadBoolean();
            Oil = e.Reader.ReadSingle();
            RPM = e.Reader.ReadSingle();
            Sand = e.Reader.ReadSingle();
            Temp = e.Reader.ReadSingle();
            Window1 = e.Reader.ReadSingle();
            Window2 = e.Reader.ReadSingle();
            Window3 = e.Reader.ReadSingle();
            Window4 = e.Reader.ReadSingle();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Bell);
            e.Writer.Write(CabLight);
            e.Writer.Write(Door1);
            e.Writer.Write(Door2);
            e.Writer.Write(DynamicBrake);
            e.Writer.Write(EmergencyOff);
            e.Writer.Write(EngineBayDoor1);
            e.Writer.Write(EngineBayDoor2);
            e.Writer.Write(EngineBayThrottle);
            e.Writer.Write(EngineIgnition);
            e.Writer.Write(FanSwitch);
            e.Writer.Write(Fuel);
            e.Writer.Write(FusePanelDoor);
            e.Writer.Write(HeadlightSwitch);
            e.Writer.Write(Horn);
            e.Writer.Write(IsEngineOn);
            e.Writer.Write(IsMainFuseOn);
            e.Writer.Write(IsSideFuse1On);
            e.Writer.Write(IsSideFuse2On);
            e.Writer.Write(IsSideFuse3On);
            e.Writer.Write(Oil);
            e.Writer.Write(RPM);
            e.Writer.Write(Sand);
            e.Writer.Write(Temp);
            e.Writer.Write(Window1);
            e.Writer.Write(Window2);
            e.Writer.Write(Window3);
            e.Writer.Write(Window4);
        }
    }
}
