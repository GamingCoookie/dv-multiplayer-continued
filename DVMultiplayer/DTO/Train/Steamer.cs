﻿using DarkRift;

namespace DVMultiplayer.DTO.Train
{
    public class Steamer : IDarkRiftSerializable
    {
        public bool IsFireOn { get; set; } = false;
        public float FireboxDoor { get; set; } = 0;
        public float DraftPuller { get; set; } = 0;
        public float WaterInjector { get; set; } = 0;
        public float Blower { get; set; } = 0;
        public float SteamRelease { get; set; } = 0;
        public float WaterDump { get; set; } = 0;
        public float GetFireOn { get; set; } = 0;

        public void Deserialize(DeserializeEvent e)
        {
            IsFireOn = e.Reader.ReadBoolean();
            FireboxDoor = e.Reader.ReadSingle();
            DraftPuller = e.Reader.ReadSingle();
            WaterInjector = e.Reader.ReadSingle();
            Blower = e.Reader.ReadSingle();
            SteamRelease = e.Reader.ReadSingle();
            WaterDump = e.Reader.ReadSingle();
            GetFireOn = e.Reader.ReadSingle();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(IsFireOn);
            e.Writer.Write(FireboxDoor);
            e.Writer.Write(DraftPuller);
            e.Writer.Write(WaterInjector);
            e.Writer.Write(Blower);
            e.Writer.Write(SteamRelease);
            e.Writer.Write(WaterDump);
            e.Writer.Write(GetFireOn);
        }
    }
}