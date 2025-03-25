// <copyright file="PrefabList.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib;

/// <summary>
/// <see cref="List{T}"/> wrapper for easier <see cref="Prefab"/> manipulation.
/// </summary>
/// <remarks>
/// Ids are automatically changed when prefabs are inserter/removed.
/// </remarks>
public class PrefabList : ICloneable
{
    internal readonly Dictionary<ushort, Prefab> _prefabs;
    internal readonly List<PrefabSegment> _segments;

    private ushort _idOffset = RawGame.CurrentNumbStockPrefabs;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabList"/> class.
    /// </summary>
    public PrefabList()
    {
        _prefabs = [];
        _segments = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabList"/> class.
    /// </summary>
    /// <param name="prefabCapacity">The initial prefab capacity.</param>
    /// <param name="segmentCapacity">The initial segment capacity.</param>
    public PrefabList(int prefabCapacity, int segmentCapacity)
    {
        _prefabs = new(prefabCapacity);
        _segments = new(segmentCapacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabList"/> class.
    /// </summary>
    /// <param name="prefabs">The <see cref="IEnumerable{T}"/> whose elements are copied to the new <see cref="PrefabList"/>.</param>
    public PrefabList(IEnumerable<Prefab> prefabs)
    {
        ThrowIfNull(prefabs, nameof(prefabs));

        _prefabs = prefabs.ToDictionary(group => group.Id);
        ValidatePrefabs(_prefabs.Values, nameof(prefabs)); // validate using _prefabs.Values to avoid iterating over collection multiple times

        _segments = [.. SegmentsFromPrefabs(_prefabs)];

        _idOffset = _prefabs.Min(item => item.Key);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabList"/> class.
    /// </summary>
    /// <param name="other">A <see cref="PrefabList"/> to copy values from.</param>
    /// <param name="deepCopy">If deep copy should be performed.</param>
    public PrefabList(PrefabList other, bool deepCopy)
    {
        ThrowIfNull(other, nameof(other));

        _idOffset = other.IdOffset;

        if (deepCopy)
        {
            _prefabs = other._prefabs.ToDictionary(item => item.Key, item => item.Value.Clone(true));
            _segments = [.. SegmentsFromPrefabs(_prefabs)];
        }
        else
        {
            _prefabs = new(other._prefabs);
            _segments = [.. SegmentsFromPrefabs(_prefabs)];
        }
    }

    private PrefabList(Dictionary<ushort, Prefab> dict, ushort idOffset)
    {
        _prefabs = dict;
        _segments = [.. SegmentsFromPrefabs(_prefabs)];
        _idOffset = idOffset;
    }

    /// <summary>
    /// Gets or sets the id offset of this list, <see cref="RawGame.CurrentNumbStockPrefabs"/> by default.
    /// Specifies the lowest prefab id.
    /// </summary>
    /// <value>Id offset of this list.</value>
    public ushort IdOffset
    {
        get => _idOffset;
        set
        {
            if (value < _idOffset)
            {
                DecreaseAfter(0, (ushort)(_idOffset - value));
                _idOffset = value;
            }
            else if (value > _idOffset)
            {
                IncreaseAfter(0, (ushort)(value - _idOffset));
                _idOffset = value;
            }
        }
    }

    /// <summary>
    /// Gets the number of prefabs in the <see cref="PrefabList"/>.
    /// </summary>
    /// <value>The number of prefabs in the <see cref="PrefabList"/>.</value>
    public int PrefabCount => _prefabs.Count;

    /// <summary>
    /// Gets the number of segments in the <see cref="PrefabList"/>.
    /// </summary>
    /// <value>The number of segments in the <see cref="PrefabList"/>.</value>
    public int SegmentCount => _segments.Count;

    /// <summary>
    /// Gets an <see cref="IEnumerable{T}"/> containing the prefabs in the <see cref="PrefabList"/>.
    /// </summary>
    /// <value>An <see cref="IEnumerable{T}"/> containing the prefabs in the <see cref="PrefabList"/>.</value>
    public IEnumerable<Prefab> Prefabs => _prefabs.Values;

    /// <summary>
    /// Gets an <see cref="IEnumerable{T}"/> containing the segments in the <see cref="PrefabList"/>.
    /// </summary>
    /// <value>An <see cref="IEnumerable{T}"/> containing the segments in the <see cref="PrefabList"/>.</value>
    public IEnumerable<PrefabSegment> Segments => _segments;

    /// <summary>
    /// Loads a <see cref="PrefabList"/> from a <see cref="FcBinaryReader"/>.
    /// </summary>
    /// <param name="reader">The reader to read the <see cref="PrefabList"/> from.</param>
    /// <returns>A <see cref="PrefabList"/> read from <paramref name="reader"/>.</returns>
    public static PrefabList Load(FcBinaryReader reader)
    {
        ThrowIfNull(reader, nameof(reader));

        uint count = reader.ReadUInt32();
        ushort idOffset = reader.ReadUInt16();

        RawPrefab[] rawPrefabs = new RawPrefab[count];

        for (int i = 0; i < count; i++)
        {
            rawPrefabs[i] = RawPrefab.Load(reader);
        }

        Dictionary<ushort, Prefab> prefabs = [];

        for (int i = 0; i < rawPrefabs.Length; i++)
        {
            if (rawPrefabs[i].IsInGroup)
            {
                int startIndex = i;
                ushort groupId = rawPrefabs[i].GroupId;
                do
                {
                    i++;
                } while (i < count && rawPrefabs[i].GroupId == groupId);

                ushort id = (ushort)(startIndex + idOffset);
                prefabs.Add(id, Prefab.FromRaw(id, rawPrefabs.Skip(startIndex).Take(i - startIndex), ushort.MaxValue, 0, false));

                i--; // incremented at the end of the loop
            }
            else
            {
                ushort id = (ushort)(i + idOffset);
                prefabs.Add(id, Prefab.FromRaw(id, [rawPrefabs[i]], ushort.MaxValue, 0, false));
            }
        }

        return new PrefabList(prefabs, idOffset);
    }

    /// <summary>
    /// Writes a <see cref="PrefabList"/> into a <see cref="FcBinaryWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="FcBinaryWriter"/> to write this instance into.</param>
    public void Save(FcBinaryWriter writer)
    {
        ThrowIfNull(writer, nameof(writer));

        writer.WriteUInt32((uint)SegmentCount);
        writer.WriteUInt16(IdOffset);

        foreach (var prefab in _prefabs.OrderBy(item => item.Key).SelectMany(item => item.Value.ToRaw(false)))
        {
            prefab.Save(writer);
        }
    }

    /// <summary>
    /// Gets the prefab with the specified id.
    /// </summary>
    /// <param name="id">Id of the prefab to get.</param>
    /// <returns>The prefab with the specified id.</returns>
    public Prefab GetPrefab(ushort id)
        => _prefabs[id];

    /// <summary>
    /// Gets the prefab with the specified id.
    /// </summary>
    /// <param name="id">Id of the prefab to get.</param>
    /// <param name="value">The prefab with the specified id.</param>
    /// <returns><see langword="true"/> if the <see cref="PrefabList"/> contains the specified prefab; otherwise <see langword="false"/>.</returns>
    public bool TryGetPrefab(ushort id, [MaybeNullWhen(false)] out Prefab value)
        => _prefabs.TryGetValue(id, out value);

    /// <summary>
    /// Determines whether the <see cref="PrefabList"/> contains the specified prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <returns><see langword="true"/> if the <see cref="PrefabList"/> contains a prefab with the specified id; otherwise, <see langword="false"/>.</returns>
    public bool ContainsPrefab(ushort id)
        => _prefabs.ContainsKey(id);

    /// <summary>
    /// Gets the segment with the specified id.
    /// </summary>
    /// <param name="id">Id of the segment to get.</param>
    /// <returns>The segment with the specified id.</returns>
    public PrefabSegment GetSegment(ushort id)
        => _segments[id - IdOffset];

    /// <summary>
    /// Gets the segment with the specified id.
    /// </summary>
    /// <param name="id">Id of the segment to get.</param>
    /// <param name="value">The segment with the specified id.</param>
    /// <returns><see langword="true"/> if the <see cref="PrefabList"/> contains the specified segment; otherwise <see langword="false"/>.</returns>
    public bool TryGetSegments(ushort id, [MaybeNullWhen(false)] out PrefabSegment value)
    {
        id -= IdOffset;

        // can skip (id >= 0) because id is unsigned
        if (id < _segments.Count)
        {
            value = _segments[id];
            return true;
        }
        else
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Determines whether the <see cref="PrefabList"/> contains the specified segment.
    /// </summary>
    /// <param name="id">Id of the segment.</param>
    /// <returns><see langword="true"/> if the <see cref="PrefabList"/> contains a segment with the specified id; otherwise, <see langword="false"/>.</returns>
    public bool ContainsSegment(ushort id)
        => id >= IdOffset && id < PrefabCount + IdOffset;

    /// <summary>
    /// Adds a prefab to the <see cref="PrefabList"/>.
    /// </summary>
    /// <remarks>
    /// The prefab's id is changed to <see cref="SegmentCount"/> + <see cref="IdOffset"/>.
    /// The prefab's segments must not be modified while it is in the <see cref="PrefabList"/>.
    /// </remarks>
    /// <param name="value">The prefab to add.</param>
    public void AddPrefab(Prefab value)
    {
        value.Id = (ushort)(SegmentCount + IdOffset);

        _prefabs.Add(value.Id, value);
        _segments.AddRange(value.Values);
    }

    /// <summary>
    /// Adds a prefab to the <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <remarks>
    /// The prefab's segments must not be modified while it is in the <see cref="PrefabList"/>.
    /// </remarks>
    /// <param name="value">The prefab to add, a prefab with it's id must already be in the <see cref="PrefabList"/>.</param>
    public void InsertPrefab(Prefab value)
    {
        if (WillBeLastPrefab(value))
        {
            AddPrefab(value);
            return;
        }

        if (!_prefabs.ContainsKey(value.Id))
        {
            ThrowArgumentException($"{nameof(_prefabs)} must contain {nameof(value)}.{nameof(Prefab.Id)}.", nameof(value));
        }

        IncreaseAfter(value.Id, (ushort)value.Count);
        _prefabs.Add(value.Id, value);
        _segments.InsertRange(value.Id - IdOffset, value.Values);
    }

    /// <summary>
    /// Changes the prefab at the specified id to <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// The prefab's segments must not be modified while it is in the <see cref="PrefabList"/>.
    /// </remarks>
    /// <param name="value">The prefab that will replace the previous prefab at it's id.</param>
    /// <param name="overwriteBlocks">
    /// If <see langword="true"/>, blocks will be overwritten,
    /// if <see langword="false"/>, if a segment of <paramref name="value"/> would be placed at a position that is already occupied, an <see cref="InvalidOperationException"/> will be thrown.
    /// </param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <returns>The prefab that was at the specified id, before the update.</returns>
    public Prefab UpdatePrefab(Prefab value, bool overwriteBlocks, BlockInstancesCache? cache = null)
    {
        if (cache is not null && cache.BLockId != value.Id)
        {
            ThrowArgumentException($"{nameof(cache)}.{nameof(BlockInstancesCache.BLockId)} must be equal to {nameof(value)}.{nameof(Prefab.Id)}.", nameof(cache));
        }

        var prev = _prefabs[value.Id];

        if (!overwriteBlocks && !CanUpdatePrefabIds(prev, value))
        {
            throw new InvalidOperationException($"Cannot update prefab because it's position is obstructed and {nameof(overwriteBlocks)} is false.");
        }

        if (prev.Count > 1)
        {
            Debug.Assert(prev.Count <= 4 * 4 * 4, "prev.Count should be smaller that it's max size.");
            Span<int3> offsets = stackalloc int3[(4 * 4 * 4) - 1];

            int len = 0;
            foreach (var (seg, id) in prev.EnumerateWithId())
            {
                if (id != prev.Id)
                {
                    offsets[len++] = seg.PosInPrefab;
                }
            }

            offsets = offsets[..len];

            if (cache is null)
            {
                RemoveIdsFromPrefab(prev.Id, offsets);
            }
            else
            {
                cache.RemoveBlocks(offsets);
            }
        }

        _segments.RemoveRange(prev.Id - IdOffset, prev.Count);

        if (prev.Count > value.Count)
        {
            DecreaseAfter((ushort)(prev.Id + 1), (ushort)(prev.Count - value.Count));
        }
        else if (prev.Count < value.Count)
        {
            IncreaseAfter((ushort)(prev.Id + 1), (ushort)(value.Count - prev.Count));
        }

        _prefabs[prev.Id] = value;
        _segments.InsertRange(value.Id - IdOffset, value.Values);

        if (value.Count > 1)
        {
            Debug.Assert(value.Count <= 4 * 4 * 4, "value.Count should be smaller that it's max size.");
            Span<(int3, ushort)> ids = stackalloc (int3, ushort)[(4 * 4 * 4) - 1];

            int len = 0;
            foreach (var (seg, id) in value.EnumerateWithId())
            {
                if (id != value.Id)
                {
                    ids[len++] = (seg.PosInPrefab, id);
                }
            }

            ids = ids[..len];

            if (cache is null)
            {
                AddIdsToPrefab(value.Id, ids);
            }
            else
            {
                cache.AddBlocks(this, ids);
            }
        }

        return prev;
    }

    /// <summary>
    /// Removed a prefab with the specified id from the <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <param name="id">Id of the prefab to remove.</param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <returns><see langword="true"/> if the prefab was removed; otherwise <see langword="false"/>.</returns>
    public bool RemovePrefab(ushort id, BlockInstancesCache? cache = null)
    {
        if (cache is not null && cache.BLockId != id)
        {
            ThrowArgumentException($"{nameof(cache)}.{nameof(BlockInstancesCache.BLockId)} must be equal to {nameof(id)}.", nameof(cache));
        }

        if (!_prefabs.Remove(id, out var prefab))
        {
            return false;
        }

        _segments.RemoveRange(id - IdOffset, prefab.Count);

        RemovePrefabFromBLocks(prefab, cache);

        if (WillBeLastPrefab(prefab))
        {
            return true;
        }

        DecreaseAfter(id, (ushort)prefab.Count);

        return true;
    }

    /// <summary>
    /// Removed a prefab with the specified id from the <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <param name="id">Id of the prefab to remove.</param>
    /// <param name="prefab">The prefab that was removed.</param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <returns><see langword="true"/> if the prefab was removed; otherwise <see langword="false"/>.</returns>
    public bool RemovePrefab(ushort id, [MaybeNullWhen(false)] out Prefab prefab, BlockInstancesCache? cache = null)
    {
        if (cache is not null && cache.BLockId != id)
        {
            ThrowArgumentException($"{nameof(cache)}.{nameof(BlockInstancesCache.BLockId)} must be equal to {nameof(id)}.", nameof(cache));
        }

        if (!_prefabs.Remove(id, out prefab))
        {
            return false;
        }

        _segments.RemoveRange(id - IdOffset, prefab.Count);

        RemovePrefabFromBLocks(prefab, cache);

        if (WillBeLastPrefab(prefab))
        {
            return true;
        }

        DecreaseAfter(id, (ushort)prefab.Count);

        return true;
    }

    /// <summary>
    /// Removed a prefab with the specified id from <see cref="Prefab.Blocks"/>.
    /// </summary>
    /// <param name="id">Id of the prefab to remove.</param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <returns><see langword="true"/> if the prefab was removed (it was found at least once); otherwise <see langword="false"/>.</returns>
    public bool RemovePrefabFromBLocks(ushort id, BlockInstancesCache? cache = null)
    {
        if (cache is not null && cache.BLockId != id)
        {
            ThrowArgumentException($"{nameof(cache)}.{nameof(BlockInstancesCache.BLockId)} must be equal to {nameof(id)}.", nameof(cache));
        }

        var prefab = _prefabs[id];

        return RemovePrefabFromBLocks(prefab, cache);
    }

    /// <summary>
    /// Adds a segment to a prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <param name="value">The segment to add to the prefab.</param>
    /// <param name="overwriteBlocks">
    /// If <see langword="true"/>, blocks will be overwritten,
    /// if <see langword="false"/>, if the segment would be placed at a position that is already occupied, an <see cref="InvalidOperationException"/> will be thrown.
    /// </param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    public void AddSegmentToPrefab(ushort id, PrefabSegment value, bool overwriteBlocks, BlockInstancesCache? cache = null)
    {
        if (cache is not null && cache.BLockId != id)
        {
            ThrowArgumentException($"{nameof(cache)}.{nameof(BlockInstancesCache.BLockId)} must be equal to {nameof(id)}.", nameof(cache));
        }

        var prefab = _prefabs[id];

        if (!overwriteBlocks && !CanAddIdToPrefab(id, value.PosInPrefab))
        {
            throw new InvalidOperationException($"Cannot add segment because it's position is obstructed and {nameof(overwriteBlocks)} is false.");
        }

        ushort segmentId = (ushort)(prefab.Id + prefab.Count);

        if (IsLastPrefab(prefab))
        {
            prefab.Add(value);
            _segments.Add(value);
            if (cache is null)
            {
                AddIdToPrefab(id, value.PosInPrefab, segmentId);
            }
            else
            {
                cache.AddBlock(this, value.PosInPrefab, segmentId);
            }

            return;
        }

        prefab.Add(value);

        IncreaseAfter(segmentId, 1);
        _segments.Insert(segmentId - IdOffset, value);
        if (cache is null)
        {
            AddIdToPrefab(id, value.PosInPrefab, segmentId);
        }
        else
        {
            cache.AddBlock(this, value.PosInPrefab, segmentId);
        }
    }

    /// <summary>
    /// Adds a segment to a prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <param name="value">The segment to add to the prefab.</param>
    /// <param name="overwriteBlocks">
    /// If <see langword="true"/>, blocks will be overwritten,
    /// if <see langword="false"/>, if the segment would be placed at a position that is already occupied, <see langword="false"/> is returned.
    /// </param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <returns><see langword="true"/> if the segment was added to the prefab; otherwise <see langword="false"/>.</returns>
    public bool TryAddSegmentToPrefab(ushort id, PrefabSegment value, bool overwriteBlocks, BlockInstancesCache? cache = null)
    {
        if (cache is not null && cache.BLockId != id)
        {
            ThrowArgumentException($"{nameof(cache)}.{nameof(BlockInstancesCache.BLockId)} must be equal to {nameof(id)}.", nameof(cache));
        }

        if (!_prefabs.TryGetValue(id, out var prefab))
        {
            return false;
        }

        if (!overwriteBlocks && !CanAddIdToPrefab(id, value.PosInPrefab))
        {
            return false;
        }

        if (!prefab.TryAdd(value))
        {
            return false;
        }

        ushort segmentId = (ushort)(prefab.Id + prefab.Count - 1);

        if (IsLastPrefab(prefab))
        {
            _segments.Add(value);
            if (cache is null)
            {
                AddIdToPrefab(id, value.PosInPrefab, segmentId);
            }
            else
            {
                cache.AddBlock(this, value.PosInPrefab, segmentId);
            }

            return true;
        }

        IncreaseAfter(segmentId, 1);
        _segments.Insert(segmentId - IdOffset, value);
        if (cache is null)
        {
            AddIdToPrefab(id, value.PosInPrefab, segmentId);
        }
        else
        {
            cache.AddBlock(this, value.PosInPrefab, segmentId);
        }

        return true;
    }

    /// <summary>
    /// Removes a segment from a prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <param name="posInPrefab">Position of the segment to remove.</param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <returns><see langword="true"/> if the segment was removed from the prefab; otherwise <see langword="false"/>.</returns>
    public bool RemoveSegmentFromPrefab(ushort id, int3 posInPrefab, BlockInstancesCache? cache = null)
    {
        if (cache is not null && cache.BLockId != id)
        {
            ThrowArgumentException($"{nameof(cache)}.{nameof(BlockInstancesCache.BLockId)} must be equal to {nameof(id)}.", nameof(cache));
        }

        var prefab = _prefabs[id];

        int segmentIndex = prefab.IndexOf(posInPrefab);
        if (!prefab.Remove(posInPrefab))
        {
            return false;
        }

        ushort segmentId = (ushort)(id + segmentIndex);

        _segments.RemoveAt(segmentId - IdOffset);
        if (cache is null)
        {
            RemoveIdFromBlocks(segmentId);
        }
        else
        {
            cache.RemoveBlock(posInPrefab);
        }

        if (segmentId == SegmentCount + IdOffset - 1)
        {
            return true;
        }

        DecreaseAfter(segmentId, 1);

        return true;
    }

    /// <summary>
    /// Removes all prefabs and segments from the <see cref="PrefabList"/>.
    /// </summary>
    public void Clear()
    {
        _prefabs.Clear();
        _segments.Clear();
    }

    /// <summary>
    /// Creates a copy of this <see cref="PrefabList"/>.
    /// </summary>
    /// <param name="deepCopy">If deep copy should be performed.</param>
    /// <returns>A copy of this <see cref="PrefabList"/>.</returns>
    public PrefabList Clone(bool deepCopy)
        => new PrefabList(this, deepCopy);

    /// <inheritdoc/>
    object ICloneable.Clone()
        => new PrefabList(this, true);

    private static void ValidatePrefabs(IEnumerable<Prefab> prefabs, string paramName)
    {
        int? nextId = null;

        foreach (var prefab in prefabs.OrderBy(group => group.Id))
        {
            if (nextId == null || prefab.Id == nextId)
            {
                nextId = prefab.Id + prefab.Count;
            }
            else
            {
                ThrowArgumentException($"Prefabs in {paramName} must have consecutive IDs. Expected ID {nextId}, but found {prefab.Id}.", paramName);
            }
        }
    }

    private static IEnumerable<PrefabSegment> SegmentsFromPrefabs(IEnumerable<KeyValuePair<ushort, Prefab>> prefabs)
        => prefabs.OrderBy(item => item.Key).SelectMany(item => item.Value.Values);

    private bool RemovePrefabFromBLocks(Prefab prefab, BlockInstancesCache? cache = null)
    {
        Debug.Assert(cache is null || cache.BLockId == prefab.Id, "The cache should be for the prefab.");

        Debug.Assert(prefab.Count <= 4 * 4 * 4, "prefab.Count should be smaller that it's max size.");
        Span<int3> offsets = stackalloc int3[4 * 4 * 4];

        int len = 0;
        foreach (var offset in prefab.Keys)
        {
            offsets[len++] = offset;
        }

        offsets = offsets[..len];

        if (cache is null)
        {
            return RemoveIdsFromPrefab(prefab.Id, offsets);
        }
        else
        {
            cache.RemoveBlocks(offsets);
            return !cache.IsEmpty;
        }
    }

    private bool IsLastPrefab(Prefab prefab)
        => prefab.Id + prefab.Count >= SegmentCount + IdOffset;

    private bool WillBeLastPrefab(Prefab prefab)
        => prefab.Id == SegmentCount + IdOffset;

    private void RemoveIdFromBlocks(ushort id)
    {
        foreach (var prefab in _prefabs.Values)
        {
            ushort[] array = prefab.Blocks.Array.Array;

            for (int z = 0; z < prefab.Blocks.Size.Z; z++)
            {
                for (int y = 0; y < prefab.Blocks.Size.Y; y++)
                {
                    for (int x = 0; x < prefab.Blocks.Size.X; x++)
                    {
                        int i = prefab.Blocks.Index(new int3(x, y, z));

                        if (array[i] == id)
                        {
                            array[i] = 0;
                        }
                    }
                }
            }
        }
    }

    private bool RemoveIdsFromPrefab(ushort prefabId, ReadOnlySpan<int3> offsets)
    {
        bool found = false;

        foreach (var prefab in _prefabs.Values)
        {
            for (int z = 0; z < prefab.Blocks.Size.Z; z++)
            {
                for (int y = 0; y < prefab.Blocks.Size.Y; y++)
                {
                    for (int x = 0; x < prefab.Blocks.Size.X; x++)
                    {
                        if (prefab.Blocks.GetBlockUnchecked(new int3(x, y, z)) == prefabId)
                        {
                            found = true;

                            foreach (var offset in offsets)
                            {
                                prefab.Blocks.SetBlock(new int3(x, y, z) + offset, 0);
                            }
                        }
                    }
                }
            }
        }

        return found;
    }

    private bool CanAddIdToPrefab(ushort prefabId, int3 offset)
    {
        foreach (var prefab in _prefabs.Values)
        {
            for (int z = 0; z < prefab.Blocks.Size.Z; z++)
            {
                for (int y = 0; y < prefab.Blocks.Size.Y; y++)
                {
                    for (int x = 0; x < prefab.Blocks.Size.X; x++)
                    {
                        int3 pos = new int3(x, y, z);

                        if (prefab.Blocks.GetBlockUnchecked(pos) == prefabId)
                        {
                            if (prefab.Blocks.GetBlockOrDefault(pos + offset) != 0)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
        }

        return true;
    }

    private bool CanUpdatePrefabIds(Prefab oldPrefab, Prefab newPrefab)
    {
        Debug.Assert(oldPrefab.Id == newPrefab.Id, "Ids should be equal.");

        ushort prefabId = oldPrefab.Id;

        List<int3> newPositions = [];

        foreach (var pos in newPrefab.Keys)
        {
            if (!oldPrefab.ContainsKey(pos))
            {
                newPositions.Add(pos);
            }
        }

        foreach (var prefab in _prefabs.Values)
        {
            for (int z = 0; z < prefab.Blocks.Size.Z; z++)
            {
                for (int y = 0; y < prefab.Blocks.Size.Y; y++)
                {
                    for (int x = 0; x < prefab.Blocks.Size.X; x++)
                    {
                        int3 pos = new int3(x, y, z);

                        if (prefab.Blocks.GetBlockUnchecked(pos) == prefabId)
                        {
                            foreach (var offset in newPositions)
                            {
                                if (prefab.Blocks.GetBlockOrDefault(pos + offset) != 0)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
        }

        return true;
    }

    private void AddIdToPrefab(ushort prefabId, int3 offset, ushort id)
    {
        foreach (var prefab in _prefabs.Values)
        {
            for (int z = 0; z < prefab.Blocks.Size.Z; z++)
            {
                for (int y = 0; y < prefab.Blocks.Size.Y; y++)
                {
                    for (int x = 0; x < prefab.Blocks.Size.X; x++)
                    {
                        int3 pos = new int3(x, y, z);

                        if (prefab.Blocks.GetBlockUnchecked(pos) == prefabId)
                        {
                            ushort idOld = prefab.Blocks.GetBlockOrDefault(pos + offset);

                            if (idOld != 0 && TryGetPrefab(idOld, out var oldPrefab))
                            {
                                int3 prefabPos = (pos + offset) - GetSegment(idOld).PosInPrefab;

                                foreach (var segPos in oldPrefab.Keys)
                                {
                                    prefab.Blocks.SetBlock(prefabPos + segPos, 0);
                                }
                            }

                            prefab.Blocks.SetBlock(pos + offset, id);
                        }
                    }
                }
            }
        }
    }

    private void AddIdsToPrefab(ushort prefabId, ReadOnlySpan<(int3 Offset, ushort Id)> ids)
    {
        foreach (var prefab in _prefabs.Values)
        {
            for (int z = 0; z < prefab.Blocks.Size.Z; z++)
            {
                for (int y = 0; y < prefab.Blocks.Size.Y; y++)
                {
                    for (int x = 0; x < prefab.Blocks.Size.X; x++)
                    {
                        int3 pos = new int3(x, y, z);

                        if (prefab.Blocks.GetBlockUnchecked(pos) == prefabId)
                        {
                            foreach (var (offset, id) in ids)
                            {
                                ushort idOld = prefab.Blocks.GetBlockOrDefault(pos + offset);

                                if (idOld != 0 && TryGetPrefab(idOld, out var oldPrefab))
                                {
                                    int3 prefabPos = (pos + offset) - GetSegment(idOld).PosInPrefab;

                                    foreach (var segPos in oldPrefab.Keys)
                                    {
                                        prefab.Blocks.SetBlock(prefabPos + segPos, 0);
                                    }
                                }

                                prefab.Blocks.SetBlock(pos + offset, id);
                            }
                        }
                    }
                }
            }
        }
    }

    private void IncreaseAfter(ushort id, ushort amount)
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            PrefabSegment segment = _segments[i];

            if (segment.PrefabId >= id)
            {
                segment.PrefabId += amount;
            }
        }

        List<ushort> prefabsToChangeId = [];

        foreach (var (prefabId, prefab) in _prefabs)
        {
            if (prefabId >= id)
            {
                prefabsToChangeId.Add(prefabId);
            }

            ushort[] array = prefab.Blocks.Array.Array;

            for (int z = 0; z < prefab.Blocks.Size.Z; z++)
            {
                for (int y = 0; y < prefab.Blocks.Size.Y; y++)
                {
                    for (int x = 0; x < prefab.Blocks.Size.X; x++)
                    {
                        int i = prefab.Blocks.Index(new int3(x, y, z));

                        if (array[i] >= id)
                        {
                            array[i] += amount;
                        }
                    }
                }
            }
        }

        foreach (ushort prefabId in prefabsToChangeId.OrderByDescending(item => item))
        {
            bool removed = _prefabs.Remove(prefabId, out var prefab);

            Debug.Assert(removed, "Prefab should have been removed.");
            Debug.Assert(prefab is not null, $"{nameof(prefab)} shouldn't be null.");

            ushort newId = (ushort)(prefabId + amount);
            prefab.Id = newId;
            _prefabs[newId] = prefab;
        }
    }

    private void DecreaseAfter(ushort id, ushort amount)
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            PrefabSegment segment = _segments[i];

            if (segment.PrefabId >= id)
            {
                segment.PrefabId -= amount;
            }
        }

        List<ushort> prefabsToChangeId = [];

        foreach (var (prefabId, prefab) in _prefabs)
        {
            if (prefabId >= id)
            {
                prefabsToChangeId.Add(prefabId);
            }

            ushort[] array = prefab.Blocks.Array.Array;

            for (int z = 0; z < prefab.Blocks.Size.Z; z++)
            {
                for (int y = 0; y < prefab.Blocks.Size.Y; y++)
                {
                    for (int x = 0; x < prefab.Blocks.Size.X; x++)
                    {
                        int i = prefab.Blocks.Index(new int3(x, y, z));

                        if (array[i] >= id)
                        {
                            array[i] -= amount;
                        }
                    }
                }
            }
        }

        foreach (ushort prefabId in prefabsToChangeId.OrderBy(item => item))
        {
            bool removed = _prefabs.Remove(prefabId, out var prefab);

            Debug.Assert(removed, "Prefab should have been removed.");
            Debug.Assert(prefab is not null, $"{nameof(prefab)} shouldn't be null.");

            ushort newId = (ushort)(prefabId - amount);
            prefab.Id = newId;
            _prefabs[newId] = prefab;
        }
    }
}
