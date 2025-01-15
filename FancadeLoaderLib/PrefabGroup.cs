// <copyright file="PrefabGroup.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Exceptions;
using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;

namespace FancadeLoaderLib;

public class PrefabGroup : IDictionary<byte3, Prefab>, ICloneable
{
	private readonly Dictionary<byte3, Prefab> _prefabs;

	private ushort _id;

	public PrefabGroup(ushort id)
	{
		_prefabs = [];
		_id = id;
		Size = byte3.Zero;
	}

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

	public byte3 Size { get; private set; }

	public ICollection<byte3> Keys => _prefabs.Keys;

	public ICollection<Prefab> Values => _prefabs.Values;

	public int Count => _prefabs.Count;

	public bool IsReadOnly => false;

	public Prefab this[byte3 index]
	{
		get => _prefabs[index];
		set => _prefabs[index] = Validate(value);
	}

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

	public void Add(byte3 key, Prefab value)
	{
		_prefabs.Add(key, Validate(value));

		Size = byte3.Max(Size, key + byte3.One);
	}

	public bool ContainsKey(byte3 key)
		=> _prefabs.ContainsKey(key);

	public bool Remove(byte3 key)
	{
		bool val = _prefabs.Remove(key);

		if (val)
		{
			CalculateSize();
		}

		return val;
	}

	public bool TryGetValue(byte3 key, [NotNullWhen(true)] out Prefab? value)
		=> _prefabs.TryGetValue(key, out value);

	public void Clear()
	{
		_prefabs.Clear();

		Size = byte3.Zero;
	}

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

	bool ICollection<KeyValuePair<byte3, Prefab>>.Contains(KeyValuePair<byte3, Prefab> item)
		=> ((ICollection<KeyValuePair<byte3, Prefab>>)_prefabs).Contains(item);

	void ICollection<KeyValuePair<byte3, Prefab>>.CopyTo(KeyValuePair<byte3, Prefab>[] array, int arrayIndex)
		=> ((ICollection<KeyValuePair<byte3, Prefab>>)_prefabs).CopyTo(array, arrayIndex);

	bool ICollection<KeyValuePair<byte3, Prefab>>.Remove(KeyValuePair<byte3, Prefab> item)
	{
		bool val = ((ICollection<KeyValuePair<byte3, Prefab>>)_prefabs).Remove(item);

		if (val)
		{
			CalculateSize();
		}

		return val;
	}

	public IEnumerator<KeyValuePair<byte3, Prefab>> GetEnumerator()
		=> _prefabs.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> _prefabs.GetEnumerator();

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

	public PrefabGroup Clone(bool deepCopy)
		=> new PrefabGroup(this, deepCopy);

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
