using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
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
    private readonly (int3 Min, int3 Max)[] _prefabMeshBounds;

    public GameMeshInfo(Dictionary<ushort, BlockMesh> blockMeshes, PrefabSegmentMeshes[] segmentMeshes, (int3 Min, int3 Max)[] prefabMeshBounds)
    {
        _blockMeshes = blockMeshes;
        _segmentMeshes = segmentMeshes;
        _prefabMeshBounds = prefabMeshBounds;
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
        (int3 Min, int3 Max)[] prefabMeshBounds = new(int3, int3)[stockSegmentMeshes.Length + prefabs.SegmentCount];

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

            int3 min = new int3(int.MaxValue, int.MaxValue, int.MaxValue);
            int3 max = new int3(int.MinValue, int.MinValue, int.MinValue);

            foreach (var (segment, segmentId) in prefab.EnumerateWithId())
            {
                var segmentMesh = segmentMeshes[segmentId];
                min = int3.Min(min, segment.PosInPrefab * 8 + segmentMesh.MinPosition);
                max = int3.Max(max, segment.PosInPrefab * 8 + segmentMesh.MaxPosition);
            }

            foreach (var (_, segmentId) in prefab.EnumerateWithId())
            {
                prefabMeshBounds[segmentId] = (min, max);
            }
        }

        return new GameMeshInfo(blockMeshes, segmentMeshes, prefabMeshBounds);
    }

    public BlockMesh GetBlockMesh(ushort id)
        => _blockMeshes[id];

    public PrefabSegmentMeshes GetSegmentMesh(ushort id)
        => _segmentMeshes[id];

    public (int3 Min, int3 Max) GetPrefabMeshBounds(ushort id)
        => _prefabMeshBounds[id];

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
