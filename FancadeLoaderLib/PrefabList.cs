// <copyright file="PrefabList.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FancadeLoaderLib;

/// <summary>
/// <see cref="List{T}"/> wrapper for easier <see cref="Prefab"/> manipulation.
/// </summary>
/// <remarks>
/// Group ids are automatically changed when prefabs are inserter/removed.
/// <para>Allows for saving/loading.</para>
/// </remarks>
public class PrefabList : IList<Prefab>, ICloneable
{
	/// <summary>
	/// The id offset of this list, <see cref="RawGame.CurrentNumbStockPrefabs"/> by default.
	/// </summary>
	public ushort IdOffset = RawGame.CurrentNumbStockPrefabs;

	internal readonly List<Prefab> _list;

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabList"/> class.
	/// </summary>
	public PrefabList()
	{
		_list = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabList"/> class.
	/// </summary>
	/// <param name="capacity">The initial capacity.</param>
	public PrefabList(int capacity)
	{
		_list = new List<Prefab>(capacity);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabList"/> class.
	/// </summary>
	/// <param name="collection">The prefabs to place into this list.</param>
	public PrefabList(IEnumerable<Prefab> collection)
	{
		_list = [.. collection];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabList"/> class.
	/// </summary>
	/// <param name="list">The <see cref="PrefabList"/> to copy values from.</param>
	/// <param name="deepCopy">If deep copy should be performed.</param>
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

	/// <inheritdoc/>
	public int Count => _list.Count;

	/// <inheritdoc/>
	public bool IsReadOnly => false;

	/// <inheritdoc/>
	public Prefab this[int index]
	{
		get => _list[index];
		set => _list[index] = value;
	}

	/// <summary>
	/// Loads a <see cref="PrefabList"/> from a <see cref="FcBinaryReader"/>.
	/// </summary>
	/// <param name="reader">The reader to read the <see cref="PrefabList"/> from.</param>
	/// <returns>A <see cref="PrefabList"/> read from <paramref name="reader"/>.</returns>
	public static PrefabList Load(FcBinaryReader reader)
	{
		if (reader is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(reader));
		}

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

	/// <summary>
	/// Writes a <see cref="PrefabList"/> into a <see cref="FcBinaryWriter"/>.
	/// </summary>
	/// <param name="writer">The <see cref="FcBinaryWriter"/> to write this instance into.</param>
	public void Save(FcBinaryWriter writer)
	{
		if (writer is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(writer));
		}

		writer.WriteUInt32((uint)Count);
		writer.WriteUInt16(IdOffset);

		for (int i = 0; i < Count; i++)
		{
			this[i].ToRaw(false).Save(writer);
		}
	}

	/// <inheritdoc/>
	public void Add(Prefab item)
		=> _list.Add(item);

	/// <summary>
	/// Adds multiple prefabs.
	/// </summary>
	/// <param name="collection">The prefabs to add.</param>
	public void AddRange(IEnumerable<Prefab> collection)
		=> _list.AddRange(collection);

	/// <summary>
	/// Adds a prefab group.
	/// </summary>
	/// <remarks>
	/// Id of the group will get set to <see cref="Count"/> (before being added).
	/// </remarks>
	/// <param name="group">The group to add.</param>
	public void AddGroup(PrefabGroup group)
	{
		if (group is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(group));
		}

		group.Id = (ushort)_list.Count;
		_list.AddRange(group.EnumerateInIdOrder());
	}

	/// <inheritdoc/>
	public void Clear()
		=> _list.Clear();

	/// <inheritdoc/>
	public bool Contains(Prefab item)
		=> _list.Contains(item);

	/// <inheritdoc/>
	public void CopyTo(Prefab[] array, int arrayIndex)
		=> _list.CopyTo(array, arrayIndex);

	/// <summary>
	/// Copied this <see cref="PrefabList"/> into <paramref name="array"/>.
	/// </summary>
	/// <param name="array">The array to copy to.</param>
	public void CopyTo(Prefab[] array)
		=> _list.CopyTo(array);

	/// <summary>
	/// Copied this <see cref="PrefabList"/> into <paramref name="array"/>.
	/// </summary>
	/// <param name="index">The index to start copying at.</param>
	/// <param name="array">The array to copy to.</param>
	/// <param name="arrayIndex">Index in <paramref name="array"/> to start copying to.</param>
	/// <param name="count">The number of prefabs to copy.</param>
	public void CopyTo(int index, Prefab[] array, int arrayIndex, int count)
		=> _list.CopyTo(index, array, arrayIndex, count);

	/// <summary>
	/// Gets if this <see cref="PrefabList"/> contains a prefab that matches the condition defined by the specified predicate.
	/// </summary>
	/// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the prefab to search for.</param>
	/// <returns><see langword="true"/> if this <see cref="PrefabList"/> contains a prefab that matches the condition defined by the specified predicate; otherwise, false.</returns>
	public bool Exists(Predicate<Prefab> match)
		=> _list.Exists(match);

	/// <summary>
	/// Gets the first prefab that matches the condition defined by the specified predicate.
	/// </summary>
	/// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the prefab to search for.</param>
	/// <returns>The first prefab that matches the condition defined by the specified predicate.</returns>
	public Prefab? Find(Predicate<Prefab> match)
		=> _list.Find(match);

	/// <summary>
	/// Gets all prefabs that match the condition defined by the specified predicate.
	/// </summary>
	/// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the prefabs to search for.</param>
	/// <returns>The prefabs that match the condition defined by the specified predicate.</returns>
#pragma warning disable CA1002 // Do not expose generic lists
	public List<Prefab> FindAll(Predicate<Prefab> match)
#pragma warning restore CA1002 // Do not expose generic lists
		=> _list.FindAll(match);

	/// <summary>
	/// Inserts a prefab at an index.
	/// </summary>
	/// <param name="index">The index at which to insert <paramref name="item"/>.</param>
	/// <param name="item">The prefab to insert.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
	public void Insert(int index, Prefab item)
	{
		IncreaseAfter(index, 1);
		_list.Insert(index, item);
	}

	/// <summary>
	/// Inserts multiple prefab at an index.
	/// </summary>
	/// <param name="index">The index at which to insert <paramref name="collection"/>.</param>
	/// <param name="collection">The prefabs to insert.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
	public void InsertRange(int index, IEnumerable<Prefab> collection)
	{
		int count = collection.Count();
		IncreaseAfter(index, (ushort)count);
		_list.InsertRange(index, collection);
	}

	/// <summary>
	/// Inserts a prefab group at an index.
	/// </summary>
	/// <param name="index">The index at which to insert <paramref name="group"/>.</param>
	/// <param name="group">The prefab group to insert.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
	public void InsertGroup(int index, PrefabGroup group)
	{
		if (group is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(group));
		}

		IncreaseAfter(index, (ushort)group.Count);
		group.Id = (ushort)index;
		_list.InsertRange(index, group.EnumerateInIdOrder());
	}

	/// <summary>
	/// Gets the last index of <paramref name="item"/>.
	/// </summary>
	/// <param name="item">The prefab to find.</param>
	/// <returns>Last index of <paramref name="item"/> or -1, if it isn't found.</returns>
	public int LastIndexOf(Prefab item)
		=> _list.LastIndexOf(item);

	/// <summary>
	/// Gets the last index of <paramref name="item"/>.
	/// </summary>
	/// <param name="item">The prefab to find.</param>
	/// <param name="index">The starting index of the search.</param>
	/// <returns>Last index of <paramref name="item"/> or -1, if it isn't found.</returns>
	public int LastIndexOf(Prefab item, int index)
		=> _list.LastIndexOf(item, index);

	/// <summary>
	/// Gets the last index of <paramref name="item"/>.
	/// </summary>
	/// <param name="item">The prefab to find.</param>
	/// <param name="index">The starting index of the search.</param>
	/// <param name="count">The number of elements in the section to search.</param>
	/// <returns>Last index of <paramref name="item"/> or -1, if it isn't found.</returns>
	public int LastIndexOf(Prefab item, int index, int count)
		=> _list.LastIndexOf(item, index, count);

	/// <summary>
	/// Gets the first index of <paramref name="item"/>.
	/// </summary>
	/// <param name="item">The prefab to find.</param>
	/// <returns>First index of <paramref name="item"/> or -1, if it isn't found.</returns>
	public int IndexOf(Prefab item)
		=> _list.IndexOf(item);

	/// <summary>
	/// Removes the first <paramref name="item"/>.
	/// </summary>
	/// <param name="item">The prefab to remove.</param>
	/// <returns><see langword="true"/> if the item was removed, <see langword="false"/> if the items wasn't found.</returns>
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

	/// <summary>
	/// Removed a prefab at a specified index.
	/// </summary>
	/// <param name="index">The index at which to remove the prefab.</param>
	public void RemoveAt(int index)
	{
		_list.RemoveAt(index);
		DecreaseAfter(index - 1, 1);
	}

	/// <summary>
	/// Removes a range of prefabs at a specified index.
	/// </summary>
	/// <param name="index">The index at which to remove the prefab.</param>
	/// <param name="count">The number of prefabs to remove.</param>
	public void RemoveRange(int index, int count)
	{
		_list.RemoveRange(index, count);
		DecreaseAfter(index - 1, (ushort)count);
	}

	/// <summary>
	/// Converts this <see cref="PrefabList"/> to an array of <see cref="PrefabList"/>s.
	/// </summary>
	/// <returns>An array of <see cref="Prefab"/>s.</returns>
	public Prefab[] ToArray()
		=> [.. _list];

	/// <inheritdoc/>
	public IEnumerator<Prefab> GetEnumerator()
		=> _list.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
		=> _list.GetEnumerator();

	/// <summary>
	/// Creates a copy of this <see cref="PrefabList"/>.
	/// </summary>
	/// <param name="deepCopy">If deep copy should be performed.</param>
	/// <returns>A copy of this <see cref="PrefabList"/>.</returns>
	public PrefabList Clone(bool deepCopy)
		=> new PrefabList(this, deepCopy);

	/// <inheritdoc/>
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
