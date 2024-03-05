using FancadeLoaderLib.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public class Game
    {
        public const ushort CurrentVersion = 31;

        public string Name;
        public string Author;
        public string Description;

        public ushort SaveVersion;
        public byte Unknown;

        public Level[] Levels;
        public BlockList CustomBlocks = new BlockList();

        public void Save(Stream stream)
        {
            using (MemoryStream writerStream = new MemoryStream())
            using (SaveWriter writer = new SaveWriter(writerStream))
            {
                Save(writer);

                byte[] compressed = GZip.Compress(writerStream);

                byte[] header = new byte[] { 0x78, 0x01 };
                byte[] resBytes = new byte[compressed.Length + 2 - 10 - 8];
                Array.Copy(header, resBytes, header.Length);
                Array.Copy(compressed, 10, resBytes, 2, compressed.Length - 18);
                stream.Write(resBytes, 0, resBytes.Length);
            }
        }
        internal void Save(SaveWriter writer)
        {
            writer.WriteUInt16(SaveVersion);
            writer.WriteString(Name);
            writer.WriteString(Author);
            writer.WriteString(Description);
            writer.WriteUInt8(Unknown);
            writer.WriteUInt8(0x02);
            writer.WriteUInt8(GetLevelsPlusCustomBlocks());
            writer.WriteUInt8(0);

            for (int i = 0; i < Levels.Length; i++)
                Levels[i].Save(writer);

            KeyValuePair<ushort, BlockSegment>[] customSegments = CustomBlocks.GetSegments();
            Array.Sort(customSegments, (a, b) =>  a.Key.CompareTo(b.Key));
            for (int i = 0; i < customSegments.Length; i++)
                customSegments[i].Value.Block.Save(writer, customSegments[i].Value.Pos, 
                    customSegments[i].Value.IsMain);
        }

        public static Game Load(SaveReader reader)
        {
            reader.ReadBytes(2);
            byte encoding = reader.ReadUInt8();
            switch (encoding) {
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
                        GZip.DecompressMain(restStream, reader.Stream);
                    reader.Position = 0;
                    break;
            }

            ushort saveVersion = reader.ReadUInt16(); // just a guess

            string name = reader.ReadString();
            string author = reader.ReadString();
            string description = reader.ReadString();

            byte unknown = reader.ReadUInt8();

            reader.ReadBytes(1);

            // probably ushort, but why, the value shouldn't normally be above 255
            byte levelsPlusCustomBlocks = reader.ReadUInt8();
            reader.ReadBytes(1);

            List<Level> levels = new List<Level>();
            BlockLoadingList customBlocks = new BlockLoadingList();
            int segmentCount = 0;
            for (int i = 0; i < 255; i++) {
                int next = reader.NextThing(true, out _);
                switch (next) {
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

            Level[] levelsArray = levels.ToArray();
            return new Game()
            {
                Name = name,
                Author = author,
                Description = description,
                SaveVersion = saveVersion,
                Unknown = unknown,
                Levels = levelsArray,
                CustomBlocks = customBlocks.Finalize(saveVersion, levelsArray, 0),
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

            ushort id = minId;
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

            for (int i = 0; i < Levels.Length; i++)
            {
                Level level = Levels[i];

                for (int j = 0; j < level.BlockIds.Length; j++)
                    if (level.BlockIds[j] >= minId) // is custom block?
                        level.BlockIds[j] = oldToNewId[level.BlockIds[j]];
            }

            CustomBlocks = newCustomBlocks;
        }

        public byte GetLevelsPlusCustomBlocks()
        {
            int numb = Levels.Length;
            Block[] blocks = CustomBlocks.GetBlocks();

            for (int i = 0; i < blocks.Length; i++)
                numb += blocks[i].Blocks.Count;

            if (numb > byte.MaxValue)
                throw new Exception($"Levels ({Levels.Length}) + Custom blocks ({numb - Levels.Length}) > {byte.MaxValue}");

            return (byte)numb;
        }

        public override string ToString() => $"[{Name}, Author: {Author}, Description: {Description}]";
    }
}
