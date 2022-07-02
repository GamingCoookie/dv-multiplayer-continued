using System;
using System.Collections.Generic;
using System.Text;
using DarkRift;
using DarkRift.Server;

namespace DVMP.DTO.Player
{
    public class License : IDarkRiftSerializable
    {
        public ushort PlayerID { get; set; } = 65535;
        public string LicenseName { get; set; } = "";
        public double Price { get; set; } = 0;
        public int PurchaseAllowed { get; set; } = 0;

        public void Deserialize(DeserializeEvent e)
        {
            PlayerID = e.Reader.ReadUInt16();
            LicenseName = e.Reader.ReadString();
            Price = e.Reader.ReadDouble();
            PurchaseAllowed = e.Reader.ReadInt32();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(PlayerID);
            e.Writer.Write(LicenseName);
            e.Writer.Write(Price);
            e.Writer.Write(PurchaseAllowed);
        }
    }
}
