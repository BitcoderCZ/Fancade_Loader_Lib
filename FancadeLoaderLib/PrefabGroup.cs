// <copyright file="PrefabGroup.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

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
public class PrefabGroup : IDictionary<byte3, Prefab>, ICloneable
{
	private readonly Dictionary<byte3, Prefab> _prefabs;

	private ushort _id;

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabGroup"/> class.
	/// </summary>
	/// <param name="id">Id of this group.</param>
	public PrefabGroup(ushort id)
	{
		_prefabs = [];
		_id = id;
		Size = byte3.Zero;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabGroup"/> class.
	/// </summary>
	/// <param name="collection">The prefabs to be placed in this group, must all have the same id.</param>
	public PrefabGroup(IEnumerable<Prefab> collection)
	{
		if (!collection.Any())
		{
			throw new ArgumentNullException(nameof(collection), $"{nameof(collection)} cannot be empty.");
		}

		ushort? id = null;

		_prefabs = collection.ToDictionary(prefab =>
		{
			// validate
			if (prefab.PosInGroup.X < 0 || prefab.PosInGroup.Y < 0 || prefab.PosInGroup.Z < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(collection), $"{nameof(Prefab.PosInGroup)} cannot be negative.");
			}
			else if (!prefab.IsInGroup)
			{
				throw new ArgumentException($"All prefabs in {nameof(collection)} must be in group", nameof(collection));
			}
			else if (id != null && prefab.GroupId != id)
			{
				throw new ArgumentException($"GroupId must be the same for all prefabs in {nameof(collection)}", nameof(collection));
			}

			id = prefab.GroupId;

			return prefab.PosInGroup;
		});

		_id = id!.Value;

		CalculateSize();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabGroup"/> class.
	/// </summary>
	/// <param name="collection">The prefabs to be placed in this group, group id must be <paramref name="id"/>.</param>
	/// <param name="id">Id of this group.</param>
	public PrefabGroup(IEnumerable<Prefab> collection, ushort id)
	{
		if (id == ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException(nameof(id), $"{nameof(id)} cannot be {ushort.MaxValue}");
		}

		_id = id;

		if (!collection.Any())
		{
			Size = byte3.Zero;
			_prefabs = [];
			return;
		}

		_prefabs = collection.ToDictionary(prefab =>
		{
			// validate
			if (prefab.PosInGroup.X < 0 || prefab.PosInGroup.Y < 0 || prefab.PosInGroup.Z < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(collection), $"{nameof(Prefab.PosInGroup)} cannot be negative.");
			}
			else if (!prefab.IsInGroup)
			{
				throw new ArgumentException($"All prefabs in {nameof(collection)} must be in group", nameof(collection));
			}
			else if (prefab.GroupId != Id)
			{
				throw new ArgumentException($"GroupId must be the same for all prefabs in {nameof(collection)}", nameof(collection));
			}

			return prefab.PosInGroup;
		});

		CalculateSize();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabGroup"/> class.
	/// </summary>
	/// <param name="group">The <see cref="PrefabGroup"/> to copy values from.</param>
	/// <param name="deepCopy">If deep copy should be performed.</param>
	public PrefabGroup(PrefabGroup group, bool deepCopy)
	{
#pragma warning disable IDE0306 // Simplify collection initialization - no it fucking can't be 
		_prefabs = deepCopy
			? new Dictionary<byte3, Prefab>(group._prefabs.Select(item => new KeyValuePair<byte3, Prefab>(item.Key, item.Value.Clone())))
			: new Dictionary<byte3, Prefab>(group._prefabs);
#pragma warning restore IDE0306

		_id = group.Id;

		Size = group.Size;
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
	public Prefab this[byte3 index]
	{
		get => _prefabs[index];
		set => _prefabs[index] = Validate(value);
	}

	/// <summary>
	/// Swaps 2 postitions.
	/// </summary>
	/// <param name="posA">The first postition.</param>
	/// <param name="posB">The second postion.</param>
	public void SwapPositions(byte3 posA, byte3 posB)
	{
		if (TryGetValue(posA, out Prefab? a))
		{
			a.PosInGroup = posB;
			this[posB] = a;
		}

		if (TryGetValue(posB, out Prefab? b))
		{
			b.PosInGroup = posA;
			this[posA] = b;
		}
		else
		{
			Remove(posA);
		}

		if (a is null)
		{
			Remove(posB);
		}
	}

	/// <inheritdoc/>
	public void Add(byte3 key, Prefab value)
	{
		_prefabs.Add(key, Validate(value));

		Size = byte3.Max(Size, key + byte3.One);
	}

	/// <inheritdoc/>
	public bool ContainsKey(byte3 key)
		=> _prefabs.ContainsKey(key);

	/// <inheritdoc/>
	public bool Remove(byte3 key)
	{
		bool val = _prefabs.Remove(key);

		if (val)
		{
			CalculateSize();
		}

		return val;
	}

	/// <inheritdoc/>
#if NET6_0_OR_GREATER
	public bool TryGetValue(byte3 key, [NotNullWhen(true)] out Prefab? value)
#else
	public bool TryGetValue(byte3 key, out Prefab value)
#endif
		=> _prefabs.TryGetValue(key, out value);

	/// <inheritdoc/>
	public void Clear()
	{
		_prefabs.Clear();

		Size = byte3.Zero;
	}

	/// <inheritdoc/>
	void ICollection<KeyValuePair<byte3, Prefab>>.Add(KeyValuePair<byte3, Prefab> item)
	{
		Prefab res = Validate(item.Value);
		if (!ReferenceEquals(item.Value, res))
		{
			item = new KeyValuePair<byte3, Prefab>(item.Key, res);
		}

		((ICollection<KeyValuePair<byte3, Prefab>>)_prefabs).Add(item);

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
		bool val = ((ICollection<KeyValuePair<byte3, Prefab>>)_prefabs).Remove(item);

		if (val)
		{
			CalculateSize();
		}

		return val;
	}

	/// <inheritdoc/>
	public IEnumerator<KeyValuePair<byte3, Prefab>> GetEnumerator()
		=> _prefabs.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
		=> _prefabs.GetEnumerator();

	/// <summary>
	/// Enumerates the prefabs in this group in the order of their ids.
	/// </summary>
	/// <returns>An enumerator that iterates through this groups in id order.</returns>
	public IEnumerable<Prefab> EnumerateInIdOrder()
	{
		for (byte z = 0; z < Size.Z; z++)
		{
			for (byte y = 0; y < Size.Y; y++)
			{
				for (byte x = 0; x < Size.X; x++)
				{
					if (_prefabs.TryGetValue(new byte3(x, y, z), out var prefab))
					{
						yield return prefab;
					}
				}
			}
		}
	}

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

	private void CalculateSize()
	{
		Size = byte3.Zero;

		foreach (var (pos, _) in _prefabs)
			Size = byte3.Max(Size, pos + byte3.One);
	}

	private Prefab Validate(Prefab prefab)
	{
		prefab.GroupId = Id;

		return prefab;
	}
}
