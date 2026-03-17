// <copyright file="ListPrefab.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitcoderCZ.Fancade.Data;
using BitcoderCZ.Fancade.Exceptions;
using BitcoderCZ.Fancade.Utils;
using BitcoderCZ.Maths.Vectors;
using BitcoderCZ.Utils;
using SegmentData = BitcoderCZ.Fancade.PrefabListB.SegmentData;

namespace BitcoderCZ.Fancade;

#pragma warning disable CS1591
#pragma warning disable SA1600

// can exist on it's own, or in (only) 1 list
// todo: make IDisposable, free segment data on dispose
public sealed class ListPrefab
{
    public const int MaxSize = BitcoderCZ.Fancade.Prefab.MaxSize;

    // id that doesn't ever change, for example when the normal id is shifted due to added/removed segments
    public readonly int PermanentId;

    internal int _id;
    internal PrefabListB? _owner;

    // todo: binary search?
    // todo: InlineList<FixedArray4<KeyValuePair<byte3, int>>, KeyValuePair<byte3, int>> _segments?
    internal Dictionary<byte3, int> _segments;

    internal Dictionary<byte3, SegmentData>? _segmentData;

    private static readonly ObjectPool<Dictionary<byte3, SegmentData>> SegmentDataPool = new(() => new(4), segmentData => segmentData.Clear());

    private string _name;

    internal ListPrefab(PrefabListB? owner, int id, int permanentId, string name, PrefabType type, PrefabCollider collider, FcColor backgroundColor, PrefabTerminalInfo terminals)
    {
        _id = id;
        PermanentId = permanentId;
        _name = name;
        _owner = owner;
        Name = name;
        Type = type;
        Collider = collider;
        BackgroundColor = backgroundColor;
        Terminals = terminals;
        _segments = new(4);
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="ListPrefab"/> is in a <see cref="PrefabListB"/>.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="ListPrefab"/> is in a <see cref="PrefabListB"/>; otherwise, <see langword="false"/>.</value>
    [MemberNotNullWhen(true, nameof(_owner))]
    public bool IsInList => _owner is not null;

    public int Id
    {
        get => _id;
        internal set  // todo: editable when not in prefab
        {
            int idChange = value - _id;
            _id = value;
            
            foreach (var item in _segments)
            {
                CollectionsMarshal.GetValueRefOrNullRef(_segments, item.Key) = item.Value + idChange;
            }
        }
    }

    /// <summary>
    /// Gets or sets the name of this prefab.
    /// </summary>
    /// <value>The name of this prefab. Cannot be empty or exceed 255 bytes when UTF-8 encoded.</value>
    /// <exception cref="ArgumentException">Thrown when attempting to set an empty or null name.</exception>
    public string Name
    {
        get => _name;
        set
        {
            EnsureCustom();

            Validator.FancadeStringNonEmpty(value);

            _name = value;
        }
    }

    public PrefabType Type { get; set; }

    public PrefabCollider Collider { get; set; }

    public FcColor BackgroundColor { get; set; }

    public PrefabTerminalInfo Terminals { get; set; }

    public List<Connection> Connections { get; } = [];

    public Dictionary<int3, PrefabSettings> Settings { get; } = [];

    public IBlockData Blocks { get; } = new ArrayBlockData();

    public IReadOnlyCollection<KeyValuePair<byte3, int>> Segments => _segments;

    public int SegmentCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _segments.Count;
    }

    public int3 Size { get; private set; }

    public IEnumerable<KeyValuePair<byte3, int>> PosOrderedValues
    {
        get
        {
            for (int z = 0; z < Size.Z; z++)
            {
                for (int y = 0; y < Size.Y; y++)
                {
                    for (int x = 0; x < Size.X; x++)
                    {
                        var pos = new byte3(x, y, z);
                        if (_segments.TryGetValue(pos, out var segment))
                        {
                            yield return new(pos, segment);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the <see cref="Voxel"/> at the specified position.
    /// </summary>
    /// <param name="position">Position of the <see cref="Voxel"/> to get.</param>
    /// <returns>The <see cref="Voxel"/> at the <paramref name="position"/>, if <paramref name="position"/> is in bounds; otherwise, <see langword="default"/>.</returns>
    public Voxel GetVoxel(int3 position)
        => position.X < 0 || position.X >= MaxSize * 8 || position.Y < 0 || position.Y >= MaxSize * 8 || position.Z < 0 || position.Z >= MaxSize * 8
            ? default
            : TryGetSegmentData((byte3)(position / 8), out var segmentData) && !segmentData.Voxels.IsEmpty
            ? segmentData.Voxels[position % 8]
            : default;

    /// <summary>
    /// Determines the index of a segment, if it was at the specified position.
    /// </summary>
    /// <param name="segmentPosition">Position of the segment.</param>
    /// <returns>The index of the segment if it is in bounds; otherwise, <c>-1</c>.</returns>
    public int GetNewSegmentIndex(int3 segmentPosition)
    {
        int index = 0;

        for (int z = 0; z < MaxSize; z++)
        {
            for (int y = 0; y < MaxSize; y++)
            {
                for (int x = 0; x < MaxSize; x++)
                {
                    var pos = new byte3(x, y, z);

                    if (pos == segmentPosition)
                    {
                        return index;
                    }

                    if (_segments.ContainsKey(pos))
                    {
                        index++;
                    }
                }
            }
        }

        return -1;
    }

    public bool ContainsSegment(int3 position)
        => _segments.ContainsKey((byte3)position);

    /// <summary>
    /// Adds a segment to the prefab.
    /// </summary>
    /// <param name="segmentPosition">Position of the segment to add.</param>
    /// <param name="voxels">Voxels of the segment to add.</param>
    /// <param name="overwriteBlocks">
    /// If <see langword="true"/>, blocks will be overwritten,
    /// if <see langword="false"/>, if the segment would be placed at a position that is already occupied, an <see cref="BlockObstructedException"/> will be thrown.
    /// </param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabListB"/> and must represent the current state of the prefabs.</param>
    /// <returns>Id of the added segment.</returns>
    /// <exception cref="BlockObstructedException">Thrown when the segment cannot be added, bacause it is obstructed by a block.</exception>
    public int AddSegment(int3 segmentPosition, Voxels voxels, bool overwriteBlocks, BlockInstancesCache? cache = null)
    {
        EnsureCustom();

        var segmentPos = ValidateSegmentPosition(segmentPosition);

        if (_segments.ContainsKey(segmentPos))
        {
            ThrowHelper.ThrowArgumentException($"A segment with the specified position is already in the prefab.", nameof(segmentPosition));
        }

        if (_owner is not null && !overwriteBlocks && !_owner.CanAddIdToPrefab((ushort)_id, segmentPos, cache, out var obstructionInfo))
        {
            throw new BlockObstructedException(obstructionInfo, $"Cannot add segment because it's position is obstructed and {nameof(overwriteBlocks)} is false.");
        }

        var segmentId = _id + GetNewSegmentIndex(segmentPos);

        // todo: voxels, allocate a temp storage if not in list? set to null when added to list
        _segments.Add(segmentPos, segmentId);

        CalculateSize();

        if (IsInList)
        {
            _owner.AddSegmentToPrefabInternal(this, segmentId, new SegmentData(_id, segmentPos, voxels), cache);
        }
        else
        {
            InitSegmentData();
            _segmentData.Add(segmentPos, new SegmentData(_id, segmentPos, voxels));
        }

        return segmentId;
    }

    public bool TryAddSegment(int3 segmentPosition, Voxels voxels, bool overwriteBlocks, out int segmentId, BlockInstancesCache? cache)
    {
        EnsureCustom();

        var segmentPos = ValidateSegmentPosition(segmentPosition);

        if (_segments.ContainsKey(segmentPos))
        {
            ThrowHelper.ThrowArgumentException($"A segment with the specified position is already in the prefab.", nameof(segmentPosition));
        }

        if (_owner is not null && !overwriteBlocks && !_owner.CanAddIdToPrefab((ushort)_id, segmentPos, cache, out _))
        {
            segmentId = default;
            return false;
        }

        segmentId = _id + GetNewSegmentIndex(segmentPos);

        // todo: voxels, allocate a temp storage if not in list? set to null when added to list
        _segments.Add(segmentPos, segmentId);

        CalculateSize();

        _owner?.AddSegmentToPrefabInternal(this, segmentId, new PrefabListB.SegmentData(_id, segmentPos, voxels), cache);

        return true;
    }

    public bool RemoveSegment(int3 segmentPosition, bool keepInPlace = true, BlockInstancesCache? cache = null)
        => RemoveSegment(segmentPosition, out _, out _, keepInPlace, cache);

    public bool RemoveSegment(int3 segmentPosition, out int segmentId, out int3 shift, bool keepInPlace = true, BlockInstancesCache? cache = null)
    {
        EnsureCustom();

        var segmentPos = ValidateSegmentPosition(segmentPosition);

        if (_segments.Count <= 1)
        {
            ThrowHelper.ThrowInvalidOperationException("Prefab must have at least 1 segment.");
        }

        if (!_segments.Remove(segmentPos, out var segmentIdShort))
        {
            segmentId = default;
            shift = default;
            return false;
        }

        segmentId = segmentIdShort;

        CalculateSize();
        shift = ShiftToZero();

        _owner?.RemoveSegmentFromPrefabInternal(this, segmentPosition, segmentId, shift, keepInPlace, cache);

        return true;
    }

    public int CopySegmentsTo(Span<KeyValuePair<byte3, int>> dest)
    {
        if (dest.Length < _segments.Count)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(dest), $"{nameof(dest)} must be longer than or equal to {nameof(_segments)}.");
        }

        int i = 0;
        foreach (var item in _segments)
        {
            dest[i++] = item;
        }

        return i;
    }

    internal void AddToList(PrefabListB prefabList)
    {
        Debug.Assert(!IsInList);

        _owner = prefabList;
        FreeSegmentData();
    }

    internal void PullSegmentData()
    {
        Debug.Assert(IsInList);

        InitSegmentData();

        foreach (var (segmentPosition, segmentId) in _segments)
        {
            _segmentData.Add(segmentPosition, _owner.GetSegment(segmentId));
        }
    }

    private static byte3 ValidateSegmentPosition(int3 pos, [CallerArgumentExpression(nameof(pos))] string argName = "")
    {
        int val = pos.X | pos.Y | pos.Z;
        if (val < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(argName, $"{argName} cannot be nagative.");
        }
        else if (val >= Prefab.MaxSize)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(argName, $"{argName} cannot be greater than or equal to {nameof(Prefab)}.{nameof(Prefab.MaxSize)} ({Prefab.MaxSize}).");
        }

        return (byte3)pos;
    }

    private bool TryGetSegmentData(byte3 segmentPos, [NotNullWhen(true)] out SegmentData? segmentData)
    {
        if (!_segments.TryGetValue(segmentPos, out var segmentId))
        {
            segmentData = null;
            return false;
        }

        if (_owner is not null)
        {
            return _owner.TryGetSegment(segmentId, out segmentData);
        }
        else if (_segmentData is not null)
        {
            return _segmentData.TryGetValue(segmentPos, out segmentData);
        }

        segmentData = null;
        return false;
    }

    private void EnsureCustom()
    {
        if (Id < Raw.RawGame.CurrentNumbStockPrefabs)
        {
            ThrowHelper.ThrowInvalidOperationException("Cannot edit a stock prefab.");
        }
    }

    private void CalculateSize()
    {
        Size = int3.Zero;

        foreach (var pos in _segments.Keys)
        {
            Size = int3.Max(Size, pos + int3.One);
        }
    }

    private int3 ShiftToZero()
    {
        var minPos = new int3(int.MaxValue, int.MaxValue, int.MaxValue);

        foreach (var pos in _segments.Keys)
        {
            minPos = int3.Min(minPos, pos);
        }

        for (int z = minPos.Z; z < MaxSize; z++)
        {
            for (int y = minPos.Y; y < MaxSize; y++)
            {
                for (int x = minPos.X; x < MaxSize; x++)
                {
                    var pos = new byte3(x, y, z);

                    if (_segments.TryGetValue(pos, out var segmentId))
                    {
                        _segments.Remove(pos);
                        _segments.Add((byte3)(pos - minPos), segmentId);
                    }
                }
            }
        }

        Size -= minPos;

        return minPos;
    }

    [MemberNotNull(nameof(_segmentData))]
    private void InitSegmentData()
        => _segmentData ??= SegmentDataPool.Allocate();

    private void FreeSegmentData()
    {
        if (_segmentData is null)
        {
            return;
        }

        SegmentDataPool.Free(_segmentData);
        _segmentData = null;
    }
}