using DarkRift;
using UnityEngine;

namespace DVMultiplayer.Darkrift
{
    public static class DarkRiftExtends
    {
        public static void Write(this DarkRiftWriter writer, Vector3 vector3)
        {
            writer.Write(vector3.x);
            writer.Write(vector3.y);
            writer.Write(vector3.z);
        }

        public static Vector3 ReadVector3(this DarkRiftReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this DarkRiftWriter writer, Vector3? vector3)
        {
            writer.Write(vector3.HasValue);
            if (vector3.HasValue)
            {
                writer.Write(vector3.Value.x);
                writer.Write(vector3.Value.y);
                writer.Write(vector3.Value.z);
            }
        }

        public static Vector3? ReadNullableVector3(this DarkRiftReader reader)
        {
            bool hasValue = reader.ReadBoolean();
            Vector3? v = null;
            if (hasValue)
            {
                v = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            return v;
        }

        public static void Write(this DarkRiftWriter writer, Quaternion quaternion)
        {
            writer.Write(quaternion.x);
            writer.Write(quaternion.y);
            writer.Write(quaternion.z);
            writer.Write(quaternion.w);
        }

        public static Quaternion ReadQuaternion(this DarkRiftReader reader)
        {
            return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this DarkRiftWriter writer, Quaternion? quaternion)
        {
            writer.Write(quaternion.HasValue);
            if (quaternion.HasValue)
            {
                writer.Write(quaternion.Value.x);
                writer.Write(quaternion.Value.y);
                writer.Write(quaternion.Value.z);
                writer.Write(quaternion.Value.w);
            }
        }

        public static Quaternion? ReadNullableQuaternion(this DarkRiftReader reader)
        {
            bool hasValue = reader.ReadBoolean();
            Quaternion? q = null;
            if (hasValue)
            {
                q = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            return q;
        }

        public static void Write(this DarkRiftWriter writer, bool? b)
        {
            writer.Write(b.HasValue);
            if (b.HasValue)
            {
                writer.Write(b.Value);
            }
        }

        public static bool? ReadNullableBoolean(this DarkRiftReader reader)
        {
            bool hasValue = reader.ReadBoolean();
            bool? b = null;
            if (hasValue)
            {
                b = reader.ReadBoolean();
            }
            return b;
        }

        public static void Write(this DarkRiftWriter writer, double? b)
        {
            writer.Write(b.HasValue);
            if (b.HasValue)
            {
                writer.Write(b.Value);
            }
        }

        public static double? ReadNullableDouble(this DarkRiftReader reader)
        {
            bool hasValue = reader.ReadBoolean();
            double? b = null;
            if (hasValue)
            {
                b = reader.ReadDouble();
            }
            return b;
        }

        public static void Write(this DarkRiftWriter writer, float? b)
        {
            writer.Write(b.HasValue);
            if (b.HasValue)
            {
                writer.Write(b.Value);
            }
        }

        public static float? ReadNullableSingle(this DarkRiftReader reader)
        {
            bool hasValue = reader.ReadBoolean();
            float? b = null;
            if (hasValue)
            {
                b = reader.ReadSingle();
            }
            return b;
        }
    }
}
