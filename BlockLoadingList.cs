using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public class BlockLoadingList
    {
        Dictionary<ushort, Block> knownIdBlocks = new Dictionary<ushort, Block>();

        List<Block> unknownIdBlocks = new List<Block>();

        List<(ushort, BlockSegment)> all = new List<(ushort, BlockSegment)>();

        public Block this[ushort index]
        {
            get => knownIdBlocks[index];
            set => knownIdBlocks[index] = value;
        }

        public void AddKnownIdBlock(ushort id, Block block)
            => knownIdBlocks.Add(id, block);
        public void AddUnknownIdBlock(Block block)
            => unknownIdBlocks.Add(block);
        public bool TryGetKnownIdBlock(ushort id, out Block block)
            => knownIdBlocks.TryGetValue(id, out block);
        public bool ContainsKnownIdBlock(ushort id)
            => knownIdBlocks.ContainsKey(id);

        public void AddAll(ushort id, Block block, Vector3I pos, bool isMain)
            => all.Add((id, new BlockSegment(pos, isMain, block)));

        /// <summary>
        /// Tries to assign all segment their id
        /// </summary>
        /// <param name="startId">Used when <paramref name="paletteVersion"/> is 0</param>
        /// <returns></returns>
        public BlockList Finalize(ushort paletteVersion, Level[] levels, ushort startId = 0)
        {
            ushort id = paletteVersion == 0 ? startId : (ushort)(Block.GetCustomBlockOffset(paletteVersion) + levels.Length);

            Dictionary<ushort, BlockSegment> segments = new Dictionary<ushort, BlockSegment>();

            for (int i = 0; i < all.Count; i++)
            {
                (ushort, BlockSegment) item = all[i];
                item.Item2.Block.Blocks[item.Item2.Pos].Id = id;
                if (item.Item2.IsMain && item.Item2.Block.MainId == 0)
                    item.Item2.Block.MainId = id;
                segments.Add(id, item.Item2);
                id++;
            }

            return new BlockList(segments);
        }
    }
}
