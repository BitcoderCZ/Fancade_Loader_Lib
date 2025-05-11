using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Bullet.Utils;

internal sealed class PrefabUsedCache
{
    private readonly bool[] _used;

    private PrefabUsedCache(bool[] used)
    {
        _used = used;
    }

    public static PrefabUsedCache Create(PrefabList prefabs, ushort mainId)
    {
        bool[] used = new bool[RawGame.CurrentNumbStockPrefabs + prefabs.SegmentCount];

        var stockPrefabs = StockBlocks.PrefabList;

        MarkUsed(mainId);

        return new PrefabUsedCache(used);

        void MarkUsed(ushort id)
        {
            Debug.Assert(!used[id]);

            used[id] = true;

            var segment = id < RawGame.CurrentNumbStockPrefabs ? stockPrefabs.GetSegment(id) : prefabs.GetSegment(id);

            if (segment.PrefabId != id)
            {
                return;
            }

            var prefab = segment.PrefabId < RawGame.CurrentNumbStockPrefabs ? stockPrefabs.GetPrefab(segment.PrefabId) : prefabs.GetPrefab(segment.PrefabId);

            var insideSize = prefab.Blocks.Array.Size;
            int insideLength = insideSize.X * insideSize.Y * insideSize.Z;

            ushort[] blocks = prefab.Blocks.Array.Array;
            for (int i = 0; i < insideLength; i++)
            {
                ushort block = blocks[i];

                if (block != 0 && !used[block])
                {
                    MarkUsed(block);
                }
            }
        }
    }

    public bool Used(ushort id)
        => _used[id];
}
