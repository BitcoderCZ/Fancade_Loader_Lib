using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
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

        public BlockData InsideBlockIds;

        public List<BlockValue> BlockValues;
        public List<Connection> Connections;

        public Block(ushort id, string name)
        {
            MainId = id;
            Name = name;

            Attribs = BlockAttribs.Default;

            Blocks = new Dictionary<Vector3I, BlockSection>()
            {
                { Vector3I.Zero, new BlockSection(new SubBlock[8*8*8], BlockAttribs.Default, id) }
            };
            InsideBlockIds = new BlockData();
            BlockValues = new List<BlockValue>();
            Connections = new List<Connection>();
        }

        private Block()
        {
            mainLoaded = false;
        }

        internal void UpdateId(short _value)
        {
            ushort value = (ushort)Math.Abs(_value);
            if (_value >= 0)
            {
                MainId += value;
                foreach (var item in Blocks)
                    item.Value.Id += value;
            }
            else
            {
                MainId -= value;
                foreach (var item in Blocks)
                    item.Value.Id -= value;
            }
        }

        public static ushort GetCustomBlockOffset(ushort paletteVersion)
        {
            switch (paletteVersion)
            {
                case 27:
                    return 556;
                case 28:
                    return 584;
                case 29:
                    return 588;
                case 30:
                    return 596;
                case 31:
                    return 597;
                default:
                    if (Opptions.ExceptionWhenUnknownCustomBlockOffset)
                        throw new Exception($"Unknown block palette version: {paletteVersion} (unknown custom block offset)");
                    else
                        return 0;
            }
        }

        public void Save(SaveWriter writer, Vector3I pos, bool isMain)
        {
            UpdateAttribs();

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

                if (InsideBlockIds.Size.X > 0)
                {
                    writer.WriteUInt16((ushort)InsideBlockIds.Size.X);
                    writer.WriteUInt16((ushort)InsideBlockIds.Size.Y);
                    writer.WriteUInt16((ushort)InsideBlockIds.Size.Z);

                    for (int z = 0; z < InsideBlockIds.Size.Z; z++)
                        for (int y = 0; y < InsideBlockIds.Size.Y; y++)
                            for (int x = 0; x < InsideBlockIds.Size.X; x++)
                                writer.WriteUInt16(InsideBlockIds.GetSegment(x, y, z));
                }
                if (BlockValues.Count > 0)
                {
                    writer.WriteUInt16((ushort)BlockValues.Count);
                    for (int i = 0; i < BlockValues.Count; i++)
                        BlockValues[i].Save(writer);
                }
                if (Connections.Count > 0)
                {
                    writer.WriteUInt16((ushort)Connections.Count);
                    for (int i = 0; i < Connections.Count; i++)
                        Connections[i].Save(writer);
                }
            }
            else
            {
                byte[] blockData = new byte[NumbSubBlocks * 6];
                if (Blocks.Count > 1)
                {
                    BlockSection section = Blocks[pos];
                    section.Attribs.Save(writer, Attribs, Name, false);
                    writer.WriteUInt16(MainId);
                    writer.WriteUInt8((byte)pos.X);
                    writer.WriteUInt8((byte)pos.Y);
                    writer.WriteUInt8((byte)pos.Z);
                    unsafe
                    {
                        for (int i = 0; i < NumbSubBlocks; i++)
                        {
                            SubBlock block = section.Blocks[i];
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

        public static void Load(SaveReader reader, BlockLoadingList customBlocks, int sectionCount)
        {
            reader.NextThing(false, out object _attribs);
            BlockAttribs attribs = (BlockAttribs)_attribs;

            bool isMain = attribs.IsMain;

            ushort id;
            Vector3I sectionPos;
            if (!isMain || (isMain && attribs.IsMultiBlock))
            {
                id = reader.ReadUInt16();
                sectionPos = new Vector3I(reader.ReadUInt8(), reader.ReadUInt8(), reader.ReadUInt8());
            }
            else
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
                    customBlocks.AddKnownIdBlock(id, thisBlock);
                    newBlock = true;
                }
            }

            if ((isMain && thisBlock.mainLoaded) || (thisBlock.mainLoaded && !thisBlock.Attribs.IsMultiBlock)
                || (!newBlock && !attribs.IsMultiBlock))
            {
                // very bad, something went really wrong
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
                thisBlock.Name = attribs.Name;
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
                Vector3I insideSize = Vector3I.Zero;
                ushort[] blockIds = new ushort[0];

                if (attribs.BlocksInside)
                {
                    insideSize = new Vector3I(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
                    blockIds = new ushort[insideSize.X * insideSize.Y * insideSize.Z];
                    int i = 0;
                    for (int x = 0; x < insideSize.X; x++)
                        for (int y = 0; y < insideSize.Y; y++)
                            for (int z = 0; z < insideSize.Z; z++)
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

                thisBlock.InsideBlockIds = new BlockData(new Array3D<ushort>(blockIds, insideSize.X, insideSize.Y, insideSize.Z));
                thisBlock.BlockValues = values.ToList();
                thisBlock.Connections = connections.ToList();
            }
        }

        public void UpdateAttribs()
        {
            Attribs.IsMultiBlock = Blocks.Count > 1;
            Attribs.BlocksInside = InsideBlockIds.Size.X > 0;
            Attribs.ValuesInside = BlockValues.Count > 0;
            Attribs.ConnectionsInside = Connections.Count > 0;
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
            => a?.Equals(b) ?? (a is null == b is null);
        public static bool operator !=(Block a, Block b)
            => !a?.Equals(b) ?? (a is null != b is null);

        public override bool Equals(object obj)
        {
            if (obj is Block other)
                return Equals(other);
            else
                return false;
        }
        public bool Equals(Block other)
        {
            if (other is null) return false;
            else return MainId == other.MainId && Name == other.Name;
        }

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

        public override string ToString()
            => $"{{Id: {Id}}}";
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

        public override string ToString()
            => $"{{BlockId: {Block.MainId}, Pos: {Pos}, IsMain: {IsMain}}}";

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
