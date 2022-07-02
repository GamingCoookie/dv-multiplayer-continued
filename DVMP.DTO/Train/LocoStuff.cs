using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class LocoStuff : IDarkRiftSerializable
    {
        public float BoilerPressure { get; set; } = 0f;
        public float BoilerWaterLevel { get; set; } = 0f;
        public bool EngineOn { get; set; } = false;
        public float FireboxCoalLevel { get; set; } = 0f;
        public bool FireOn { get; set; } = false;
        public float FuelLevel { get; set; } = 0f;
        public float OilLevel { get; set; } = 0f;
        public float SandLevel { get; set; } = 0f;
        public float Temp { get; set; } = 0f;
        public float TenderCoalLevel { get; set; } = 0f;
        public float TenderWaterLevel { get; set; } = 0f;
        
        public void Deserialize(DeserializeEvent e)
        {
            BoilerPressure = e.Reader.ReadSingle();
            BoilerWaterLevel = e.Reader.ReadSingle();
            EngineOn = e.Reader.ReadBoolean();
            FireboxCoalLevel = e.Reader.ReadSingle();
            FireOn = e.Reader.ReadBoolean();
            FuelLevel = e.Reader.ReadSingle();
            OilLevel = e.Reader.ReadSingle();
            SandLevel = e.Reader.ReadSingle();
            Temp = e.Reader.ReadSingle();
            TenderCoalLevel = e.Reader.ReadSingle();
            TenderWaterLevel = e.Reader.ReadSingle();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(BoilerPressure);
            e.Writer.Write(BoilerWaterLevel);
            e.Writer.Write(EngineOn);
            e.Writer.Write(FireboxCoalLevel);
            e.Writer.Write(FireOn);
            e.Writer.Write(FuelLevel);
            e.Writer.Write(OilLevel);
            e.Writer.Write(SandLevel);
            e.Writer.Write(Temp);
            e.Writer.Write(TenderCoalLevel);
            e.Writer.Write(TenderWaterLevel);
        }
    }
}
