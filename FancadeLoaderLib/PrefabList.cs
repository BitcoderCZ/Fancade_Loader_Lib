// <copyright file="PrefabList.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FancadeLoaderLib;

/// <summary>
/// <see cref="List{T}"/> wrapper for easier <see cref="Prefab"/> handeling.
/// Also allows for saving/loading.
/// </summary>
public class PrefabList : IList<Prefab>, ICloneable
{
	public ushort IdOffset = RawGame.CurrentNumbStockPrefabs;

	internal readonly List<Prefab> _list;

	public PrefabList()
	{
		_list = [];
	}

	public PrefabList(int capacity)
	{
		_list = new List<Prefab>(capacity);
	}

	public PrefabList(IEnumerable<Prefab> collection)
	{
		_list = [.. collection];
	}

	public PrefabList(PrefabList list, bool deepCopy)
	{
		_list = deepCopy
			? [.. list.Select(prefab => prefab.Clone())]
			: [.. list];
	}

	private PrefabList(List<Prefab> list)
	{
		_list = list;
	}

	public int Count => _list.Count;

	public bool IsReadOnly => false;

	public Prefab this[int index]
	{
		get => _list[index];
		set => _list[index] = value;
	}

	public static PrefabList Load(FcBinaryReader reader)
	{
		uint count = reader.ReadUInt32();
		ushort idOffset = reader.ReadUInt16();

		List<Prefab> list = new List<Prefab>((int)count);

		for (int i = 0; i < count; i++)
		{
			list.Add(Prefab.FromRaw(RawPrefab.Load(reader), ushort.MaxValue, 0, false));
		}

		return new PrefabList(list)
		{
			IdOffset = idOffset,
		};
	}

	public void Save(FcBinaryWriter writer)
	{
		writer.WriteUInt32((uint)Count);
		writer.WriteUInt16(IdOffset);

		for (int i = 0; i < Count; i++)
		{
			this[i].ToRaw(false).Save(writer);
		}
	}

	public void Add(Prefab item)
		=> _list.Add(item);

	public void AddRange(IEnumerable<Prefab> collection)
		=> _list.AddRange(collection);

	public void AddGroup(PrefabGroup group)
	{
		group.Id = (ushort)_list.Count;
		_list.AddRange(group.EnumerateInIdOrder());
	}

	public void Clear()
		=> _list.Clear();

	public bool Contains(Prefab item)
		=> _list.Contains(item);

	public void CopyTo(Prefab[] array, int arrayIndex)
		=> _list.CopyTo(array, arrayIndex);

	public void CopyTo(Prefab[] array)
		=> _list.CopyTo(array);

	public void CopyTo(int index, Prefab[] array, int arrayIndex, int count)
		=> _list.CopyTo(index, array, arrayIndex, count);

	public bool Exists(Predicate<Prefab> match)
		=> _list.Exists(match);

	public Prefab? Find(Predicate<Prefab> match)
		=> _list.Find(match);

	public List<Prefab> FindAll(Predicate<Prefab> match)
		=> _list.FindAll(match);

	public int FindIndex(int startIndex, int count, Predicate<Prefab> match)
		=> _list.FindIndex(startIndex, count, match);

	public int FindIndex(int startIndex, Predicate<Prefab> match)
		=> _list.FindIndex(startIndex, match);

	public int FindIndex(Predicate<Prefab> match)
		=> _list.FindIndex(match);

	public Prefab? FindLast(Predicate<Prefab> match)
		=> _list.FindLast(match);

	public int FindLastIndex(int startIndex, int count, Predicate<Prefab> match)
		=> _list.FindLastIndex(startIndex, count, match);

	public int FindLastIndex(int startIndex, Predicate<Prefab> match)
		=> _list.FindLastIndex(startIndex, match);

	public int FindLastIndex(Predicate<Prefab> match)
		=> _list.FindLastIndex(match);

	public void Insert(int index, Prefab item)
	{
		IncreaseAfter(index, 1);
		_list.Insert(index, item);
	}

	public void InsertRange(int index, IEnumerable<Prefab> collection)
	{
		int count = collection.Count();
		IncreaseAfter(index, (ushort)count);
		_list.InsertRange(index, collection);
	}

	public void InsertGroup(int index, PrefabGroup group)
	{
		IncreaseAfter(index, (ushort)group.Count);
		group.Id = (ushort)index;
		_list.InsertRange(index, group.EnumerateInIdOrder());
	}

	public int LastIndexOf(Prefab item)
		=> _list.LastIndexOf(item);

	public int LastIndexOf(Prefab item, int index)
		=> _list.LastIndexOf(item, index);

	public int LastIndexOf(Prefab item, int index, int count)
		=> _list.LastIndexOf(item, index, count);

	public int IndexOf(Prefab item)
		=> _list.IndexOf(item);

	public bool Remove(Prefab item)
	{
		int index = _list.IndexOf(item);

		if (index < 0)
		{
			return false;
		}
		else
		{
			RemoveAt(index);
			return true;
		}
	}

	public void RemoveAt(int index)
	{
		_list.RemoveAt(index);
		DecreaseAfter(index - 1, 1);
	}

	public void RemoveRange(int index, int count)
	{
		_list.RemoveRange(index, count);
		DecreaseAfter(index - 1, (ushort)count);
	}

	public bool TrueForAll(Predicate<Prefab> match)
		=> _list.TrueForAll(match);

	public Prefab[] ToArray()
		=> [.. _list];

	public IEnumerator<Prefab> GetEnumerator()
		=> _list.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> _list.GetEnumerator();

	public PrefabList Clone(bool deepCopy)
		=> new PrefabList(this, deepCopy);

	object ICloneable.Clone()
		=> new PrefabList(this, true);

	private void IncreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _list.Count; i++)
		{
			Prefab prefab = this[i];

			if (prefab.IsInGroup && prefab.GroupId >= index)
			{
				prefab.GroupId += amount;
			}

			if (!(prefab.Blocks is null))
			{
				ushort[] array = prefab.Blocks.Array.Array;

				for (int j = 0; j < array.Length; j++)
				{
					if (array[j] >= index)
					{
						array[j] += amount;
					}
				}
			}
		}
	}

	private void DecreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _list.Count; i++)
		{
			Prefab prefab = this[i];

			if (prefab.IsInGroup && prefab.GroupId >= index)
			{
				prefab.GroupId -= amount;
			}

			if (!(prefab.Blocks is null))
			{
				ushort[] array = prefab.Blocks.Array.Array;

				for (int j = 0; j < array.Length; j++)
				{
					if (array[j] >= index)
					{
						array[j] -= amount;
					}
				}
			}
		}
	}
}
