// <copyright file="Prefab.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib;

/// <summary>
/// Represents a fancade prefab, processed for easier manipulation.
/// </summary>
public sealed class Prefab : IDictionary<int3, PrefabSegment>, ICloneable
{
    /// <summary>
    /// The maximum allowed size for a prefab in each axis.
    /// </summary>
    public const int MaxSize = 4;

    private readonly Dictionary<int3, PrefabSegment> _segments;

    private ushort _id;

    private string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="Prefab"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of this prefab.</param>
    /// <param name="name">The name of this prefab.</param>
    /// <param name="collider">The collider type of this prefab.</param>
    /// <param name="type">The type of this prefab.</param>
    /// <param name="backgroundColor">The background color of this prefab.</param>
    /// <param name="editable">If this prefab should be editable.</param>
    /// <param name="blocks">The blocks contained within this prefab.</param>
    /// <param name="settings">The settings applied to blocks in this prefab.</param>
    /// <param name="connections">The connections between blocks in this prefab.</param>
    /// <param name="segments">The segments to be placed in this prefab, all of which must have the same ID.</param>
    public Prefab(ushort id, string name, PrefabCollider collider, PrefabType type, FcColor backgroundColor, bool editable, BlockData? blocks, List<PrefabSetting>? settings, List<Connection>? connections, IEnumerable<PrefabSegment> segments)
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
        Collider = collider;
        Type = type;
        BackgroundColor = backgroundColor;
        Editable = editable;
        Blocks = blocks ?? new BlockData();
        Settings = settings ?? [];
        Connections = connections ?? [];

        _segments = new(segments.Select(segment =>
        {
            // validate
            if (segment.PosInPrefab.X >= MaxSize || segment.PosInPrefab.Y >= MaxSize || segment.PosInPrefab.Z >= MaxSize)
            {
                ThrowArgumentOutOfRangeException(nameof(segments), $"{nameof(PrefabSegment.PosInPrefab)} cannot be larger than {MaxSize}.");
            }
            else if (segment.PrefabId != Id)
            {
                ThrowArgumentException($"{nameof(PrefabSegment.PrefabId)} must be the same for all segments in {nameof(segments)}", nameof(segments));
            }

            return new KeyValuePair<int3, PrefabSegment>(segment.PosInPrefab, segment);
        }));

        CalculateSize();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Prefab"/> class.
    /// </summary>
    /// <param name="name">Name of this prefab.</param>
    /// <param name="collider">The collider of this prefab.</param>
    /// <param name="type">The type of this prefab.</param>
    /// <param name="backgroundColor">The background color of this prefab.</param>
    /// <param name="editable">If this prefab is editable.</param>
    /// <param name="blocks">The blocks inside this prefab.</param>
    /// <param name="settings">Settings of the blocks inside this prefab.</param>
    /// <param name="connections">Connections between blocks inside this prefab, block-block and block-outside of this prefab.</param>
    /// <param name="segments">The prefabs to be placed in this prefab, must all have the same id.</param>
    public Prefab(string name, PrefabCollider collider, PrefabType type, FcColor backgroundColor, bool editable, BlockData? blocks, List<PrefabSetting>? settings, List<Connection>? connections, IEnumerable<PrefabSegment> segments)
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
        Collider = collider;
        Type = type;
        BackgroundColor = backgroundColor;
        Editable = editable;
        Blocks = blocks ?? new BlockData();
        Settings = settings ?? [];
        Connections = connections ?? [];

        ushort? id = null;

        _segments = new(segments.Select(segment =>
        {
            // validate
            if (segment.PosInPrefab.X >= MaxSize || segment.PosInPrefab.Y >= MaxSize || segment.PosInPrefab.Z >= MaxSize)
            {
                ThrowArgumentOutOfRangeException(nameof(segments), $"{nameof(PrefabSegment.PosInPrefab)} cannot be larger than {MaxSize}.");
            }
            else if (id == null && segment.PrefabId != id)
            {
                ThrowArgumentException($"{nameof(PrefabSegment.PrefabId)} must be the same for all segments in {nameof(segments)}.", nameof(segments));
            }

            id = segment.PrefabId;

            return new KeyValuePair<int3, PrefabSegment>(segment.PosInPrefab, segment);
        }));

        _id = id!.Value;

        CalculateSize();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Prefab"/> class.
    /// </summary>
    /// <param name="id">Id of this prefab.</param>
    public Prefab(ushort id)
        : this(id, "New Block", PrefabCollider.Box, PrefabType.Normal, FcColorUtils.DefaultBackgroundColor, true, new BlockData(), [], [], [new PrefabSegment(id, int3.Zero)])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Prefab"/> class.
    /// </summary>
    /// <param name="other">The <see cref="Prefab"/> to copy values from.</param>
    /// <param name="deepCopy">If deep copy should be performed.</param>
    public Prefab(Prefab other, bool deepCopy)
    {
        ThrowIfNull(other, nameof(other));

#pragma warning disable IDE0306 // Simplify collection initialization - no it fucking can't be 
        _segments = deepCopy
            ? new Dictionary<int3, PrefabSegment>(other._segments.Select(item => new KeyValuePair<int3, PrefabSegment>(item.Key, item.Value.Clone())))
            : new Dictionary<int3, PrefabSegment>(other._segments);
#pragma warning restore IDE0306

        _id = other.Id;

        Size = other.Size;

        _name = other._name;
        Collider = other.Collider;
        Type = other.Type;
        BackgroundColor = other.BackgroundColor;
        Editable = other.Editable;
        Blocks = deepCopy ? other.Blocks.Clone() : other.Blocks;
        Settings = deepCopy ? [.. other.Settings] : other.Settings;
        Connections = deepCopy ? [.. other.Connections] : other.Connections;
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
    /// Gets the blocks contained within this prefab.
    /// </summary>
    /// <value>The blocks contained within this prefab.</value>
    public BlockData Blocks { get; }

    /// <summary>
    /// Gets the settings applied to the blocks in this prefab.
    /// </summary>
    /// <value>The settings applied to the blocks in this prefab.</value>
    public List<PrefabSetting> Settings { get; }

    /// <summary>
    /// Gets the connections between blocks inside this prefab and connections to inputs/outputs of this prefab.
    /// </summary>
    /// <value>The connections inside this prefab.</value>
    public List<Connection> Connections { get; }

    /// <summary>
    /// Gets or sets the collider of this prefab.
    /// </summary>
    /// <value>The collider of this prefab.</value>
    public PrefabCollider Collider { get; set; }

    /// <summary>
    /// Gets or sets the type of this prefab.
    /// </summary>
    /// <value>The type of this prefab.</value>
    public PrefabType Type { get; set; }

    /// <summary>
    /// Gets or sets the background color of this prefab.
    /// </summary>
    /// <value>The background color of this prefab.</value>
    public FcColor BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this prefab is editable.
    /// </summary>
    /// <value><see langword="true"/> if the prefab is editable; otherwise, <see langword="false"/>.</value>
    public bool Editable { get; set; }

    /// <summary>
    /// Gets the size of this prefab.
    /// </summary>
    /// <value>Size of this prefab.</value>
    public int3 Size { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="Prefab"/> is empty.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="PrefabSegment.IsEmpty"/> is <see langword="true"/> for all of the segments; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty
    {
        get
        {
            foreach (var segment in _segments.Values)
            {
                if (!segment.IsEmpty)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <inheritdoc/>
    public ICollection<int3> Keys => _segments.Keys;

    /// <inheritdoc/>
    public ICollection<PrefabSegment> Values => _segments.Values;

    /// <summary>
    /// Gets the number of segments in the <see cref="Prefab"/>.
    /// </summary>
    /// <value>The number of segments in the <see cref="Prefab"/>.</value>
    public int Count => _segments.Count;

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<int3, PrefabSegment>>.IsReadOnly => false;

    /// <inheritdoc/>
    [SuppressMessage("Design", "CA1043:Use Integral Or String Argument For Indexers", Justification = "It makes sense to use int3 here.")]
    public PrefabSegment this[int3 index]
    {
        get => _segments[index];
        set => _segments[index] = ValidateSegment(value, nameof(value));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Prefab"/> class, with the default values for a block.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <param name="name">Name of the prefab.</param>
    /// <returns>The new instance of <see cref="Prefab"/>.</returns>
    public static Prefab CreateBlock(ushort id, string name)
        => new Prefab(id, name, PrefabCollider.Box, PrefabType.Normal, FcColorUtils.DefaultBackgroundColor, true, new(), [], [], [new PrefabSegment(id, int3.Zero, new Voxel[8 * 8 * 8])]);

    /// <summary>
    /// Initializes a new instance of the <see cref="Prefab"/> class, with the default values for a level.
    /// </summary>
    /// <param name="id">Id of the prefab.</param>
    /// <param name="name">Name of the prefab.</param>
    /// <returns>The new instance of <see cref="Prefab"/>.</returns>
    public static Prefab CreateLevel(ushort id, string name)
        => new Prefab(id)
        {
            Name = name,
            Collider = PrefabCollider.None,
            Type = PrefabType.Level,
        };

    /// <summary>
    /// Creates <see cref="Prefab"/> from <see cref="RawPrefab"/>s.
    /// </summary>
    /// <remarks>
    /// For <see cref="Blocks"/>, <see cref="Settings"/>, ... uses the first <see cref="RawPrefab"/>.
    /// </remarks>
    /// <param name="id">The id of the prefab.</param>
    /// <param name="rawPrefabs">The <see cref="RawPrefab"/>s to convert. All prefabs must have a distinct <see cref="RawPrefab.PosInGroup"/>.</param>
    /// <param name="idOffset">The offset at which <paramref name="idOffsetAddition"/> starts to be applied.</param>
    /// <param name="idOffsetAddition">Added to blocks, if the block's id is >= <paramref name="idOffset"/>.</param>
    /// <param name="clone">If true clones Blocks, Settings and Connections; else the values are assigned directly and the prefabs in <paramref name="rawPrefabs"/> shouldn't be used anymore.</param>
    /// <returns>The converted <see cref="PrefabSegment"/>.</returns>
    public static unsafe Prefab FromRaw(ushort id, IEnumerable<RawPrefab> rawPrefabs, ushort idOffset, short idOffsetAddition, bool clone = true)
    {
        ThrowIfNull(rawPrefabs, nameof(rawPrefabs));

        RawPrefab? rawPrefab = rawPrefabs.FirstOrDefault();

        if (rawPrefab is null)
        {
            ThrowArgumentException($"{nameof(rawPrefabs)} cannot be empty.", nameof(rawPrefabs));
        }

        PrefabType type = PrefabType.Normal;
        if (rawPrefab.HasTypeByte)
        {
            type = (PrefabType)rawPrefab.TypeByte;
        }

        string name = "New Block";
        if (rawPrefab.NonDefaultName)
        {
            name = rawPrefab.Name;
        }

        FcColor backgroundColor = FcColorUtils.DefaultBackgroundColor;
        if (rawPrefab.NonDefaultBackgroundColor)
        {
            backgroundColor = (FcColor)rawPrefab.BackgroundColor;
        }

        bool editable = !rawPrefab.UnEditable && !rawPrefab.UnEditable2;

        PrefabCollider collider = PrefabCollider.Box;
        if (rawPrefab.HasColliderByte)
        {
            collider = (PrefabCollider)rawPrefab.ColliderByte;
        }

        BlockData? blockData = null;
        if (rawPrefab.HasBlocks && rawPrefab.Blocks is not null)
        {
            ushort[] blocks = clone
                ? (ushort[])rawPrefab.Blocks.Array.Clone()
                : rawPrefab.Blocks.Array;

            for (int i = 0; i < blocks.Length; i++)
            {
                if (idOffset <= blocks[i])
                {
                    blocks[i] = (ushort)(blocks[i] + idOffsetAddition);
                }
            }

            blockData = new BlockData(new Array3D<ushort>(blocks, rawPrefab.Blocks.Size));
            blockData.Trim(false);
        }

        List<PrefabSetting>? settings = null;
        if (rawPrefab.HasSettings && rawPrefab.Settings is not null)
        {
            settings = clone
                ? [.. rawPrefab.Settings]
                : rawPrefab.Settings;
        }

        // add settings to stock prefabs
        if (blockData is not null && blockData.Size != int3.Zero)
        {
            if (settings is null)
            {
                settings = [];
            }

            for (int i = 0; i < blockData.Array.Length; i++)
            {
                ushort blockId = blockData.Array[i];

                if (blockId != 0)
                {
                    int numbStockSettings = 0; // TODO: getNumbStockSettings(id);
#pragma warning disable CA1508 // Avoid dead conditional code
                    if (numbStockSettings != 0)
                    {
                        for (int setI = 0; setI < numbStockSettings; setI++)
                        {
                            ushort3 pos = (ushort3)blockData.Index(i);

                            PrefabSetting setting = settings.FirstOrDefault(s => s.Index == setI && s.Position == pos);

                            if (setting == default)
                            {
                                // Wasn't found
                                // TODO: settings.Add(getStockSetting(id, setI));
                            }
                        }
                    }
#pragma warning restore CA1508
                }
            }
        }

        List<Connection>? connections = null;
        if (rawPrefab.HasConnections)
        {
            if (rawPrefab.Connections is null)
            {
                ThrowArgumentException($"{nameof(rawPrefab)}.{nameof(RawPrefab.HasConnections)} is true, while {nameof(rawPrefab)}.{nameof(RawPrefab.Connections)} is null", nameof(rawPrefab));
            }

            connections = clone
                ? [.. rawPrefab.Connections]
                : rawPrefab.Connections;
        }

        return new Prefab(id, name, collider, type, backgroundColor, editable, blockData, settings, connections, rawPrefabs.Select(prefab =>
        {
            Voxel[]? voxels = null;
            if (prefab.HasVoxels && prefab.Voxels is not null)
            {
                voxels = PrefabSegment.VoxelsFromRaw(prefab.Voxels);
            }

            return new PrefabSegment(id, prefab.PosInGroup, voxels);
        }));
    }

    /// <summary>
    /// Converts this <see cref="Prefab"/> into <see cref="RawPrefab"/>s.
    /// </summary>
    /// <param name="clone">If the prefabs should be copied, if <see langword="true"/>, this <see cref="RawPrefab"/> instance shouldn't be used anymore.</param>
    /// <returns>A new instance of the <see cref="RawPrefab"/> class from this <see cref="Prefab"/>.</returns>
    public IEnumerable<RawPrefab> ToRaw(bool clone)
    {
        Blocks.Trim();

        int i = 0;
        foreach (var (posInGroup, segment) in this)
        {
            byte[]? voxels = null;

            if (segment.Voxels is not null)
            {
                voxels = PrefabSegment.VoxelsToRaw(segment.Voxels);
            }

            yield return i == 0
                ? new RawPrefab(
                    hasConnections: Connections is not null && Connections.Count > 0,
                    hasSettings: Settings is not null && Settings.Count > 0,
                    hasBlocks: Blocks is not null && Blocks.Size != int3.Zero,
                    hasVoxels: Type != PrefabType.Level && segment.Voxels is not null,
                    isInGroup: Count > 1,
                    hasColliderByte: Collider != PrefabCollider.Box,
                    unEditable: !Editable,
                    unEditable2: !Editable,
                    nonDefaultBackgroundColor: BackgroundColor != FcColorUtils.DefaultBackgroundColor,
                    hasData2: false,
                    hasData1: false,
                    nonDefaultName: Name != "New Block",
                    hasTypeByte: Type != 0,
                    typeByte: (byte)Type,
                    name: Name,
                    data1: 0,
                    data2: 0,
                    backgroundColor: (byte)BackgroundColor,
                    colliderByte: (byte)Collider,
                    groupId: Id,
                    posInGroup: (byte3)posInGroup,
                    voxels: voxels,
                    blocks: Blocks is null ? null : (clone ? Blocks.Array.Clone() : Blocks.Array),
                    settings: clone && Settings is not null ? [.. Settings] : Settings,
                    connections: clone && Connections is not null ? [.. Connections] : Connections)
                : new RawPrefab(
                    hasConnections: false,
                    hasSettings: false,
                    hasBlocks: false,
                    hasVoxels: Type != PrefabType.Level && segment.Voxels is not null,
                    isInGroup: Count > 1,
                    hasColliderByte: Collider != PrefabCollider.Box,
                    unEditable: !Editable,
                    unEditable2: !Editable,
                    nonDefaultBackgroundColor: BackgroundColor != FcColorUtils.DefaultBackgroundColor,
                    hasData2: false,
                    hasData1: false,
                    nonDefaultName: Name != "New Block",
                    hasTypeByte: Type != 0,
                    typeByte: (byte)Type,
                    name: Name,
                    data1: 0,
                    data2: 0,
                    backgroundColor: (byte)BackgroundColor,
                    colliderByte: (byte)Collider,
                    groupId: Id,
                    posInGroup: (byte3)posInGroup,
                    voxels: voxels,
                    blocks: null,
                    settings: null,
                    connections: null);

            i++;
        }
    }

    /// <inheritdoc/>
    void IDictionary<int3, PrefabSegment>.Add(int3 key, PrefabSegment value)
    {
        ValidatePos(key, nameof(key));

        _segments.Add(key, ValidateSegment(value, nameof(value)));

        value.PosInPrefab = key; // only change pos if successfully added

        Size = int3.Max(Size, key + int3.One);
    }

    /// <summary>
    /// Adds a segment to this prefab.
    /// </summary>
    /// <param name="value">The segment to add.</param>
    public void Add(PrefabSegment value)
    {
        ValidatePos(value.PosInPrefab, $"{nameof(value)}.{nameof(value.PosInPrefab)}");

        _segments.Add(value.PosInPrefab, ValidateSegment(value, nameof(value)));

        Size = int3.Max(Size, value.PosInPrefab + int3.One);
    }

    /// <summary>
    /// Adds a segment to this prefab, if one isn't already at it's position.
    /// </summary>
    /// <param name="value">The segment to add.</param>
    /// <returns><see langword="true"/> if the segment was added; otherwise, <see langword="true"/>.</returns>
    public bool TryAdd(PrefabSegment value)
    {
        ValidatePos(value.PosInPrefab, $"{nameof(value)}.{nameof(value.PosInPrefab)}");

        if (!_segments.TryAdd(value.PosInPrefab, ValidateSegment(value, nameof(value))))
        {
            return false;
        }

        Size = int3.Max(Size, value.PosInPrefab + int3.One);
        return true;
    }

    /// <summary>
    /// Adds segments to this prefab.
    /// </summary>
    /// <param name="values">The segments to add.</param>
    public void AddRange(IEnumerable<PrefabSegment> values)
    {
        foreach (var value in values)
        {
            ValidatePos(value.PosInPrefab, $"{nameof(value)}.{nameof(value.PosInPrefab)}");

            if (_segments.ContainsKey(value.PosInPrefab))
            {
                ThrowArgumentException($"Cannot add the segment, because a segment with it's position is already in the prefab.");
            }
        }

        foreach (var value in values)
        {
            _segments.Add(value.PosInPrefab, ValidateSegment(value, nameof(value)));

            Size = int3.Max(Size, value.PosInPrefab + int3.One);
        }
    }

    /// <inheritdoc/>
    public bool ContainsKey(int3 key)
        => _segments.ContainsKey(key);

    /// <summary>
    /// Removes a segment at the specified position.
    /// </summary>
    /// <remarks>
    /// <see cref="Prefab"/> cannot be empty, this method will not succeed if <see cref="Count"/> is <c>1</c>.
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
    /// Removes a segment at the specified position.
    /// </summary>
    /// <remarks>
    /// <see cref="Prefab"/> cannot be empty, this method will not succeed if <see cref="Count"/> is <c>1</c>.
    /// </remarks>
    /// <param name="key">Position of the segment to remove.</param>
    /// <param name="value">The segment that was at the specified position, if there was one.</param>
    /// <returns><see langword="true"/> if the segment was removed; otherwise, <see langword="true"/>.</returns>
    public bool Remove(int3 key, [MaybeNullWhen(false)] out PrefabSegment value)
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
    public bool TryGetValue(int3 key, [MaybeNullWhen(false)] out PrefabSegment value)
#else
    public bool TryGetValue(int3 key, out PrefabSegment value)
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
    void ICollection<KeyValuePair<int3, PrefabSegment>>.Add(KeyValuePair<int3, PrefabSegment> item)
    {
        ValidatePos(item.Key, $"{nameof(item)}.Key");

        PrefabSegment res = ValidateSegment(item.Value, nameof(item) + ".Value");

        res.PosInPrefab = item.Key;
        Add(res);

        Size = int3.Max(Size, item.Key + int3.One);
    }

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<int3, PrefabSegment>>.Contains(KeyValuePair<int3, PrefabSegment> item)
        => ((ICollection<KeyValuePair<int3, PrefabSegment>>)_segments).Contains(item);

    /// <inheritdoc/>
    void ICollection<KeyValuePair<int3, PrefabSegment>>.CopyTo(KeyValuePair<int3, PrefabSegment>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<int3, PrefabSegment>>)_segments).CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<int3, PrefabSegment>>.Remove(KeyValuePair<int3, PrefabSegment> item)
    {
        if (Count == 1)
        {
            ThrowInvalidOperationException($"{nameof(Prefab)} cannot be empty.");
        }

        item.Value.PosInPrefab = item.Key;

        bool removed = ((ICollection<KeyValuePair<int3, PrefabSegment>>)_segments).Remove(item);

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
    public IEnumerable<(PrefabSegment Segment, ushort Id)> EnumerateWithId()
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
    public IEnumerator<KeyValuePair<int3, PrefabSegment>> GetEnumerator()
    {
        for (int z = 0; z < Size.Z; z++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    if (_segments.TryGetValue(new int3(x, y, z), out var segment))
                    {
                        yield return new KeyValuePair<int3, PrefabSegment>(new int3(x, y, z), segment);
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    /// Creates a copy of this <see cref="Prefab"/>.
    /// </summary>
    /// <param name="deepCopy">If deep copy should be performed.</param>
    /// <returns>A copy of this <see cref="Prefab"/>.</returns>
    public Prefab Clone(bool deepCopy)
        => new Prefab(this, deepCopy);

    /// <inheritdoc/>
    object ICloneable.Clone()
        => new Prefab(this, true);

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

    private PrefabSegment ValidateSegment(PrefabSegment? segment, string paramName)
    {
        if (segment is null)
        {
            ThrowArgumentNullException(paramName);
        }

        segment.PrefabId = Id;

        return segment;
    }
}
