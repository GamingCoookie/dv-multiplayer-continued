using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkRift;

namespace DVMP.DTO
{
    public static class Ctrls
    {
        public const string
            DEIndepBrake = "C independent_brake_lever",
            DEReverser = "C reverser",
            DESand = "C deploy_sand button",
            DEThrottle = "C throttle",
            DETrainBrake = "C train_brake_lever",
            DieselEBThrottle = "C engine_thottle", //Engine bay trhottle
            DieselMainFuse = "C main_battery_switch",
            DieselSideFuse1 = "C fuse_1_switch",
            DieselSideFuse2 = "C fuse_2_switch",
            DieselSideFuse3 = "C fuse_3_switch",
            S282Reverser = "C cutoff reverser",
            S282Throttle = "C throttle regulator",
            S282TrainBrake = "C brake",
            S282IndepBrake = "C independent brake",
            S282Sand = "C sand valve",
            ShunterSideFuse1 = "fuse 3 side",
            ShunterSideFuse2 = "fuse 5 side",
            ShunterMainFuse = "fuse 1 main";

        public static bool IsMUControl(string lever)
        {
            return new[] { DEThrottle, DEReverser, DEIndepBrake, DETrainBrake, DESand, DieselEBThrottle }.Any(x => lever == x);
        }
    }

    public class SerializableDictionary<TKey, TValue> : Dictionary<string, float>, IDarkRiftSerializable
    {
        public void Deserialize(DeserializeEvent e)
        {
            int Length = e.Reader.ReadInt32();
            for (int i = 0; i < Length; i++)
            {
                string key = e.Reader.ReadString();
                float value = e.Reader.ReadSingle();
                this[key] = value;
            }
        }
        
        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(this.Count);
            foreach (KeyValuePair<string, float> pair in this)
            {
                e.Writer.Write(pair.Key);
                e.Writer.Write(pair.Value);
            }
        }
    }
}
