// <copyright file="ListPrefab.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;
using BitcoderCZ.Fancade.Data;
using BitcoderCZ.Fancade.Exceptions;
using BitcoderCZ.Fancade.Utils;
using BitcoderCZ.Maths.Vectors;
using BitcoderCZ.Utils;

namespace BitcoderCZ.Fancade;

#pragma warning disable CS1591
#pragma warning disable SA1600

// can exist on it's own, or in (only) 1 list
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
    public bool IsInList => _owner is not null;

    public int Id => _id; // todo: editable when not in prefab

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

    public IBlockData Blocks { get; } = new ArrayBlockData();

    public IEnumerable<KeyValuePair<byte3, int>> Segments => _segments;

    public int SegmentCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _segments.Count;
    }

    // TODO: cache?
    public int3 Size
    {
        get
        {
            var maxPos = byte3.Zero;
            foreach (var pos in _segments.Keys)
            {
                maxPos = byte3.Max(maxPos, pos);
            }

            return maxPos + byte3.One;
        }
    }

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
    /// Determines the index of a segment, if it was at the specified position.
    /// </summary>
    /// <param name="key">Position of the segment.</param>
    /// <returns>The index of the segment if it is in bounds; otherwise, <c>-1</c>.</returns>
    public int GetNewSegmentIndex(int3 key)
    {
        int index = 0;

        for (int z = 0; z < MaxSize; z++)
        {
            for (int y = 0; y < MaxSize; y++)
            {
                for (int x = 0; x < MaxSize; x++)
                {
                    var pos = new byte3(x, y, z);

                    if (pos == key)
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

    public int AddSegment(int3 segmentPosition, Voxels voxels, bool overwriteBlocks, BlockInstancesCache? cache = null)
    {
        EnsureCustom();

        var segmentPos = ValidateSegmentPosition(segmentPosition);

        if (_segments.ContainsKey(segmentPos))
        {
            ThrowHelper.ThrowArgumentException($"A segment with the specified position is already in the prefab.", nameof(segmentPosition));
        }

        if (_owner is not null && !overwriteBlocks && !_owner.CanAddIdToPrefab(_id, segmentPos, cache, out var obstructionInfo))
        {
            throw new BlockObstructedException(obstructionInfo, $"Cannot add segment because it's position is obstructed and {nameof(overwriteBlocks)} is false.");
        }

        var segmentId = _id + GetNewSegmentIndex(segmentPos);

        // todo: voxels
        _segments.Add(segmentPos, segmentId);

        _owner?.AddSegmentToPrefabInternal(this, segmentId, new PrefabListB.SegmentData(_id, segmentPos, voxels), cache);

        return segmentId;
    }

    public bool RemoveSegment(int3 segmentPosition, out int segmentId, out int3 shift)
    {
        EnsureCustom();
        throw new NotImplementedException(); // _owner.RemoveSegmentInternal, ensure custom
        if (_segments.Count <= 1)
        {
            segmentId = 0;
            shift = int3.Zero;
            return false;
        }

        bool removed = _segments.Remove(posInPrefab, out segmentId);

        if (removed)
        {
            shift = ShiftToZero();
        }
        else
        {
            shift = int3.Zero;
        }

        return removed;
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

    private void EnsureCustom()
    {
        if (Id < Raw.RawGame.CurrentNumbStockPrefabs)
        {
            ThrowHelper.ThrowInvalidOperationException("Cannot edit a stock prefab.");
        }
    }

    private int3 ShiftToZero()
    {
        var minPos = new int3(int.MaxValue, int.MaxValue, int.MaxValue);

        foreach (var pos in _segments.Keys)
        {
            minPos = int3.Min(minPos, pos);
        }

        if (minPos == int3.Zero)
        {
            return int3.Zero;
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

        return minPos;
    }
}