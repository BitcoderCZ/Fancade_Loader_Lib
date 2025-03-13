// <copyright file="PartialPrefabGroup.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Partial;

public sealed class PartialPrefabGroup : ICollection<byte3>, ICloneable
{
	public const int MaxSize = 4;

	/// <summary>
	/// The type of this prefab.
	/// </summary>
	public PrefabType Type;

	// not ordered set
	private readonly List<byte3> _prefabs;

	private ushort _id;

	private string _name;

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabGroup"/> class.
	/// </summary>
	/// <param name="id">Id of this group.</param>
	/// <param name="name">Name of this group.</param>
	/// <param name="type">The type of this group.</param>
	/// <param name="prefabs">The positions of prefabs to be placed in this group.</param>
	public PartialPrefabGroup(ushort id, string name, PrefabType type, IEnumerable<byte3> prefabs)
	{
		if (!prefabs.Any())
		{
			ThrowArgumentException($"{nameof(prefabs)} cannot be empty.", nameof(prefabs));
		}

		_id = id;
		_name = name;
		Type = type;

		_prefabs = [.. prefabs.Select(prefab =>
		{
			// validate
			if (prefab.X >= MaxSize || prefab.Y >= MaxSize || prefab.Z >= MaxSize)
			{
				ThrowArgumentOutOfRangeException(nameof(prefabs), $"{nameof(Prefab.PosInGroup)} cannot be larger than {MaxSize}.");
			}

			return prefab;
		})];

		CalculateSize();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabGroup"/> class.
	/// </summary>
	/// <param name="id">Id of this group.</param>
	public PartialPrefabGroup(ushort id)
		: this(id, "New Block", PrefabType.Normal, [byte3.Zero])
	{
	}

	public PartialPrefabGroup(PrefabGroup group)
	{
		ThrowIfNull(group, nameof(group));

		_id = group.Id;
		_name = group.Name;
		Size = group.Size;
		Type = group.Type;

		_prefabs = [.. group.Keys];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabGroup"/> class.
	/// </summary>
	/// <param name="group">The <see cref="PrefabGroup"/> to copy values from.</param>
	/// <param name="deepCopy">If deep copy should be performed.</param>
	public PartialPrefabGroup(PartialPrefabGroup group)
	{
		if (group is null)
		{
			ThrowArgumentNullException(nameof(group));
		}

		_prefabs = [.. group._prefabs];

		_id = group.Id;
		_name = group._name;
		Type = group.Type;
		Size = group.Size;
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
				ThrowArgumentNullException(nameof(value), $"{nameof(Name)} cannot be null.");
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
		set => _id = value;
	}

	/// <summary>
	/// Gets the size of this group.
	/// </summary>
	/// <value>Size of this group.</value>
	public byte3 Size { get; private set; }

	/// <inheritdoc/>
	public int Count => _prefabs.Count;

	/// <inheritdoc/>
	public bool IsReadOnly => false;

	/// <inheritdoc/>
	public void Add(byte3 pos)
	{
		ValidatePos(pos, nameof(pos));

		if (!_prefabs.Contains(pos))
		{
			_prefabs.Add(pos);
			Size = byte3.Max(Size, pos + byte3.One);
		}
	}

	public bool TryAdd(byte3 pos)
	{
		ValidatePos(pos, nameof(pos));

		if (_prefabs.Contains(pos))
		{
			return false;
		}

		_prefabs.Add(pos);
		Size = byte3.Max(Size, pos + byte3.One);
		return true;
	}

	/// <inheritdoc/>
	public bool Contains(byte3 key)
		=> _prefabs.Contains(key);

	/// <inheritdoc/>
	public bool Remove(byte3 key)
	{
		if (Count == 1)
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

	public int IndexOf(byte3 key)
		=> _prefabs.IndexOf(key);

	/// <inheritdoc/>
	public void Clear()
	{
		_prefabs.Clear();

		Size = byte3.Zero;
	}

	public IEnumerable<(byte3 Pos, ushort Id)> EnumerateWithId()
	{
		ushort id = Id;

		foreach (var prefab in _prefabs)
		{
			yield return (prefab, id++);
		}
	}

	/// <inheritdoc/>
	public IEnumerator<byte3> GetEnumerator()
		=> _prefabs.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
		=> _prefabs.GetEnumerator();

	/// <summary>
	/// Creates a copy of this <see cref="PartialPrefabGroup"/>.
	/// </summary>
	/// <returns>A copy of this <see cref="PartialPrefabGroup"/>.</returns>
	public PartialPrefabGroup Clone()
		=> new PartialPrefabGroup(this);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PartialPrefabGroup(this);

	public void CopyTo(byte3[] array, int arrayIndex)
	{
		if (Count + arrayIndex > array.Length)
		{
			ThrowArgumentOutOfRangeException(nameof(arrayIndex));
		}

		CollectionsMarshal.AsSpan(_prefabs).CopyTo(array.AsSpan(arrayIndex));
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

		foreach (var pos in _prefabs)
		{
			Size = byte3.Max(Size, pos + byte3.One);
		}
	}
}