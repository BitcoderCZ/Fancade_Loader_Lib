using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fancade.LevelEditor
{
    public class Block
    {
        const int NumbSubBlocks = 8 * 8 * 8;

        public static class Opptions
        {
            public static bool ExceptionWhenUnknownCustomBlockOffset = true;
        }

        private bool mainLoaded;

        public ushort MainId;
        public BlockAttribs Attribs;
        public string Name;
        public Dictionary<Vector3I, BlockSection> Blocks = new Dictionary<Vector3I, BlockSection>();

        public Vector3I InsideSize;
        public ushort[] InsideBlockIds;

        public BlockValue[] BlockValues;
        public Connection[] Connections;

        public Block()
        {
            mainLoaded = false;
        }

        public static ushort GetCustomBlockOffset(ushort saveVersion)
        {
            switch (saveVersion)
            {
                case 27:
                    return 557;
                case 31:
                    return 598;
                default:
                    if (Opptions.ExceptionWhenUnknownCustomBlockOffset)
                        throw new Exception($"Unsuported save version: {saveVersion} (unknown custom block offset)");
                    else
                        return 0;
            }
        }

        public void Save(SaveWriter writer, Vector3I pos, bool isMain)
        {
            // TODO: Name should probably be a part of attribs
            if (isMain)
            {
                Attribs.Save(writer, Attribs, Name, true);

                BlockSection mainSection = Blocks[pos];

                if (Attribs.IsMultiBlock)
                {
                    writer.WriteUInt16(MainId);
                    writer.WriteUInt8((byte)pos.X);
                    writer.WriteUInt8((byte)pos.Y);
                    writer.WriteUInt8((byte)pos.Z);
                }

                byte[] blockData = new byte[NumbSubBlocks * 6];
                unsafe
                {
                    for (int i = 0; i < NumbSubBlocks; i++)
                    {
                        SubBlock block = mainSection.Blocks[i];
                        blockData[i + NumbSubBlocks * 0] = (byte)(block.Colors[0] | block.Attribs[0] << 6);
                        blockData[i + NumbSubBlocks * 1] = (byte)(block.Colors[1] | block.Attribs[1] << 6);
                        blockData[i + NumbSubBlocks * 2] = (byte)(block.Colors[2] | block.Attribs[2] << 6);
                        blockData[i + NumbSubBlocks * 3] = (byte)(block.Colors[3] | block.Attribs[3] << 6);
                        blockData[i + NumbSubBlocks * 4] = (byte)(block.Colors[4] | block.Attribs[4] << 6);
                        blockData[i + NumbSubBlocks * 5] = (byte)(block.Colors[5] | block.Attribs[5] << 6);
                    }
                }
                writer.WriteBytes(blockData);

                if (InsideBlockIds.Length > 0)
                {
                    writer.WriteUInt16((ushort)InsideSize.X);
                    writer.WriteUInt16((ushort)InsideSize.Y);
                    writer.WriteUInt16((ushort)InsideSize.Z);

                    for (int i = 0; i < InsideBlockIds.Length; i++)
                        writer.WriteUInt16(InsideBlockIds[i]);
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
            else
            {
                byte[] blockData = new byte[NumbSubBlocks * 6];
                if (Blocks.Count > 1)
                {
                    BlockSection item = Blocks[pos];
                    item.Attribs.Save(writer, Attribs, Name, false);
                    writer.WriteUInt16(MainId);
                    writer.WriteUInt8((byte)pos.X);
                    writer.WriteUInt8((byte)pos.Y);
                    writer.WriteUInt8((byte)pos.Z);
                    unsafe
                    {
                        for (int i = 0; i < NumbSubBlocks; i++)
                        {
                            SubBlock block = item.Blocks[i];
                            blockData[i + NumbSubBlocks * 0] = (byte)(block.Colors[0] | block.Attribs[0] << 6);
                            blockData[i + NumbSubBlocks * 1] = (byte)(block.Colors[1] | block.Attribs[1] << 6);
                            blockData[i + NumbSubBlocks * 2] = (byte)(block.Colors[2] | block.Attribs[2] << 6);
                            blockData[i + NumbSubBlocks * 3] = (byte)(block.Colors[3] | block.Attribs[3] << 6);
                            blockData[i + NumbSubBlocks * 4] = (byte)(block.Colors[4] | block.Attribs[4] << 6);
                            blockData[i + NumbSubBlocks * 5] = (byte)(block.Colors[5] | block.Attribs[5] << 6);
                        }
                    }
                    writer.WriteBytes(blockData);
                }
            }
        }

        public static void Load(SaveReader reader, BlockLoadingList customBlocks, int sectionCount) {
            // figure out if only segment or attribs and do stuff
            // remove static method k?
            reader.NextThing(false, out object _attribs);
            BlockAttribs attribs = (BlockAttribs)_attribs;

            long afterAttribsIndex = reader.Position;

            // kinda cursed but fuck you, sometimes might not work ig... fuck it
            bool isMain = ((Func<bool>)(() =>
            {
                byte stringLength = reader.ReadUInt8();
                if (stringLength >= reader.BytesLeft || stringLength < 1)
                    return false;

                for (int i = 0; i < stringLength; i++)
                    if (char.IsControl((char)reader.ReadUInt8()))
                        return false;

                return true;
            }))();

            reader.Position = afterAttribsIndex;

            string name = null;
            if (isMain)
                name = reader.ReadString();

            if (attribs.Type != BlockAttribs.Type_T.Section)
            {
                if (attribs.Collider.AddtionalUsed)
                    attribs.Collider.AdditionalValue = reader.ReadUInt8();
                else if (attribs.Type == BlockAttribs.Type_T.Script)
                    reader.ReadBytes(1);
            }

            ushort id;
            Vector3I sectionPos;
            if (!isMain || (isMain && attribs.IsMultiBlock))
            {
                id = reader.ReadUInt16();
                sectionPos = new Vector3I(reader.ReadUInt8(), reader.ReadUInt8(), reader.ReadUInt8());
            } else
            {
                id = ushort.MaxValue;
                sectionPos = Vector3I.Zero;
            }

            Block thisBlock;
            bool newBlock = false;
            if (id == ushort.MaxValue)
            {
                thisBlock = new Block();
                customBlocks.AddUnknownIdBlock(thisBlock);
                newBlock = true;
            }
            else
            {
                if (customBlocks.ContainsKnownIdBlock(id))
                    thisBlock = customBlocks[id];
                else
                {
                    thisBlock = new Block();
                    /*{
                        MainId = id,
                    };*/
                    customBlocks.AddKnownIdBlock(id, thisBlock);
                    newBlock = true;
                }
            }

            if ((isMain && thisBlock.mainLoaded) || (thisBlock.mainLoaded && !thisBlock.Attribs.IsMultiBlock)
                || (!newBlock && !attribs.IsMultiBlock))
            {
                // thisBlock? more like """"thisBlock""" hahahhahahahahhahahha
                if (!thisBlock.Attribs.IsMultiBlock)
                    throw new Exception($"Loaded main, but main was aleardy loaded for: {thisBlock}, case: 0");
                else if (!attribs.IsMultiBlock)
                    throw new Exception($"Loaded main, but main was aleardy loaded for: {thisBlock}, case: 1");
                else // we're fucked, the id was actually specified
                    throw new Exception($"Loaded main, but main was aleardy loaded for: {thisBlock}, case: 2");
            }
            else if (isMain)
            {
                thisBlock.Name = name;
                thisBlock.Attribs = attribs;
                thisBlock.mainLoaded = true;
            }

            customBlocks.AddAll(id, thisBlock, sectionPos, isMain);

            byte[] sides = reader.ReadBytes(NumbSubBlocks * 6);
            unsafe
            {
                SubBlock[] subBlocks = new SubBlock[NumbSubBlocks];

                for (int i = 0; i < subBlocks.Length; i++)
                {
                    SubBlock block = new SubBlock();
                    byte s0 = sides[i + NumbSubBlocks * 0];
                    byte s1 = sides[i + NumbSubBlocks * 1];
                    byte s2 = sides[i + NumbSubBlocks * 2];
                    byte s3 = sides[i + NumbSubBlocks * 3];
                    byte s4 = sides[i + NumbSubBlocks * 4];
                    byte s5 = sides[i + NumbSubBlocks * 5];

                    block.Colors[0] = (byte)(s0 & 0b_0011_1111);
                    block.Colors[1] = (byte)(s1 & 0b_0011_1111);
                    block.Colors[2] = (byte)(s2 & 0b_0011_1111);
                    block.Colors[3] = (byte)(s3 & 0b_0011_1111);
                    block.Colors[4] = (byte)(s4 & 0b_0011_1111);
                    block.Colors[5] = (byte)(s5 & 0b_0011_1111);
                    block.Attribs[0] = (byte)((s0 & 0b_1100_0000) >> 6);
                    block.Attribs[1] = (byte)((s1 & 0b_1100_0000) >> 6);
                    block.Attribs[2] = (byte)((s2 & 0b_1100_0000) >> 6);
                    block.Attribs[3] = (byte)((s3 & 0b_1100_0000) >> 6);
                    block.Attribs[4] = (byte)((s4 & 0b_1100_0000) >> 6);
                    block.Attribs[5] = (byte)((s5 & 0b_1100_0000) >> 6);

                    subBlocks[i] = block;
                }
                thisBlock.Blocks.Add(sectionPos, new BlockSection(subBlocks, attribs, 0));
            }

            if (isMain)
            {
                Vector3I size = Vector3I.Zero;
                ushort[] blockIds = new ushort[0];

                if (attribs.BlocksInside)
                {
                    size = new Vector3I(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
                    blockIds = new ushort[size.X * size.Y * size.Z];
                    int i = 0;
                    for (int x = 0; x < size.X; x++)
                        for (int y = 0; y < size.Y; y++)
                            for (int z = 0; z < size.Z; z++)
                            {
                                blockIds[i++] = reader.ReadUInt16();
                            }
                }

                BlockValue[] values;
                if (attribs.ValuesInside)
                {
                    values = new BlockValue[reader.ReadUInt16()];
                    for (int i = 0; i < values.Length; i++)
                        values[i] = BlockValue.Load(reader);
                }
                else
                    values = new BlockValue[0];

                Connection[] connections;
                if (attribs.ConnectionsInside)
                {
                    connections = new Connection[reader.ReadUInt16()];
                    for (int i = 0; i < connections.Length; i++)
                        connections[i] = Connection.Load(reader);
                }
                else
                    connections = new Connection[0];

                thisBlock.InsideSize = size;
                thisBlock.InsideBlockIds = blockIds;
                thisBlock.BlockValues = values;
                thisBlock.Connections = connections;
            }
        }

        [Obsolete("This methoed is obsolete and will be removed, please use GetSize instead.", true)]
        public Vector3I CalculateSize()
        {
            int highX = 0;
            int highY = 0;
            int highZ = 0;

            foreach (KeyValuePair<Vector3I, BlockSection> section in Blocks)
            {
                Vector3I pos = section.Key;
                if (pos.X > highX)
                    highX = pos.X;
                if (pos.Y > highY)
                    highY = pos.Y;
                if (pos.Z > highZ)
                    highZ = pos.Z;
            }

            return new Vector3I(highX, highY, highZ);
        }
        public Vector3I GetSize()
        {
            int highX = 0;
            int highY = 0;
            int highZ = 0;

            foreach (KeyValuePair<Vector3I, BlockSection> section in Blocks)
            {
                Vector3I pos = section.Key;
                if (pos.X > highX)
                    highX = pos.X;
                if (pos.Y > highY)
                    highY = pos.Y;
                if (pos.Z > highZ)
                    highZ = pos.Z;
            }

            return new Vector3I(highX + 1, highY + 1, highZ + 1);
        }

        public static bool operator ==(Block a, Block b)
            => a is null ? b is null : a.Equals(b);
        public static bool operator !=(Block a, Block b)
            => a is null ? !(b is null) : !a.Equals(b);

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            else if (obj is Block other)
                return Equals(other);
            else
                return false;
        }
        public bool Equals(Block other)
            => MainId == other.MainId && Name == other.Name;

        public override int GetHashCode() => MainId ^ Name.GetHashCode();

        public override string ToString() => $"[{Name}, Attribs: {Attribs}]";
    }

    public class BlockSection
    {
        public ushort Id;
        public SubBlock[] Blocks;
        public BlockAttribs Attribs;

        public BlockSection(SubBlock[] _blocks, BlockAttribs _attribs, ushort _id)
        {
            Blocks = _blocks;
            Attribs = _attribs;
            Id = _id;
        }

        public int Index(Vector3I pos)
            => Index(pos.X, pos.Y, pos.Z);
        public int Index(int x, int y, int z)
            => x + y * 8 + z * 64;
    }
    public struct BlockSegment
    {
        public Vector3I Pos;
        public bool IsMain;
        public Block Block;

        public BlockSegment(Vector3I _pos, bool _isMain, Block _block)
        {
            Pos = _pos;
            IsMain = _isMain;
            Block = _block;
        }

        public ushort GetIDOffset()
        {
            Vector3I size = Block.GetSize();
            ushort id = 0;
            for (int z = 0; z < size.Z; z++)
                for (int y = 0; y < size.Y; y++)
                    for (int x = 0; x < size.X; x++)
                    {
                        Vector3I pos = new Vector3I(x, y, z);
                        if (Block.Blocks.ContainsKey(pos))
                        {
                            if (pos == Pos)
                                return id;
                            id++;
                        }
                    }

            return 0;
        }

        public static bool operator ==(BlockSegment a, BlockSegment b)
            => a.Equals(b);
        public static bool operator !=(BlockSegment a, BlockSegment b)
            => !a.Equals(b);

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else if (obj is BlockSegment other)
                return Equals(other);
            else
                return false;
        }
        public bool Equals(BlockSegment other)
            => Pos == other.Pos && IsMain == other.IsMain && Block == other.Block;

        public override int GetHashCode() => Block.GetHashCode() ^ Pos.GetHashCode();
    }

    public struct BlockAttribs
    {
        public bool Unknown;
        public bool Uneditable;
        public bool IsMultiBlock;
        public bool BlocksInside;
        public bool ValuesInside;
        public bool ConnectionsInside;
        public Collider_T Collider;
        public Type_T Type;

        public void Save(SaveWriter writer, BlockAttribs mainAttribs, string name, bool mainSave)
        {
            if (mainSave)
                writer.WriteUInt8((byte)(
                      (ConnectionsInside ? 0b_0000_0001 : 0)
                    | (ValuesInside ? 0b_0000_0010 : 0)
                    | (BlocksInside ? 0b_0000_0100 : 0)
                    | (IsMultiBlock ? 0b_0001_0000 : 0)
                    | (Uneditable ? 0b_0100_0000 : 0)
                    | (Unknown ? 0b_1000_0000 : 0)
                    | Collider.Value
                    ));
            else
                writer.WriteUInt8((byte)(
                      0b_0001_0000 /*IsMultiBlock should always be true*/
                    | (Uneditable ? 0b_0100_0000 : 0)
                    | (Unknown ? 0b_1000_0000 : 0)
                    | Collider.Value
                    ));

            switch (Type)
            {
                case Type_T.Normal:
                    writer.WriteUInt8(0x08);
                    break;
                case Type_T.Physics:
                    writer.WriteUInt8(0x18);
                    writer.WriteUInt8(0x01);
                    break;
                case Type_T.Script:
                    writer.WriteUInt8(0x18);
                    writer.WriteUInt8(0x02);
                    break;
                case Type_T.Section:
                    if (mainAttribs.Type == Type_T.Script)
                    {
                        writer.WriteUInt8(0x10);
                        writer.WriteUInt8(0x02);
                    }
                    else
                        writer.WriteUInt8(0x00);
                    break;
            }

            if (mainSave)
                writer.WriteString(name);

            if (mainAttribs.Collider.AddtionalUsed)
                writer.WriteUInt8(mainAttribs.Collider.AdditionalValue);
        }

        public static bool TryLoad(SaveReader reader, bool seek, out BlockAttribs blockAttribs)
        {
            blockAttribs = default;

            if (reader.BytesLeft < 2)
                return false;

            byte b1 = reader.ReadUInt8();

            bool unknown = (b1 & 0b_1000_0000) != 0;
            bool uneditable = (b1 & 0b_0100_0000) != 0;
            bool isMultiBlock = (b1 & 0b_0001_0000) != 0;
            bool blocksInside = (b1 & 0b_0000_0100) != 0;
            bool valuesInside = (b1 & 0b_0000_0010) != 0;
            bool connectionsInside = (b1 & 0b_0000_0001) != 0;
            b1 &= 0b_0010_1000;

            Collider_T collider;
            if (b1 == 0x28)
                collider = new Collider_T(b1);
            else if (b1 == 0x08)
                collider = new Collider_T(b1);
            else if (b1 == 0x48)
                collider = new Collider_T(b1);
            else if (b1 == 0x68)
                collider = new Collider_T(b1);
            else
            {
                reader.Position--;
                return false;
            }

            byte b2 = reader.ReadUInt8();
            Type_T type;
            if (b2 == 0x08)
                type = Type_T.Normal;
            else if (b2 == 0x18 && reader.BytesLeft > 0)
            {
                byte b3 = reader.ReadUInt8();
                if (b3 == 0x01)
                    type = Type_T.Physics;
                else if (b3 == 0x02)
                    type = Type_T.Script;
                else
                {
                    reader.Position -= 3;
                    return false;
                }

                if (seek)
                    reader.Position--;
            }
            else if (b2 == 0x00)
            {
                type = Type_T.Section;
                if (collider.AddtionalUsed && !seek)
                    reader.ReadUInt8();
            }
            else if (b2 == 0x10)
            {
                type = Type_T.Section;
                if (!seek)
                    reader.ReadUInt8();
                if (collider.AddtionalUsed && !seek)
                    reader.ReadUInt8();
            }
            else
            {
                reader.Position -= 2;
                return false;
            }

            if (seek)
                reader.Position -= 2;

            blockAttribs = new BlockAttribs()
            {
                Unknown = unknown,
                Uneditable = uneditable,
                IsMultiBlock = isMultiBlock,
                ValuesInside = valuesInside,
                ConnectionsInside = connectionsInside,
                BlocksInside = blocksInside,
                Collider = collider,
                Type = type
            };
            return true;
        }

        public override string ToString() => $"[Collider: {Collider}, Type: {Type}]";

        public struct Collider_T
        {
            public byte Value;
            public byte AdditionalValue { get => additionalValue;
                set
                {
                    AdditionalSet = true;
                    additionalValue = value;
                }
            }
            private byte additionalValue;
            public bool AdditionalSet { get; private set; }

            public bool AddtionalUsed
            {
                get => (Value & 0b_0010_0000) != 0;
            }

            public Collider_T(byte _value)
            {
                Value = _value;
                additionalValue = 0;
                AdditionalSet = false;
            }

            public static Collider_T FromEnum(ColliderEnum value)
            {
                switch (value)
                {
                    case ColliderEnum.Box:
                        return new Collider_T(0x08);
                    case ColliderEnum.None:
                        return new Collider_T(0x28)
                        {
                            AdditionalValue = 0x00
                        };
                    case ColliderEnum.Sphere:
                        return new Collider_T(0x28)
                        {
                            AdditionalValue = 0x02
                        };
                    default:
                        throw new ArgumentException($"Cannot convert {value} to Collider_T", "value");
                }
            }

            public ColliderEnum ToEnum()
            {
                if (AddtionalUsed && !AdditionalSet)
                    return ColliderEnum.AddtionalRequired;

                if (Value == 0x08)
                    return ColliderEnum.Box;
                else if (Value == 0x28)
                {
                    if (additionalValue == 0x00)
                        return ColliderEnum.None;
                    else if (additionalValue == 0x02)
                        return ColliderEnum.Sphere;
                    else
                        return ColliderEnum.Unknown;
                } else
                    return ColliderEnum.Unknown;
            }
        }
        public enum ColliderEnum
        {
            None,
            AddtionalRequired,
            Box,
            Sphere,
            Unknown
        }
        public enum Type_T
        {
            Normal,
            Physics,
            Script,
            Section // 0x00
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SubBlock
    {
        //  X
        // -X
        //  Y
        // -Y
        //  Z
        // -Z
        public fixed byte Colors[6]; // colors
        public fixed byte Attribs[6]; // "legos"

        public bool IsEmpty => Colors[0] == 0;

        public override string ToString() => 
            $"[{Colors[0]}, {Colors[1]}, {Colors[2]}, {Colors[3]}, {Colors[4]}, {Colors[5]}; Attribs:" +
            $"{Attribs[0]}, {Attribs[1]}, {Attribs[2]}, {Attribs[3]}, {Attribs[4]}, {Attribs[5]}]";
    }
}
