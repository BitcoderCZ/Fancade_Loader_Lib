using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Simulated;

/// <summary>
/// Stores the meshes of a game.
/// </summary>
public readonly struct GameMeshInfo
{
    private static readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        _initLock = new();

    private static (ushort Id, BlockMesh Mesh)[]? stockBlockMeshes;
    private static PrefabSegmentMeshes[]? stockSegmentMeshes;
    private static bool stockInitialized = false;

    private readonly Dictionary<ushort, BlockMesh> _blockMeshes;
    private readonly PrefabSegmentMeshes[] _segmentMeshes;
    private readonly (int3 Min, int3 Max)[] _prefabMeshBounds;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameMeshInfo"/> struct.
    /// </summary>
    /// <param name="blockMeshes">A dictionary of prefab id to <see cref="BlockMesh"/>.</param>
    /// <param name="segmentMeshes">A <see cref="PrefabSegmentMeshes"/> array, where the index corresponds to the segment id.</param>
    /// <param name="prefabMeshBounds">An array,of voxel mesh bounds of a prefab, where the index corresponds to the segment id.</param>
    public GameMeshInfo(Dictionary<ushort, BlockMesh> blockMeshes, PrefabSegmentMeshes[] segmentMeshes, (int3 Min, int3 Max)[] prefabMeshBounds)
    {
        _blockMeshes = blockMeshes;
        _segmentMeshes = segmentMeshes;
        _prefabMeshBounds = prefabMeshBounds;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="GameMeshInfo"/>.
    /// </summary>
    /// <param name="prefabs"><see cref="PrefabList"/> of the game.</param>
    /// <param name="mainPrefabId">Id of the main(open) prefab.</param>
    /// <returns>The created <see cref="GameMeshInfo"/>.</returns>
    public static GameMeshInfo Create(PrefabList prefabs, ushort mainPrefabId)
    {
        if (prefabs.IdOffset != RawGame.CurrentNumbStockPrefabs)
        {
            ThrowArgumentException($"{nameof(prefabs)}.{nameof(prefabs.IdOffset)} must be equal to {nameof(RawGame)}.{nameof(RawGame.CurrentNumbStockPrefabs)}.", nameof(prefabs));
        }

        InitStock();

        Dictionary<ushort, BlockMesh> blockMeshes = new Dictionary<ushort, BlockMesh>(stockBlockMeshes.Length + prefabs.PrefabCount);
        PrefabSegmentMeshes[] segmentMeshes = new PrefabSegmentMeshes[stockSegmentMeshes.Length + prefabs.SegmentCount];
        (int3 Min, int3 Max)[] prefabMeshBounds = new (int3, int3)[stockSegmentMeshes.Length + prefabs.SegmentCount];

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
                min = int3.Min(min, (segment.PosInPrefab * 8) + segmentMesh.MinPosition);
                max = int3.Max(max, (segment.PosInPrefab * 8) + segmentMesh.MaxPosition);
            }

            foreach (var (_, segmentId) in prefab.EnumerateWithId())
            {
                prefabMeshBounds[segmentId] = (min, max);
            }
        }

        return new GameMeshInfo(blockMeshes, segmentMeshes, prefabMeshBounds);
    }

    /// <summary>
    /// Gets the <see cref="BlockMesh"/> of a prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <returns><see cref="BlockMesh"/> for the prefab.</returns>
    public BlockMesh GetBlockMesh(ushort id)
        => _blockMeshes[id];

    /// <summary>
    /// Gets the <see cref="PrefabSegmentMeshes"/> for a segment.
    /// </summary>
    /// <param name="id">Id of the segment.</param>
    /// <returns>The <see cref="PrefabSegmentMeshes"/> for the segment.</returns>
    public PrefabSegmentMeshes GetSegmentMesh(ushort id)
        => _segmentMeshes[id];

    /// <summary>
    /// Gets the voxel mesh bounds of a prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <returns>Mesh bounds of the prefab.</returns>
    public (int3 Min, int3 Max) GetPrefabMeshBounds(ushort id)
        => _prefabMeshBounds[id];

    [MemberNotNull(nameof(stockBlockMeshes), nameof(stockSegmentMeshes))]
    private static void InitStock()
    {
        lock (_initLock)
        {
            if (stockInitialized)
            {
                Debug.Assert(stockBlockMeshes is not null, $"{nameof(stockBlockMeshes)} should not be null after initialization.");
                Debug.Assert(stockSegmentMeshes is not null, $"{nameof(stockSegmentMeshes)} should not be null after initialization.");
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
}