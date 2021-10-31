using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Debt
{
    class DebtPaid : IDarkRiftSerializable
    {
        public string Id { get; set; }
        public bool isDestroyed { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadString();
            isDestroyed = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(isDestroyed);
        }
    }
}
