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
using static BitcoderCZ.Utils.ThrowHelper;

using UniqueMesh = (BitcoderCZ.Fancade.Runtime.Simulated.Utils.ValueList<BitcoderCZ.Fancade.Runtime.Simulated.FcMesh.Block> Mesh, ushort PrefabId, int MeshIndex);

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
    private static List<UniqueMesh>? stockUniqueMeshes;
    private static PrefabSegmentMeshes[]? stockSegmentMeshes;
    private static bool stockInitialized = false;

    private readonly Dictionary<ushort, BlockMesh> _blockMeshes;
    private readonly PrefabSegmentMeshes[] _segmentMeshes;
    private readonly (int3 Min, int3 Max)[] _prefabMeshBounds;

    private readonly List<UniqueMesh> _uniqueMeshes;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameMeshInfo"/> struct.
    /// </summary>
    /// <param name="blockMeshes">A dictionary of prefab id to <see cref="BlockMesh"/>.</param>
    /// <param name="segmentMeshes">A <see cref="PrefabSegmentMeshes"/> array, where the index corresponds to the segment id.</param>
    /// <param name="prefabMeshBounds">An array,of voxel mesh bounds of a prefab, where the index corresponds to the segment id.</param>
    /// <param name="uniqueMeshes">List of unique meshes.</param>
    public GameMeshInfo(Dictionary<ushort, BlockMesh> blockMeshes, PrefabSegmentMeshes[] segmentMeshes, (int3 Min, int3 Max)[] prefabMeshBounds, List<UniqueMesh> uniqueMeshes)
    {
        _blockMeshes = blockMeshes;
        _segmentMeshes = segmentMeshes;
        _prefabMeshBounds = prefabMeshBounds;
        _uniqueMeshes = uniqueMeshes;
    }

    /// <summary>
    /// Gets the amount of unique meshes.
    /// </summary>
    /// <value>The amount of unique meshess.</value>
    public readonly int UniqueMeshCount => _uniqueMeshes.Count;

    /// <summary>
    /// Creates a new instance of the <see cref="GameMeshInfo"/>.
    /// </summary>
    /// <param name="prefabs"><see cref="PrefabList"/> of the game.</param>
    /// <param name="mainPrefabId">Id of the main(open) prefab.</param>
    /// <param name="createScriptMesh">Whether to create mesh for script blocks.</param>
    /// <param name="createMultiThreaded">Whether to use multiple threads to create the <see cref="GameMeshInfo"/>.</param>
    /// <returns>The created <see cref="GameMeshInfo"/>.</returns>
    public static GameMeshInfo Create(PrefabList prefabs, ushort mainPrefabId, bool createScriptMesh, bool createMultiThreaded = true)
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

        var uniqueMeshes = new List<UniqueMesh>(stockUniqueMeshes.Count + 512);
        uniqueMeshes.AddRange(stockUniqueMeshes);

        if (createMultiThreaded)
        {
#if NET9_0_OR_GREATER
            Lock blockMeshesLock = new();
            Lock uniqueMeshesLock = new();
#else
            object blockMeshesLock = new();
            object uniqueMeshesLock = new();
#endif

            var lookup = new ConcurrentDictionary<ValueListWithHash<FcMesh.Block>, int>(stockUniqueMeshes.Select(static (mesh, index) =>
            {
                ValueListWithHash<FcMesh.Block> list = default;
                list.List = mesh.Mesh;
                list.ComputeHash();
                return new KeyValuePair<ValueListWithHash<FcMesh.Block>, int>(list, index);
            }));

            Parallel.ForEach(prefabs.OrderBy(prefab => prefab.Id), prefab =>
            {
                BlockMesh blockMesh = prefab.Type is PrefabType.Level && prefab.Id != mainPrefabId
                    ? BlockMesh.Empty
                    : BlockMesh.Create(prefab.Blocks, createScriptMesh && prefab.Id == mainPrefabId, prefabs, ImmutableCollectionsMarshal.AsImmutableArray(segmentMeshes), (mesh, meshIndex) =>
                    {
                        int index = lookup.GetOrAdd(mesh, static (mesh, item) =>
                        {
                            var (uniqueMeshes, uniqueMeshesLock, prefabId, meshIndex) = item;
                            lock (uniqueMeshesLock)
                            {
                                uniqueMeshes.Add((mesh.List, prefabId, meshIndex));
                                return uniqueMeshes.Count - 1;
                            }
                        }, (uniqueMeshes, uniqueMeshesLock, prefab.Id, meshIndex));

                        var uniqueMesh = uniqueMeshes[index].Mesh;
                        if (ReferenceEquals(mesh.List._list, uniqueMesh._list))
                        {
                            // new mesh/buffer only, so list can be reused
                            return (index, null);
                        }
                        else
                        {
                            // existing mesh
                            return (index, uniqueMesh);
                        }
                    });

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
            var lookup = new Dictionary<ValueListWithHash<FcMesh.Block>, int>(stockUniqueMeshes.Select(static (mesh, index) =>
            {
                ValueListWithHash<FcMesh.Block> list = default;
                list.List = mesh.Mesh;
                list.ComputeHash();
                return new KeyValuePair<ValueListWithHash<FcMesh.Block>, int>(list, index);
            }));

            foreach (var prefab in prefabs.OrderBy(prefab => prefab.Id))
            {
                if (prefab.Id == mainPrefabId || prefab.Type != PrefabType.Level)
                {
                    blockMeshes.Add(prefab.Id, BlockMesh.Create(prefab.Blocks, createScriptMesh && prefab.Id == mainPrefabId, prefabs, ImmutableCollectionsMarshal.AsImmutableArray(segmentMeshes), (mesh, meshIndex) =>
                    {
                        ref int index = ref CollectionsMarshal.GetValueRefOrAddDefault(lookup, mesh, out bool exists);

                        if (!exists)
                        {
                            index = uniqueMeshes.Count;
                            uniqueMeshes.Add((mesh.List, prefab.Id, meshIndex));

                            if (mesh.List._list is not null)
                            {
                                return (index, null);
                            }
                        }

                        return (index, uniqueMeshes[index].Mesh);
                    }));
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

        return new GameMeshInfo(blockMeshes, segmentMeshes, prefabMeshBounds, uniqueMeshes);
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
        => _uniqueMeshes[uniqueMeshIndex].Mesh;

    public readonly (ushort PrefabId, int MeshIndex) GetUniqueMeshFirstOccurrence(int uniqueMeshIndex)
    {
        var item = _uniqueMeshes[uniqueMeshIndex];
        return (item.PrefabId, item.MeshIndex);
    }

    [MemberNotNull(nameof(stockBlockMeshes), nameof(stockSegmentMeshes), nameof(stockUniqueMeshes))]
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
            stockUniqueMeshes = new(1);
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
#if NET9_0_OR_GREATER
                Lock uniqueMeshesLock = new();
#else
                object uniqueMeshesLock = new();
#endif

                var lookup = new ConcurrentDictionary<ValueListWithHash<FcMesh.Block>, int>(/*uniqueMeshes.Capacity*/);

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
                    stockBlockMeshes[prefabIndex] = (prefab.Id, BlockMesh.Create(prefab.Blocks, false, emptyList, ImmutableCollectionsMarshal.AsImmutableArray(stockSegmentMeshes), (mesh, meshIndex) =>
                        {
                            int index = lookup.GetOrAdd(mesh, static (mesh, item) =>
                            {
                                var (uniqueMeshesLock, prefabId, meshIndex) = item;

                                lock (uniqueMeshesLock)
                                {
                                    stockUniqueMeshes.Add((mesh.List, prefabId, meshIndex));
                                    return stockUniqueMeshes.Count - 1;
                                }
                            }, (uniqueMeshesLock, prefab.Id, meshIndex));

                            var uniqueMesh = stockUniqueMeshes[index].Mesh;
                            if (ReferenceEquals(mesh.List._list, uniqueMesh._list))
                            {
                                // new mesh/buffer only, so list can be reused
                                return (index, null);
                            }
                            else
                            {
                                // existing mesh
                                return (index, uniqueMesh);
                            }
                        }));
                });
            }
            else
            {
                var lookup = new Dictionary<ValueListWithHash<FcMesh.Block>, int>(stockUniqueMeshes.Capacity);

                int prefabIndex = 0;
                foreach (var prefab in stockPrefabs.OrderBy(prefab => prefab.Id))
                {
                    stockBlockMeshes[prefabIndex++] = (prefab.Id, BlockMesh.Create(prefab.Blocks, false, emptyList, ImmutableCollectionsMarshal.AsImmutableArray(stockSegmentMeshes), (mesh, meshIndex) =>
                    {
                        ref int index = ref CollectionsMarshal.GetValueRefOrAddDefault(lookup, mesh, out bool exists);

                        if (!exists)
                        {
                            index = stockUniqueMeshes.Count;
                            stockUniqueMeshes.Add((mesh.List, prefab.Id, meshIndex));

                            if (mesh.List._list is not null)
                            {
                                return (index, null);
                            }
                        }

                        return (index, stockUniqueMeshes[index].Mesh);
                    }));
                }
            }

            stockInitialized = true;
        }
    }
}