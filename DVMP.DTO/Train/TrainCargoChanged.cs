using DarkRift;
using DV.Logic.Job;

namespace DVMultiplayer.DTO.Train
{
    public class TrainCargoChanged : IDarkRiftSerializable
    {
        public string Id { get; set; }
        public float Amount { get; set; } = 0;
        public CargoType Type { get; set; } = CargoType.None;
        public string WarehouseId { get; set; } = "";
        public bool IsLoading { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadString();
            Amount = e.Reader.ReadSingle();
            Type = (CargoType)e.Reader.ReadUInt32();
            WarehouseId = e.Reader.ReadString();
            IsLoading = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Amount);
            e.Writer.Write((uint)Type);
            e.Writer.Write(WarehouseId);
            e.Writer.Write(IsLoading);
        }
    }
}
