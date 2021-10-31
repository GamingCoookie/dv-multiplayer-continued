using DarkRift;
using DV.Logic.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.DTO.Job
{
    public class Job : IDarkRiftSerializable
    {
        public string Id { get; set; }
        public string ChainId { get; set; }
        public string GameId { get; set; }
        public bool IsTaken { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
        public bool IsCurrentJob { get; set; } = false;
        public JobType Type { get; set; }

#if DVCLIENT // Client Stuff
        internal bool CanTakeJob { get; set; } = true;
        internal bool IsTakenByLocalPlayer { get; set; } = false;
        internal StaticJobDefinition Definition { get; set; }
#endif
        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadString();
            ChainId = e.Reader.ReadString();
            GameId = e.Reader.ReadString();
            IsTaken = e.Reader.ReadBoolean();
            IsCompleted = e.Reader.ReadBoolean();
            IsCurrentJob = e.Reader.ReadBoolean();
            Type = (JobType)e.Reader.ReadUInt32();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(ChainId);
            e.Writer.Write(GameId);
            e.Writer.Write(IsTaken);
            e.Writer.Write(IsCompleted);
            e.Writer.Write(IsCurrentJob);
            e.Writer.Write((uint)Type);
        }
    }
}
