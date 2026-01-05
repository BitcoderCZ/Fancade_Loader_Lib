// <copyright file="GameMeshInfo.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Fancade.Runtime.Simulated.Utils;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Simulated;

/// <summary>
/// Stores the meshes of a game.
/// </summary>
public struct GameMeshInfo
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

    private List<ValueList<FcMesh.Block>>? _uniqueMeshes;

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
    /// Gets the amount of unique meshes, call <see cref="DeduplicateMeshes"/> to initialize.
    /// </summary>
    /// <value>The amount of unique meshess; or <see langword="null"/>, if <see cref="DeduplicateMeshes"/> has not been called.</value>
    public readonly int? UniqueMeshCount => _uniqueMeshes?.Count;

    /// <summary>
    /// Creates a new instance of the <see cref="GameMeshInfo"/>.
    /// </summary>
    /// <param name="prefabs"><see cref="PrefabList"/> of the game.</param>
    /// <param name="mainPrefabId">Id of the main(open) prefab.</param>
    /// <param name="createMultiThreaded">Whether to use multiple threads to create the <see cref="GameMeshInfo"/>.</param>
    /// <returns>The created <see cref="GameMeshInfo"/>.</returns>
    public static GameMeshInfo Create(PrefabList prefabs, ushort mainPrefabId, bool createMultiThreaded = true)
    {
        if (prefabs.IdOffset != RawGame.CurrentNumbStockPrefabs)
        {
            ThrowArgumentException($"{nameof(prefabs)}.{nameof(prefabs.IdOffset)} must be equal to {nameof(RawGame)}.{nameof(RawGame.CurrentNumbStockPrefabs)}.", nameof(prefabs));
        }

        InitStock(createMultiThreaded);

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

        if (createMultiThreaded)
        {
#if NET9_0_OR_GREATER
            Lock blockMeshesLock = new();
#else
            object blockMeshesLock = new();
#endif

            Parallel.ForEach(prefabs.OrderBy(prefab => prefab.Id), prefab =>
            {
                BlockMesh blockMesh = prefab.Type is PrefabType.Level && prefab.Id != mainPrefabId
                    ? BlockMesh.Empty
                    : BlockMesh.Create(prefab.Blocks, prefabs, segmentMeshes);

                lock (blockMeshesLock)
                {
                    blockMeshes.Add(prefab.Id, blockMesh);
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
            });
        }
        else
        {
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
        }

        return new GameMeshInfo(blockMeshes, segmentMeshes, prefabMeshBounds);
    }

    /// <param name="multiThreaded">Whether to perform the operation using multiple threads.</param>
    public void DeduplicateMeshes(bool multiThreaded = true)
    {
        if (_uniqueMeshes is not null)
        {
            return;
        }

        var uniqueMeshes = _uniqueMeshes = new List<ValueList<FcMesh.Block>>(512);

        if (multiThreaded)
        {
            var lookup = new ConcurrentDictionary<FcMesh, int>(FcMesh.BlocksEqualityComparer.Instance);

#if NET9_0_OR_GREATER
            Lock uniqueMeshesLock = new();
#else
            object uniqueMeshesLock = new();
#endif

            Parallel.ForEach(_blockMeshes, meshKVP =>
            {
                var meshes = CollectionsMarshal.AsSpan(meshKVP.Value._meshes);
                for (int i = meshes.Length - 1; i >= 0; i--)
                {
                    ref var item = ref meshes[i];

                    int index = lookup.GetOrAdd(item.Mesh, static (mesh, item) =>
                    {
                        var (uniqueMeshes, uniqueMeshesLock) = item;
                        lock (uniqueMeshesLock)
                        {
                            uniqueMeshes.Add(mesh.Blocks);
                            return uniqueMeshes.Count - 1;
                        }
                    }, (uniqueMeshes, uniqueMeshesLock));

                    item.Mesh.Blocks = uniqueMeshes[index]; // maybe different instance, same values. allow GC to free the list
                    item.UniqueMeshIndex = index;
                }
            });
        }
        else
        {
            var lookup = new Dictionary<FcMesh, int>(uniqueMeshes.Capacity, FcMesh.BlocksEqualityComparer.Instance);

            foreach (var (_, mesh) in _blockMeshes)
            {
                var meshes = CollectionsMarshal.AsSpan(mesh._meshes);
                for (int i = meshes.Length - 1; i >= 0; i--)
                {
                    ref var item = ref meshes[i];

                    ref int index = ref CollectionsMarshal.GetValueRefOrAddDefault(lookup, item.Mesh, out bool exists);

                    if (exists)
                    {
                        item.Mesh.Blocks = uniqueMeshes[index]; // different instance, same values. allow GC to free the list
                    }
                    else
                    {
                        index = uniqueMeshes.Count;
                        uniqueMeshes.Add(item.Mesh.Blocks);
                    }

                    item.UniqueMeshIndex = index;
                }
            }
        }
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


    public readonly ValueList<FcMesh.Block> GetUniqueMesh(int uniqueMeshIndex)
        => _uniqueMeshes![uniqueMeshIndex];

    [MemberNotNull(nameof(stockBlockMeshes), nameof(stockSegmentMeshes))]
    private static void InitStock(bool createMultiThreaded)
    {
        if (stockInitialized)
        {

            Debug.Assert(stockBlockMeshes is not null, $"{nameof(stockBlockMeshes)} should not be null after initialization.");
            Debug.Assert(stockSegmentMeshes is not null, $"{nameof(stockSegmentMeshes)} should not be null after initialization.");
            return;
        }

        lock (_initLock)
        {
            if (stockInitialized)
            {
                Debug.Assert(stockBlockMeshes is not null, $"{nameof(stockBlockMeshes)} should not be null after initialization.");
                Debug.Assert(stockSegmentMeshes is not null, $"{nameof(stockSegmentMeshes)} should not be null after initialization.");
                return;
            }

            var stockPrefabs = StockBlocks.PrefabList;

            stockBlockMeshes = new (ushort, BlockMesh)[stockPrefabs.PrefabCount];
            stockSegmentMeshes = new PrefabSegmentMeshes[stockPrefabs.SegmentCount];

            if (createMultiThreaded)
            {
                Parallel.For(0, stockSegmentMeshes.Length, i =>
                {
                    stockSegmentMeshes[i] = PrefabSegmentMeshes.Create(stockPrefabs.GetSegment((ushort)i));
                });
            }
            else
            {
                for (ushort i = 0; i < stockSegmentMeshes.Length; i++)
                {
                    stockSegmentMeshes[i] = PrefabSegmentMeshes.Create(stockPrefabs.GetSegment(i));
                }
            }

            PrefabList emptyList = new();

            if (createMultiThreaded)
            {
                Parallel.ForEach(
                    stockPrefabs.OrderBy(prefab => prefab.Id)
#if NET9_0_OR_GREATER
                    .Index(),
#else
                    .Select((prefab, index) => (index, prefab)),
#endif
                    item =>
                {
                    var (prefabIndex, prefab) = item;
                    stockBlockMeshes[prefabIndex] = (prefab.Id, BlockMesh.Create(prefab.Blocks, emptyList, stockSegmentMeshes));
                });
            }
            else
            {
                int prefabIndex = 0;
                foreach (var prefab in stockPrefabs.OrderBy(prefab => prefab.Id))
                {
                    stockBlockMeshes[prefabIndex++] = (prefab.Id, BlockMesh.Create(prefab.Blocks, emptyList, stockSegmentMeshes));
                }
            }

            stockInitialized = true;
        }
    }
}