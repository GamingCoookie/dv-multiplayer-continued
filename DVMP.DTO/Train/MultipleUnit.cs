using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Train
{
    public class MultipleUnit : IDarkRiftSerializable
    {
        public string IsFrontMUConnectedTo { get; set; } = "";
        public string IsRearMUConnectedTo { get; set; } = "";

        public void Deserialize(DeserializeEvent e)
        {
            IsFrontMUConnectedTo = e.Reader.ReadString();
            IsRearMUConnectedTo = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(IsFrontMUConnectedTo);
            e.Writer.Write(IsRearMUConnectedTo);
        }
    }
}
