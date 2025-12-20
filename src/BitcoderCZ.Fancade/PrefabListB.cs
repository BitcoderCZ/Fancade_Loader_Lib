// <copyright file="PrefabListB.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Utils;
using BitcoderCZ.Maths.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace BitcoderCZ.Fancade;

// TODO: Replace PrefabList
#pragma warning disable CS1591
#pragma warning disable SA1600
#pragma warning disable SA1203 // Constants should appear before fields
public sealed class PrefabListB
{
    private static readonly int StockCount = BitcoderCZ.Fancade.Raw.RawGame.CurrentNumbStockPrefabs;
    private const int MaxCustomCount = 512;

    private readonly ListPrefab?[] _prefabs;
    private readonly SegmentData[] _segments;

    // make setable
    private int _permanentIdCounter = StockCount;

    private int _customPrefabCount;
    private int _customSegmentCount;

    public PrefabListB()
        : this(MaxCustomCount)
    {
    }

    public PrefabListB(int maxCustomCount)
    {
        ThrowHelper.ThrowIfNegative(maxCustomCount);

        _prefabs = new ListPrefab?[StockCount + maxCustomCount];
        _segments = new SegmentData[StockCount + maxCustomCount];
    }

    private PrefabListB(ListPrefab?[] prefabs, SegmentData[] segments)
    {
        _prefabs = prefabs;
        _segments = segments;
    }

    public int CustomPrefabCount => _customPrefabCount;

    public int TotalSegmentCount => StockCount + _customSegmentCount;

    public int CustomSegmentCount => _customSegmentCount;

    public PrefabEnumerable AllPrefabs => new PrefabEnumerable(_prefabs, 0, StockCount + _customSegmentCount);

    public PrefabEnumerable StockPrefabs => new PrefabEnumerable(_prefabs, 0, StockCount);

    public PrefabEnumerable CustomPrefabs => new PrefabEnumerable(_prefabs, StockCount, _customSegmentCount);

    // TODO: extension method in editing
    public static PrefabListB CreateStock()
    {
        var list = new PrefabListB();
        list.LoadStock();
        return list;
    }

    // TODO: extension method in editing
    public void LoadStock()
    {
        var terminalBuilder = ImmutableArray.CreateBuilder<TerminalInfo>();
        foreach (var prefab in StockBlocks.PrefabList)
        {
            PrefabTerminalInfo? terminalInfo = null;

            if (StockBlocks.TryGetBlockDef(prefab.Id, out var def) && def.Terminals.Length > 0)
            {
                foreach (var terminal in def.Terminals)
                {
                    TerminalDirection direction;
                    if (terminal.Position.X == 0)
                    {
                        direction = TerminalDirection.NegativeX;
                    }
                    else if (terminal.Position.Z == 0)
                    {
                        direction = TerminalDirection.NegativeZ;
                    }
                    else if (terminal.Position.X % 8 == 6)
                    {
                        direction = TerminalDirection.PositiveX;
                    }
                    else
                    {
                        UnityEngine.Debug.Assert(terminal.Position.Z % 8 == 6);
                        direction = TerminalDirection.PositiveZ;
                    }

                    terminalBuilder.Add(new TerminalInfo(terminal.Position, terminal.SignalType, direction, terminal.Type is TerminalType.In)
                    {
                        Name = terminal.Name,
                    });
                }

                terminalInfo = new PrefabTerminalInfo(terminalBuilder.DrainToImmutable());
            }

            SetPrefabInternal(prefab, terminalInfo);
        }
    }

    public bool ContainsPrefab(int prefabId)
        => _prefabs[prefabId] is not null;

    public ListPrefab AddPrefab(string name, PrefabType type, PrefabCollider collider)
        => AddPrefab(name, type, collider, FcColorUtils.DefaultBackgroundColor, PrefabTerminalInfo.Empty, [byte3.Zero]);

    public ListPrefab AddPrefab(string name, PrefabType type, PrefabCollider collider, FcColor defaultColor, PrefabTerminalInfo terminals, ReadOnlySpan<byte3> segmentPositions)
    {
        var prefab = new ListPrefab(Interlocked.Increment(ref _permanentIdCounter), name, type, collider, defaultColor, terminals);

        prefab.Id = TotalSegmentCount;
        _prefabs[prefab.Id] = prefab;

        int segmentIndex = 0;
        foreach (var segmentPos in segmentPositions)
        {
            prefab.Segments.Add(segmentPos, prefab.Id);
            _segments[prefab.Id + segmentIndex] = new SegmentData(prefab.Id, segmentPos, Voxels.Empty);
            segmentIndex++;
        }

        _customPrefabCount++;
        _customSegmentCount += segmentPositions.Length;

        ValidateState();

        return prefab;
    }

    public ListPrefab InsertPrefab(int id, string name, PrefabType type, PrefabCollider collider, FcColor defaultColor, PrefabTerminalInfo terminals, ReadOnlySpan<byte3> segmentPositions)
    {
        EnsureCustom(id);

        if (WillBeLastPrefab(id))
        {
            return AddPrefab(name, type, collider, defaultColor, terminals, segmentPositions);
        }

        if (!ContainsPrefab(id))
        {
            ThrowHelper.ThrowArgumentException($"{nameof(PrefabList)} must contain {nameof(id)}.", nameof(id));
        }

        ShiftBlockIds(id, segmentPositions.Length);

        var prefab = new ListPrefab(Interlocked.Increment(ref _permanentIdCounter), name, type, collider, defaultColor, terminals);

        prefab.Id = TotalSegmentCount;
        _prefabs[prefab.Id] = prefab;

        int segmentIndex = 0;
        foreach (var segmentPos in segmentPositions)
        {
            prefab.Segments.Add(segmentPos, prefab.Id);
            _segments[prefab.Id + segmentIndex] = new SegmentData(prefab.Id, segmentPos, Voxels.Empty);
            segmentIndex++;
        }

        _customPrefabCount++;
        _customSegmentCount += segmentPositions.Length;

        ValidateState();

        return prefab;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureCustom(ListPrefab prefab)
    {
        if (prefab.Id < StockCount)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(prefab), $"{nameof(prefab)} cannot be stock.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureCustom(int prefabId, [CallerArgumentExpression("prefabId")] string paramName = "")
    {
        if (prefabId < StockCount)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(paramName, $"{paramName} cannot be stock.");
        }
    }

    // idShiftStart - 1 less than the first id to shift (blockId > idShiftStart)
    private void ShiftBlockIds(int idShiftStart, int shiftAmount)
    {
        Debug.Assert(shiftAmount != 0);

        Span<KeyValuePair<byte3, int>> segments = stackalloc KeyValuePair<byte3, int>[Prefab.MaxSize * Prefab.MaxSize * Prefab.MaxSize];

        for (int i = Math.Max(idShiftStart - (Prefab.MaxSize * Prefab.MaxSize * Prefab.MaxSize), 0); i < TotalSegmentCount; i++)
        {
            if (_segments[i].PrefabId >= idShiftStart)
            {
                _segments[i].PrefabId += shiftAmount;
            }

            if (_prefabs[i] is { } prefab)
            {
                prefab.Id += shiftAmount;

                Debug.Assert(prefab.Segments.Count <= segments.Length);
                int segmentCount = prefab.CopySegmentsTo(segments);

                foreach (var segment in segments[..segmentCount])
                {
                    if (segment.Value >= idShiftStart)
                    {
                        prefab.Segments[segment.Key] = segment.Value + shiftAmount;
                    }
                }
            }
        }

        _prefabs.AsSpan(idShiftStart, TotalSegmentCount - idShiftStart)
            .CopyTo(_prefabs.AsSpan(idShiftStart + shiftAmount));
        _segments.AsSpan(idShiftStart, TotalSegmentCount - idShiftStart)
            .CopyTo(_segments.AsSpan(idShiftStart + shiftAmount));

        if (shiftAmount < 0)
        {
            _prefabs.AsSpan(TotalSegmentCount + shiftAmount).Clear();
            _segments.AsSpan(TotalSegmentCount + shiftAmount).Clear();
        }
    }

    [Conditional("DEBUG")]
    private void ValidateState()
    {
        var segments = new HashSet<(int PrefabId, int3 PosInPrefab)>(StockCount + _customSegmentCount);

        // segments don't repeat
        for (int i = 0; i < StockCount + _customSegmentCount; i++)
        {
            var segment = _segments[i];

            if (!segments.Add((segment.PrefabId, segment.PosInPrefab)))
            {
                Debug.Fail($"Repeated segment, prefab id: {segment.PrefabId}, pos: {segment.PosInPrefab}")
            }
        }

        // "air"
        Debug.Assert(_segments[0] == default);

        // segments are valid up to count
        for (int i = 1; i < _segments.Length; i++)
        {
            Debug.Assert(_segments[i] != default == (i < StockCount + _customSegmentCount));
        }

        // validate prefab cound and segment order
        int customPrefabCount = 0;
        for (int i = StockCount; i < _segments.Length; i++)
        {
            if (_prefabs[i] is { } prefabData)
            {
                customPrefabCount++;

                foreach (var item in prefabData.Segments)
                {
                    var segment = _segments[item.Value];

                    Debug.Assert(segment.PrefabId == i);
                    Debug.Assert(segment.PosInPrefab == item.Key);
                }
            }
        }

        Debug.Assert(customPrefabCount == _customPrefabCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsLastPrefab(ListPrefab prefab)
          => IsLastPrefab(prefab.Id, prefab.Segments.Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsLastPrefab(int prefabId, int segmentCount)
        => prefabId + segmentCount >= TotalSegmentCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WillBeLastPrefab(ListPrefab prefab)
        => WillBeLastPrefab(prefab.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WillBeLastPrefab(int prefabId)
        => prefabId == TotalSegmentCount;

    public readonly struct PrefabEnumerable : IEnumerable<KeyValuePair<int, ListPrefab>>
    {
        private readonly ListPrefab?[] _prefabs;
        private readonly int _skip;
        private readonly int _take;

        internal PrefabEnumerable(ListPrefab?[] prefabs, int skip, int take)
        {
            _prefabs = prefabs;
            _skip = skip;
            _take = take;
        }

        public PrefabEnumerator GetEnumerator()
            => new PrefabEnumerator(_prefabs, _skip, _take);

        IEnumerator<KeyValuePair<int, ListPrefab>> IEnumerable<KeyValuePair<int, ListPrefab>>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    public struct PrefabEnumerator : IEnumerator<KeyValuePair<int, ListPrefab>>
    {
        private readonly ListPrefab?[] _prefabs;
        private readonly int _skip;
        private readonly int _take;
        private int _index;
        private KeyValuePair<int, ListPrefab> _current;

        internal PrefabEnumerator(ListPrefab?[] prefabs, int skip, int take)
        {
            _prefabs = prefabs;
            _skip = skip;
            _take = take;
            _index = -1;
            _current = default;
        }

        public readonly KeyValuePair<int, ListPrefab> Current => _current;

        readonly object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (++_index < _take)
            {
                int actualIndex = _skip + _index;
                var prefab = _prefabs[actualIndex];
                if (prefab is not null)
                {
                    _current = new KeyValuePair<int, ListPrefab>(actualIndex, prefab);
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            _index = -1;
            _current = default;
        }

        public readonly void Dispose()
        {
        }
    }

    public struct SegmentData : IEquatable<SegmentData>
    {
        public SegmentData(int prefabId, byte3 posInPrefab, Voxels voxels)
            : this(prefabId, posInPrefab, voxels, Voxels.Empty)
        {
        }

        public SegmentData(int prefabId, byte3 posInPrefab, Voxels voxels, Voxels visibleFaces)
        {
            PrefabId = prefabId;
            PosInPrefab = posInPrefab;
            Voxels = voxels;
            VisibleFaces = visibleFaces;
        }

        public int PrefabId { get; internal set; }

        public byte3 PosInPrefab { get; internal set; }

        public Voxels Voxels { get; set; }

        public Voxels VisibleFaces { get; private set; }

        public object? UserData { get; set; }

        public static bool operator ==(SegmentData left, SegmentData right)
            => left.PrefabId == right.PrefabId && left.PosInPrefab == right.PosInPrefab;

        public static bool operator !=(SegmentData left, SegmentData right)
            => !(left == right);

        public readonly bool Equals(SegmentData other)
            => this == other;

        public readonly override int GetHashCode()
            => HashCode.Combine(PrefabId, PosInPrefab);

        public readonly override bool Equals(object? obj)
            => obj is SegmentData other && Equals(other);
    }
}
