﻿using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public class Game
    {
        public const ushort OldestBlockPaletteVersion = 27;
        public static readonly ushort CurrentBlockPaletteVersion = 31;

        public static class Opptions
        {
            public static bool FixAndUpdate = true;
        }

        private string name;
        public string Name
        {
            get => name;
            set
            {
                if (value is null) throw new ArgumentNullException(nameof(value));

                name = value;
            }
        }
        private string author;
        public string Author
        {
            get => author;
            set
            {
                if (value is null) throw new ArgumentNullException(nameof(value));

                author = value;
            }
        }
        private string description;
        public string Description
        {
            get => description;
            set
            {
                if (value is null) throw new ArgumentNullException(nameof(value));

                description = value;
            }
        }

        public ushort PaletteVersion { get; private set; }

        public List<Level> Levels { get; private set; }
        public BlockList CustomBlocks { get; private set; }

        public Game(string _name)
        {
            if (_name is null) throw new ArgumentNullException(nameof(_name));

            name = _name;
            author = "Unknown Author";
            description = string.Empty;
            PaletteVersion = CurrentBlockPaletteVersion;
            Levels = new List<Level>();
            CustomBlocks = new BlockList();
        }

        private Game(string _name, string _author, string _description, ushort _paletteVersion, List<Level> _levels, BlockList _customBlocks)
        {
            name = _name;
            author = _author;
            description = _description;
            PaletteVersion = _paletteVersion;
            Levels = _levels;
            CustomBlocks = _customBlocks;
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
            writer.WriteInt8((sbyte)Block.GetBlocksAdded(PaletteVersion));
            writer.WriteUInt8(0x02);

            writer.WriteUInt16(GetLevelsPlusCustomBlocks());

            for (int i = 0; i < Levels.Count; i++)
                Levels[i].Save(writer);

            KeyValuePair<ushort, BlockSegment>[] customSegments = CustomBlocks.GetSegments();
            Array.Sort(customSegments, (a, b) => a.Key.CompareTo(b.Key));
            for (int i = 0; i < customSegments.Length; i++)
                customSegments[i].Value.Block.Save(writer, customSegments[i].Value.Pos, customSegments[i].Value.IsMain);
        }

        public static (ushort paletteVersion, string name, string author, string description) LoadInfo(Stream stream)
        {
            // decompress
            SaveReader reader = new SaveReader(new MemoryStream());
            Zlib.Decompress(stream, reader.Stream);
            reader.Reset();

            ushort paletteVersion = reader.ReadUInt16();

            string name = reader.ReadString();
            string author = reader.ReadString();
            string description = reader.ReadString();

            return (paletteVersion, name, author, description);
        }

        public static Game Load(Stream stream)
        {
            // decompress
            SaveReader reader = new SaveReader(new MemoryStream());
            Zlib.Decompress(stream, reader.Stream);
            reader.Reset();

            ushort paletteVersion = reader.ReadUInt16();

            if (paletteVersion > CurrentBlockPaletteVersion)
                throw new Exception($"Palette version {paletteVersion} isn't supported, highest supported is {CurrentBlockPaletteVersion}");

            string name = reader.ReadString();
            string author = reader.ReadString();
            string description = reader.ReadString();

            sbyte blockIdOffset = reader.ReadInt8();

            reader.ReadUInt8(); // only seen 2 (when blockIdOffset is positive) and 1 (when blockIdOffset is negative)

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
                        Block.Load(reader, customBlocks);
                        segmentCount++;
                        break;
                }
            }
        exit:

            if (levels.Count + segmentCount != levelsPlusCustomBlocks)
                throw new Exception($"Levels ({levels.Count}) + Custom blocks ({segmentCount}) != Saved levels + custom blocks count ({levelsPlusCustomBlocks})");

            Game game = new Game(name, author, description, paletteVersion, levels, customBlocks.Finalize(paletteVersion, levels.Count));

            if (Opptions.FixAndUpdate)
            {
                game.fixBlockIdOffset(blockIdOffset);
                game.FixIds(CurrentBlockPaletteVersion);
            }

            return game;
        }

        /// <summary>
        /// Sets Uneditable to !<paramref name="editable"/> on <see cref="CustomBlocks"/> and <see cref="Levels"/>
        /// </summary>
        /// <param name="editable">If this <see cref="Game"/> should be editable or not</param>
        /// <param name="changeAuthor">If this and <paramref name="editable"/> are both <see href="true"/>, <see cref="Author"/> gets set to "Unknown Author"</param>
        public void SetEditable(bool editable, bool changeAuthor)
        {
            bool b = !editable;
            if (editable && changeAuthor)
                Author = "Unknown Author";

            CustomBlocks.EnumerateBlocks(item =>
            {
                item.Value.Attribs.Uneditable = b;
                foreach (KeyValuePair<Vector3I, BlockSection> item2 in item.Value.Sections)
                    item2.Value.Attribs.Uneditable = b;
            });

            foreach (Level level in Levels)
                level.Uneditable = b;
        }

        /// <summary>
        /// Reorders custom block ids in order to make more sense, updates <see cref="PaletteVersion"/> if <paramref name="newVersion"/> isn't <see cref="ushort.MaxValue"/>
        /// </summary>
        /// <param name="newVersion">New <see cref="PaletteVersion"/> or <see cref="ushort.MaxValue"/> to not change the version</param>
        public void FixIds(ushort newVersion = ushort.MaxValue)
        {
            ushort minId = ushort.MaxValue;

            if (newVersion == ushort.MaxValue)
                newVersion = PaletteVersion;
            else if (newVersion < PaletteVersion || newVersion > CurrentBlockPaletteVersion)
                throw new ArgumentOutOfRangeException(nameof(newVersion), $"{nameof(newVersion)}) must be between current {nameof(PaletteVersion)} ({PaletteVersion}) and newest supported ({CurrentBlockPaletteVersion})");

            Dictionary<(ushort id, Vector3I pos), ushort> sectionToId = new Dictionary<(ushort id, Vector3I pos), ushort>();
            CustomBlocks.EnumerateSegments(item =>
            {
                sectionToId.Add((item.Value.Block.MainId, item.Value.Pos), item.Key);
                if (item.Key < minId)
                    minId = item.Key;
            });

            Dictionary<ushort, ushort> oldToNewId = new Dictionary<ushort, ushort>();
            BlockList newCustomBlocks = new BlockList();

            // segment id, to how the blocks origin moved
            Dictionary<ushort, Vector3I> idToOriginMove = new Dictionary<ushort, Vector3I>();

            ushort id = (ushort)(Block.GetFirstCustomBlockId(newVersion) + Levels.Count);
            CustomBlocks.EnumerateBlocksSorted(item =>
            {
                Block block = item.Value;
                ushort oldMainId = block.MainId;
                block.MainId = id;
                Vector3I size = block.GetSize();

                Dictionary<Vector3I, BlockSection> newSections = new Dictionary<Vector3I, BlockSection>();

                Vector3I originMove = Vector3I.Zero;
                foreach (var section in block.Sections)
                    if (section.Value.Attribs.IsMain)
                    {
                        originMove = section.Key;
                        break;
                    }

                bool isMain = true;
                for (int z = 0; z < size.Z; z++)
                    for (int y = 0; y < size.Y; y++)
                        for (int x = 0; x < size.X; x++)
                        {
                            Vector3I pos = new Vector3I(x, y, z);
                            if (!block.Sections.ContainsKey(pos))
                                continue;

                            BlockSection section = block.Sections[pos];
                            section.Id = id;

                            if (isMain)
                            {
                                section.Attribs = block.Attribs;
                                originMove = pos - originMove;
                            }

                            if (block.Sections.Count > 0)
                                idToOriginMove.Add(id, originMove);

                            newSections.Add(pos, section);

                            // also move attribs?
                            oldToNewId.Add(sectionToId[(oldMainId, pos)], id);
                            newCustomBlocks.AddSegment(id++, new BlockSegment(pos, isMain, block));
                            isMain = false;
                        }

                block.Sections = newSections;
            });

            // fix ids in levels and blocks
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
                for (int i = 0; i < block.BlockIds.Length; i++)
                    if (block.BlockIds[i] >= minId) // is custom block?
                        block.BlockIds[i] = oldToNewId[block.BlockIds[i]];
            });
            CustomBlocks = newCustomBlocks;

            // fix connections
            minId = (ushort)(Block.GetFirstCustomBlockId(PaletteVersion) + Levels.Count);

            for (int i = 0; i < Levels.Count; i++)
            {
                Level level = Levels[i];
                for (int j = 0; j < level.Connections.Count; j++)
                {
                    Connection con = level.Connections[j];
                    // connector position doesn't seem to change (isn't related to origin, can't be negative, TODO: should do more testing)
                    fixConnection(level, ref con.From);
                    fixConnection(level, ref con.To);
                    level.Connections[j] = con;
                }
            }
            CustomBlocks.EnumerateBlocks(item =>
            {
                for (int i = 0; i < item.Value.Connections.Count; i++)
                {
                    Connection con = item.Value.Connections[i];
                    // connector position doesn't seem to change (isn't related to origin, can't be negative, TODO: should do more testing)
                    if (con.From.X != 32769)
                        fixConnection(item.Value, ref con.From);
                    if (con.To.X != 32769)
                        fixConnection(item.Value, ref con.To);
                    item.Value.Connections[i] = con;
                }
            });

            PaletteVersion = newVersion;

            void fixConnection(BlockContainer container, ref Vector3US pos)
            {
                ushort id = container.BlockIds.GetSegment(pos);

                if (id >= minId)
                    pos = (Vector3US)(pos + idToOriginMove[id]);
            }
        }

        private void fixBlockIdOffset(sbyte blockIdOffset)
        {
            ushort offset = (ushort)(-blockIdOffset + Block.GetBlocksAdded(PaletteVersion));

            ushort minCustomId = (ushort)(Block.GetFirstCustomBlockId(PaletteVersion) + Levels.Count);

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

                for (int i = 0; i < block.BlockIds.Length; i++)
                    if (block.BlockIds[i] + offset >= minCustomId)
                        block.BlockIds[i] += offset;
            });
        }

        public ushort GetLevelsPlusCustomBlocks()
        {
            int numb = Levels.Count;
            Block[] blocks = CustomBlocks.GetBlocks();

            for (int i = 0; i < blocks.Length; i++)
                numb += blocks[i].Sections.Count;

            if (numb > ushort.MaxValue)
                throw new Exception($"Levels ({Levels.Count}) + Custom blocks ({numb - Levels.Count}) > {byte.MaxValue}");

            return (ushort)numb;
        }

        public override string ToString() => $"[Name: {Name}, Author: {Author}, Description: {Description}]";
    }
}
