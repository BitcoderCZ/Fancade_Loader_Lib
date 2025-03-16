// <copyright file="PartialPrefabGroup.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Partial;

public sealed class PartialPrefabGroup : IDictionary<byte3, PartialPrefab>, ICloneable
{
	public const int MaxSize = 4;

	/// <summary>
	/// The type of this prefab.
	/// </summary>
	public PrefabType Type;

	private readonly OrderedDictionary<byte3, PartialPrefab> _prefabs;

	private ushort _id;

	private string _name;

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabGroup"/> class.
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
	public PartialPrefabGroup(ushort id, string name, PrefabType type, IEnumerable<PartialPrefab> prefabs)
	{
		if (!prefabs.Any())
		{
			ThrowArgumentException($"{nameof(prefabs)} cannot be empty.", nameof(prefabs));
		}

		if (string.IsNullOrEmpty(name))
		{
			ThrowArgumentException($"{nameof(name)} cannot be null or empty.", nameof(name));
		}

		_id = id;
		_name = name;
		Type = type;

		_prefabs = new(prefabs.Select(prefab =>
		{
			// validate
			if (prefab.PosInGroup.X >= MaxSize || prefab.PosInGroup.Y >= MaxSize || prefab.PosInGroup.Z >= MaxSize)
			{
				ThrowArgumentOutOfRangeException(nameof(prefabs), $"{nameof(PartialPrefab.PosInGroup)} cannot be larger than {MaxSize}.");
			}
			else if (prefab.GroupId != Id)
			{
				ThrowArgumentException($"GroupId must be the same for all prefabs in {nameof(prefabs)}", nameof(prefabs));
			}

			return new KeyValuePair<byte3, PartialPrefab>(prefab.PosInGroup, prefab);
		}));

		CalculateSize();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabGroup"/> class.
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
	public PartialPrefabGroup(string name, PrefabType type, IEnumerable<PartialPrefab> prefabs)
	{
		if (!prefabs.Any())
		{
			ThrowArgumentException(nameof(prefabs), $"{nameof(prefabs)} cannot be empty.");
		}

		if (string.IsNullOrEmpty(name))
		{
			ThrowArgumentException($"{nameof(name)} cannot be null or empty.", nameof(name));
		}

		_name = name;
		Type = type;

		ushort? id = null;

		_prefabs = new(prefabs.Select(prefab =>
		{
			// validate
			if (prefab.PosInGroup.X >= MaxSize || prefab.PosInGroup.Y >= MaxSize || prefab.PosInGroup.Z >= MaxSize)
			{
				ThrowArgumentOutOfRangeException(nameof(prefabs), $"{nameof(PartialPrefab.PosInGroup)} cannot be larger than {MaxSize}.");
			}
			else if (id == null && prefab.GroupId != id)
			{
				ThrowArgumentException($"{nameof(PartialPrefab.GroupId)} must be the same for all prefabs in {nameof(prefabs)}.", nameof(prefabs));
			}

			id = prefab.GroupId;

			return new KeyValuePair<byte3, PartialPrefab>(prefab.PosInGroup, prefab);
		}));

		_id = id!.Value;

		CalculateSize();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabGroup"/> class.
	/// </summary>
	/// <param name="id">Id of this group.</param>
	public PartialPrefabGroup(ushort id)
		: this(id, "New Block", PrefabType.Normal, [new PartialPrefab(id, byte3.Zero)])
	{
	}

	public PartialPrefabGroup(PrefabGroup group)
	{
		ThrowIfNull(group, nameof(group));

		_id = group.Id;
		_name = group.Name;
		Size = group.Size;
		Type = group.Type;

		_prefabs = new OrderedDictionary<byte3, PartialPrefab>(group.Select(item => new KeyValuePair<byte3, PartialPrefab>(item.Key, new PartialPrefab(item.Value.GroupId, item.Value.PosInGroup))));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabGroup"/> class.
	/// </summary>
	/// <param name="group">The <see cref="PrefabGroup"/> to copy values from.</param>
	/// <param name="deepCopy">If deep copy should be performed.</param>
	public PartialPrefabGroup(PartialPrefabGroup group, bool deepCopy)
	{
		if (group is null)
		{
			ThrowArgumentNullException(nameof(group));
		}

#pragma warning disable IDE0306 // Simplify collection initialization - no it fucking can't be 
		_prefabs = deepCopy
			? new OrderedDictionary<byte3, PartialPrefab>(group._prefabs.Select(item => new KeyValuePair<byte3, PartialPrefab>(item.Key, item.Value.Clone())))
			: new OrderedDictionary<byte3, PartialPrefab>(group._prefabs);
#pragma warning restore IDE0306

		_id = group.Id;

		Size = group.Size;

		_name = group._name;
		Type = group.Type;
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
			if (string.IsNullOrEmpty(value))
			{
				ThrowArgumentException($"{nameof(Name)} cannot be null or empty.", nameof(value));
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
	public ICollection<PartialPrefab> Values => _prefabs.Values;

	/// <inheritdoc/>
	public int Count => _prefabs.Count;

	/// <inheritdoc/>
	public bool IsReadOnly => false;

	/// <inheritdoc/>
	[SuppressMessage("Design", "CA1043:Use Integral Or String Argument For Indexers", Justification = "It makes sense to use byte3 here.")]
	public PartialPrefab this[byte3 index]
	{
		get => _prefabs[index];
		set => _prefabs[index] = ValidatePrefab(value, nameof(value));
	}

	/// <inheritdoc/>
	public void Add(byte3 key, PartialPrefab value)
	{
		ValidatePos(key, nameof(key));

		_prefabs.Add(key, ValidatePrefab(value, nameof(value)));

		value.PosInGroup = key; // only change pos if successfully added

		Size = byte3.Max(Size, key + byte3.One);
	}

	public void Add(PartialPrefab prefab)
	{
		ValidatePos(prefab.PosInGroup, $"{nameof(prefab)}.{nameof(prefab.PosInGroup)}");

		_prefabs.Add(prefab.PosInGroup, ValidatePrefab(prefab, nameof(prefab)));

		Size = byte3.Max(Size, prefab.PosInGroup + byte3.One);
	}

	public bool TryAdd(byte3 key, PartialPrefab prefab)
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

	public bool TryAdd(PartialPrefab prefab)
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

	public bool Remove(byte3 key, [MaybeNullWhen(false)] out PartialPrefab prefab)
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
	public bool TryGetValue(byte3 key, [MaybeNullWhen(false)] out PartialPrefab value)
#else
	public bool TryGetValue(byte3 key, out PartialPrefab value)
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
	void ICollection<KeyValuePair<byte3, PartialPrefab>>.Add(KeyValuePair<byte3, PartialPrefab> item)
	{
		ValidatePos(item.Key, $"{nameof(item)}.Key");

		PartialPrefab res = ValidatePrefab(item.Value, nameof(item) + ".Value");

		if (!ReferenceEquals(item.Value, res))
		{
			item = new KeyValuePair<byte3, PartialPrefab>(item.Key, res);
		}

		((ICollection<KeyValuePair<byte3, PartialPrefab>>)_prefabs).Add(item);

		item.Value.PosInGroup = item.Key; // only change pos if successfully added

		Size = byte3.Max(Size, item.Key + byte3.One);
	}

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<byte3, PartialPrefab>>.Contains(KeyValuePair<byte3, PartialPrefab> item)
		=> ((ICollection<KeyValuePair<byte3, PartialPrefab>>)_prefabs).Contains(item);

	/// <inheritdoc/>
	void ICollection<KeyValuePair<byte3, PartialPrefab>>.CopyTo(KeyValuePair<byte3, PartialPrefab>[] array, int arrayIndex)
		=> ((ICollection<KeyValuePair<byte3, PartialPrefab>>)_prefabs).CopyTo(array, arrayIndex);

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<byte3, PartialPrefab>>.Remove(KeyValuePair<byte3, PartialPrefab> item)
	{
		// can't remove the first prefab
		if (Count == 1 || item.Key == _prefabs.GetAt(0).Key)
		{
			ThrowInvalidOperationException($"{nameof(PartialPrefabGroup)} cannot be empty.");
		}

		bool removed = ((ICollection<KeyValuePair<byte3, PartialPrefab>>)_prefabs).Remove(item);

		if (removed)
		{
			CalculateSize();
		}

		return removed;
	}

	public IEnumerable<(PartialPrefab Prefab, ushort Id)> EnumerateWithId()
	{
		ushort id = Id;

		foreach (var prefab in _prefabs.Values)
		{
			yield return (prefab, id++);
		}
	}

	/// <inheritdoc/>
	public IEnumerator<KeyValuePair<byte3, PartialPrefab>> GetEnumerator()
		=> _prefabs.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
		=> _prefabs.GetEnumerator();

	/// <summary>
	/// Creates a copy of this <see cref="PrefabGroup"/>.
	/// </summary>
	/// <param name="deepCopy">If deep copy should be performed.</param>
	/// <returns>A copy of this <see cref="PrefabGroup"/>.</returns>
	public PartialPrefabGroup Clone(bool deepCopy)
		=> new PartialPrefabGroup(this, deepCopy);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PartialPrefabGroup(this, true);

	/// <summary>
	/// Creates <see cref="PartialPrefabGroup"/> from <see cref="OldPartialPrefab"/>s.
	/// </summary>
	/// <remarks>
	/// For <see cref="Name"/> and <see cref="Type"/> uses the first <see cref="OldPartialPrefab"/>.
	/// </remarks>
	/// <param name="id">The id of the group.</param>
	/// <param name="prefabs">The <see cref="OldPartialPrefab"/>s to convert. All prefabs must have a distinct <see cref="OldPartialPrefab.PosInGroup"/>.</param>
	/// <returns>The converted <see cref="Prefab"/>.</returns>
	internal static unsafe PartialPrefabGroup FromRaw(ushort id, IEnumerable<OldPartialPrefab> prefabs)
	{
		if (prefabs is null)
		{
			ThrowArgumentNullException(nameof(prefabs));
		}

		OldPartialPrefab? rawPrefab = prefabs.FirstOrDefault();

		if (rawPrefab is null)
		{
			ThrowArgumentException($"{nameof(prefabs)} cannot be empty.", nameof(prefabs));
		}

		return new PartialPrefabGroup(id, rawPrefab.Value.Name, rawPrefab.Value.Type, prefabs.Select(prefab =>
		{
			return new PartialPrefab(id, prefab.PosInGroup);
		}));
	}

	/// <summary>
	/// Converts this <see cref="PartialPrefabGroup"/> into <see cref="OldPartialPrefab"/>s.
	/// </summary>
	/// <returns>A new instance of the <see cref="OldPartialPrefab"/> class from this <see cref="PartialPrefabGroup"/>.</returns>
	internal IEnumerable<OldPartialPrefab> ToRaw()
	{
		int i = 0;
		foreach (var (posInGroup, prefab) in this)
		{
			yield return i == 0
				? new OldPartialPrefab(
					name: Name,
					type: Type,
					groupId: Id,
					posInGroup: posInGroup)
				: new OldPartialPrefab(
					name: "New Block",
					type: Type,
					groupId: Id,
					posInGroup: posInGroup);

			i++;
		}
	}

	private static void ValidatePos(byte3 pos, string paramName)
	{
		if (pos.X >= MaxSize || pos.Y >= MaxSize || pos.Z >= MaxSize)
		{
			ThrowArgumentOutOfRangeException(paramName, $"{paramName} cannot be larger than {MaxSize}.");
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

	private PartialPrefab ValidatePrefab(PartialPrefab? prefab, string paramName)
	{
		if (prefab is null)
		{
			ThrowArgumentNullException(paramName);
		}

		prefab.GroupId = Id;

		return prefab;
	}
}