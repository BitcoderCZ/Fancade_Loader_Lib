namespace FancadeLoaderLib
{
    public class BlockList
    {
        private Dictionary<ushort, Block> blocks = new Dictionary<ushort, Block>();
        private Dictionary<ushort, BlockSegment> segments = new Dictionary<ushort, BlockSegment>();

        public int BlockCount => blocks.Count;
        public int SegmentCount => segments.Count;

        public BlockList()
        { }

        public BlockList(Dictionary<ushort, Block> _blocks)
        {
            blocks = _blocks;
            foreach (KeyValuePair<ushort, Block> item in blocks)
            {
                ushort id = item.Key;
                Vector3I size = item.Value.GetSize();
                for (int z = 0; z < size.Z; z++)
                    for (int y = 0; y < size.Y; y++)
                        for (int x = 0; x < size.X; x++)
                        {
                            Vector3I pos = new Vector3I(x, y, z);
                            if (!item.Value.Sections.ContainsKey(pos))
                                continue;

                            segments.Add(id, new BlockSegment(pos, id == item.Key, item.Value));
                            id++;
                        }
            }
        }

        public void Save(SaveWriter writer)
        {
            writer.WriteUInt32((uint)segments.Count);
            writer.WriteUInt16(segments.ToArray().Min(item => item.Key));
            EnumerateSegmentsSorted(item =>
            {
                item.Value.Block.Save(writer, item.Value.Pos, item.Value.IsMain);
            });
        }

        public static BlockList Load(SaveReader reader)
        {
            uint count = reader.ReadUInt32();
            ushort startId = reader.ReadUInt16();

            int segmentCount = 0;
            BlockLoadingList loadingList = new BlockLoadingList();
            for (int i = 0; i < count; i++)
                Block.Load(reader, loadingList, segmentCount);

            return loadingList.Finalize(0, 0, startId);
        }

        public BlockList(Dictionary<ushort, BlockSegment> _segments)
        {
            segments = _segments;
            foreach (KeyValuePair<ushort, BlockSegment> item in segments)
                if (!blocks.ContainsValue(item.Value.Block))
                    blocks.Add(item.Key, item.Value.Block);
        }

        public void AddBlock(Block block)
        {
            if (blocks.ContainsValue(block))
                return;

            blocks.Add(block.MainId, block);

            ushort id = block.MainId;
            Vector3I size = block.GetSize();
            for (int z = 0; z < size.Z; z++)
                for (int y = 0; y < size.Y; y++)
                    for (int x = 0; x < size.X; x++)
                    {
                        Vector3I pos = new Vector3I(x, y, z);
                        if (!block.Sections.ContainsKey(pos))
                            continue;
                        segments.Add(id, new BlockSegment(pos, id == block.MainId, block));
                        id++;
                    }
        }
        public void AddSegment(BlockSegment segment)
        {
            if (!blocks.ContainsKey(segment.Block.MainId))
                blocks.Add(segment.Block.MainId, segment.Block);

            segments.Add((ushort)(segment.Block.MainId + segment.GetIDOffset()), segment);
        }
        public void AddSegment(ushort id, BlockSegment segment)
        {
            if (!blocks.ContainsValue(segment.Block))
                blocks.Add(segment.Block.MainId, segment.Block);

            segments.Add(id, segment);
        }

        public Block GetBlock(ushort id)
            => blocks[id];
        public BlockSegment GetSegment(ushort id)
            => segments[id];
        public void SetBlock(ushort id, Block value)
            => blocks[id] = value;
        public void SetSegment(ushort id, BlockSegment value)
            => segments[id] = value;
        public bool TryGetBlock(ushort id, out Block block)
            => blocks.TryGetValue(id, out block);
        public bool TryGetSegment(ushort id, out BlockSegment segment)
            => segments.TryGetValue(id, out segment);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pos"></param>
        /// <param name="segmentId">Id of the segment that was added</param>
        /// <returns><see langword="true"/>If the segment was added, <see langword="false"/> if the block already has a segment at pos</returns>
        public bool AddSegmentToBlock(ushort id, Vector3I pos, out ushort segmentId)
        {
            Block block = blocks[id];
            if (block.Sections.ContainsKey(pos))
            {
                segmentId = ushort.MaxValue;
                return false;
            }

            // get id the segment will have
            Vector3I size = Vector3I.Max(block.GetSize(), pos + Vector3I.One);
            segmentId = block.MainId;
            for (int z = 0; z < size.Z; z++)
                for (int y = 0; y < size.Y; y++)
                    for (int x = 0; x < size.X; x++)
                        if (x == pos.X && y == pos.Y && z == pos.Z)
                            goto exit;
                        else if (block.Sections.ContainsKey(new Vector3I(x, y, z)))
                            segmentId++;

                        exit:

            // increment ids
            updateAfter((ushort)(segmentId - 1), 1);

            // increment the block's segment ids
            bool hitBlock = false;
            for (int z = 0; z < size.Z; z++)
                for (int y = 0; y < size.Y; y++)
                    for (int x = 0; x < size.X; x++)
                    {
                        Vector3I cPos = new Vector3I(x, y, z);
                        if (cPos == pos)
                            hitBlock = true;
                        else if (hitBlock && block.Sections.TryGetValue(cPos, out BlockSection section))
                            section.Id++;
                    }

            segments.Add(segmentId, new BlockSegment(pos, pos == Vector3I.Zero, block));
            block.Sections.Add(pos, new BlockSection(new SubBlock[8 * 8 * 8], block.Attribs, segmentId));
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pos"></param>
        /// <param name="segmentId">Id of the segment that was removed</param>
        /// <returns><see langword="true"/>If the segment was removed, <see langword="false"/> if the block already has a segment at pos</returns>
        public bool RemoveSegmentFromBlock(ushort id, Vector3I pos, out ushort segmentId)
        {
            Block block = blocks[id];
            if (!block.Sections.TryGetValue(pos, out BlockSection section))
            {
                segmentId = ushort.MaxValue;
                return false;
            }

            // get id the segment will have
            Vector3I size = Vector3I.Max(block.GetSize(), pos + Vector3I.One);
            segmentId = section.Id;

            // remove the segment
            segments.Remove(segmentId);
            block.Sections.Remove(pos);

            // decrement ids
            updateAfter(segmentId, -1);

            // decrement the block's segment ids
            bool hitBlock = false;
            for (int z = 0; z < size.Z; z++)
                for (int y = 0; y < size.Y; y++)
                    for (int x = 0; x < size.X; x++)
                    {
                        Vector3I cPos = new Vector3I(x, y, z);
                        if (cPos == pos)
                            hitBlock = true;
                        else if (hitBlock && block.Sections.TryGetValue(cPos, out BlockSection bs))
                            bs.Id--;
                    }

            return true;
        }

        private void updateAfter(ushort id, short value)
        {
            List<ushort> segmentsToUpdate = new List<ushort>();
            foreach (var item in segments)
                if (item.Key > id)
                    segmentsToUpdate.Add(item.Key);

            segmentsToUpdate.Sort();
            if (value > 0)
                for (int i = segmentsToUpdate.Count - 1; i >= 0; i--)
                {
                    ushort bId = segmentsToUpdate[i];
                    BlockSegment b = segments[bId];
                    segments.Remove(bId);
                    segments.Add((ushort)(bId + value), b);
                }
            else
                for (int i = 0; i < segmentsToUpdate.Count; i++)
                {
                    ushort bId = segmentsToUpdate[i];
                    BlockSegment b = segments[bId];
                    segments.Remove(bId);
                    segments.Add((ushort)(bId + value), b);
                }

            List<ushort> blocksToUpdate = new List<ushort>();
            foreach (var item in blocks)
                if (item.Key > id)
                    blocksToUpdate.Add(item.Key);

            blocksToUpdate.Sort();
            if (value > 0)
                for (int i = blocksToUpdate.Count - 1; i >= 0; i--)
                {
                    ushort bId = blocksToUpdate[i];
                    Block b = blocks[bId];
                    b.UpdateId(value);
                    blocks.Remove(bId);
                    blocks.Add((ushort)(bId + value), b);
                }
            else
                for (int i = 0; i < blocksToUpdate.Count; i++)
                {
                    ushort bId = blocksToUpdate[i];
                    Block b = blocks[bId];
                    b.UpdateId(value);
                    blocks.Remove(bId);
                    blocks.Add((ushort)(bId + value), b);
                }
        }

        public void EnumerateBlocks(Action<KeyValuePair<ushort, Block>> action)
        {
            foreach (KeyValuePair<ushort, Block> item in blocks)
                action(item);
        }
        public void EnumerateBlocksSorted(Action<KeyValuePair<ushort, Block>> action)
        {
            KeyValuePair<ushort, Block>[] array = blocks.ToArray();
            Array.Sort(array, (a, b) => a.Key.CompareTo(b.Key));
            for (int i = 0; i < array.Length; i++)
                action(array[i]);
        }
        public void EnumerateSegments(Action<KeyValuePair<ushort, BlockSegment>> action)
        {
            foreach (KeyValuePair<ushort, BlockSegment> item in segments)
                action(item);
        }
        public void EnumerateSegmentsSorted(Action<KeyValuePair<ushort, BlockSegment>> action)
        {
            KeyValuePair<ushort, BlockSegment>[] array = segments.ToArray();
            Array.Sort(array, (a, b) => a.Key.CompareTo(b.Key));
            for (int i = 0; i < array.Length; i++)
                action(array[i]);
        }

        public Block[] GetBlocks()
        {
            Block[] blocksArray = new Block[blocks.Count];
            int i = 0;
            foreach (KeyValuePair<ushort, Block> item in blocks)
                blocksArray[i++] = item.Value;

            return blocksArray;
        }
        public KeyValuePair<ushort, BlockSegment>[] GetSegments()
            => segments.ToArray();

        public void FixIds(Level[] levels)
        {
            ushort lowestID = LowestSegmentID();
            ushort idOffset = (ushort)(Block.GetFirstCustomBlockId(Game.CurrentBlockPaletteVersion) + levels.Length);
            if (lowestID >= idOffset)
                return;

            ushort add = (ushort)(idOffset - lowestID);
            Parallel.For(0, levels.Length, i =>
            {
                for (int j = 0; j < levels[i].BlockIds.Length; j++)
                    levels[i].BlockIds[j] += add;
            });

            blocks = blocks.ToDictionary(item => (ushort)(item.Key + add), item =>
            {
                item.Value.MainId += add;
                return item.Value;
            });
            segments = segments.ToDictionary(item => (ushort)(item.Key + add), item => item.Value);
        }

        /// <returns>Lowest Segment ID, if there are none returns <see cref="ushort.MaxValue"/></returns>
        public ushort LowestSegmentID()
        {
            ushort id = ushort.MaxValue;

            foreach (KeyValuePair<ushort, BlockSegment> item in segments)
                if (item.Key < id)
                    id = item.Key;

            return id;
        }

        /// <returns>Highest Segment ID, if there are none returns 0</returns>
        public ushort HightestSegmentID()
        {
            ushort id = 0;

            foreach (KeyValuePair<ushort, BlockSegment> item in segments)
                if (item.Key > id)
                    id = item.Key;

            return id;
        }

        public void Clear()
        {
            blocks.Clear();
            segments.Clear();
        }
    }
}
