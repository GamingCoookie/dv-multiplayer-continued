using System;
using System.Collections.Generic;
using System.Text;
using DarkRift;

namespace DVMP.DTO.Player
{
    public class ChatMessage : IDarkRiftSerializable
    {
        public string Message { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Message = e.Reader.ReadString();
        }
        
        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Message);
        }
    }
}
