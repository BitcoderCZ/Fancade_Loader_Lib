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
    public class Level : BlockContainer
    {
        private bool unknownFlag; // aditional byte after the color (so far only seen 0, in ECVV_SET_ANG_LIMITS_E)

        public string Name;
        public byte BackgroundColor;
        public bool LevelUnEditable;

        public Level(string name)
            : base()
        {
            Name = name;
            BackgroundColor = 26;
            LevelUnEditable = false;
        }

        public void Save(SaveWriter writer)
        {
            writer.WriteUInt8((byte)(
                  (LevelUnEditable ? 0b_0100_0000 : 0)
                | (unknownFlag ? 0b_0100_0000 : 0)
                | (BlockIds.Size.X > 0 ? 0b_0100 : 0)
                | (BlockValues.Count > 0 ? 0b_0010 : 0)
                | (Connections.Count > 0 ? 0b_0001 : 0)
                ));
            writer.WriteUInt8((byte)(BackgroundColor == 26 ? 0x18 : 0x19));
            writer.WriteUInt8(0x03);
            writer.WriteString(Name);
            if (BackgroundColor != 26)
                writer.WriteUInt8(BackgroundColor);

            if (unknownFlag)
                writer.WriteUInt8(0);

            save(writer);
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

            var (blockIds, values, connections) = load(reader, hasBlocks: (info[0] & 0b_0000_0100) != 0, hasValue: (info[0] & 0b_0000_0010) != 0, hasConnections: (info[0] & 0b_0000_0001) != 0);

			return new Level(name)
            {
                unknownFlag = (info[0] & 0b_0010_0000) != 0,
                BackgroundColor = backgroundColor,
                LevelUnEditable = (info[0] & 0b_0100_0000) != 0,
                BlockIds = blockIds,
                BlockValues = values,
                Connections = connections
            };
        }

        public override string ToString() => $"[{Name}, Size: {BlockIds.Size}]";
    }
}
