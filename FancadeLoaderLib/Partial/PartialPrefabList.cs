// <copyright file="PartialPrefabList.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CA1716
namespace FancadeLoaderLib.Partial;
#pragma warning restore CA1716

/// <summary>
/// <see cref="PrefabList"/> for <see cref="PartialPrefab"/>.
/// </summary>
public class PartialPrefabList : IList<PartialPrefab>, ICloneable
{
	/// <summary>
	/// The id offset of this list, <see cref="RawGame.CurrentNumbStockPrefabs"/> by default.
	/// </summary>
	public ushort IdOffset = RawGame.CurrentNumbStockPrefabs;

	internal readonly List<PartialPrefab> _list;

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabList"/> class.
	/// </summary>
	public PartialPrefabList()
	{
		_list = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabList"/> class.
	/// </summary>
	/// <param name="capacity">The initial capacity.</param>
	public PartialPrefabList(int capacity)
	{
		_list = new List<PartialPrefab>(capacity);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabList"/> class.
	/// </summary>
	/// <param name="collection">The prefabs to place into this list.</param>
	public PartialPrefabList(IEnumerable<PartialPrefab> collection)
	{
		_list = [.. collection];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefabList"/> class.
	/// </summary>
	/// <param name="list">The <see cref="PartialPrefabList"/> to copy values from.</param>
	/// <param name="deepCopy">If deep copy should be performed.</param>
	public PartialPrefabList(PartialPrefabList list, bool deepCopy)
	{
		_list = deepCopy
			? [.. list.Select(prefab => prefab.Clone())]
			: [.. list];
	}

	private PartialPrefabList(List<PartialPrefab> list)
	{
		_list = list;
	}

	/// <inheritdoc/>
	public int Count => _list.Count;

	/// <inheritdoc/>
	public bool IsReadOnly => false;

	/// <inheritdoc/>
	public PartialPrefab this[int index]
	{
		get => _list[index];
		set => _list[index] = value;
	}

	/// <summary>
	/// Loads a <see cref="PartialPrefabList"/> from a <see cref="FcBinaryReader"/>.
	/// </summary>
	/// <param name="reader">The reader to read the <see cref="PartialPrefabList"/> from.</param>
	/// <returns>A <see cref="PartialPrefabList"/> read from <paramref name="reader"/>.</returns>
	public static PartialPrefabList Load(FcBinaryReader reader)
	{
		if (reader is null)
		{
			throw new ArgumentNullException(nameof(reader));
		}

		uint count = reader.ReadUInt32();
		ushort idOffset = reader.ReadUInt16();

		List<PartialPrefab> list = new List<PartialPrefab>((int)count);

		for (int i = 0; i < count; i++)
		{
			list.Add(PartialPrefab.Load(reader));
		}

		return new PartialPrefabList(list)
		{
			IdOffset = idOffset,
		};
	}

	/// <summary>
	/// Writes a <see cref="PartialPrefabList"/> into a <see cref="FcBinaryWriter"/>.
	/// </summary>
	/// <param name="writer">The <see cref="FcBinaryWriter"/> to write this instance into.</param>
	public void Save(FcBinaryWriter writer)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		writer.WriteUInt32((uint)Count);
		writer.WriteUInt16(IdOffset);

		for (int i = 0; i < Count; i++)
		{
			this[i].Save(writer);
		}
	}

	/// <inheritdoc/>
	public void Add(PartialPrefab item)
		=> _list.Add(item);

	/// <summary>
	/// Adds multiple prefabs.
	/// </summary>
	/// <param name="collection">The prefabs to add.</param>
	public void AddRange(IEnumerable<PartialPrefab> collection)
		=> _list.AddRange(collection);

	/// <summary>
	/// Adds a prefab group.
	/// </summary>
	/// <remarks>
	/// Id of the group will get set to <see cref="Count"/> (before being added).
	/// </remarks>
	/// <param name="group">The group to add.</param>
	public void AddGroup(PartialPrefabGroup group)
	{
		if (group is null)
		{
			throw new ArgumentNullException(nameof(group));
		}

		group.Id = (ushort)_list.Count;
		_list.AddRange(group.EnumerateInIdOrder());
	}

	/// <inheritdoc/>
	public void Clear()
		=> _list.Clear();

	/// <inheritdoc/>
	public bool Contains(PartialPrefab item)
		=> _list.Contains(item);

	/// <inheritdoc/>
	public void CopyTo(PartialPrefab[] array, int arrayIndex)
		=> _list.CopyTo(array, arrayIndex);

	/// <summary>
	/// Copied this <see cref="PartialPrefabList"/> into <paramref name="array"/>.
	/// </summary>
	/// <param name="array">The array to copy to.</param>
	public void CopyTo(PartialPrefab[] array)
		=> _list.CopyTo(array);

	/// <summary>
	/// Copied this <see cref="PartialPrefabList"/> into <paramref name="array"/>.
	/// </summary>
	/// <param name="index">The index to start copying at.</param>
	/// <param name="array">The array to copy to.</param>
	/// <param name="arrayIndex">Index in <paramref name="array"/> to start copying to.</param>
	/// <param name="count">The number of prefabs to copy.</param>
	public void CopyTo(int index, PartialPrefab[] array, int arrayIndex, int count)
		=> _list.CopyTo(index, array, arrayIndex, count);

	/// <summary>
	/// Gets if this <see cref="PartialPrefabList"/> contains a prefab that matches the condition defined by the specified predicate.
	/// </summary>
	/// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the prefab to search for.</param>
	/// <returns><see langword="true"/> if this <see cref="PartialPrefabList"/> contains a prefab that matches the condition defined by the specified predicate; otherwise, false.</returns>
	public bool Exists(Predicate<PartialPrefab> match)
		=> _list.Exists(match);

	/// <summary>
	/// Gets the first prefab that matches the condition defined by the specified predicate.
	/// </summary>
	/// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the prefab to search for.</param>
	/// <returns>The first prefab that matches the condition defined by the specified predicate.</returns>
	public PartialPrefab? Find(Predicate<PartialPrefab> match)
		=> _list.Find(match);

	/// <summary>
	/// Gets all prefabs that match the condition defined by the specified predicate.
	/// </summary>
	/// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the prefabs to search for.</param>
	/// <returns>The prefabs that match the condition defined by the specified predicate.</returns>
#pragma warning disable CA1002 // Do not expose generic lists
	public List<PartialPrefab> FindAll(Predicate<PartialPrefab> match)
#pragma warning restore CA1002 // Do not expose generic lists
		=> _list.FindAll(match);

	/// <summary>
	/// Inserts a prefab at an index.
	/// </summary>
	/// <param name="index">The index at which to insert <paramref name="item"/>.</param>
	/// <param name="item">The prefab to insert.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
	public void Insert(int index, PartialPrefab item)
	{
		if (index < 0 || index > _list.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		IncreaseAfter(index, 1);
		_list.Insert(index, item);
	}

	/// <summary>
	/// Inserts multiple prefab at an index.
	/// </summary>
	/// <param name="index">The index at which to insert <paramref name="collection"/>.</param>
	/// <param name="collection">The prefabs to insert.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
	public void InsertRange(int index, IEnumerable<PartialPrefab> collection)
	{
		if (index < 0 || index > _list.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		int count = collection.Count();
		IncreaseAfter(index, (ushort)count);
		_list.InsertRange(index, collection);
	}

	/// <summary>
	/// Inserts a prefab group at an index.
	/// </summary>
	/// <param name="index">The index at which to insert <paramref name="group"/>.</param>
	/// <param name="group">The prefab group to insert.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="index"/> is out of range.</exception>
	public void InsertGroup(int index, PartialPrefabGroup group)
	{
		if (group is null)
		{
			throw new ArgumentNullException(nameof(group));
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
	public int LastIndexOf(PartialPrefab item)
		=> _list.LastIndexOf(item);

	/// <summary>
	/// Gets the last index of <paramref name="item"/>.
	/// </summary>
	/// <param name="item">The prefab to find.</param>
	/// <param name="index">The starting index of the search.</param>
	/// <returns>Last index of <paramref name="item"/> or -1, if it isn't found.</returns>
	public int LastIndexOf(PartialPrefab item, int index)
		=> _list.LastIndexOf(item, index);

	/// <summary>
	/// Gets the last index of <paramref name="item"/>.
	/// </summary>
	/// <param name="item">The prefab to find.</param>
	/// <param name="index">The starting index of the search.</param>
	/// <param name="count">The number of elements in the section to search.</param>
	/// <returns>Last index of <paramref name="item"/> or -1, if it isn't found.</returns>
	public int LastIndexOf(PartialPrefab item, int index, int count)
		=> _list.LastIndexOf(item, index, count);

	/// <summary>
	/// Gets the first index of <paramref name="item"/>.
	/// </summary>
	/// <param name="item">The prefab to find.</param>
	/// <returns>First index of <paramref name="item"/> or -1, if it isn't found.</returns>
	public int IndexOf(PartialPrefab item)
		=> _list.IndexOf(item);

	/// <summary>
	/// Removes the first <paramref name="item"/>.
	/// </summary>
	/// <param name="item">The prefab to remove.</param>
	/// <returns><see langword="true"/> if the item was removed, <see langword="false"/> if the items wasn't found.</returns>
	public bool Remove(PartialPrefab item)
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
	/// Converts this <see cref="PartialPrefabList"/> to an array of <see cref="PartialPrefab"/>s.
	/// </summary>
	/// <returns>An array of <see cref="PartialPrefab"/>s.</returns>
	public PartialPrefab[] ToArray()
		=> [.. _list];

	/// <inheritdoc/>
	public IEnumerator<PartialPrefab> GetEnumerator()
		=> _list.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
		=> _list.GetEnumerator();

	/// <summary>
	/// Creates a copy of this <see cref="PartialPrefabList"/>.
	/// </summary>
	/// <param name="deepCopy">If deep copy should be performed.</param>
	/// <returns>A copy of this <see cref="PartialPrefabList"/>.</returns>
	public PartialPrefabList Clone(bool deepCopy)
		=> new PartialPrefabList(this, deepCopy);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PartialPrefabList(this, true);

	private void IncreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _list.Count; i++)
		{
			PartialPrefab prefab = this[i];

			if (prefab.IsInGroup && prefab.GroupId >= index)
			{
				prefab.GroupId += amount;
			}
		}
	}

	private void DecreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _list.Count; i++)
		{
			PartialPrefab prefab = this[i];

			if (prefab.IsInGroup && prefab.GroupId >= index)
			{
				prefab.GroupId -= amount;
			}
		}
	}
}
