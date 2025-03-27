// <copyright file="PartialPrefab.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Partial;

/// <summary>
/// Represents a fancade prefab, with only <see cref="Id"/>, <see cref="Name"/>, <see cref="Type"/> and the positions of segments.
/// </summary>
public sealed class PartialPrefab : IDictionary<int3, PartialPrefabSegment>, ICloneable
{
    private const int MaxSize = Prefab.MaxSize;

    private readonly Dictionary<int3, PartialPrefabSegment> _segments;

    private ushort _id;

    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartialPrefab"/> class.
    /// </summary>
    /// <param name="id">Id of this prefab.</param>
    /// <param name="name">Name of this prefab.</param>
    /// <param name="type">The type of this prefab.</param>
    /// <param name="segments">The segmetns to be placed in this prefab, must all have the same id.</param>
    public PartialPrefab(ushort id, string name, PrefabType type, IEnumerable<PartialPrefabSegment> segments)
    {
        if (!segments.Any())
        {
            ThrowArgumentException($"{nameof(segments)} cannot be empty.", nameof(segments));
        }

        if (string.IsNullOrEmpty(name))
        {
            ThrowArgumentException($"{nameof(name)} cannot be null or empty.", nameof(name));
        }

        _id = id;
        Unsafe.SkipInit(out _name); // initialized by the property
        Name = name;
        Type = type;

        _segments = new(segments.Select(segment =>
        {
            // validate
            if (segment.PosInPrefab.X >= MaxSize || segment.PosInPrefab.Y >= MaxSize || segment.PosInPrefab.Z >= MaxSize)
            {
                ThrowArgumentOutOfRangeException(nameof(segments), $"{nameof(PartialPrefabSegment.PosInPrefab)} cannot be larger than {MaxSize}.");
            }
            else if (segment.PrefabId != Id)
            {
                ThrowArgumentException($"{nameof(PartialPrefabSegment.PrefabId)} must be the same for all segments in {nameof(segments)}", nameof(segments));
            }

            return new KeyValuePair<int3, PartialPrefabSegment>(segment.PosInPrefab, segment);
        }));

        CalculateSize();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartialPrefab"/> class.
    /// </summary>
    /// <param name="name">Name of this prefab.</param>
    /// <param name="type">The type of this prefab.</param>
    /// <param name="segments">The segments to be placed in this prefab, must all have the same id.</param>
    public PartialPrefab(string name, PrefabType type, IEnumerable<PartialPrefabSegment> segments)
    {
        if (!segments.Any())
        {
            ThrowArgumentException(nameof(segments), $"{nameof(segments)} cannot be empty.");
        }

        if (string.IsNullOrEmpty(name))
        {
            ThrowArgumentException($"{nameof(name)} cannot be null or empty.", nameof(name));
        }

        Unsafe.SkipInit(out _name); // initialized by the property
        Name = name;
        Type = type;

        ushort? id = null;

        _segments = new(segments.Select(segment =>
        {
            // validate
            if (segment.PosInPrefab.X >= MaxSize || segment.PosInPrefab.Y >= MaxSize || segment.PosInPrefab.Z >= MaxSize)
            {
                ThrowArgumentOutOfRangeException(nameof(segments), $"{nameof(PartialPrefabSegment.PosInPrefab)} cannot be larger than {MaxSize}.");
            }
            else if (id == null && segment.PrefabId != id)
            {
                ThrowArgumentException($"{nameof(PartialPrefabSegment.PrefabId)} must be the same for all segments in {nameof(segments)}.", nameof(segments));
            }

            id = segment.PrefabId;

            return new KeyValuePair<int3, PartialPrefabSegment>(segment.PosInPrefab, segment);
        }));

        _id = id!.Value;

        CalculateSize();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartialPrefab"/> class.
    /// </summary>
    /// <param name="id">Id of this prefab.</param>
    public PartialPrefab(ushort id)
        : this(id, "New Block", PrefabType.Normal, [new PartialPrefabSegment(id, int3.Zero)])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartialPrefab"/> class.
    /// </summary>
    /// <param name="prefab">A <see cref="Prefab"/> to copy values from.</param>
    public PartialPrefab(Prefab prefab)
    {
        ThrowIfNull(prefab, nameof(prefab));

        _id = prefab.Id;
        _name = prefab.Name;
        Size = prefab.Size;
        Type = prefab.Type;

        _segments = new Dictionary<int3, PartialPrefabSegment>(prefab.Select(item => new KeyValuePair<int3, PartialPrefabSegment>(item.Key, new PartialPrefabSegment(item.Value.PrefabId, item.Value.PosInPrefab))));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartialPrefab"/> class.
    /// </summary>
    /// <param name="other">A <see cref="PartialPrefab"/> to copy values from.</param>
    /// <param name="deepCopy">If deep copy should be performed.</param>
    public PartialPrefab(PartialPrefab other, bool deepCopy)
    {
        ThrowIfNull(other, nameof(other));

#pragma warning disable IDE0306 // Simplify collection initialization - no it fucking can't be 
        _segments = deepCopy
            ? new Dictionary<int3, PartialPrefabSegment>(other._segments.Select(item => new KeyValuePair<int3, PartialPrefabSegment>(item.Key, item.Value.Clone())))
            : new Dictionary<int3, PartialPrefabSegment>(other._segments);
#pragma warning restore IDE0306

        _id = other.Id;

        Size = other.Size;

        _name = other._name;
        Type = other.Type;
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
            if (string.IsNullOrEmpty(value))
            {
                ThrowArgumentException($"{nameof(Name)} cannot be null or empty.", nameof(value));
            }
            else if (Encoding.UTF8.GetByteCount(value) > 255)
            {
                ThrowArgumentException($"{nameof(Name)}, when UTF-8 encoded, cannot be longer than 255 bytes.", nameof(value));
            }

            _name = value;
        }
    }

    /// <summary>
    /// Gets or sets the ID of this prefab.
    /// </summary>
    /// <value>The unique identifier of this prefab.</value>
    public ushort Id
    {
        get => _id;
        set
        {
            foreach (var prefab in Values)
            {
                prefab.PrefabId = value;
            }

            _id = value;
        }
    }

    /// <summary>
    /// Gets or sets the type of this prefab.
    /// </summary>
    /// <value>The type of this prefab.</value>
    public PrefabType Type { get; set; }

    /// <summary>
    /// Gets the size of this prefab.
    /// </summary>
    /// <value>Size of this prefab.</value>
    public int3 Size { get; private set; }

    /// <inheritdoc/>
    public ICollection<int3> Keys => _segments.Keys;

    /// <inheritdoc/>
    public ICollection<PartialPrefabSegment> Values => _segments.Values;

    /// <summary>
    /// Gets the number of segments in the <see cref="Prefab"/>.
    /// </summary>
    /// <value>The number of segments in the <see cref="Prefab"/>.</value>
    public int Count => _segments.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    [SuppressMessage("Design", "CA1043:Use Integral Or String Argument For Indexers", Justification = "It makes sense to use int3 here.")]
    public PartialPrefabSegment this[int3 index]
    {
        get => _segments[index];
        set => _segments[index] = ValidateSegment(value, nameof(value));
    }

    /// <inheritdoc/>
    void IDictionary<int3, PartialPrefabSegment>.Add(int3 key, PartialPrefabSegment value)
    {
        ValidatePos(key, nameof(key));

        _segments.Add(key, ValidateSegment(value, nameof(value)));

        value.PosInPrefab = key; // only change pos if successfully added

        Size = int3.Max(Size, key + int3.One);
    }

    /// <summary>
    /// Adds a segment to the prefab.
    /// </summary>
    /// <param name="value">The segment to add.</param>
    public void Add(PartialPrefabSegment value)
    {
        ValidatePos(value.PosInPrefab, $"{nameof(value)}.{nameof(value.PosInPrefab)}");

        _segments.Add(value.PosInPrefab, ValidateSegment(value, nameof(value)));

        Size = int3.Max(Size, value.PosInPrefab + int3.One);
    }

    /// <summary>
    /// Adds a segment to the prefab, if one isn't already at it's position.
    /// </summary>
    /// <param name="value">The segment to add.</param>
    /// <returns><see langword="true"/> if the value was added; otherwise, <see langword="false"/>.</returns>
    public bool TryAdd(PartialPrefabSegment value)
    {
        ValidatePos(value.PosInPrefab, $"{nameof(value)}.{nameof(value.PosInPrefab)}");

        if (!_segments.TryAdd(value.PosInPrefab, ValidateSegment(value, nameof(value))))
        {
            return false;
        }

        Size = int3.Max(Size, value.PosInPrefab + int3.One);
        return true;
    }

    /// <inheritdoc/>
    public bool ContainsKey(int3 key)
        => _segments.ContainsKey(key);

    /// <summary>
    /// Removes a segment at the specified position.
    /// </summary>
    /// <remarks>
    /// <see cref="PartialPrefab"/> cannot be empty, this method will not succeed if <see cref="Count"/> is <c>1</c>.
    /// </remarks>
    /// <param name="key">Position of the segment to remove.</param>
    /// <returns><see langword="true"/> if the segment was removed; otherwise, <see langword="true"/>.</returns>
    public bool Remove(int3 key)
    {
        if (Count == 1)
        {
            return false;
        }

        bool removed = _segments.Remove(key);

        if (removed)
        {
            CalculateSize();
        }

        return removed;
    }

    /// <summary>
    /// Removes the segment at the specified position.
    /// </summary>
    /// <remarks>
    /// <see cref="PartialPrefab"/> cannot be empty, this method will not succeed if <see cref="Count"/> is <c>1</c>.
    /// </remarks>
    /// <param name="key">The position of the segment to remove.</param>
    /// <param name="value">The removed segment.</param>
    /// <returns><see langword="true"/> if the segment is successfully found and removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(int3 key, [MaybeNullWhen(false)] out PartialPrefabSegment value)
    {
        if (Count == 1)
        {
            value = null;
            return false;
        }

        bool removed = _segments.Remove(key, out value);

        if (removed)
        {
            CalculateSize();
        }

        return removed;
    }

    /// <inheritdoc/>
#if NET5_0_OR_GREATER
    public bool TryGetValue(int3 key, [MaybeNullWhen(false)] out PartialPrefabSegment value)
#else
    public bool TryGetValue(int3 key, out PartialPrefabSegment value)
#endif
        => _segments.TryGetValue(key, out value);

    /// <summary>
    /// Determines the indes of a specified segment.
    /// </summary>
    /// <param name="key">Position of the segment.</param>
    /// <returns>The index of the segment if found; otherwise, <c>-1</c>.</returns>
    public int IndexOf(int3 key)
    {
        int index = 0;

        for (int z = 0; z < Size.Z; z++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    int3 pos = new int3(x, y, z);

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

    /// <summary>
    /// Removes all, but the first segment.
    /// </summary>
    public void Clear()
    {
        var first = this.First().Value;

        _segments.Clear();

        first.PosInPrefab = int3.Zero;
        _segments.Add(first.PosInPrefab, first);

        Size = int3.One;
    }

    /// <inheritdoc/>
    void ICollection<KeyValuePair<int3, PartialPrefabSegment>>.Add(KeyValuePair<int3, PartialPrefabSegment> item)
    {
        ValidatePos(item.Key, $"{nameof(item)}.Key");

        PartialPrefabSegment res = ValidateSegment(item.Value, nameof(item) + ".Value");

        res.PosInPrefab = item.Key;
        Add(res);

        Size = int3.Max(Size, item.Key + int3.One);
    }

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<int3, PartialPrefabSegment>>.Contains(KeyValuePair<int3, PartialPrefabSegment> item)
        => ((ICollection<KeyValuePair<int3, PartialPrefabSegment>>)_segments).Contains(item);

    /// <inheritdoc/>
    void ICollection<KeyValuePair<int3, PartialPrefabSegment>>.CopyTo(KeyValuePair<int3, PartialPrefabSegment>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<int3, PartialPrefabSegment>>)_segments).CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<int3, PartialPrefabSegment>>.Remove(KeyValuePair<int3, PartialPrefabSegment> item)
    {
        if (Count == 1)
        {
            ThrowInvalidOperationException($"{nameof(PartialPrefab)} cannot be empty.");
        }

        bool removed = ((ICollection<KeyValuePair<int3, PartialPrefabSegment>>)_segments).Remove(item);

        if (removed)
        {
            CalculateSize();
        }

        return removed;
    }

    /// <summary>
    /// Returns an <see cref="IEnumerable{T}"/>, that enumerates the segments with their ids.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/>, that enumerates the segments with their ids.</returns>
    public IEnumerable<(PartialPrefabSegment Segment, ushort Id)> EnumerateWithId()
    {
        ushort id = Id;

        for (int z = 0; z < Size.Z; z++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    if (_segments.TryGetValue(new int3(x, y, z), out var segment))
                    {
                        yield return (segment, id++);
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<int3, PartialPrefabSegment>> GetEnumerator()
    {
        for (int z = 0; z < Size.Z; z++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    if (_segments.TryGetValue(new int3(x, y, z), out var segment))
                    {
                        yield return new KeyValuePair<int3, PartialPrefabSegment>(new int3(x, y, z), segment);
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    /// Creates a copy of this <see cref="PartialPrefab"/>.
    /// </summary>
    /// <param name="deepCopy">If deep copy should be performed.</param>
    /// <returns>A copy of this <see cref="PartialPrefab"/>.</returns>
    public PartialPrefab Clone(bool deepCopy)
        => new PartialPrefab(this, deepCopy);

    /// <inheritdoc/>
    object ICloneable.Clone()
        => new PartialPrefab(this, true);

    /// <summary>
    /// Creates <see cref="PartialPrefab"/> from <see cref="OldPartialPrefab"/>s.
    /// </summary>
    /// <remarks>
    /// For <see cref="Name"/> and <see cref="Type"/> uses the first <see cref="OldPartialPrefab"/>.
    /// </remarks>
    /// <param name="id">The id of the prefab.</param>
    /// <param name="prefabs">The <see cref="OldPartialPrefab"/>s to convert. All prefabs must have a distinct <see cref="OldPartialPrefab.PosInGroup"/>.</param>
    /// <returns>The converted <see cref="PrefabSegment"/>.</returns>
    internal static unsafe PartialPrefab FromRaw(ushort id, IEnumerable<OldPartialPrefab> prefabs)
    {
        ThrowIfNull(prefabs, nameof(prefabs));

        OldPartialPrefab? rawPrefab = prefabs.FirstOrDefault();

        if (rawPrefab is null)
        {
            ThrowArgumentException($"{nameof(prefabs)} cannot be empty.", nameof(prefabs));
        }

        return new PartialPrefab(id, rawPrefab.Value.Name, rawPrefab.Value.Type, prefabs.Select(prefab =>
        {
            return new PartialPrefabSegment(id, prefab.PosInGroup);
        }));
    }

    /// <summary>
    /// Converts this <see cref="PartialPrefab"/> into <see cref="OldPartialPrefab"/>s.
    /// </summary>
    /// <returns>A new instance of the <see cref="OldPartialPrefab"/> class from this <see cref="PartialPrefab"/>.</returns>
    internal IEnumerable<OldPartialPrefab> ToRaw()
    {
        int i = 0;
        foreach (var (posInPrefab, prefab) in this)
        {
            yield return i == 0
                ? new OldPartialPrefab(
                    name: Name,
                    type: Type,
                    groupId: Id,
                    posInGroup: (byte3)posInPrefab)
                : new OldPartialPrefab(
                    name: "New Block",
                    type: Type,
                    groupId: Id,
                    posInGroup: (byte3)posInPrefab);

            i++;
        }
    }

    private static void ValidatePos(int3 pos, string paramName)
    {
        if (pos.X >= MaxSize || pos.Y >= MaxSize || pos.Z >= MaxSize)
        {
            ThrowArgumentOutOfRangeException(paramName, $"{paramName} cannot be larger than {MaxSize}.");
        }
        else if (pos.X < 0 || pos.Y < 0 || pos.Z < 0)
        {
            ThrowArgumentOutOfRangeException(paramName, $"{paramName} cannot be negative.");
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

    private PartialPrefabSegment ValidateSegment(PartialPrefabSegment? segment, string paramName)
    {
        if (segment is null)
        {
            ThrowArgumentNullException(paramName);
        }

        segment.PrefabId = Id;

        return segment;
    }
}