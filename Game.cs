using FancadeLoaderLib.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public class Game
    {
        public static readonly ushort CurrentBlockPaletteVersion = 31;

        public string Name;
        public string Author;
        public string Description;

        public ushort PaletteVersion;
        /// <summary>
        /// Custom block ids in levels will be incremented by 85 - [value] (really weird, might not be true)
        /// </summary>
        public byte BlockIdOffset;

        public List<Level> Levels;
        public BlockList CustomBlocks = new BlockList();

        public Game(string name)
        {
            Name = name;
            Author = "Unknown Author";
            Description = string.Empty;
            PaletteVersion = CurrentBlockPaletteVersion;
            CustomBlocks = new BlockList();
            Levels = new List<Level>();
            BlockIdOffset = 85;
        }

        public void Save(Stream stream)
        {
            using (MemoryStream writerStream = new MemoryStream())
            using (SaveWriter writer = new SaveWriter(writerStream))
            {
                Save(writer);

                Zlib.Compress(writerStream, stream);
            }
        }
        internal void Save(SaveWriter writer)
        {
            writer.WriteUInt16(PaletteVersion);
            writer.WriteString(Name);
            writer.WriteString(Author);
            writer.WriteString(Description);
            writer.WriteUInt8(BlockIdOffset);
            writer.WriteUInt8(0x02);

            writer.WriteUInt16(GetLevelsPlusCustomBlocks());

            for (int i = 0; i < Levels.Count; i++)
                Levels[i].Save(writer);

            KeyValuePair<ushort, BlockSegment>[] customSegments = CustomBlocks.GetSegments();
            Array.Sort(customSegments, (a, b) => a.Key.CompareTo(b.Key));
            for (int i = 0; i < customSegments.Length; i++)
                customSegments[i].Value.Block.Save(writer, customSegments[i].Value.Pos, customSegments[i].Value.IsMain);
        }

        public static (ushort paletteVersion, string name, string author, string description) LoadInfo(SaveReader reader)
        {
            reader.ReadBytes(2);
            byte encoding = reader.ReadUInt8();
            switch (encoding)
            {
                case 1: // plain text
                    throw new Exception($"Plain text levels aren't supported");
                    //reader.ReadBytes(/*7*/6);
                    break;
                default: // gzip
                    reader.Position = 0;
                    byte[] rest = reader.ReadBytes((int)reader.BytesLeft);
                    reader.Dispose();
                    reader = new SaveReader(new MemoryStream());
                    using (MemoryStream restStream = new MemoryStream(rest))
                        Zlib.Decompress(restStream, reader.Stream);
                    reader.Position = 0;
                    break;
            }

            ushort paletteVersion = reader.ReadUInt16(); // just a guess

            string name = reader.ReadString();
            string author = reader.ReadString();
            string description = reader.ReadString();

            return (paletteVersion, name, author, description);
        }

        public static Game Load(SaveReader reader)
        {
            // decompress
            byte[] rest = reader.ReadBytes((int)reader.BytesLeft);
            reader.Dispose();
            reader = new SaveReader(new MemoryStream());
            using (MemoryStream restStream = new MemoryStream(rest))
                Zlib.Decompress(restStream, reader.Stream);
            reader.Position = 0;

            ushort paletteVersion = reader.ReadUInt16(); // just a guess

            string name = reader.ReadString();
            string author = reader.ReadString();
            string description = reader.ReadString();

            byte blockIdOffset = reader.ReadUInt8();

            reader.ReadBytes(1);

            ushort levelsPlusCustomBlocks = reader.ReadUInt16();

            List<Level> levels = new List<Level>();
            BlockLoadingList customBlocks = new BlockLoadingList();
            int segmentCount = 0;
            for (int i = 0; i < levelsPlusCustomBlocks; i++)
            {
                int next = reader.NextThing(true, out _);
                switch (next)
                {
                    case 0:
                    case int.MaxValue:
                        goto exit;
                    case 1:
                        levels.Add(Level.Load(reader));
                        break;
                    case 2:
                        Block.Load(reader, customBlocks, segmentCount++);
                        break;
                }
            }
        exit:

            if (levels.Count + segmentCount != levelsPlusCustomBlocks)
                throw new Exception($"Levels ({levels.Count}) + Custom blocks ({segmentCount}) != Saved levels + custom blocks count ({levelsPlusCustomBlocks})");
            else if (reader.BytesLeft > 0)
                throw new Exception($"{reader.BytesLeft} bytes were left, this probably means the level was loaded incorrectly");

            return new Game(name)
            {
                Author = author,
                Description = description,
                PaletteVersion = paletteVersion,
                BlockIdOffset = blockIdOffset,
                Levels = levels,
                CustomBlocks = customBlocks.Finalize(paletteVersion, levels.ToArray(), 0),
            };
        }

        /// <summary>
        /// Reorders custom block ids in order to make more sense
        /// </summary>
        public void FixIds()
        {
            ushort minId = ushort.MaxValue;

            Dictionary<(ushort id, Vector3I pos), ushort> sectionToId = new Dictionary<(ushort id, Vector3I pos), ushort>();
            CustomBlocks.EnumerateSegments(item =>
            {
                sectionToId.Add((item.Value.Block.MainId, item.Value.Pos), item.Key);
                if (item.Key < minId)
                    minId = item.Key;
            });

            Dictionary<ushort, ushort> oldToNewId = new Dictionary<ushort, ushort>();
            BlockList newCustomBlocks = new BlockList();

            ushort id = (ushort)(Block.GetCustomBlockOffset(PaletteVersion) + Levels.Count);//minId;
            CustomBlocks.EnumerateBlocksSorted(item =>
            {
                Block block = item.Value;
                ushort oldMainId = block.MainId;
                block.MainId = id;
                Vector3I size = block.GetSize();

                bool isMain = true;
                for (int z = 0; z < size.Z; z++)
                    for (int y = 0; y < size.Y; y++)
                        for (int x = 0; x < size.X; x++)
                        {
                            Vector3I pos = new Vector3I(x, y, z);
                            if (!block.Blocks.ContainsKey(pos))
                                continue;

                            oldToNewId.Add(sectionToId[(oldMainId, pos)], id);
                            newCustomBlocks.AddSegment(id++, new BlockSegment(pos, isMain, block));
                            isMain = false;
                        }
            });

            for (int i = 0; i < Levels.Count; i++)
            {
                Level level = Levels[i];

                for (int j = 0; j < level.BlockIds.Length; j++)
                    if (level.BlockIds[j] >= minId) // is custom block?
                        level.BlockIds[j] = oldToNewId[level.BlockIds[j]];
            }

            CustomBlocks.EnumerateBlocks(item =>
            {
                Block block = item.Value;
                for (int i = 0; i < block.InsideBlockIds.Length; i++)
                    if (block.InsideBlockIds[i] >= minId) // is custom block?
                        block.InsideBlockIds[i] = oldToNewId[block.InsideBlockIds[i]];
            });

            CustomBlocks = newCustomBlocks;
        }

        public void FixBlockIdOffset()
        {
            if (BlockIdOffset > 85)
                throw new Exception($"Invalid {nameof(BlockIdOffset)}");
            else if (BlockIdOffset == 85)
                return; // BlockIdOffset is already correct

            ushort offset = (ushort)(85 - BlockIdOffset);

            ushort minCustomId = Block.GetCustomBlockOffset(PaletteVersion);

            Parallel.For(0, Levels.Count, i =>
            {
                Level level = Levels[i];
                for (int j = 0; j < level.BlockIds.Length; j++)
                    if (level.BlockIds[j] + offset >= minCustomId)
                        level.BlockIds[j] += offset;
            });

            CustomBlocks.EnumerateBlocks(item =>
            {
                Block block = item.Value;

                for (int i = 0; i < block.InsideBlockIds.Length; i++)
                    if (block.InsideBlockIds[i] + offset >= minCustomId)
                        block.InsideBlockIds[i] += offset;
            });

            BlockIdOffset = 85;
        }

        public ushort GetLevelsPlusCustomBlocks()
        {
            int numb = Levels.Count;
            Block[] blocks = CustomBlocks.GetBlocks();

            for (int i = 0; i < blocks.Length; i++)
                numb += blocks[i].Blocks.Count;

            if (numb > ushort.MaxValue)
                throw new Exception($"Levels ({Levels.Count}) + Custom blocks ({numb - Levels.Count}) > {byte.MaxValue}");

            return (ushort)numb;
        }

        public override string ToString() => $"[{Name}, Author: {Author}, Description: {Description}]";
    }
}
