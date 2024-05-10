namespace FancadeLoaderLib
{
    public class Game
    {
        public static readonly ushort CurrentBlockPaletteVersion = 31;

        public static class Opptions
        {
            public static bool AutofixBlockIdOffset = true;
        }

        public string Name;
        public string Author;
        public string Description;

        public ushort PaletteVersion;

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

        public static (ushort paletteVersion, string name, string author, string description) LoadInfo(SaveReader reader)
        {
            byte[] rest = reader.ReadBytes((int)reader.BytesLeft);
            reader.Dispose();
            reader = new SaveReader(new MemoryStream());
            using (MemoryStream restStream = new MemoryStream(rest))
                Zlib.Decompress(restStream, reader.Stream);
            reader.Position = 0;

            ushort paletteVersion = reader.ReadUInt16();

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

            ushort paletteVersion = reader.ReadUInt16();

            if (paletteVersion > CurrentBlockPaletteVersion)
                throw new Exception($"Palette version {paletteVersion} isn't supported, highest supported is {CurrentBlockPaletteVersion}");

            string name = reader.ReadString();
            string author = reader.ReadString();
            string description = reader.ReadString();

            sbyte blockIdOffset = reader.ReadInt8();

            reader.ReadUInt8(); // only seen 2 (when blockIdOffset +) and 1 (when blockIdOffset -)

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

            Game game = new Game(name)
            {
                Author = author,
                Description = description,
                PaletteVersion = paletteVersion,
                Levels = levels,
                CustomBlocks = customBlocks.Finalize(paletteVersion, levels.Count, 0),
            };

            if (Opptions.AutofixBlockIdOffset)
                game.fixBlockIdOffset(blockIdOffset);

            return game;
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

            // segment id, to how the blocks origin moved
            Dictionary<ushort, Vector3I> idToOriginMove = new Dictionary<ushort, Vector3I>();

            ushort id = (ushort)(Block.GetFirstCustomBlockId(PaletteVersion) + Levels.Count);
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
            void fixConnection(BlockContainer container, ref Vector3S pos)
            {
                ushort id = container.BlockIds.GetSegment(pos);

                if (id >= minId)
                    pos += idToOriginMove[id];
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

        public override string ToString() => $"[{Name}, Author: {Author}, Description: {Description}]";
    }
}
