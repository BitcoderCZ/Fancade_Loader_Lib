// <copyright file="PrefabGroup.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FancadeLoaderLib;

/// <summary>
/// Represents a group of prefabs - a block made from multiple prefabs.
/// </summary>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Group is a better suffix.")]
public sealed class PrefabGroup : IDictionary<byte3, Prefab>, ICloneable
{
	public const int MaxSize = 4;

	/// <summary>
	/// The blocks inside this group.
	/// </summary>
	public readonly BlockData Blocks;

	/// <summary>
	/// Settings of the blocks inside this prefab.
	/// </summary>
	public readonly List<PrefabSetting> Settings;

	/// <summary>
	/// Connections between blocks inside this prefab, block-block and block-outside of this prefab.
	/// </summary>
	public readonly List<Connection> Connections;

	/// <summary>
	/// The collider of this prefab.
	/// </summary>
	public PrefabCollider Collider;

	/// <summary>
	/// The type of this prefab.
	/// </summary>
	public PrefabType Type;

	/// <summary>
	/// The background color of this prefab.
	/// </summary>
	public FcColor BackgroundColor;

	/// <summary>
	/// If this prefab is editable.
	/// </summary>
	public bool Editable;

	private readonly OrderedDictionary<byte3, Prefab> _prefabs;

	private ushort _id;

	private string _name;

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabGroup"/> class.
	/// </summary>
	/// <param name="id">Id of this group.</param>
	/// <param name="name">Name of this group.</param>
	/// <param name="collider">The collider of this group.</param>
	/// <param name="type">The type of this group.</param>
	/// <param name="backgroundColor">The background color of this group.</param>
	/// <param name="editable">If this group is editable.</param>
	/// <param name="voxels">Voxels/model of this group.</param>
	/// <param name="blocks">The blocks inside this group.</param>
	/// <param name="settings">Settings of the blocks inside this group.</param>
	/// <param name="connections">Connections between blocks inside this group, block-block and block-outside of this group.</param>
	/// <param name="prefabs">The prefabs to be placed in this group, must all have the same id.</param>
	public PrefabGroup(ushort id, string name, PrefabCollider collider, PrefabType type, FcColor backgroundColor, bool editable, BlockData? blocks, List<PrefabSetting>? settings, List<Connection>? connections, IEnumerable<Prefab> prefabs)
	{
		if (!prefabs.Any())
		{
			ThrowHelper.ThrowArgumentException($"{nameof(prefabs)} cannot be empty.", nameof(prefabs));
		}

		_id = id;
		_name = name;
		Collider = collider;
		Type = type;
		BackgroundColor = backgroundColor;
		Editable = editable;
		Blocks = blocks ?? new BlockData();
		Settings = settings ?? [];
		Connections = connections ?? [];

		_prefabs = new(prefabs.Select(prefab =>
		{
			// validate
			if (prefab.PosInGroup.X >= MaxSize || prefab.PosInGroup.Y >= MaxSize || prefab.PosInGroup.Z >= MaxSize)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(nameof(prefabs), $"{nameof(Prefab.PosInGroup)} cannot be larger than {MaxSize}.");
			}
			else if (prefab.GroupId != Id)
			{
				ThrowHelper.ThrowArgumentException($"GroupId must be the same for all prefabs in {nameof(prefabs)}", nameof(prefabs));
			}

			return new KeyValuePair<byte3, Prefab>(prefab.PosInGroup, prefab);
		}));

		CalculateSize();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabGroup"/> class.
	/// </summary>
	/// <param name="id">Id of this group.</param>
	/// <param name="name">Name of this group.</param>
	/// <param name="collider">The collider of this group.</param>
	/// <param name="type">The type of this group.</param>
	/// <param name="backgroundColor">The background color of this group.</param>
	/// <param name="editable">If this group is editable.</param>
	/// <param name="voxels">Voxels/model of this group.</param>
	/// <param name="blocks">The blocks inside this group.</param>
	/// <param name="settings">Settings of the blocks inside this group.</param>
	/// <param name="connections">Connections between blocks inside this group, block-block and block-outside of this group.</param>
	/// <param name="prefabs">The prefabs to be placed in this group, must all have the same id.</param>
	public PrefabGroup(string name, PrefabCollider collider, PrefabType type, FcColor backgroundColor, bool editable, BlockData? blocks, List<PrefabSetting>? settings, List<Connection>? connections, IEnumerable<Prefab> prefabs)
	{
		if (!prefabs.Any())
		{
			ThrowHelper.ThrowArgumentException(nameof(prefabs), $"{nameof(prefabs)} cannot be empty.");
		}

		_name = name;
		Collider = collider;
		Type = type;
		BackgroundColor = backgroundColor;
		Editable = editable;
		Blocks = blocks ?? new BlockData();
		Settings = settings ?? [];
		Connections = connections ?? [];

		ushort? id = null;

		_prefabs = new(prefabs.Select(prefab =>
		{
			// validate
			if (prefab.PosInGroup.X >= MaxSize || prefab.PosInGroup.Y >= MaxSize || prefab.PosInGroup.Z >= MaxSize)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(nameof(prefabs), $"{nameof(Prefab.PosInGroup)} cannot be larger than {MaxSize}.");
			}
			else if (id == null && prefab.GroupId != id)
			{
				ThrowHelper.ThrowArgumentException($"{nameof(Prefab.GroupId)} must be the same for all prefabs in {nameof(prefabs)}.", nameof(prefabs));
			}

			id = prefab.GroupId;

			return new KeyValuePair<byte3, Prefab>(prefab.PosInGroup, prefab);
		}));

		_id = id!.Value;

		CalculateSize();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabGroup"/> class.
	/// </summary>
	/// <param name="id">Id of this group.</param>
	public PrefabGroup(ushort id)
		: this(id, "New Block", PrefabCollider.Box, PrefabType.Normal, FcColorUtils.DefaultBackgroundColor, true, new BlockData(), [], [], [new Prefab(id, byte3.Zero)])
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabGroup"/> class.
	/// </summary>
	/// <param name="group">The <see cref="PrefabGroup"/> to copy values from.</param>
	/// <param name="deepCopy">If deep copy should be performed.</param>
	public PrefabGroup(PrefabGroup group, bool deepCopy)
	{
		if (group is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(group));
		}

#pragma warning disable IDE0306 // Simplify collection initialization - no it fucking can't be 
		_prefabs = deepCopy
			? new OrderedDictionary<byte3, Prefab>(group._prefabs.Select(item => new KeyValuePair<byte3, Prefab>(item.Key, item.Value.Clone())))
			: new OrderedDictionary<byte3, Prefab>(group._prefabs);
#pragma warning restore IDE0306

		_id = group.Id;

		Size = group.Size;

		_name = group._name;
		Collider = group.Collider;
		Type = group.Type;
		BackgroundColor = group.BackgroundColor;
		Editable = group.Editable;
		Blocks = deepCopy ? group.Blocks.Clone() : group.Blocks;
		Settings = deepCopy ? [.. group.Settings] : group.Settings;
		Connections = deepCopy ? [.. group.Connections] : group.Connections;
	}

	/// <summary>
	/// Gets or sets the name of this group.
	/// </summary>
	/// <remarks>
	/// Cannot be longer than 255 bytes when encoded as UTF8.
	/// </remarks>
	/// <value>Name of this group.</value>
	public string Name
	{
		get => _name;
		set
		{
			if (value is null)
			{
				ThrowHelper.ThrowArgumentNullException(nameof(value), $"{nameof(Name)} cannot be null.");
			}

			_name = value;
		}
	}

	/// <summary>
	/// Gets or sets the id of this groups.
	/// </summary>
	/// <value>Id of this group.</value>
	public ushort Id
	{
		get => _id;
		set
		{
			foreach (var prefab in Values)
			{
				prefab.GroupId = value;
			}

			_id = value;
		}
	}

	/// <summary>
	/// Gets the size of this group.
	/// </summary>
	/// <value>Size of this group.</value>
	public byte3 Size { get; private set; }

	/// <inheritdoc/>
	public ICollection<byte3> Keys => _prefabs.Keys;

	/// <inheritdoc/>
	public ICollection<Prefab> Values => _prefabs.Values;

	/// <inheritdoc/>
	public int Count => _prefabs.Count;

	/// <inheritdoc/>
	public bool IsReadOnly => false;

	/// <inheritdoc/>
	[SuppressMessage("Design", "CA1043:Use Integral Or String Argument For Indexers", Justification = "It makes sense to use byte3 here.")]
	public Prefab this[byte3 index]
	{
		get => _prefabs[index];
		set => _prefabs[index] = ValidatePrefab(value, nameof(value));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabGroup"/> class, with the default values for a block.
	/// </summary>
	/// <param name="id">Id of the group.</param>
	/// <param name="name">Name of the group.</param>
	/// <returns>The new instance of <see cref="PrefabGroup"/>.</returns>
	public static PrefabGroup CreateBlock(ushort id, string name)
		=> new PrefabGroup(id, name, PrefabCollider.Box, PrefabType.Normal, FcColorUtils.DefaultBackgroundColor, true, new(), [], [], [new Prefab(id, byte3.Zero, new Voxel[8 * 8 * 8])]);

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabGroup"/> class, with the default values for a level.
	/// </summary>
	/// <param name="id">Id of the group.</param>
	/// <param name="name">Name of the group.</param>
	/// <returns>The new instance of <see cref="PrefabGroup"/>.</returns>
	public static PrefabGroup CreateLevel(ushort id, string name)
		=> new PrefabGroup(id)
		{
			Name = name,
			Collider = PrefabCollider.None,
			Type = PrefabType.Level,
		};

	/// <summary>
	/// Creates <see cref="PrefabGroup"/> from <see cref="RawPrefab"/>s.
	/// </summary>
	/// <remarks>
	/// For <see cref="Blocks"/>, <see cref="Settings"/>, ... uses the first <see cref="RawPrefab"/>.
	/// </remarks>
	/// <param name="id">The id of the group.</param>
	/// <param name="rawPrefabs">The <see cref="RawPrefab"/>s to convert. All prefabs must have a distinct <see cref="RawPrefab.PosInGroup"/>.</param>
	/// <param name="idOffset">The offset at which <paramref name="idOffsetAddition"/> starts to be applied.</param>
	/// <param name="idOffsetAddition">Added to blocks, if the block's id is >= <paramref name="idOffset"/>.</param>
	/// <param name="clone">If true clones Blocks, Settings and Connections; else the values are assigned directly and the prefabs in <paramref name="rawPrefabs"/> shouldn't be used anymore.</param>
	/// <returns>The converted <see cref="Prefab"/>.</returns>
	public static unsafe PrefabGroup FromRaw(ushort id, IEnumerable<RawPrefab> rawPrefabs, ushort idOffset, short idOffsetAddition, bool clone = true)
	{
		if (rawPrefabs is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(rawPrefabs));
		}

		RawPrefab? rawPrefab = rawPrefabs.FirstOrDefault();

		if (rawPrefab is null)
		{
			ThrowHelper.ThrowArgumentException($"{nameof(rawPrefabs)} cannot be empty.", nameof(rawPrefabs));
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
				ThrowHelper.ThrowArgumentException($"{nameof(rawPrefab)}.{nameof(RawPrefab.HasConnections)} is true, while {nameof(rawPrefab)}.{nameof(RawPrefab.Connections)} is null", nameof(rawPrefab));
			}

			connections = clone
				? [.. rawPrefab.Connections]
				: rawPrefab.Connections;
		}

		return new PrefabGroup(id, name, collider, type, backgroundColor, editable, blockData, settings, connections, rawPrefabs.Select(prefab =>
		{
			Voxel[]? voxels = null;
			if (prefab.HasVoxels && prefab.Voxels is not null)
			{
				voxels = Prefab.VoxelsFromRaw(prefab.Voxels);
			}

			return new Prefab(id, prefab.PosInGroup, voxels);
		}));
	}

	/// <summary>
	/// Converts this <see cref="PrefabGroup"/> into <see cref="RawPrefab"/>s.
	/// </summary>
	/// <param name="clone">If the prefabs should be copied, if <see langword="true"/>, this <see cref="RawPrefab"/> instance shouldn't be used anymore.</param>
	/// <returns>A new instance of the <see cref="RawPrefab"/> class from this <see cref="game"/>.</returns>
	public IEnumerable<RawPrefab> ToRaw(bool clone)
	{
		Blocks.Trim();

		int i = 0;
		foreach (var (posInGroup, prefab) in this)
		{
			byte[]? voxels = null;

			if (prefab.Voxels is not null)
			{
				voxels = Prefab.VoxelsToRaw(prefab.Voxels);
			}

			yield return i == 0
				? new RawPrefab(
					hasConnections: Connections is not null && Connections.Count > 0,
					hasSettings: Settings is not null && Settings.Count > 0,
					hasBlocks: Blocks is not null && Blocks.Size != int3.Zero,
					hasVoxels: Type != PrefabType.Level && prefab.Voxels is not null,
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
					posInGroup: posInGroup,
					voxels: voxels,
					blocks: Blocks is null ? null : (clone ? Blocks.Array.Clone() : Blocks.Array),
					settings: clone && Settings is not null ? [.. Settings] : Settings,
					connections: clone && Connections is not null ? [.. Connections] : Connections)
				: new RawPrefab(
					hasConnections: false,
					hasSettings: false,
					hasBlocks: false,
					hasVoxels: Type != PrefabType.Level && prefab.Voxels is not null,
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
					posInGroup: posInGroup,
					voxels: voxels,
					blocks: null,
					settings: null,
					connections: null);

			i++;
		}
	}

	/// <inheritdoc/>
	public void Add(byte3 key, Prefab value)
	{
		ValidatePos(key, nameof(key));

		_prefabs.Add(key, ValidatePrefab(value, nameof(value)));

		value.PosInGroup = key; // only change pos if successfully added

		Size = byte3.Max(Size, key + byte3.One);
	}

	public void Add(Prefab prefab)
	{
		ValidatePos(prefab.PosInGroup, $"{nameof(prefab)}.{nameof(prefab.PosInGroup)}");

		_prefabs.Add(prefab.PosInGroup, ValidatePrefab(prefab, nameof(prefab)));

		Size = byte3.Max(Size, prefab.PosInGroup + byte3.One);
	}

	public bool TryAdd(byte3 key, Prefab prefab)
	{
		ValidatePos(key, nameof(key));

		if (!_prefabs.TryAdd(key, ValidatePrefab(prefab, nameof(prefab))))
		{
			return false;
		}

		prefab.PosInGroup = key; // only change pos if successfully added

		Size = byte3.Max(Size, key + byte3.One);
		return true;
	}

	public bool TryAdd(Prefab prefab)
	{
		ValidatePos(prefab.PosInGroup, $"{nameof(prefab)}.{nameof(prefab.PosInGroup)}");

		if (!_prefabs.TryAdd(prefab.PosInGroup, ValidatePrefab(prefab, nameof(prefab))))
		{
			return false;
		}

		Size = byte3.Max(Size, prefab.PosInGroup + byte3.One);
		return true;
	}

	/// <inheritdoc/>
	public bool ContainsKey(byte3 key)
		=> _prefabs.ContainsKey(key);

	/// <inheritdoc/>
	public bool Remove(byte3 key)
	{
		// can't remove the first prefab
		if (Count == 1 || key == _prefabs.GetAt(0).Key)
		{
			return false;
		}

		bool removed = _prefabs.Remove(key);

		if (removed)
		{
			CalculateSize();
		}

		return removed;
	}

	public bool Remove(byte3 key, [MaybeNullWhen(false)] out Prefab prefab)
	{
		// can't remove the first prefab
		if (Count == 1 || key == _prefabs.GetAt(0).Key)
		{
			prefab = null;
			return false;
		}

		bool removed = _prefabs.Remove(key, out prefab);

		if (removed)
		{
			CalculateSize();
		}

		return removed;
	}

	/// <inheritdoc/>
#if NET5_0_OR_GREATER
	public bool TryGetValue(byte3 key, [MaybeNullWhen(false)] out Prefab value)
#else
	public bool TryGetValue(byte3 key, out Prefab value)
#endif
		=> _prefabs.TryGetValue(key, out value);

	public int IndexOf(byte3 key)
		=> _prefabs.IndexOf(key);

	/// <inheritdoc/>
	public void Clear()
	{
		_prefabs.Clear();

		Size = byte3.Zero;
	}

	/// <inheritdoc/>
	void ICollection<KeyValuePair<byte3, Prefab>>.Add(KeyValuePair<byte3, Prefab> item)
	{
		ValidatePos(item.Key, $"{nameof(item)}.Key");

		Prefab res = ValidatePrefab(item.Value, nameof(item) + ".Value");

		if (!ReferenceEquals(item.Value, res))
		{
			item = new KeyValuePair<byte3, Prefab>(item.Key, res);
		}

		((ICollection<KeyValuePair<byte3, Prefab>>)_prefabs).Add(item);

		item.Value.PosInGroup = item.Key; // only change pos if successfully added

		Size = byte3.Max(Size, item.Key + byte3.One);
	}

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<byte3, Prefab>>.Contains(KeyValuePair<byte3, Prefab> item)
		=> ((ICollection<KeyValuePair<byte3, Prefab>>)_prefabs).Contains(item);

	/// <inheritdoc/>
	void ICollection<KeyValuePair<byte3, Prefab>>.CopyTo(KeyValuePair<byte3, Prefab>[] array, int arrayIndex)
		=> ((ICollection<KeyValuePair<byte3, Prefab>>)_prefabs).CopyTo(array, arrayIndex);

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<byte3, Prefab>>.Remove(KeyValuePair<byte3, Prefab> item)
	{
		// can't remove the first prefab
		if (Count == 1 || item.Key == _prefabs.GetAt(0).Key)
		{
			ThrowHelper.ThrowInvalidOperationException($"{nameof(PrefabGroup)} cannot be empty.");
		}

		bool removed = ((ICollection<KeyValuePair<byte3, Prefab>>)_prefabs).Remove(item);

		if (removed)
		{
			CalculateSize();
		}

		return removed;
	}

	public IEnumerable<(Prefab Prefab, ushort Id)> EnumerateWithId()
	{
		ushort id = Id;

		foreach (var prefab in _prefabs.Values)
		{
			yield return (prefab, id++);
		}
	}

	/// <inheritdoc/>
	public IEnumerator<KeyValuePair<byte3, Prefab>> GetEnumerator()
		=> _prefabs.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
		=> _prefabs.GetEnumerator();

	/// <summary>
	/// Creates a copy of this <see cref="PrefabGroup"/>.
	/// </summary>
	/// <param name="deepCopy">If deep copy should be performed.</param>
	/// <returns>A copy of this <see cref="PrefabGroup"/>.</returns>
	public PrefabGroup Clone(bool deepCopy)
		=> new PrefabGroup(this, deepCopy);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PrefabGroup(this, true);

	private static void ValidatePos(byte3 pos, string paramName)
	{
		if (pos.X >= MaxSize || pos.Y >= MaxSize || pos.Z >= MaxSize)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(paramName, $"{paramName} cannot be larger than {MaxSize}.");
		}
	}

	private void CalculateSize()
	{
		Size = byte3.Zero;

		foreach (var pos in _prefabs.Keys)
		{
			Size = byte3.Max(Size, pos + byte3.One);
		}
	}

	private Prefab ValidatePrefab(Prefab? prefab, string paramName)
	{
		if (prefab is null)
		{
			ThrowHelper.ThrowArgumentNullException(paramName);
		}

		prefab.GroupId = Id;

		return prefab;
	}
}
