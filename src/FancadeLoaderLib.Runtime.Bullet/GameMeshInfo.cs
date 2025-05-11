using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Runtime.Bullet;

public readonly struct GameMeshInfo
{
    private static (ushort Id, BlockMesh Mesh)[]? stockBlockMeshes;
    private static PrefabSegmentMeshes[]? stockSegmentMeshes;
    private static bool stockInitialized = false;

    private readonly Dictionary<ushort, BlockMesh> _blockMeshes;
    private readonly PrefabSegmentMeshes[] _segmentMeshes;

    public GameMeshInfo(Dictionary<ushort, BlockMesh> blockMeshes, PrefabSegmentMeshes[] segmentMeshes)
    {
        _blockMeshes = blockMeshes;
        _segmentMeshes = segmentMeshes;
    }

    public static GameMeshInfo Create(PrefabList prefabs, ushort mainPrefabId)
    {
        if (prefabs.IdOffset != RawGame.CurrentNumbStockPrefabs)
        {
            ThrowArgumentException($"{nameof(prefabs)}.{nameof(prefabs.IdOffset)} must be equal to {nameof(RawGame)}.{nameof(RawGame.CurrentNumbStockPrefabs)}.", nameof(prefabs));
        }

        InitStock();

        Dictionary<ushort, BlockMesh> blockMeshes = new Dictionary<ushort, BlockMesh>(stockBlockMeshes.Length + prefabs.PrefabCount);
        PrefabSegmentMeshes[] segmentMeshes = new PrefabSegmentMeshes[stockSegmentMeshes.Length + prefabs.SegmentCount];

        stockSegmentMeshes.AsSpan().CopyTo(segmentMeshes);

        int i = stockSegmentMeshes.Length;
        foreach (var segment in prefabs.Segments)
        {
            segmentMeshes[i++] = PrefabSegmentMeshes.Create(segment);
        }

        foreach (var (id, mesh) in stockBlockMeshes)
        {
            blockMeshes.Add(id, mesh);
        }

        foreach (var prefab in prefabs.OrderBy(prefab => prefab.Id))
        {
            if (prefab.Id == mainPrefabId || prefab.Type != PrefabType.Level)
            {
                blockMeshes.Add(prefab.Id, BlockMesh.Create(prefab.Blocks, prefabs, segmentMeshes));
            }
            else
            {
                blockMeshes.Add(prefab.Id, BlockMesh.Empty);
            }
        }

        return new GameMeshInfo(blockMeshes, segmentMeshes);
    }

    public BlockMesh GetBlockMesh(ushort id)
        => _blockMeshes[id];

    public PrefabSegmentMeshes GetSegmentMesh(ushort id)
        => _segmentMeshes[id];

    [MemberNotNull(nameof(stockBlockMeshes), nameof(stockSegmentMeshes))]
    private static void InitStock()
    {
        if (stockInitialized)
        {
            Debug.Assert(stockBlockMeshes is not null);
            Debug.Assert(stockSegmentMeshes is not null);
            return;
        }

        stockInitialized = true;

        var stockPrefabs = StockBlocks.PrefabList;

        stockBlockMeshes = new (ushort, BlockMesh)[stockPrefabs.PrefabCount];
        stockSegmentMeshes = new PrefabSegmentMeshes[stockPrefabs.SegmentCount];

        for (ushort i = 0; i < stockSegmentMeshes.Length; i++)
        {
            stockSegmentMeshes[i] = PrefabSegmentMeshes.Create(stockPrefabs.GetSegment(i));
        }

        PrefabList emptyList = new();

        int prefabIndex = 0;
        foreach (var prefab in stockPrefabs.OrderBy(prefab => prefab.Id))
        {
            stockBlockMeshes[prefabIndex++] = (prefab.Id, BlockMesh.Create(prefab.Blocks, emptyList, stockSegmentMeshes));
        }
    }
}
