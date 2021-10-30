using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Train
{
    public class CarMUChange : IDarkRiftSerializable
    {
        public string TrainId1 { get; set; }
        public string TrainId2 { get; set; }
        public bool Train1IsFront { get; set; }
        public bool Train2IsFront { get; set; }
        public bool IsConnected { get; set; }
        public bool AudioPlayed { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            TrainId1 = e.Reader.ReadString();
            TrainId2 = e.Reader.ReadString();
            Train1IsFront = e.Reader.ReadBoolean();
            Train2IsFront = e.Reader.ReadBoolean();
            IsConnected = e.Reader.ReadBoolean();
            AudioPlayed = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainId1);
            e.Writer.Write(TrainId2);
            e.Writer.Write(Train1IsFront);
            e.Writer.Write(Train2IsFront);
            e.Writer.Write(IsConnected);
            e.Writer.Write(AudioPlayed);
        }
    }
}
