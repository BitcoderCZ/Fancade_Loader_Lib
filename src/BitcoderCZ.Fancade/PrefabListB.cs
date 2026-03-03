// <copyright file="PrefabListB.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Exceptions;
using BitcoderCZ.Fancade.Utils;
using BitcoderCZ.Maths.Vectors;
using BitcoderCZ.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BitcoderCZ.Fancade;

// TODO: Replace PrefabList
#pragma warning disable CS1591
#pragma warning disable SA1600
#pragma warning disable SA1203 // Constants should appear before fields
public sealed class PrefabListB
{
    private static readonly int StockSegmentCount = Raw.RawGame.CurrentNumbStockPrefabs;
    private static readonly int StockPrefabCount = Raw.RawGame.CurrentNumbStockGroups;
    private const int DefaultCustomCapacity = 512;
    private static readonly int DefaultCapacity = StockSegmentCount + DefaultCustomCapacity;

    private ListPrefab?[] _prefabs;
    private SegmentData[] _segments;

    // make setable
    private int _permanentIdCounter = StockSegmentCount + (1024 * 4);

    private int _customPrefabCount;
    private int _customSegmentCount;

    public PrefabListB()
        : this(DefaultCustomCapacity)
    {
    }

    public PrefabListB(int initialCustomCapacity)
    {
        ThrowHelper.ThrowIfNegative(initialCustomCapacity);

        _prefabs = new ListPrefab?[StockSegmentCount + initialCustomCapacity];
        _segments = new SegmentData[StockSegmentCount + initialCustomCapacity];
    }

    private PrefabListB(ListPrefab?[] prefabs, SegmentData[] segments)
    {
        _prefabs = prefabs;
        _segments = segments;
    }

    public int TotalPrefabCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => StockPrefabCount + _customPrefabCount;
    }

    public int CustomPrefabCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _customPrefabCount;
    }

    public int TotalSegmentCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => StockSegmentCount + _customSegmentCount;
    }

    public int CustomSegmentCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _customSegmentCount;
    }

    public int CustomSegmentCapacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TotalSegmentCapacity - StockSegmentCount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelper.ThrowIfNegative(value);
            TotalSegmentCapacity = value + StockSegmentCount;
        }
    }

    public PrefabEnumerable AllPrefabs => new(_prefabs, 0, StockSegmentCount + _customSegmentCount);

    public ReadOnlySpan<ListPrefab?> AllPrefabsSpan => _prefabs[..(StockSegmentCount + _customSegmentCount)];

    public PrefabEnumerable StockPrefabs => new(_prefabs, 0, StockSegmentCount);

    public ReadOnlySpan<ListPrefab?> StockPrefabsSpan => _prefabs[..StockSegmentCount];

    public PrefabEnumerable CustomPrefabs => new(_prefabs, StockSegmentCount, _customSegmentCount);

    public ReadOnlySpan<ListPrefab?> CustomPrefabsSpan => _prefabs[StockSegmentCount..(StockSegmentCount + _customSegmentCount)];

    public SegmentEnumerable AllSegments => new(_segments, 0, StockSegmentCount + _customSegmentCount);

    public Span<SegmentData> AllSegmentsSpan => _segments[..(StockSegmentCount + _customSegmentCount)];

    public SegmentEnumerable StockSegments => new(_segments, 0, StockSegmentCount);

    public Span<SegmentData> StockSegmentsSpan => _segments[..StockSegmentCount];

    public SegmentEnumerable CustomSegments => new(_segments, StockSegmentCount, _customSegmentCount);

    public Span<SegmentData> CustomSegmentsSpan => _segments[StockSegmentCount..(StockSegmentCount + _customSegmentCount)];

    internal int TotalSegmentCapacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _segments.Length;
        set
        {
            ThrowHelper.ThrowIfLessThan(value, TotalSegmentCount);

            if (value != _segments.Length)
            {
                var newSegments = new SegmentData[value];
                Array.Copy(_segments, newSegments, TotalSegmentCount);
                _segments = newSegments;

                var newPrefabs = new ListPrefab?[value];
                Array.Copy(_prefabs, newPrefabs, TotalSegmentCount);
                _prefabs = newPrefabs;
            }
        }
    }

    // TODO: extension method in editing
    public static PrefabListB CreateStock()
    {
        var list = new PrefabListB();
        /*list.LoadStock();*/
        return list;
    }

    // TODO: extension method in editing
    /*public void LoadStock()
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
    }*/

    public bool ContainsPrefab(int prefabId)
        => PrefabIdInBounds(prefabId) && _prefabs[prefabId] is not null;

    public bool ContainsSegment(int segmentId)
        => SegmentIdInBounds(segmentId) && (segmentId is 0 || _segments[segmentId].PrefabId != 0);

    public ListPrefab GetPrefab(int prefabId)
    {
        ListPrefab? prefab = null;
        if (!PrefabIdInBounds(prefabId) || (prefab = _prefabs[prefabId]) is null)
        {
            ThrowHelper.ThrowKeyNotFound(prefabId);
        }

        return prefab;
    }

    public SegmentData GetSegment(int segmentId)
        => _segments[segmentId];

    public bool TryGetPrefab(int prefabId, [MaybeNullWhen(false)] out ListPrefab prefab)
    {
        if (!PrefabIdInBounds(prefabId) || (prefab = _prefabs[prefabId]) is null)
        {
            prefab = null;
            return false;
        }

        return true;
    }

    public bool TryGetSegment(int segmentId, [MaybeNullWhen(false)] out SegmentData segment)
    {
        if (!SegmentIdInBounds(segmentId))
        {
            segment = null;
            return false;
        }

        segment = _segments[segmentId];
        return true;
    }

    public ListPrefab AddPrefab(string name, PrefabType type, PrefabCollider collider)
        => AddPrefab(name, type, collider, FcColorUtils.DefaultBackgroundColor, PrefabTerminalInfo.Empty, [byte3.Zero]);

    public ListPrefab AddPrefab(string name, PrefabType type, PrefabCollider collider, FcColor backgroundColor, PrefabTerminalInfo terminals, ReadOnlySpan<byte3> segmentPositions)
    {
        Validator.FancadeStringNonEmpty(name);

        Span<byte3> sortedSegmentPositions = stackalloc byte3[Prefab.MaxSegmentCount];
        ValidateAndSortSegments(segmentPositions, ref sortedSegmentPositions);

        var prefab = new ListPrefab(this, TotalSegmentCount, Interlocked.Increment(ref _permanentIdCounter), name, type, collider, backgroundColor, terminals);

        if (prefab.Id + segmentPositions.Length >= _segments.Length)
        {
            Grow(TotalSegmentCount + prefab.Id + segmentPositions.Length);
        }

        _prefabs[prefab.Id] = prefab;

        int segmentIndex = 0;
        foreach (var segmentPos in sortedSegmentPositions)
        {
            prefab._segments.Add(segmentPos, prefab.Id);
            _segments[prefab.Id + segmentIndex] = new SegmentData(prefab.Id, segmentPos, Voxels.Empty);
            segmentIndex++;
        }

        _customPrefabCount++;
        _customSegmentCount += segmentPositions.Length;

        ValidateState();

        return prefab;
    }

    public void AddExisting(ListPrefab prefab)
    {
        EnsureCustom(prefab);

        if (prefab._owner is not null)
        {
            ThrowHelper.ThrowArgumentException($"{nameof(prefab)} is already in another list.", nameof(prefab));
        }

        _permanentIdCounter = Math.Max(_permanentIdCounter, prefab.PermanentId + 1);
        throw new NotImplementedException();
    }

    public ListPrefab InsertPrefab(int id, string name, PrefabType type, PrefabCollider collider)
        => InsertPrefab(id, name, type, collider, FcColorUtils.DefaultBackgroundColor, PrefabTerminalInfo.Empty, [byte3.Zero]);

    public ListPrefab InsertPrefab(int id, string name, PrefabType type, PrefabCollider collider, FcColor backgroundColor, PrefabTerminalInfo terminals, ReadOnlySpan<byte3> segmentPositions)
    {
        EnsureCustom(id);

        if (WillBeLastPrefab(id))
        {
            return AddPrefab(name, type, collider, backgroundColor, terminals, segmentPositions);
        }

        if (!ContainsPrefab(id))
        {
            ThrowHelper.ThrowArgumentException($"{nameof(PrefabList)} must contain {nameof(id)}.", nameof(id));
        }

        Validator.FancadeStringNonEmpty(name);

        Span<byte3> sortedSegmentPositions = stackalloc byte3[Prefab.MaxSegmentCount];
        ValidateAndSortSegments(segmentPositions, ref sortedSegmentPositions);

        if (id + segmentPositions.Length >= _segments.Length)
        {
            Grow(TotalSegmentCount + id + segmentPositions.Length);
        }

        ShiftBlockIds(id, segmentPositions.Length);

        var prefab = new ListPrefab(this, TotalSegmentCount, Interlocked.Increment(ref _permanentIdCounter), name, type, collider, backgroundColor, terminals);

        _prefabs[prefab.Id] = prefab;

        int segmentIndex = 0;
        foreach (var segmentPos in sortedSegmentPositions)
        {
            prefab._segments.Add(segmentPos, prefab.Id);
            _segments[prefab.Id + segmentIndex] = new SegmentData(prefab.Id, segmentPos, Voxels.Empty);
            segmentIndex++;
        }

        _customPrefabCount++;
        _customSegmentCount += segmentPositions.Length;

        ValidateState();

        return prefab;
    }

    public bool RemovePrefab(int prefabId, BlockInstancesCache? cache = null)
        => RemovePrefab(prefabId, out _, cache);

    public bool RemovePrefab(int prefabId, [MaybeNullWhen(false)] out ListPrefab prefab, BlockInstancesCache? cache = null)
    {
        EnsureCustom(prefabId);

        if (!TryGetPrefab(prefabId, out prefab))
        {
            return false;
        }

        RemovePrefabFromBlocks(prefab, cache);

        if (IsLastPrefab(prefab))
        {
            _prefabs[prefabId] = null;
            _segments.AsSpan(prefabId, prefab.SegmentCount).Clear();
        }
        else
        {
            ShiftBlockIds(prefabId + prefab.SegmentCount, -prefab.SegmentCount);
        }

        _customPrefabCount--;
        _customSegmentCount -= prefab.SegmentCount;

        prefab._owner = null;
        return true;
    }

    /// <summary>
    /// Determines if a segment can be added to a prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <param name="segmentPosition">Position of the segment.</param>
    /// <param name="overwriteBlocks">
    /// If <see langword="true"/>, block overwritting is allowed,
    /// if <see langword="false"/>, if the segment would be placed at a position that is already occupied, <see langword="false"/> is returned.
    /// </param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabListB"/> and must represent the current state of the prefabs.</param>
    /// <returns><see langword="true"/> if <paramref name="segmentPosition"/> can be added to the prefab; otherwise <see langword="false"/>.</returns>
    public bool CanAddSegmentToPrefab(int id, int3 segmentPosition, bool overwriteBlocks, BlockInstancesCache? cache = null)
        => TryGetPrefab(id, out var prefab) &&
            (overwriteBlocks || CanAddIdToPrefab((ushort)id, segmentPosition, cache, out _)) &&
            !prefab.ContainsSegment(segmentPosition);

    /// <summary>
    /// Adds a segment to a prefab.
    /// </summary>
    /// <param name="prefabId">Id of the prefab.</param>
    /// <param name="segmentPosition">Position of the segment to add.</param>
    /// <param name="voxels">Voxels of the segment to add.</param>
    /// <param name="overwriteBlocks">
    /// If <see langword="true"/>, blocks will be overwritten,
    /// if <see langword="false"/>, if the segment would be placed at a position that is already occupied, an <see cref="BlockObstructedException"/> will be thrown.
    /// </param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabListB"/> and must represent the current state of the prefabs.</param>
    /// <returns>Id of the added segment.</returns>
    /// <exception cref="BlockObstructedException">Thrown when the segment cannot be added, bacause it is obstructed by a block.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the list does not contain the prefab with the specified id.</exception>
    public int AddSegmentToPrefab(int prefabId, int3 segmentPosition, Voxels voxels, bool overwriteBlocks, BlockInstancesCache? cache = null)
    {
        var prefab = GetPrefab(prefabId);

        return prefab.AddSegment(segmentPosition, voxels, overwriteBlocks, cache);
    }

    /// <summary>
    /// Adds a segment to a prefab.
    /// </summary>
    /// <param name="prefabId">Id of the prefab.</param>
    /// <param name="segmentPosition">Position of the segment to add.</param>
    /// <param name="voxels">Voxels of the segment to add.</param>
    /// <param name="overwriteBlocks">
    /// If <see langword="true"/>, blocks will be overwritten,
    /// if <see langword="false"/>, if the segment would be placed at a position that is already occupied, <see langword="false"/> is returned.
    /// </param>
    /// <param name="segmentId">Id of the added segment.</param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabListB"/> and must represent the current state of the prefabs.</param>
    /// <returns><see langword="true"/> if the segment was added to the prefab; otherwise <see langword="false"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the list does not contain the prefab with the specified id.</exception>
    public bool TryAddSegmentToPrefab(int prefabId, int3 segmentPosition, Voxels voxels, bool overwriteBlocks, out int segmentId, BlockInstancesCache? cache = null)
    {
        EnsureCustom(prefabId);

        var prefab = GetPrefab(prefabId);

        return prefab.TryAddSegmentToPrefab(segmentPosition, voxels, overwriteBlocks, out segmentId, cache);
    }

    /// <summary>
    /// Removes a segment from a prefab.
    /// </summary>
    /// <param name="prefabId">Id of the prefab.</param>
    /// <param name="segmentPosition">Position of the segment to remove.</param>
    /// <param name="keepInPlace">
    /// If <see langword="true"/>, the prefab will be moved back, keeping the position the same;
    /// if <see langword="false"/>, the prefab may "move" (segments stay in the same place, but the postion increases).
    /// </param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <returns><see langword="true"/> if the segment was removed from the prefab; otherwise <see langword="false"/>.</returns>
    public bool RemoveSegmentFromPrefab(int prefabId, int3 segmentPosition, bool keepInPlace = true, BlockInstancesCache? cache = null)
        => RemoveSegmentFromPrefab(prefabId, segmentPosition, out _, out _, keepInPlace, cache);

    /// <summary>
    /// Removes a segment from a prefab.
    /// </summary>
    /// <param name="prefabId">Id of the prefab.</param>
    /// <param name="segmentPosition">Position of the segment to remove.</param>
    /// <param name="segmentId">Id of the removed segment.</param>
    /// <param name="shift">Specifies by how much the prefab moved, always positive.</param>
    /// <param name="keepInPlace">
    /// If <see langword="true"/>, the prefab will be moved back, keeping the position the same;
    /// if <see langword="false"/>, the prefab may "move" (segments stay in the same place, but the postion increases).
    /// </param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <returns><see langword="true"/> if the segment was removed from the prefab; otherwise <see langword="false"/>.</returns>
    public bool RemoveSegmentFromPrefab(int prefabId, int3 segmentPosition, out int segmentId, out int3 shift, bool keepInPlace = true, BlockInstancesCache? cache = null)
    {
        var prefab = GetPrefab(prefabId);
        return prefab.RemoveSegment(segmentPosition, out segmentId, out shift, keepInPlace, cache);
    }

    /// <summary>
    /// Ensures that the capacity of this list is at least the specified <paramref name="customSegmentCount"/>.
    /// </summary>
    /// <param name="customSegmentCount">The minimum capacity to ensure.</param>
    /// <returns>The new custom segment capacity.</returns>
    public int EnsureCapacity(int customSegmentCount)
    {
        ThrowHelper.ThrowIfNegative(customSegmentCount);

        int capacity = Math.Max(_segments.Length, customSegmentCount + StockSegmentCount);

        if (_segments.Length < capacity)
        {
            Grow(capacity);
        }

        return CustomSegmentCapacity;
    }

    public void Clear()
    {
        foreach (var prefab in _prefabs.AsSpan(StockSegmentCount, _customSegmentCount))
        {
            if (prefab is not null)
            {
                prefab._owner = null;
            }
        }

        _prefabs.AsSpan(StockSegmentCount).Clear();
        _segments.AsSpan(StockSegmentCount).Clear();
        _customPrefabCount = 0;
        _customSegmentCount = 0;

        ValidateState();
    }

    internal void Grow(int totalSegmentCapacity)
        => TotalSegmentCapacity = GetNewCapacity(totalSegmentCapacity);

    internal void AddSegmentToPrefabInternal(ListPrefab prefab, int segmentId, SegmentData value, BlockInstancesCache? cache)
    {
        if (!IsLastPrefab(prefab))
        {
            ShiftBlockIds(segmentId, 1);
        }

        _segments[segmentId] = value;
        AddIdToPrefab((ushort)prefab._id, value.PosInPrefab, (ushort)segmentId, cache);

        ValidateState();
    }

    internal void RemoveSegmentFromPrefabInternal(ListPrefab prefab, int3 segmentPosition, int segmentId, int3 shift, bool keepInPlace, BlockInstancesCache? cache)
    {
        _segments[segmentId] = null!;
        RemoveIdFromPrefab(id, posInPrefab, cache);

        if (segmentId == TotalSegmentCount)
        {
            return;
        }

        ShiftBlockIds(segmentId + 1, -1);

        if (shift != int3.Zero)
        {
            if (keepInPlace)
            {
                ShiftPrefabRemovedSegment(prefab, segmentPosition, -shift, false, cache);
            }
            else
            {
                cache?.MovePositions(shift);
            }
        }

        ValidateState();
    }

    internal bool RemovePrefabFromBlocks(ListPrefab prefab, BlockInstancesCache? cache = null)
    {
        Debug.Assert(cache is null || cache.BlockId == prefab.Id, "The cache should be for the prefab.");

        Debug.Assert(prefab.SegmentCount <= 4 * 4 * 4, "prefab.Count should be smaller that it's max size.");
        Span<int3> offsets = stackalloc int3[4 * 4 * 4];

        int len = 0;
        foreach (var segment in prefab._segments)
        {
            offsets[len++] = segment.Key;
        }

        offsets = offsets[..len];

        return RemoveIdsFromPrefab((ushort)prefab._id, offsets, cache);
    }

    internal bool RemoveIdsFromPrefab(ushort prefabId, ReadOnlySpan<int3> offsets, BlockInstancesCache? cache)
    {
        if (cache is not null)
        {
            cache.RemoveBlocks(offsets);
            return !cache.IsEmpty;
        }

        bool found = false;

        foreach (var prefab in _prefabs)
        {
            if (prefab is null)
            {
                continue;
            }

            unsafe
            {
                fixed (int3* offsetsPtr = offsets)
                {
                    var action = new RemoveIdsFromPrefabAction(prefabId, prefab.Blocks, offsetsPtr, offsets.Length);
                    prefab.Blocks.EnumerateNonEmptyBlocks(ref action);
                    if (!found)
                    {
                        found = action.Found;
                    }
                }
            }
        }

        return found;
    }

    internal bool CanAddIdToPrefab(ushort prefabId, int3 offset, BlockInstancesCache? cache, out BlockObstructionInfo obstructionInfo)
    {
        if (cache is not null)
        {
            return cache.CanAddBlock(offset, out obstructionInfo);
        }

        foreach (var prefab in _prefabs)
        {
            if (prefab is null)
            {
                continue;
            }

            var action = new CanAddIdToPrefabFunc(prefabId, offset, prefab.Blocks);
            prefab.Blocks.EnumerateNonEmptyBlocksWithBreak(ref action);

            if (action.ObstructionInfo is not null)
            {
                obstructionInfo = action.ObstructionInfo.Value;
                return false;
            }
        }

        obstructionInfo = default;
        return true;
    }

    internal void AddIdToPrefab(ushort prefabId, int3 offset, ushort id, BlockInstancesCache? cache)
    {
        if (cache is not null)
        {
            cache.AddBlock(this, offset, id);
            return;
        }

        foreach (var prefab in _prefabs)
        {
            if (prefab is null)
            {
                continue;
            }

            var action = new AddIdToPrefabAction(prefabId, id, offset, prefab.Blocks, this);
            prefab.Blocks.EnumerateNonEmptyBlocks(ref action);
        }
    }

    // todo: update - allows chaning multiple segments at a time
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureCustom(ListPrefab prefab)
    {
        if (prefab.Id < StockSegmentCount)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(prefab), $"{nameof(prefab)} cannot be a stock prefab.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureCustom(int prefabId, [CallerArgumentExpression("prefabId")] string paramName = "")
    {
        if (prefabId < StockSegmentCount)
        {
            // todo: custom exception type
            ThrowHelper.ThrowArgumentOutOfRangeException(paramName, $"{paramName} cannot be the id of a stock prefab.");
        }
    }

    private static void ValidateAndSortSegments(ReadOnlySpan<byte3> segmentPositions, ref Span<byte3> sortedSegmentPositions, [CallerArgumentExpression("segmentPositions")] string segmentsName = "")
    {
        if (segmentPositions.IsEmpty)
        {
            ThrowHelper.ThrowArgumentException($"{segmentsName} must not be empty.", segmentsName);
        }
        else if (segmentPositions.Length > Prefab.MaxSegmentCount)
        {
            ThrowHelper.ThrowArgumentException($"{segmentsName}.Length must not be larger than {nameof(Prefab)}.{nameof(Prefab.MaxSegmentCount)}", segmentsName);
        }

        sortedSegmentPositions = sortedSegmentPositions[..segmentPositions.Length];
        segmentPositions.CopyTo(sortedSegmentPositions);
        sortedSegmentPositions.Sort(PositionComparer.Instance);

        var lastValue = new byte3(byte.MaxValue, byte.MaxValue, byte.MaxValue);
        foreach (var pos in sortedSegmentPositions)
        {
            if (pos == lastValue)
            {
                ThrowHelper.ThrowArgumentException($"{segmentsName} must not contain duplicate values.", segmentsName);
            }
            else if (pos.X >= Prefab.MaxSize || pos.Y >= Prefab.MaxSize || pos.Z >= Prefab.MaxSize)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException($"All {segmentsName} elements must be in bounds of {nameof(Prefab)}.{nameof(Prefab.MaxSize)}.", segmentsName);
            }

            lastValue = pos;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetNewCapacity(int totalSegmentCapacity)
    {
        Debug.Assert(_segments.Length < totalSegmentCapacity);

        int newCapacity = _segments.Length == StockSegmentCount ? DefaultCapacity : 2 * _segments.Length;

        // If the computed capacity is still less than specified, set to the original argument.
        if (newCapacity < totalSegmentCapacity)
        {
            newCapacity = totalSegmentCapacity;
        }

        return newCapacity;
    }

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
                prefab._id += shiftAmount;

                Debug.Assert(prefab._segments.Count <= segments.Length);
                int segmentCount = prefab.CopySegmentsTo(segments);

                foreach (var segment in segments[..segmentCount])
                {
                    if (segment.Value >= idShiftStart)
                    {
                        prefab._segments[segment.Key] = segment.Value + shiftAmount;
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
            int clearStart = TotalSegmentCount + shiftAmount; // shiftAmount is negative
            int clearLength = -shiftAmount;

            // clear end
            _prefabs.AsSpan(clearStart, clearLength).Clear();
            _segments.AsSpan(clearStart, clearLength).Clear();
        }
    }

    [Conditional("DEBUG")]
    private void ValidateState()
    {
        var segments = new HashSet<(int PrefabId, int3 PosInPrefab)>(StockSegmentCount + _customSegmentCount);

        // segments don't repeat
        for (int i = 0; i < StockSegmentCount + _customSegmentCount; i++)
        {
            var segment = _segments[i];

            if (!segments.Add((segment.PrefabId, segment.PosInPrefab)))
            {
                Debug.Fail($"Repeated segment, prefab id: {segment.PrefabId}, pos: {segment.PosInPrefab}");
            }
        }

        // "air"
        Debug.Assert(_segments[0] is { PrefabId: 0 });

        // segments are valid up to count
        for (int i = 1; i < _segments.Length; i++)
        {
            Debug.Assert((_segments[i] is not null) == (i < StockSegmentCount + _customSegmentCount));
        }

        // validate prefab cound and segment order
        int customPrefabCount = 0;
        for (int i = StockSegmentCount; i < _segments.Length; i++)
        {
            if (_prefabs[i] is { } prefabData)
            {
                Debug.Assert(prefabData.Id == i);

                customPrefabCount++;

                foreach (var item in prefabData._segments)
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
          => IsLastPrefab(prefab.Id, prefab._segments.Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsLastPrefab(int prefabId, int segmentCount)
        => prefabId + segmentCount >= TotalSegmentCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WillBeLastPrefab(ListPrefab prefab)
        => WillBeLastPrefab(prefab.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WillBeLastPrefab(int prefabId)
        => prefabId == TotalSegmentCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool PrefabIdInBounds(int prefabId)
        => (uint)prefabId < (uint)TotalSegmentCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SegmentIdInBounds(int segmentId)
        => (uint)segmentId < (uint)TotalSegmentCount;

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

    public readonly struct SegmentEnumerable : IEnumerable<KeyValuePair<int, SegmentData>>
    {
        private readonly SegmentData[] _segments;
        private readonly int _skip;
        private readonly int _take;

        internal SegmentEnumerable(SegmentData[] segments, int skip, int take)
        {
            _segments = segments;
            _skip = skip;
            _take = take;
        }

        public SegmentEnumerator GetEnumerator()
            => new SegmentEnumerator(_segments, _skip, _take);

        IEnumerator<KeyValuePair<int, SegmentData>> IEnumerable<KeyValuePair<int, SegmentData>>.GetEnumerator()
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

    public struct SegmentEnumerator : IEnumerator<KeyValuePair<int, SegmentData>>
    {
        private readonly SegmentData[] _segments;
        private readonly int _skip;
        private readonly int _take;
        private int _index;
        private KeyValuePair<int, SegmentData> _current;

        internal SegmentEnumerator(SegmentData[] segments, int skip, int take)
        {
            _segments = segments;
            _skip = skip;
            _take = take;
            _index = -1;
            _current = default;
        }

        public readonly KeyValuePair<int, SegmentData> Current => _current;

        readonly object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (++_index < _take)
            {
                int actualIndex = _skip + _index;
                _current = new(actualIndex, _segments[actualIndex]);
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

    private struct CanAddIdToPrefabFunc : IRefValueFunc<ushort, int3, bool>
    {
        private readonly ushort _prefabId;
        private readonly int3 _offset;
        private readonly IBlockData _blocks;

        public CanAddIdToPrefabFunc(ushort prefabId, int3 offset, IBlockData blocks)
        {
            _prefabId = prefabId;
            _offset = offset;
            _blocks = blocks;
        }

        public BlockObstructionInfo? ObstructionInfo { get; private set; }

        public bool Invoke(ref ushort segmentId, int3 pos)
        {
            if (segmentId == _prefabId)
            {
                if (_blocks.GetBlock(pos + _offset) != 0)
                {
                    ObstructionInfo = new BlockObstructionInfo(_prefabId, pos, pos + _offset);
                    return true;
                }
            }

            return false;
        }
    }

    private readonly struct AddIdToPrefabAction : IRefValueAction<ushort, int3>
    {
        private readonly ushort _prefabId;
        private readonly ushort _idToAdd;
        private readonly int3 _offset;
        private readonly IBlockData _blocks;
        private readonly PrefabListB _prefabs;

        public AddIdToPrefabAction(ushort prefabId, ushort idToAdd, int3 offset, IBlockData blocks, PrefabListB prefabs)
        {
            _prefabId = prefabId;
            _idToAdd = idToAdd;
            _offset = offset;
            _blocks = blocks;
            _prefabs = prefabs;
        }

        public readonly void Invoke(ref ushort segmentId, int3 pos)
        {
            if (segmentId == _prefabId)
            {
                ushort idOld = _blocks.GetBlock(pos + _offset);

                if (idOld != 0 && _prefabs.TryGetPrefab(idOld, out var oldPrefab))
                {
                    var prefabPos = pos + _offset - _prefabs.GetSegment(idOld).PosInPrefab;

                    foreach (var segment in oldPrefab._segments)
                    {
                        _blocks.SetBlock(prefabPos + segment.Key, 0);
                    }
                }

                _blocks.SetBlock(pos + _offset, _idToAdd);
            }
        }
    }

    private unsafe struct RemoveIdsFromPrefabAction : IRefValueAction<ushort, int3>
    {
        private readonly ushort _prefabId;
        private readonly IBlockData _blocks;
        private readonly int3* _offsets;
        private readonly int _offsetsLength;

        public RemoveIdsFromPrefabAction(ushort prefabId, IBlockData blocks, int3* offsets, int offsetsLength)
        {
            _prefabId = prefabId;
            _blocks = blocks;
            _offsets = offsets;
            _offsetsLength = offsetsLength;
        }

        public bool Found { get; private set; } = false;

        public void Invoke(ref ushort segmentId, int3 pos)
        {
            if (segmentId == _prefabId)
            {
                Found = true;

                for (int i = 0; i < _offsetsLength; i++)
                {
                    _blocks.SetBlock(pos + _offsets[i], 0);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    public class SegmentData : IEquatable<SegmentData>
    {
        internal Voxels _voxels;
        internal Voxels _visibleFaces;

        public SegmentData(int prefabId, byte3 posInPrefab, Voxels voxels)
            : this(prefabId, posInPrefab, voxels, Fancade.Voxels.Empty)
        {
        }

        public SegmentData(int prefabId, byte3 posInPrefab, Voxels voxels, Voxels visibleFaces)
        {
            PrefabId = prefabId;
            PosInPrefab = posInPrefab;
            _voxels = voxels;
            _visibleFaces = visibleFaces;
        }

        public int PrefabId { get; internal set; }

        public byte3 PosInPrefab { get; internal set; }

        public ReadOnlyVoxels VoxelsView => new(_voxels);

        public Voxels Voxels
        {
            get
            {
                if (PrefabId < StockSegmentCount)
                {
                    ThrowHelper.ThrowInvalidOperationException($"Cannot get writable voxels of a stock prefab, use {nameof(VoxelsView)} instead.");
                }

                return _voxels;
            }

            set
            {
                if (PrefabId < StockSegmentCount)
                {
                    ThrowHelper.ThrowInvalidOperationException("Cannot set the voxels of a stock prefab.");
                }

                _voxels = value;
            }
        }

        public ReadOnlyVoxels VisibleFaces => new(_visibleFaces);

        public object? UserData { get; set; }

        public static bool operator ==(SegmentData left, SegmentData right)
            => left?.Equals(right) ?? ReferenceEquals(left, right);

        public static bool operator !=(SegmentData left, SegmentData right)
            => !(left == right);

        public bool Equals(SegmentData? other)
            => other is not null && PrefabId == other.PrefabId && PosInPrefab == other.PosInPrefab;

        public override int GetHashCode()
            => HashCode.Combine(PrefabId, PosInPrefab);

        public override bool Equals(object? obj)
            => obj is SegmentData other && Equals(other);
    }
}
