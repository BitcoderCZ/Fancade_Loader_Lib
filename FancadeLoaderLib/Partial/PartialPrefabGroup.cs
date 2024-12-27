// <copyright file="PartialPrefabGroup.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Exceptions;
using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FancadeLoaderLib.Partial;

/// <summary>
/// <see cref="PrefabGroup"/> for <see cref="PartialPrefab"/>, usefull for when just the dimensions of groups are needed.
/// </summary>
public class PartialPrefabGroup : IDictionary<byte3, PartialPrefab>, ICloneable
{
	private readonly Dictionary<byte3, PartialPrefab> _prefabs;

	private ushort _id;

	public PartialPrefabGroup()
	{
		_prefabs = [];
		Size = byte3.Zero;
	}

	public PartialPrefabGroup(IEnumerable<PartialPrefab> collection)
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

		Id = id!.Value;

		CalculateSize();
	}

	public PartialPrefabGroup(IEnumerable<PartialPrefab> collection, ushort id)
	{
		if (id == ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException(nameof(id), $"{nameof(id)} cannot be {ushort.MaxValue}");
		}

		Id = id;

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
				throw new ArgumentOutOfRangeException(nameof(collection), $"{nameof(PartialPrefab.PosInGroup)} cannot be negative.");
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

	public PartialPrefabGroup(PartialPrefabGroup group, bool deepCopy)
	{
#pragma warning disable IDE0028 // Simplify collection initialization - no it fucking can't be
		_prefabs = deepCopy
			? new Dictionary<byte3, PartialPrefab>(group._prefabs.Select(item => new KeyValuePair<byte3, PartialPrefab>(item.Key, item.Value.Clone())))
			: new Dictionary<byte3, PartialPrefab>(group._prefabs);
#pragma warning restore IDE0028

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

	public InvalidGroupIdBehaviour InvalidGroupIdBehaviour { get; set; } = InvalidGroupIdBehaviour.ThrowException;

	public ICollection<byte3> Keys => _prefabs.Keys;

	public ICollection<PartialPrefab> Values => _prefabs.Values;

	public int Count => _prefabs.Count;

	public bool IsReadOnly => false;

	public PartialPrefab this[byte3 index]
	{
		get => _prefabs[index];
		set => _prefabs[index] = Validate(value);
	}

	public void SwapPositions(byte3 posA, byte3 posB)
	{
		TryGetValue(posA, out PartialPrefab? a);
		TryGetValue(posB, out PartialPrefab? b);

		if (a is not null)
		{
			a.PosInGroup = posB;
		}

		if (b is not null)
		{
			b.PosInGroup = posA;
		}

		this[posA] = b;
		this[posB] = a;
	}

	public void Add(byte3 key, PartialPrefab value)
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

	public bool TryGetValue(byte3 key, out PartialPrefab value)
		=> _prefabs.TryGetValue(key, out value);

	public void Clear()
	{
		_prefabs.Clear();

		Size = byte3.Zero;
	}

	void ICollection<KeyValuePair<byte3, PartialPrefab>>.Add(KeyValuePair<byte3, PartialPrefab> item)
	{
		PartialPrefab res = Validate(item.Value);
		if (!ReferenceEquals(item.Value, res))
		{
			item = new KeyValuePair<byte3, PartialPrefab>(item.Key, res);
		}

		((ICollection<KeyValuePair<byte3, PartialPrefab>>)_prefabs).Add(item);

		Size = byte3.Max(Size, item.Key + byte3.One);
	}

	bool ICollection<KeyValuePair<byte3, PartialPrefab>>.Contains(KeyValuePair<byte3, PartialPrefab> item)
		=> ((ICollection<KeyValuePair<byte3, PartialPrefab>>)_prefabs).Contains(item);

	void ICollection<KeyValuePair<byte3, PartialPrefab>>.CopyTo(KeyValuePair<byte3, PartialPrefab>[] array, int arrayIndex)
		=> ((ICollection<KeyValuePair<byte3, PartialPrefab>>)_prefabs).CopyTo(array, arrayIndex);

	bool ICollection<KeyValuePair<byte3, PartialPrefab>>.Remove(KeyValuePair<byte3, PartialPrefab> item)
	{
		bool val = ((ICollection<KeyValuePair<byte3, PartialPrefab>>)_prefabs).Remove(item);

		if (val)
		{
			CalculateSize();
		}

		return val;
	}

	public IEnumerator<KeyValuePair<byte3, PartialPrefab>> GetEnumerator()
		=> _prefabs.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> _prefabs.GetEnumerator();

	public IEnumerable<PartialPrefab> EnumerateInIdOrder()
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

	public PartialPrefabGroup Clone(bool deepCopy)
		=> new PartialPrefabGroup(this, deepCopy);

	object ICloneable.Clone()
		=> new PartialPrefabGroup(this, true);

	private void CalculateSize()
	{
		Size = byte3.Zero;

		foreach (var (pos, _) in _prefabs)
			Size = byte3.Max(Size, pos + byte3.One);
	}

	private PartialPrefab Validate(PartialPrefab prefab)
	{
		if (prefab.GroupId != Id)
		{
			switch (InvalidGroupIdBehaviour)
			{
				case InvalidGroupIdBehaviour.ChangeGroupId:
					prefab.GroupId = Id;
					break;
				case InvalidGroupIdBehaviour.CloneAndChangeGroupId:
					{
						PartialPrefab newPrefab = prefab.Clone();
						newPrefab.GroupId = Id;
						return newPrefab;
					}

				case InvalidGroupIdBehaviour.ThrowException:
				default:
					throw new InvalidGroupIdException(Id, prefab.GroupId);
			}
		}

		return prefab;
	}
}
