using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Job
{
    public class Chain : IDarkRiftSerializable
    {
        public string Id { get; set; }
        public string Data { get; set; }
        public bool IsCompleted { get; set; } = false;
        public bool IsExpired { get; set; } = false;


        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadString();
            Data = e.Reader.ReadString();
            IsCompleted = e.Reader.ReadBoolean();
            IsExpired = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Data);
            e.Writer.Write(IsCompleted);
            e.Writer.Write(IsExpired);
        }
    }
}
