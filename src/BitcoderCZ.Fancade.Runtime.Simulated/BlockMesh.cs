// <copyright file="BlockMesh.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Fancade.Runtime.Simulated.Utils;
using BitcoderCZ.Maths.Vectors;
using Microsoft.Extensions.ObjectPool;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BitcoderCZ.Fancade.Runtime.Simulated;

/// <summary>
/// Stores the mesh of the inside of a prefab.
/// </summary>
public readonly struct BlockMesh
{
    /// <summary>
    /// An empty <see cref="BlockMesh"/> instance.
    /// </summary>
    public static readonly BlockMesh Empty = new BlockMesh(0, new Array3D<int>(int3.Zero), [], []);

    internal readonly Array3D<int> _blockMeshIdOffsets;

    private const int ScriptMeshIndex = 0;

    private static readonly DefaultObjectPool<Stack<(ushort SegmentId, int3 Pos, ushort MeshIndex)>> _stackPool = new DefaultObjectPool<Stack<(ushort, int3, ushort)>>(new PooledStackPolicy<(ushort, int3, ushort)>() { DefaultCapacity = 256, });

    private static readonly short3[] NeighborOffsets =
    [
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0),
        new(0, 0, 1),
        new(0, 0, -1),
    ];

    private readonly short[] _blockMeshIds;
    private readonly List<(FcMesh Mesh, int UniqueMeshIndex)> _meshes;

    private BlockMesh(int meshCount, Array3D<int> blockMeshIdOffsets, List<(FcMesh Mesh, int UniqueMeshIndex)> meshes, short[] blockMeshIds)
    {
        Debug.Assert(meshes.Count == meshCount, $"{nameof(meshes)} should have {meshCount} elements.");

        MeshCount = meshCount;
        _blockMeshIdOffsets = blockMeshIdOffsets;
        _meshes = meshes;
        _blockMeshIds = blockMeshIds;
    }

    /// <summary>
    /// Gets the number of meshes.
    /// </summary>
    /// <value>Number of meshes.</value>
    public readonly int MeshCount { get; }

    /// <summary>
    /// Gets the size of the inside of the prefab.
    /// </summary>
    /// <value>Size of the inside of the prefab.</value>
    public readonly int3 Size => _blockMeshIdOffsets.Size;

    /// <summary>
    /// Gets the mesh offsets.
    /// </summary>
    /// <value>Mesh offsets.</value>
    public readonly ReadOnlySpan<int> BlockMeshIdOffsets => _blockMeshIdOffsets.Array;

    /// <summary>
    /// Gets the mesh ids as <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <value>Mesh ids as <see cref="ReadOnlySpan{T}"/>.</value>
    public readonly ReadOnlySpan<short> BlockMeshIds => _blockMeshIds;

    /// <summary>
    /// Gets the mesh ids.
    /// </summary>
    /// <value>Mesh ids.</value>
    public readonly IReadOnlyList<short> BlockMeshIdsList => _blockMeshIds;

    /// <summary>
    /// Gets the meshes as <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <value>The meshes as <see cref="ReadOnlySpan{T}"/>.</value>
    public readonly ReadOnlySpan<(FcMesh Mesh, int UniqueMeshIndex)> Meshes => CollectionsMarshal.AsSpan(_meshes);

    /// <summary>
    /// Gets the meshes.
    /// </summary>
    /// <value>The meshes.</value>
    public readonly IReadOnlyList<(FcMesh Mesh, int UniqueMeshIndex)> MeshesList => _meshes;

    /// <summary>
    /// Creates a new <see cref="BlockMesh"/> instance.
    /// </summary>
    /// <param name="blocks">The <see cref="IBlockData"/> to create the <see cref="BlockMesh"/> for.</param>
    /// <param name="createScriptMesh">Whether to create mesh for script blocks.</param>
    /// <param name="prefabs">A <see cref="PrefabList"/> used to resolve prefab types and voxels.</param>
    /// <param name="segmentMeshes">A <see cref="ReadOnlySpan{T}"/> of <see cref="PrefabSegmentMeshes"/>, where the index corresponds to the segment id.</param>
    /// <param name="getUnique">A method that given a mesh (blocks, mesh index), gets it's unique index, and if it was already encountered, returns the stored mesh (the input list can be reused).</param>
    /// <returns>The created <see cref="BlockMesh"/>.</returns>
    public static BlockMesh Create(IBlockData blocks, bool createScriptMesh, PrefabList prefabs, ImmutableArray<PrefabSegmentMeshes> segmentMeshes, Func<ValueListWithHash<FcMesh.Block>, int, (int UniqueIndex, ValueList<FcMesh.Block>? UniqueMesh)> getUnique)
    {
        if (blocks.Size == int3.Zero)
        {
            return Empty;
        }

        var blocksSize = blocks.Size;
        int blocksLength = blocksSize.X * blocksSize.Y * blocksSize.Z;

        var blockMeshIdOffsets = new Array3D<int>(blocksSize);

        var countSegmentsAction = new CountMeshSegmentsAction(blocks.BoundsMin, createScriptMesh, prefabs, blockMeshIdOffsets, segmentMeshes);
        blocks.EnumerateNonEmptyBlocks(ref countSegmentsAction);
        int totalSegmentMeshCount = countSegmentsAction.TotalSegmentMeshCount;

        if (totalSegmentMeshCount == 0)
        {
            return Empty;
        }

        var stockPrefabs = StockBlocks.PrefabList;

        short[] blockMeshIds = new short[totalSegmentMeshCount];
        var meshes = new List<(FcMesh Mesh, int UniqueMeshIndex)>(totalSegmentMeshCount);

        blockMeshIds.AsSpan().Fill(-1);

        Stack<(ushort SegmentId, int3 Pos, ushort MeshIndex)> stack = _stackPool.Get();

        if (createScriptMesh)
        {
            var createScriptMeshAction = new CreateScriptMeshAction(blocks.BoundsMin, prefabs, stockPrefabs, blockMeshIds, blockMeshIdOffsets, segmentMeshes);
            blocks.EnumerateNonEmptyBlocks(ref createScriptMeshAction);
            var blockList = createScriptMeshAction.BlockList;

            int3 minPos;
            if (blockList.List.Count is 0)
            {
                minPos = int3.Zero;
            }
            else
            {
                minPos = new int3(int.MaxValue, int.MaxValue, int.MaxValue);
                foreach (var block in blockList.List)
                {
                    minPos = int3.Min(minPos, block.Offset);
                }

                // make offsets zero based
                if (minPos != int3.Zero)
                {
                    for (int i = blockList.List.Count - 1; i >= 0; i--)
                    {
                        ref var block = ref blockList.List.GetRef(i);
                        block = new FcMesh.Block(block.SegmentId, block.Offset - minPos, block.LocalMeshIndex);
                    }
                }

                blockList.List.Sort();
            }

            blockList.ComputeHash();

            var (uniqueIndex, uniqueMesh) = getUnique(blockList, ScriptMeshIndex);

            if (uniqueMesh is null)
            {
                // new unique, blockList was added to unique mesh list
                meshes.Add((new FcMesh(blockList.List, minPos), uniqueIndex));
            }
            else
            {
                // existing mesh, blockList can be reused
                meshes.Add((new FcMesh(uniqueMesh.Value, minPos), uniqueIndex));
                blockList.List.Clear();
            }
        }

        var createMeshAction = new CreateMeshAction(blocks, prefabs, stockPrefabs, blockMeshIds, blockMeshIdOffsets, getUnique, segmentMeshes, meshes, createScriptMesh);
        blocks.EnumerateNonEmptyBlocks(ref createMeshAction);
        var meshIndex = createMeshAction.MeshIndex;

        return new BlockMesh(meshIndex, blockMeshIdOffsets, meshes, blockMeshIds);
    }

    /// <summary>
    /// Gets the mesh offset <paramref name="position"/>.
    /// </summary>
    /// <param name="position">The position of which the mesh offset should be retrieved.</param>
    /// <returns>Mesh offset at <paramref name="position"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetMeshOffset(int3 position)
        => _blockMeshIdOffsets.Get(position);

    /// <summary>
    /// Gets the mesh offset <paramref name="position"/>, without bounds checking.
    /// </summary>
    /// <param name="position">The position of which the mesh offset should be retrieved.</param>
    /// <returns>Mesh offset at <paramref name="position"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetMeshOffsetUnchecked(int3 position)
        => _blockMeshIdOffsets.GetUnchecked(position);

    /// <summary>
    /// Gets the mesh offset <paramref name="position"/>.
    /// </summary>
    /// <param name="position">The position of which the mesh offset should be retrieved.</param>
    /// <returns>Mesh offset at <paramref name="position"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetMeshOffsetOrZero(int3 position)
        => _blockMeshIdOffsets.InBounds(position)
        ? _blockMeshIdOffsets.GetUnchecked(position)
        : 0;

    /// <summary>
    /// Gets the id of the mesh at <paramref name="position"/>.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <param name="segmentMeshIndex">The local segment mesh index of the block.</param>
    /// <returns>Id of the mesh at <paramref name="position"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetMeshAtPos(int3 position, int segmentMeshIndex)
        => _blockMeshIds[GetMeshOffset(position) + segmentMeshIndex];

    /// <summary>
    /// Gets the positions a certain mesh occupies, may contain duplicates.
    /// </summary>
    /// <param name="meshIndex">Index of the mesh whose positions should be retrieved.</param>
    /// <returns>Positions the mesh occupies, may contain duplicates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly FcMesh.PositionsEnumerable EnumerateMeshBlocks(int meshIndex)
        => _meshes[meshIndex].Mesh.Positions;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void GetMesh(int meshIndex, out FcMesh mesh, out int uniqueMeshIndex)
    {
        var item = _meshes[meshIndex];
        mesh = item.Mesh;
        uniqueMeshIndex = item.UniqueMeshIndex;
    }

    private struct CountMeshSegmentsAction : IRefValueAction<ushort, int3>
    {
        private readonly int3 _boundsMin;
        private readonly bool _createScriptMesh;
        private readonly PrefabList _prefabs;
        private readonly Array3D<int> _blockMeshIdOffsets;
        private readonly ImmutableArray<PrefabSegmentMeshes> _segmentMeshes;
        private int _totalSegmentMeshCount;

        public CountMeshSegmentsAction(int3 boundsMin, bool createScriptMesh, PrefabList prefabs, Array3D<int> blockMeshIdOffsets, ImmutableArray<PrefabSegmentMeshes> segmentMeshes)
        {
            _boundsMin = boundsMin;
            _createScriptMesh = createScriptMesh;
            _prefabs = prefabs;
            _blockMeshIdOffsets = blockMeshIdOffsets;
            _segmentMeshes = segmentMeshes;

            _totalSegmentMeshCount = createScriptMesh ? 1 : 0;
        }

        public int TotalSegmentMeshCount => _totalSegmentMeshCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(ref ushort segmentId, int3 segmentPos)
        {
            _blockMeshIdOffsets[segmentPos - _boundsMin] = _totalSegmentMeshCount;

            Debug.Assert(segmentId is not 0, "Invoke should only be called for non 0 segments.");
            if (!_createScriptMesh && (segmentId < RawGame.CurrentNumbStockPrefabs ? StockBlocks.IsScriptSegment(segmentId) : _prefabs.GetPrefab(_prefabs.GetSegment(segmentId).PrefabId).Type == PrefabType.Script))
            {
                return;
            }

            _totalSegmentMeshCount += _segmentMeshes[segmentId].MeshCount;
        }
    }

    private struct CreateScriptMeshAction : IRefValueAction<ushort, int3>
    {
        public ValueListWithHash<FcMesh.Block> BlockList;

        private readonly int3 _boundsMin;
        private readonly PrefabList _prefabs;
        private readonly PrefabList _stockPrefabs;
        private readonly short[] _blockMeshIds;
        private readonly Array3D<int> _blockMeshIdOffsets;
        private readonly ImmutableArray<PrefabSegmentMeshes> _segmentMeshes;

        public CreateScriptMeshAction(int3 boundsMin, PrefabList prefabs, PrefabList stockPrefabs, short[] blockMeshIds, Array3D<int> blockMeshIdOffsets, ImmutableArray<PrefabSegmentMeshes> segmentMeshes)
        {
            _boundsMin = boundsMin;
            _prefabs = prefabs;
            _stockPrefabs = stockPrefabs;
            _blockMeshIds = blockMeshIds;
            _blockMeshIdOffsets = blockMeshIdOffsets;
            _segmentMeshes = segmentMeshes;
        }

        public void Invoke(ref ushort blockId, int3 blockPos)
        {
            var segment = GetSegment(blockId);

            var prefab = GetPrefab(segment.PrefabId);

            var segmentMesh = _segmentMeshes[blockId];

            if (prefab.Type is not PrefabType.Script)
            {
                return;
            }

            var blocksSize = _blockMeshIdOffsets.Size;
            var idOffsetIndex = (blockPos - _boundsMin).ToIndex(blocksSize.X, blocksSize.Y);

            for (int segmentMeshIndex = 0; segmentMeshIndex < segmentMesh.MeshCount; segmentMeshIndex++)
            {
                Debug.Assert(_blockMeshIds[_blockMeshIdOffsets[idOffsetIndex] + segmentMeshIndex] is -1);
                _blockMeshIds[_blockMeshIdOffsets[idOffsetIndex] + segmentMeshIndex] = ScriptMeshIndex;

                BlockList.List.Add(new FcMesh.Block(blockId, blockPos, (ushort)segmentMeshIndex));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PrefabSegment GetSegment(ushort id)
         => id < RawGame.CurrentNumbStockPrefabs ? _stockPrefabs.GetSegment(id) : _prefabs.GetSegment(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Prefab GetPrefab(ushort prefabId)
            => prefabId < RawGame.CurrentNumbStockPrefabs ? _stockPrefabs.GetPrefab(prefabId) : _prefabs.GetPrefab(prefabId);
    }

    private struct CreateMeshAction : IRefValueAction<ushort, int3>, IDisposable
    {
        private readonly IBlockData _blocks;
        private readonly PrefabList _prefabs;
        private readonly PrefabList _stockPrefabs;
        private readonly Stack<(ushort SegmentId, int3 Pos, ushort MeshIndex)> _stack = _stackPool.Get();
        private readonly short[] _blockMeshIds;
        private readonly Array3D<int> _blockMeshIdOffsets;
        private readonly Func<ValueListWithHash<FcMesh.Block>, int, (int UniqueIndex, ValueList<FcMesh.Block>? UniqueMesh)> _getUnique;
        private readonly ImmutableArray<PrefabSegmentMeshes> _segmentMeshes;
        private readonly List<(FcMesh Mesh, int UniqueMeshIndex)> _meshes;
        private ValueListWithHash<FcMesh.Block> _blockList;
        private short _meshIndex;

        public CreateMeshAction(IBlockData blocks, PrefabList prefabs, PrefabList stockPrefabs, short[] blockMeshIds, Array3D<int> blockMeshIdOffsets, Func<ValueListWithHash<FcMesh.Block>, int, (int UniqueIndex, ValueList<FcMesh.Block>? UniqueMesh)> getUnique, ImmutableArray<PrefabSegmentMeshes> segmentMeshes, List<(FcMesh Mesh, int UniqueMeshIndex)> meshes, bool createScriptMesh)
        {
            _blocks = blocks;
            _prefabs = prefabs;
            _stockPrefabs = stockPrefabs;
            _blockMeshIds = blockMeshIds;
            _blockMeshIdOffsets = blockMeshIdOffsets;
            _getUnique = getUnique;
            _segmentMeshes = segmentMeshes;
            _meshes = meshes;
            _meshIndex = createScriptMesh ? (short)1 : (short)0;
        }

        public short MeshIndex => _meshIndex;

        public void Invoke(ref ushort blockId, int3 blockPos)
        {
            var segment = GetSegment(blockId);

            var prefab = GetPrefab(segment.PrefabId);

            var segmentMeshes = _segmentMeshes.AsSpan();

            var segmentMesh = _segmentMeshes[blockId];

            if (prefab.Type is PrefabType.Script)
            {
                return;
            }

            var blocksSize = _blockMeshIdOffsets.Size;
            var idOffsetIndex = (blockPos - _blocks.BoundsMin).ToIndex(blocksSize.X, blocksSize.Y);

            for (int segmentMeshIndex = 0; segmentMeshIndex < segmentMesh.MeshCount; segmentMeshIndex++)
            {
                if (_blockMeshIds[segmentMeshIndex + _blockMeshIdOffsets[idOffsetIndex]] is not -1)
                {
                    continue;
                }

                Debug.Assert(_stack.Count is 0, $"{nameof(_stack)} should be empty.");

                _stack.Push((blockId, blockPos, (ushort)segmentMeshIndex));

                while (_stack.TryPop(out var item))
                {
                    var currentPos = item.Pos;
                    _blockList.List.Add(new FcMesh.Block(item.SegmentId, currentPos, item.MeshIndex));

                    int currentBLockIndex = currentPos.X + ((currentPos.Y + (currentPos.Z * blocksSize.Y)) * blocksSize.X);

                    ushort currentBlockId = _blocks.GetBlockUnchecked(currentPos);

                    _blockMeshIds[_blockMeshIdOffsets[currentBLockIndex] + item.MeshIndex] = _meshIndex;

                    for (int sideIndex = 0; sideIndex < 6; sideIndex++)
                    {
                        int3 neighborPos = currentPos + NeighborOffsets[sideIndex];

                        if (!_blocks.IsInBounds(neighborPos))
                        {
                            continue;
                        }

                        ushort neighborId = _blocks.GetBlockUnchecked(neighborPos);

                        int neighborMeshCount = segmentMeshes[neighborId].MeshCount;

                        if (neighborId is 0 ||
                            GetPrefab(GetSegment(neighborId).PrefabId).Type is PrefabType.Script ||
                            neighborMeshCount is 0)
                        {
                            continue;
                        }

                        int neighborIdOffsetIndex = neighborPos.X + ((neighborPos.Y + (neighborPos.Z * blocksSize.Y)) * blocksSize.X);
                        int neighborMaxMeshCountUpToPos = _blockMeshIdOffsets[neighborIdOffsetIndex];

                        for (ushort neighborMeshIndex = 0; neighborMeshIndex < neighborMeshCount; neighborMeshIndex++)
                        {
                            if (_blockMeshIds[neighborMeshIndex + neighborMaxMeshCountUpToPos] is -1 &&
                                Glues(currentBlockId, item.MeshIndex, sideIndex, neighborId, neighborMeshIndex, segmentMeshes))
                            {
                                _stack.Push((neighborId, neighborPos, neighborMeshIndex));
                            }
                        }
                    }
                }

                var minPos = new int3(int.MaxValue, int.MaxValue, int.MaxValue);
                foreach (var block in _blockList.List)
                {
                    minPos = int3.Min(minPos, block.Offset);
                }

                // make offsets zero based
                for (int i = _blockList.List.Count - 1; i >= 0; i--)
                {
                    ref var block = ref _blockList.List.GetRef(i);
                    block = new FcMesh.Block(block.SegmentId, block.Offset - minPos, block.LocalMeshIndex);
                }

                _blockList.List.Sort();

                _blockList.ComputeHash();

                var (uniqueIndex, uniqueMesh) = _getUnique(_blockList, _meshIndex);

                if (uniqueMesh is null)
                {
                    // new unique, blockList was added to unique mesh list
                    _meshes.Add((new FcMesh(_blockList.List, minPos), uniqueIndex));
                    _blockList = default;
                }
                else
                {
                    // existing mesh, blockList can be reused
                    _meshes.Add((new FcMesh(uniqueMesh.Value, minPos), uniqueIndex));
                    _blockList.List.Clear();
                }

                _meshIndex++;
            }
        }

        public void Dispose()
            => _stackPool.Return(_stack);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Glues(ushort segmentId1, int meshIndex1, int sideIndex, ushort segmentId2, int index2, ReadOnlySpan<PrefabSegmentMeshes> segmentMeshes)
        {
            if (sideIndex >= 6)
            {
                return false;
            }

            Debug.Assert(sideIndex >= 0);

            var meshA = segmentMeshes[segmentId1].Meshes[meshIndex1];
            var meshB = segmentMeshes[segmentId2].Meshes[index2];

            int neighborSide = sideIndex ^ 1;

            return (meshA.GetSideGlue(sideIndex) & meshB.GetSideGlue(neighborSide)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PrefabSegment GetSegment(ushort id)
            => id < RawGame.CurrentNumbStockPrefabs ? _stockPrefabs.GetSegment(id) : _prefabs.GetSegment(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Prefab GetPrefab(ushort prefabId)
            => prefabId < RawGame.CurrentNumbStockPrefabs ? _stockPrefabs.GetPrefab(prefabId) : _prefabs.GetPrefab(prefabId);
    }
}