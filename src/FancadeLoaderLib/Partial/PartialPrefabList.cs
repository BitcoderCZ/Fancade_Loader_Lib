// <copyright file="PartialPrefabList.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static FancadeLoaderLib.Utils.ThrowHelper;

#pragma warning disable CA1716
namespace FancadeLoaderLib.Partial;
#pragma warning restore CA1716

/// <summary>
/// <see cref="List{T}"/> wrapper for easier <see cref="PartialPrefab"/> manipulation.
/// </summary>
/// <remarks>
/// Ids are automatically changed when prefabs are inserter/removed.
/// </remarks>
public partial class PartialPrefabList : IEnumerable<PartialPrefab>, ICloneable
{
    internal readonly Dictionary<ushort, PartialPrefab> _prefabs;
    internal readonly List<PartialPrefabSegment> _segments;

    private ushort _idOffset = RawGame.CurrentNumbStockPrefabs;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartialPrefabList"/> class.
    /// </summary>
    public PartialPrefabList()
    {
        _prefabs = [];
        _segments = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartialPrefabList"/> class.
    /// </summary>
    /// <param name="prefabCapacity">The initial prefab capacity.</param>
    /// <param name="segmentCapacity">The initial segment capacity.</param>
    public PartialPrefabList(int prefabCapacity, int segmentCapacity)
    {
        _prefabs = new(prefabCapacity);
        _segments = new(segmentCapacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartialPrefabList"/> class.
    /// </summary>
    /// <param name="prefabs">The <see cref="IEnumerable{T}"/> whose elements are copied to the new <see cref="PartialPrefabList"/>.</param>
    public PartialPrefabList(IEnumerable<PartialPrefab> prefabs)
    {
        ThrowIfNull(prefabs, nameof(prefabs));

        _prefabs = prefabs.ToDictionary(prefab => prefab.Id);
        ValidatePrefabs(_prefabs.Values, nameof(prefabs)); // validate using _prefabs.Values to avoid iterating over collection multiple times

        _segments = [.. SegmentsFromPrefabs(_prefabs)];

        _idOffset = _prefabs.Min(item => item.Key);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartialPrefabList"/> class.
    /// </summary>
    /// <param name="list">A <see cref="PrefabList"/> to copy values from.</param>
    public PartialPrefabList(PrefabList list)
    {
        ThrowIfNull(list, nameof(list));

        _prefabs = list.Prefabs.ToDictionary(prefab => prefab.Id, prefab => new PartialPrefab(prefab));
        _segments = [.. SegmentsFromPrefabs(_prefabs)];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartialPrefabList"/> class.
    /// </summary>
    /// <param name="other">A <see cref="PartialPrefabList"/> to copy values from.</param>
    /// <param name="deepCopy">If deep copy should be performed.</param>
    public PartialPrefabList(PartialPrefabList other, bool deepCopy)
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

    private PartialPrefabList(Dictionary<ushort, PartialPrefab> dict, ushort idOffset)
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
    /// Gets the number of prefabs in the <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <value>The number of prefabs in the <see cref="PartialPrefabList"/>.</value>
    public int PrefabCount => _prefabs.Count;

    /// <summary>
    /// Gets the number of segments in the <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <value>The number of segments in the <see cref="PartialPrefabList"/>.</value>
    public int SegmentCount => _segments.Count;

    /// <summary>
    /// Gets an <see cref="IEnumerable{T}"/> containing the prefabs in the <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <value>An <see cref="IEnumerable{T}"/> containing the prefabs in the <see cref="PartialPrefabList"/>.</value>
    public IEnumerable<PartialPrefab> Prefabs => _prefabs.Values;

    /// <summary>
    /// Gets an <see cref="IEnumerable{T}"/> containing the segments in the <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <value>An <see cref="IEnumerable{T}"/> containing the segments in the <see cref="PartialPrefabList"/>.</value>
    public IEnumerable<PartialPrefabSegment> Segments => _segments;

    /// <summary>
    /// Loads a <see cref="PartialPrefabList"/> from a <see cref="FcBinaryReader"/>.
    /// </summary>
    /// <param name="reader">The reader to read the <see cref="PartialPrefabList"/> from.</param>
    /// <returns>A <see cref="PartialPrefabList"/> read from <paramref name="reader"/>.</returns>
    public static PartialPrefabList Load(FcBinaryReader reader)
    {
        ThrowIfNull(reader, nameof(reader));

        uint count = reader.ReadUInt32();
        ushort idOffset = reader.ReadUInt16();

        OldPartialPrefab[] rawPrefabs = new OldPartialPrefab[count];

        for (int i = 0; i < count; i++)
        {
            rawPrefabs[i] = OldPartialPrefab.Load(reader);
        }

        Dictionary<ushort, PartialPrefab> prefabs = [];

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
                prefabs.Add(id, PartialPrefab.FromRaw(id, rawPrefabs.Skip(startIndex).Take(i - startIndex)));

                i--; // incremented at the end of the loop
            }
            else
            {
                ushort id = (ushort)(i + idOffset);
                prefabs.Add(id, PartialPrefab.FromRaw(id, [rawPrefabs[i]]));
            }
        }

        return new PartialPrefabList(prefabs, idOffset);
    }

    /// <summary>
    /// Writes a <see cref="PartialPrefabList"/> into a <see cref="FcBinaryWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="FcBinaryWriter"/> to write this instance into.</param>
    public void Save(FcBinaryWriter writer)
    {
        ThrowIfNull(writer, nameof(writer));

        writer.WriteUInt32((uint)SegmentCount);
        writer.WriteUInt16(IdOffset);

        foreach (var prefab in _prefabs.OrderBy(item => item.Key).SelectMany(item => item.Value.ToRaw()))
        {
            prefab.Save(writer);
        }
    }

    /// <summary>
    /// Gets the prefab with the specified id.
    /// </summary>
    /// <param name="id">Id of the prefab to get.</param>
    /// <returns>The prefab with the specified id.</returns>
    public PartialPrefab GetPrefab(ushort id)
        => _prefabs[id];

    /// <summary>
    /// Determines whether the <see cref="PartialPrefabSegment"/> contains the specified prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <returns><see langword="true"/> if the <see cref="PartialPrefabSegment"/> contains a prefab with the specified id; otherwise, <see langword="false"/>.</returns>
    public bool ContainsPrefab(ushort id)
        => _prefabs.ContainsKey(id);

    /// <summary>
    /// Gets the prefab with the specified id.
    /// </summary>
    /// <param name="id">Id of the prefab to get.</param>
    /// <param name="value">The prefab with the specified id.</param>
    /// <returns><see langword="true"/> if the <see cref="PartialPrefabList"/> contains the specified prefab; otherwise <see langword="false"/>.</returns>
    public bool TryGetPrefab(ushort id, [MaybeNullWhen(false)] out PartialPrefab value)
        => _prefabs.TryGetValue(id, out value);

    /// <summary>
    /// Gets the segment with the specified id.
    /// </summary>
    /// <param name="id">Id of the segment to get.</param>
    /// <returns>The segment with the specified id.</returns>
    public PartialPrefabSegment GetSegment(ushort id)
        => _segments[id - IdOffset];

    /// <summary>
    /// Gets the segment with the specified id.
    /// </summary>
    /// <param name="id">Id of the segment to get.</param>
    /// <param name="value">The segment with the specified id.</param>
    /// <returns><see langword="true"/> if the <see cref="PartialPrefabList"/> contains the specified segment; otherwise <see langword="false"/>.</returns>
    public bool TryGetSegment(ushort id, [MaybeNullWhen(false)] out PartialPrefabSegment value)
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
    /// Determines whether the <see cref="PartialPrefabSegment"/> contains the specified segment.
    /// </summary>
    /// <param name="id">Id of the segment.</param>
    /// <returns><see langword="true"/> if the <see cref="PartialPrefabSegment"/> contains a segment with the specified id; otherwise, <see langword="false"/>.</returns>
    public bool ContainsSegment(ushort id)
        => id >= IdOffset && id < PrefabCount + IdOffset;

    /// <summary>
    /// Adds a prefab to the <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <remarks>
    /// The prefab's id is changed to <see cref="SegmentCount"/> + <see cref="IdOffset"/>.
    /// The prefab's segments must not be modified while it is in the <see cref="PartialPrefabList"/>.
    /// </remarks>
    /// <param name="value">The prefab to add.</param>
    public void AddPrefab(PartialPrefab value)
    {
        value.Id = (ushort)(SegmentCount + IdOffset);

        _prefabs.Add(value.Id, value);
        _segments.AddRange(value.OrderedValues);
    }

    /// <summary>
    /// Adds a prefab to the <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <remarks>
    /// The prefab's segments must not be modified while it is in the <see cref="PartialPrefabList"/>.
    /// </remarks>
    /// <param name="value">The prefab to add, a prefab with it's id must already be in the <see cref="PartialPrefabList"/>.</param>
    public void InsertPrefab(PartialPrefab value)
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
        _segments.InsertRange(value.Id - IdOffset, value.OrderedValues);
    }

    /// <summary>
    /// Changes the prefab at the specified id to <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// The prefab's segments must not be modified while it is in the <see cref="PartialPrefabList"/>.
    /// </remarks>
    /// <param name="value">The prefab that will replace the previous prefab at it's id.</param>
    public void UpdatePrefab(PartialPrefab value)
    {
        var prev = _prefabs[value.Id];

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
        _segments.InsertRange(value.Id - IdOffset, value.OrderedValues);
    }

    /// <summary>
    /// Removed a prefab with the specified id from the <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <param name="id">Id of the prefab to remove.</param>
    /// <returns><see langword="true"/> if the prefab was removed; otherwise <see langword="false"/>.</returns>
    public bool RemovePrefab(ushort id)
    {
        if (!_prefabs.Remove(id, out var prefab))
        {
            return false;
        }

        _segments.RemoveRange(id - IdOffset, prefab.Count);

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
    /// <returns><see langword="true"/> if the prefab was removed; otherwise <see langword="false"/>.</returns>
    public bool RemovePrefab(ushort id, [MaybeNullWhen(false)] out PartialPrefab prefab)
    {
        if (!_prefabs.Remove(id, out prefab))
        {
            return false;
        }

        _segments.RemoveRange(id - IdOffset, prefab.Count);

        if (WillBeLastPrefab(prefab))
        {
            return true;
        }

        DecreaseAfter(id, (ushort)prefab.Count);

        return true;
    }

    /// <summary>
    /// Determines if a segment can be added to a prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <param name="segmentPos">Position of the segment.</param>
    /// <returns><see langword="true"/> if <paramref name="segmentPos"/> can be added to the prefab; otherwise <see langword="false"/>.</returns>
    public bool CanAddSegmentToPrefab(ushort id, int3 segmentPos)
        => _prefabs.TryGetValue(id, out var prefab) &&
            !prefab.ContainsKey(segmentPos);

    /// <summary>
    /// Adds a segment to a prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <param name="value">The segment to add to the prefab.</param>
    public void AddSegmentToPrefab(ushort id, PartialPrefabSegment value)
    {
        var prefab = _prefabs[id];

        ushort segmentId = (ushort)(prefab.Id + prefab.GetNewSegmentIndex(value.PosInPrefab));

        if (IsLastPrefab(prefab))
        {
            prefab.Add(value);
            _segments.Insert(segmentId - IdOffset, value);
            return;
        }

        prefab.Add(value);

        IncreaseAfter(segmentId, 1);
        _segments.Insert(segmentId - IdOffset, value);
    }

    /// <summary>
    /// Adds a segment to a prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <param name="value">The segment to add to the prefab.</param>
    /// <returns><see langword="true"/> if the segment was added to the prefab; otherwise <see langword="false"/>.</returns>
    public bool TryAddSegmentToPrefab(ushort id, PartialPrefabSegment value)
    {
        if (!_prefabs.TryGetValue(id, out var prefab))
        {
            return false;
        }

        if (!prefab.TryAdd(value))
        {
            return false;
        }

        ushort segmentId = (ushort)(prefab.Id + prefab.GetNewSegmentIndex(value.PosInPrefab));

        if (IsLastPrefab(prefab))
        {
            _segments.Insert(segmentId - IdOffset, value);
            return true;
        }

        IncreaseAfter(segmentId, 1);
        _segments.Insert(segmentId - IdOffset, value);

        return true;
    }

    /// <summary>
    /// Removes a segment from a prefab.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <param name="posInPrefab">Position of the segment to remove.</param>
    /// <returns><see langword="true"/> if the segment was removed from the prefab; otherwise <see langword="false"/>.</returns>
    public bool RemoveSegmentFromPrefab(ushort id, int3 posInPrefab)
    {
        var prefab = _prefabs[id];

        int segmentIndex = prefab.IndexOf(posInPrefab);
        if (!prefab.Remove(posInPrefab))
        {
            return false;
        }

        Debug.Assert(segmentIndex != -1, "Because the segment was succesfully removed, it's index before removal shoudn't be -1.");

        ushort segmentId = (ushort)(id + segmentIndex);

        _segments.RemoveAt(segmentId - IdOffset);

        if (segmentId == SegmentCount + IdOffset - 1)
        {
            return true;
        }

        DecreaseAfter((ushort)(segmentId + 1), 1);

        return true;
    }

    /// <inheritdoc/>
    public IEnumerator<PartialPrefab> GetEnumerator()
        => _prefabs.Values.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    /// Removes all prefabs and segments from the <see cref="PartialPrefabList"/>.
    /// </summary>
    public void Clear()
    {
        _prefabs.Clear();
        _segments.Clear();
    }

    /// <summary>
    /// Creates a copy of this <see cref="PartialPrefabList"/>.
    /// </summary>
    /// <param name="deepCopy">If deep copy should be performed.</param>
    /// <returns>A copy of this <see cref="PartialPrefabList"/>.</returns>
    public PartialPrefabList Clone(bool deepCopy)
        => new PartialPrefabList(this, deepCopy);

    /// <inheritdoc/>
    object ICloneable.Clone()
        => new PartialPrefabList(this, true);

    private static void ValidatePrefabs(IEnumerable<PartialPrefab> prefabs, string paramName)
    {
        int? nextId = null;

        foreach (var prefab in prefabs.OrderBy(prefab => prefab.Id))
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

    private static IEnumerable<PartialPrefabSegment> SegmentsFromPrefabs(IEnumerable<KeyValuePair<ushort, PartialPrefab>> prefabs)
        => prefabs.OrderBy(item => item.Key).SelectMany(item => item.Value.OrderedValues);

    private bool IsLastPrefab(PartialPrefab prefab)
        => prefab.Id + prefab.Count >= SegmentCount + IdOffset;

    private bool WillBeLastPrefab(PartialPrefab prefab)
        => prefab.Id == SegmentCount + IdOffset;

    private void IncreaseAfter(ushort id, ushort amount)
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            PartialPrefabSegment segment = _segments[i];

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
            PartialPrefabSegment segment = _segments[i];

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
