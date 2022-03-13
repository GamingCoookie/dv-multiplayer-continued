using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class Steamer : IDarkRiftSerializable
    {
        public float BlankValve { get; set; } = 0f;
        public float Blower { get; set; } = 0f;
        public float BoilerPressure { get; set; } = 0f;
        public float BoilerWater { get; set; } = 0f;
        public float CoalInFirebox { get; set; } = 0f;
        public float CoalInTender { get; set; } = 0f;
        public float Draft { get; set; } = 0f;
        public float FireDoorPos { get; set; } = 0f;
        public float FireOn { get; set; } = 0f;
        public float FireOut { get; set; } = 0f;
        public float FireTemp { get; set; } = 0f;
        public float Injector { get; set; } = 0f;
        public float LightLever { get; set; } = 0f;
        public float LightSwitch { get; set; } = 0f;
        public float Sand { get; set; } = 0f;
        public float Sander { get; set; } = 0f;
        public float SteamRelease { get; set; } = 0f;
        public string TrainID { get; set; } = "";
        public float WaterDump { get; set; } = 0f;
        public float WaterInTender { get; set; } = 0f;
        public float Whistle { get; set; } = 0f;

        public void Deserialize(DeserializeEvent e)
        {
            BlankValve = e.Reader.ReadSingle();
            Blower = e.Reader.ReadSingle();
            BoilerPressure = e.Reader.ReadSingle();
            BoilerWater = e.Reader.ReadSingle();
            CoalInFirebox = e.Reader.ReadSingle();
            CoalInTender = e.Reader.ReadSingle();
            Draft = e.Reader.ReadSingle();
            FireDoorPos = e.Reader.ReadSingle();
            FireOn = e.Reader.ReadSingle();
            FireOut = e.Reader.ReadSingle();
            FireTemp = e.Reader.ReadSingle();
            Injector = e.Reader.ReadSingle();
            LightLever = e.Reader.ReadSingle();
            LightSwitch = e.Reader.ReadSingle();
            Sand = e.Reader.ReadSingle();
            Sander = e.Reader.ReadSingle();
            SteamRelease = e.Reader.ReadSingle();
            TrainID = e.Reader.ReadString();
            WaterDump = e.Reader.ReadSingle();
            WaterInTender = e.Reader.ReadSingle();
            Whistle = e.Reader.ReadSingle();
        }
        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(BlankValve);
            e.Writer.Write(Blower);
            e.Writer.Write(BoilerPressure);
            e.Writer.Write(BoilerWater);
            e.Writer.Write(CoalInFirebox);
            e.Writer.Write(CoalInTender);
            e.Writer.Write(Draft);
            e.Writer.Write(FireDoorPos);
            e.Writer.Write(FireOn);
            e.Writer.Write(FireOut);
            e.Writer.Write(FireTemp);
            e.Writer.Write(Injector);
            e.Writer.Write(LightLever);
            e.Writer.Write(LightSwitch);
            e.Writer.Write(Sand);
            e.Writer.Write(Sander);
            e.Writer.Write(SteamRelease);
            e.Writer.Write(TrainID);
            e.Writer.Write(WaterDump);
            e.Writer.Write(WaterInTender);
            e.Writer.Write(Whistle);
        }
    }
}