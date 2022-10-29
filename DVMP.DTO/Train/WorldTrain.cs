using DarkRift;
using DV.Logic.Job;
using DVMultiplayer.Darkrift;
using UnityEngine;
using DVMP.DTO;

namespace DVMultiplayer.DTO.Train
{
    public class WorldTrain : IDarkRiftSerializable
    {
        // Positioning and Physics
        public string Guid { get; set; }
        public string Id { get; set; } = "";
        public TrainCarType CarType { get; set; } = TrainCarType.NotSet;
        public string CCLCarId { get; set; } = "";
        public bool IsLoco { get; set; }
        public bool IsRemoved { get; set; } = false;

        // Player
        public bool IsPlayerSpawned { get; set; }
        public ushort AuthorityPlayerId { get; set; } = 0xffff;

        // Position and physics
        public Vector3 Position { get; set; }
        public Vector3 Forward { get; set; }
        public Quaternion Rotation { get; set; }
        public bool IsStationary { get; set; }
        public TrainBogie[] Bogies { get; set; }

        // Couplers
        public string FrontCouplerCoupledTo { get; set; } = "";
        public string RearCouplerCoupledTo { get; set; } = "";
        public bool IsFrontCouplerCockOpen { get; set; } = false;
        public bool IsRearCouplerCockOpen { get; set; } = false;
        public string FrontCouplerHoseConnectedTo { get; set; } = "";
        public string RearCouplerHoseConnectedTo { get; set; } = "";

        // Damage
        public float CarHealth { get; set; }
        public string CarHealthData { get; set; } = "";

        // Specific Train states
        public LocoStuff LocoStuff { get; set; } = new LocoStuff();
        public SerializableDictionary<string, float> Controls { get; set; } = new SerializableDictionary<string, float>();
        public MultipleUnit MultipleUnit { get; set; } = new MultipleUnit();

        // Cargo based trains
        public float CargoAmount { get; set; }
        public CargoType CargoType { get; set; } = CargoType.None;
        public float CargoHealth { get; set; }

        //Data specific
        public long updatedAt { get; set; }
        public void Deserialize(DeserializeEvent e)
        {
            Guid = e.Reader.ReadString();
            Id = e.Reader.ReadString();
            CarType = (TrainCarType)e.Reader.ReadUInt32();
            CCLCarId = e.Reader.ReadString();
            IsLoco = e.Reader.ReadBoolean();
            IsRemoved = e.Reader.ReadBoolean();

            Position = e.Reader.ReadVector3();
            Forward = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadQuaternion();
            IsStationary = e.Reader.ReadBoolean();
            Bogies = e.Reader.ReadSerializables<TrainBogie>();

            CarHealth = e.Reader.ReadSingle();
            CarHealthData = e.Reader.ReadString();

            FrontCouplerCoupledTo = e.Reader.ReadString();
            RearCouplerCoupledTo = e.Reader.ReadString();
            IsFrontCouplerCockOpen = e.Reader.ReadBoolean();
            IsRearCouplerCockOpen = e.Reader.ReadBoolean();
            FrontCouplerHoseConnectedTo = e.Reader.ReadString();
            RearCouplerHoseConnectedTo = e.Reader.ReadString();

            IsPlayerSpawned = e.Reader.ReadBoolean();
            AuthorityPlayerId = e.Reader.ReadUInt16();

            if (IsLoco)
            {
                Controls = e.Reader.ReadSerializable<SerializableDictionary<string, float>>();
                LocoStuff = e.Reader.ReadSerializable<LocoStuff>();
            }
            else
            {
                CargoType = (CargoType)e.Reader.ReadUInt32();
                CargoAmount = e.Reader.ReadSingle();
                CargoHealth = e.Reader.ReadSingle();
            }

            switch (CarType)
            {
                case TrainCarType.LocoShunter:
                case TrainCarType.LocoDiesel:
                    MultipleUnit = e.Reader.ReadSerializable<MultipleUnit>();
                    break;
            }

            updatedAt = e.Reader.ReadInt64();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Guid);
            e.Writer.Write(Id);
            e.Writer.Write((uint)CarType);
            e.Writer.Write(CCLCarId);
            e.Writer.Write(IsLoco);
            e.Writer.Write(IsRemoved);

            e.Writer.Write(Position);
            e.Writer.Write(Forward);
            e.Writer.Write(Rotation);
            e.Writer.Write(IsStationary);
            e.Writer.Write(Bogies);

            e.Writer.Write(CarHealth);
            e.Writer.Write(CarHealthData);

            e.Writer.Write(FrontCouplerCoupledTo);
            e.Writer.Write(RearCouplerCoupledTo);
            e.Writer.Write(IsFrontCouplerCockOpen);
            e.Writer.Write(IsRearCouplerCockOpen);
            e.Writer.Write(FrontCouplerHoseConnectedTo);
            e.Writer.Write(RearCouplerHoseConnectedTo);

            e.Writer.Write(IsPlayerSpawned);
            e.Writer.Write(AuthorityPlayerId);

            if (IsLoco)
            {
                e.Writer.Write(Controls);
                e.Writer.Write(LocoStuff);
            }
            else
            {
                e.Writer.Write((uint)CargoType);
                e.Writer.Write(CargoAmount);
                e.Writer.Write(CargoHealth);
            }

            switch (CarType)
            {
                case TrainCarType.LocoShunter:
                case TrainCarType.LocoDiesel:
                    e.Writer.Write(MultipleUnit);
                    break;
            }
            e.Writer.Write(updatedAt);
        }
    }
}
