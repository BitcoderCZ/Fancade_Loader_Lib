﻿using MathUtils.Vectors;
using System;
using System.Numerics;

namespace FancadeLoaderLib
{
    /// <summary>
    /// If value == default, no block value
    /// </summary>
    public struct BlockValue
    {
        public byte ValueIndex; // first, second...
        // TODO: make enum
        public byte Type;
        public Vector3US Position;
        // TODO: validate that Value type == this.Type
        public object Value;

        public void Save(SaveWriter writer)
        {
            writer.WriteUInt8(ValueIndex);
            writer.WriteUInt8(Type);
            writer.WriteUInt16(Position.X);
            writer.WriteUInt16(Position.Y);
            writer.WriteUInt16(Position.Z);

            switch (Type)
            {
                case 1: // byte
                    writer.WriteUInt8((byte)Value);
                    break;
                case 2: // ushort, used by sound
                    writer.WriteUInt16((ushort)Value);
                    break;
                case 4: // float
                    writer.WriteFloat((float)Value);
                    break;
                case 5: // vec3, rot
                    Vector3F vec = (Vector3F)Value;
                    writer.WriteFloat(vec.X);
                    writer.WriteFloat(vec.Y);
                    writer.WriteFloat(vec.Z);
                    break;
                // TODO: test
                // I think this also describes the connector type and if it's in/out
                case 6: // string
                case 7: // also connector name? was for "execute"
                case 8: // connector name
                case 9: // also also also also connector name? was for "On"
                case 10: // also also also connector name? was for "Axis"
                case 11: // connector idk
                case 12: // in rot
                case 13: // connector idk
                case 14: // connector idk
                case 15: // out bool
                case 16: // also also connector name? was for "Object"
                case 17: // connector idk
                    writer.WriteString((string)Value);
                    break;
                default:
                    throw new Exception($"Unknown value type \"{Type}\" at pos: {Position}");
            }
        }

        public static BlockValue Load(SaveReader reader)
        {
            byte valueIndex = reader.ReadUInt8();
            byte type = reader.ReadUInt8();
            Vector3US pos = new Vector3US(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
            object value;

            switch (type)
            {
                case 1: // byte
                    value = reader.ReadUInt8();
                    break;
                case 2: // ushort, used by sound
                    value = reader.ReadUInt16();
                    break;
                case 4: // float
                    value = reader.ReadFloat();
                    break;
                case 5: // vec3, rot
                    value = new Vector3F(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
                    break;
                // TODO: test
                // I think this also describes the connector type and if it's in/out
                case 6: // string
                case 7: // also connector name? was for "execute"
                case 8: // connector name
                case 9: // also also also also connector name? was for "On"
                case 10: // also also also connector name? was for "Axis"
                case 11: // connector idk
                case 12: // in rot
                case 13: // connector idk
                case 14: // connector idk
                case 15: // out bool
                case 16: // also also connector name? was for "Object"
                case 17: // connector idk
                    value = reader.ReadString();
                    break;
                default:
                    throw new Exception($"Unknown value type \"{type}\" at pos: {pos}");
            }

            return new BlockValue()
            {
                ValueIndex = valueIndex,
                Type = type,
                Position = pos,
                Value = value
            };
        }

        public override string ToString() => $"[Type: {Type}, Value: {Value}, Pos: {Position}]";
    }
}
