using MathUtils.Vectors;
using System;

namespace FancadeLoaderLib
{
    /// <summary>
    /// If value == default, no block value
    /// </summary>
    public struct PrefabSetting
    {
        public byte Index;
        private SettingType type;
        public SettingType Type
        {
            get => type;
            set
            {
                if (!IsValueValid(Value, value))
                    this.value = GetDefaultValueForType(value);

                type = value;
            }
        }
        public Vector3US Position;
        private object value;
        public object Value
        {
            get => value;
            set
            {
                if (!IsValueValid(value, Type))
                    throw new ArgumentException($"Type of value '{value?.GetType()?.FullName ?? "null"}' isn't valid for {nameof(SettingType)} '{Type}'", nameof(value));

                this.value = value;
            }
        }

        public PrefabSetting(byte _index, SettingType _type, Vector3US _pos, object _value)
        {
            if (!IsValueValid(_value, _type))
                throw new ArgumentException($"Type of value '{_value?.GetType()?.FullName ?? "null"}' isn't valid for {nameof(_type)} '{_type}'", nameof(_value));

            Index = _index;
            type = _type;
            Position = _pos;
            value = _value;
        }

        public void SetValue(SettingType type, object value)
        {
            if (!IsValueValid(value, type))
                throw new ArgumentException($"Type of value '{value?.GetType()?.FullName ?? "null"}' isn't valid for {nameof(type)} '{type}'", nameof(value));

            this.type = type;
            this.value = value;
        }
        public void Save(FcBinaryWriter writer)
        {
            writer.WriteUInt8(Index);
            writer.WriteUInt8((byte)Type);
            writer.WriteVec3US(Position);

            switch (Type)
            {
                case SettingType.Byte: // byte
                    writer.WriteUInt8((byte)Value);
                    break;
                case SettingType.Ushort: // ushort
                    writer.WriteUInt16((ushort)Value);
                    break;
                case SettingType.Int: // int
                    writer.WriteInt32((int)Value);
                    break;
                case SettingType.Float: // float
                    writer.WriteFloat((float)Value);
                    break;
                case SettingType.Vec3: // vec3 or rot (euler angles)
                    writer.WriteVec3F((Vector3F)Value);
                    break;
                case SettingType.String:
                default: // string (string value (6) or connector name (7+))
                    writer.WriteString((string)Value);
                    break;
            }
        }

        public static PrefabSetting Load(FcBinaryReader reader)
        {
            byte valueIndex = reader.ReadUInt8();
            SettingType type = (SettingType)reader.ReadUInt8();
            Vector3US pos = reader.ReadVec3US();
            object value;

            switch (type)
            {
                case SettingType.Byte: // byte
                    value = reader.ReadUInt8();
                    break;
                case SettingType.Ushort: // ushort
                    value = reader.ReadUInt16();
                    break;
                case SettingType.Int: // int
                    value = reader.ReadInt32();
                    break;
                case SettingType.Float: // float
                    value = reader.ReadFloat();
                    break;
                case SettingType.Vec3: // vec3 or rot (euler angles)
                    value = new Vector3F(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
                    break;
                case SettingType.String:
                default: // string (string value (6) or connector name (7+))
                    value = reader.ReadString();
                    break;
            }

            return new PrefabSetting()
            {
                Index = valueIndex,
                Type = type,
                Position = pos,
                Value = value
            };
        }

        public static bool IsValueValid(object value, SettingType type)
        {
            switch (type)
            {
                case SettingType.Byte:
                    return value is byte;
                case SettingType.Ushort:
                    return value is ushort;
                case SettingType.Int:
                    return value is int;
                case SettingType.Float:
                    return value is float;
                case SettingType.Vec3:
                    return value is Vector3F;
                case SettingType.String:
                default:
                    return value is string;
            }
        }

        public static object GetDefaultValueForType(SettingType type)
        {
            switch (type)
            {
                case SettingType.Byte:
                    return default(byte);
                case SettingType.Ushort:
                    return default(ushort);
                case SettingType.Int:
                    return default(int);
                case SettingType.Float:
                    return default(float);
                case SettingType.Vec3:
                    return default(Vector3F);
                case SettingType.String:
                default:
                    return string.Empty;
            }
        }

        public override string ToString() => $"[Type: {Type}, Value: {Value}, Pos: {Position}]";

    }

    public enum SettingType : byte
    {
        Byte = 1,
        Ushort = 2,
        Int = 3,
        Float = 4,
        Vec3 = 5,
        String = 6,
    }
}
