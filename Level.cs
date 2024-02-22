using FancadeLoaderLib.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public class Level
    {
        private bool unknownFlag; // aditional byte after the color (so far only seen 0, in ECVV_SET_ANG_LIMITS_E)

        public string Name;
        public byte BackgroundColor;
        public bool LevelUnEditable;
        public BlockData BlockIds;
        public BlockValue[] BlockValues;
        public Connection[] Connections;

        public void Save(SaveWriter writer)
        {
            writer.WriteUInt8((byte)(
                  (LevelUnEditable ? 0b_0100_0000 : 0)
                | (unknownFlag ? 0b_0100_0000 : 0)
                | (BlockIds.Length > 0 ? 0b_0100 : 0)
                | (BlockValues.Length > 0 ? 0b_0010 : 0)
                | (Connections.Length > 0 ? 0b_0001 : 0)
                ));
            writer.WriteUInt8((byte)(BackgroundColor == 26 ? 0x18 : 0x19));
            writer.WriteUInt8(0x03);
            writer.WriteString(Name);
            if (BackgroundColor != 26)
                writer.WriteUInt8(BackgroundColor);

            if (unknownFlag)
                writer.WriteUInt8(0);

            if (BlockIds.Length > 0)
            {
                writer.WriteUInt16((ushort)BlockIds.Size.X);
                writer.WriteUInt16((ushort)BlockIds.Size.Y);
                writer.WriteUInt16((ushort)BlockIds.Size.Z);
                for (int z = 0; z < BlockIds.Size.Z; z++)
                    for (int y = 0; y < BlockIds.Size.Y; y++)
                        for (int x = 0; x < BlockIds.Size.X; x++)
                            writer.WriteUInt16(BlockIds.GetSegment(x, y, z));
            }
            if (BlockValues.Length > 0)
            {
                writer.WriteUInt16((ushort)BlockValues.Length);
                for (int i = 0; i < BlockValues.Length; i++)
                    BlockValues[i].Save(writer);
            }
            if (Connections.Length > 0)
            {
                writer.WriteUInt16((ushort)Connections.Length);
                for (int i = 0; i < Connections.Length; i++)
                    Connections[i].Save(writer);
            }
        }

        public static Level Load(SaveReader reader)
        {
            reader.NextThing(false, out object _info);
            byte[] info = (byte[])_info;

            string name = reader.ReadString();

            byte backgroundColor = 26;
            if ((info[1] & 0b_0000_0001) != 0)
                backgroundColor = reader.ReadUInt8();

            if ((info[0] & 0b_0010_0000) != 0)
                reader.ReadUInt8();

            Vector3I size;
            ushort[] blockIds;
            if ((info[0] & 0b_0000_0100) != 0) {
                size = new Vector3I(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());

                blockIds = new ushort[size.X * size.Y * size.Z];
                int i = 0;
                for (int x = 0; x < size.X; x++)
                    for (int y = 0; y < size.Y; y++)
                        for (int z = 0; z < size.Z; z++)
                            blockIds[i++] = reader.ReadUInt16();
            } else {
                size = Vector3I.One;
                blockIds = new ushort[0];
            }

            BlockValue[] values;
            // block values (for number, vec3, probably touch (touch 1,2 or 3), ...)
            if ((info[0] & 0b_0000_0010) != 0) {
                values = new BlockValue[reader.ReadUInt16()];
                for (int i = 0; i < values.Length; i++)
                    values[i] = BlockValue.Load(reader);
            } else
                values = new BlockValue[0];

            Connection[] connections;
            // connections (between value and variable, ...)
            if ((info[0] & 0b_0000_0001) != 0) {
                connections = new Connection[reader.ReadUInt16()];
                for (int i = 0; i < connections.Length; i++)
                    connections[i] = Connection.Load(reader);
            }
            else
                connections = new Connection[0];

            return new Level()
            {
                unknownFlag = (info[0] & 0b_0010_0000) != 0,
                Name = name,
                BackgroundColor = backgroundColor,
                LevelUnEditable = (info[0] & 0b_0100_0000) != 0,
                BlockIds = new BlockData(new Array3D<ushort>(blockIds, size.X, size.Y, size.Z)),
                BlockValues = values,
                Connections = connections
            };
        }

        public override string ToString() => $"[{Name}, Size: {BlockIds.Size}]";
    }
}
