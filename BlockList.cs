using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fancade.LevelEditor
{
    public class BlockList
    {
        private Dictionary<ushort, Block> blocks = new Dictionary<ushort, Block>();
        private Dictionary<ushort, BlockSegment> segments = new Dictionary<ushort, BlockSegment>();

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
                            segments.Add(id, new BlockSegment(new Vector3I(x, y, z), id == item.Key, item.Value));
                            id++;
                        }
            }
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
                        if (!block.Blocks.ContainsKey(pos))
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
        public bool TryGetBlock(ushort id, out Block block)
            => blocks.TryGetValue(id, out block);
        public bool TryGetSegment(ushort id, out BlockSegment segment)
            => segments.TryGetValue(id, out segment);

        public void EnumerateBlocks(Action<KeyValuePair<ushort, Block>> action)
        {
            foreach (KeyValuePair<ushort, Block> item in blocks)
                action(item);
        }
        public void EnumerateSegments(Action<KeyValuePair<ushort, BlockSegment>> action)
        {
            foreach (KeyValuePair<ushort, BlockSegment> item in segments)
                action(item);
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
            ushort lowestID = LowestID();
            ushort idOffset = Block.GetCustomBlockOffset(Game.CurrentVersion);
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
        public ushort LowestID()
        {
            ushort id = ushort.MaxValue;

            foreach (var item in blocks)
                if (item.Key < id)
                    id = item.Key;

            return id;
        }

        /// <returns>Highest Segment ID, if there are none returns 0</returns>
        public ushort HightestID()
        {
            ushort id = 0;

            foreach (var item in blocks)
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
