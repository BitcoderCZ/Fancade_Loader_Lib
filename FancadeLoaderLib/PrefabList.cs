// <copyright file="PrefabList.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib;

/// <summary>
/// <see cref="List{T}"/> wrapper for easier <see cref="Prefab"/> manipulation.
/// </summary>
/// <remarks>
/// Ids are automatically changed when prefabs are inserter/removed.
/// <para>Allows for saving/loading.</para>
/// </remarks>
public class PrefabList : ICloneable
{
	/// <summary>
	/// The id offset of this list, <see cref="RawGame.CurrentNumbStockPrefabs"/> by default.
	/// </summary>
	public ushort IdOffset = RawGame.CurrentNumbStockPrefabs;

	internal readonly Dictionary<ushort, Prefab> _prefabs;
	internal readonly List<PrefabSegment> _segments;

	public PrefabList()
	{
		_prefabs = [];
		_segments = [];
	}

	public PrefabList(int prefabCapacity, int segmentCapacity)
	{
		_prefabs = new(prefabCapacity);
		_segments = new(segmentCapacity);
	}

	public PrefabList(IEnumerable<Prefab> prefabs)
	{
		if (prefabs is null)
		{
			ThrowArgumentNullException(nameof(prefabs));
		}

		_prefabs = prefabs.ToDictionary(group => group.Id);
		ValidatePrefabs(_prefabs.Values, nameof(prefabs)); // validate using _prefabs.Values to avoid iterating over collection multiple times

		_segments = [.. SegmentsFromPrefabs(_prefabs)];

		IdOffset = _prefabs.Min(item => item.Key);
	}

	public PrefabList(PrefabList other, bool deepCopy)
	{
		ThrowIfNull(other, nameof(other));

		if (deepCopy)
		{
			_prefabs = other._prefabs.ToDictionary(item => item.Key, item => item.Value.Clone(true));
			_segments = [.. SegmentsFromPrefabs(_prefabs)];
		}
		else
		{
			_prefabs = new(other._prefabs);
			_segments = [.. SegmentsFromPrefabs(_prefabs)];
		}
	}

	private PrefabList(Dictionary<ushort, Prefab> dict)
	{
		_prefabs = dict;
		_segments = [.. SegmentsFromPrefabs(_prefabs)];
	}

	public int PrefabCount => _prefabs.Count;

	public int SegmentCount => _segments.Count;

	public IEnumerable<Prefab> Prefabs => _prefabs.Values;

	public IEnumerable<PrefabSegment> Segments => _segments;

	public static PrefabList Load(FcBinaryReader reader)
	{
		if (reader is null)
		{
			ThrowArgumentNullException(nameof(reader));
		}

		uint count = reader.ReadUInt32();
		ushort idOffset = reader.ReadUInt16();

		RawPrefab[] rawPrefabs = new RawPrefab[count];

		for (int i = 0; i < count; i++)
		{
			rawPrefabs[i] = RawPrefab.Load(reader);
		}

		Dictionary<ushort, Prefab> prefabs = [];

		for (int i = 0; i < rawPrefabs.Length; i++)
		{
			if (rawPrefabs[i].IsInGroup)
			{
				int startIndex = i;
				ushort groupId = rawPrefabs[i].GroupId;
				do
				{
					i++;
				} while (i < count && rawPrefabs[i].GroupId == groupId);

				ushort id = (ushort)(startIndex + idOffset);
				prefabs.Add(id, Prefab.FromRaw(id, rawPrefabs.Skip(startIndex).Take(i - startIndex), ushort.MaxValue, 0, false));

				i--; // incremented at the end of the loop
			}
			else
			{
				ushort id = (ushort)(i + idOffset);
				prefabs.Add(id, Prefab.FromRaw(id, [rawPrefabs[i]], ushort.MaxValue, 0, false));
			}
		}

		return new PrefabList(prefabs)
		{
			IdOffset = idOffset,
		};
	}

	public void Save(FcBinaryWriter writer)
	{
		if (writer is null)
		{
			ThrowArgumentNullException(nameof(writer));
		}

		writer.WriteUInt32((uint)SegmentCount);
		writer.WriteUInt16(IdOffset);

		foreach (var prefab in _prefabs.OrderBy(item => item.Key).SelectMany(item => item.Value.ToRaw(false)))
		{
			prefab.Save(writer);
		}
	}

	public Prefab GetPrefab(ushort id)
		=> _prefabs[id];

	public bool TryGetPrefab(ushort id, [MaybeNullWhen(false)] out Prefab value)
		=> _prefabs.TryGetValue(id, out value);

	public PrefabSegment GetSegment(ushort id)
		=> _segments[id - IdOffset];

	public bool TryGetSegments(ushort id, [MaybeNullWhen(false)] out PrefabSegment value)
	{
		id -= IdOffset;

		// can skip (id >= 0) because id is unsigned
		if (id < _segments.Count)
		{
			value = _segments[id];
			return true;
		}
		else
		{
			value = null;
			return false;
		}
	}

	public void AddPrefab(Prefab value)
	{
		if (value.Id != SegmentCount + IdOffset)
		{
			ThrowArgumentException($"{nameof(value)}.{nameof(value.Id)} ({value.Id}) must be equal to {nameof(SegmentCount)} + {nameof(IdOffset)}.", $"{nameof(value)}.{nameof(value.Id)}");
		}

		_prefabs.Add(value.Id, value);
		_segments.AddRange(value.Values);
	}

	public void InsertPrefab(Prefab value)
	{
		if (WillBeLastPrefab(value))
		{
			AddPrefab(value);
			return;
		}

		if (!_prefabs.ContainsKey(value.Id))
		{
			ThrowArgumentException($"{nameof(_prefabs)} must contain {nameof(value)}.{nameof(Prefab.Id)}.", nameof(value));
		}

		IncreaseAfter(value.Id, (ushort)value.Count);
		_prefabs.Add(value.Id, value);
		_segments.InsertRange(value.Id - IdOffset, value.Values);
	}

	public bool RemovePrefab(ushort id)
	{
		if (!_prefabs.Remove(id, out var prefab))
		{
			return false;
		}

		_segments.RemoveRange(id - IdOffset, prefab.Count);

		for (int i = 0; i < prefab.Count; i++)
		{
			RemoveIdFromBlocks((ushort)(id + i));
		}

		if (WillBeLastPrefab(prefab))
		{
			return true;
		}

		DecreaseAfter(id, (ushort)prefab.Count);

		return true;
	}

	public void AddSegmentToPrefab(ushort id, PrefabSegment value, bool overwriteBlocks)
	{
		var prefab = _prefabs[id];

		if (!overwriteBlocks && !CanAddIdToPrefab(id, value.PosInPrefab))
		{
			throw new InvalidOperationException($"Cannot add prefab because it's position is obstructed and {nameof(overwriteBlocks)} is false.");
		}

		ushort segmentId = (ushort)(prefab.Id + prefab.Count);

		if (IsLastPrefab(prefab))
		{
			prefab.Add(value);
			_segments.Add(value);
			AddIdToPrefab(id, value.PosInPrefab, segmentId);
			return;
		}

		prefab.Add(value);

		IncreaseAfter(segmentId, 1);
		_segments.Insert(segmentId, value);
		AddIdToPrefab(id, value.PosInPrefab, segmentId);
	}

	public bool TryAddSegmentToPrefab(ushort id, PrefabSegment value, bool overwriteBlocks)
	{
		if (!_prefabs.TryGetValue(id, out var prefab))
		{
			return false;
		}

		if (!overwriteBlocks && !CanAddIdToPrefab(id, value.PosInPrefab))
		{
			return false;
		}

		if (!prefab.TryAdd(value))
		{
			return false;
		}

		ushort segmentId = (ushort)(prefab.Id + prefab.Count - 1);

		if (IsLastPrefab(prefab))
		{
			_segments.Add(value);
			AddIdToPrefab(id, value.PosInPrefab, segmentId);
			return true;
		}

		IncreaseAfter(segmentId, 1);
		_segments.Insert(segmentId, value);
		AddIdToPrefab(id, value.PosInPrefab, segmentId);

		return true;
	}

	public bool RemoveSegmentFromPrefab(ushort id, byte3 posInPrefab)
	{
		var prefab = _prefabs[id];

		int segmentIndex = prefab.IndexOf(posInPrefab);
		if (!prefab.Remove(posInPrefab))
		{
			return false;
		}

		ushort segmentId = (ushort)(id + segmentIndex);

		_segments.RemoveAt(segmentId - IdOffset);
		RemoveIdFromBlocks(segmentId);

		if (segmentId == SegmentCount + IdOffset - 1)
		{
			return true;
		}

		DecreaseAfter(segmentId, 1);

		return true;
	}

	public void Clear()
	{
		_prefabs.Clear();
		_segments.Clear();
	}

	public PrefabList Clone(bool deepCopy)
		=> new PrefabList(this, deepCopy);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PrefabList(this, true);

	private static void ValidatePrefabs(IEnumerable<Prefab> prefabs, string paramName)
	{
		int? nextId = null;

		foreach (var prefab in prefabs.OrderBy(group => group.Id))
		{
			if (nextId == null || prefab.Id == nextId)
			{
				nextId = prefab.Id + prefab.Count;
			}
			else
			{
				ThrowArgumentException($"Prefabs in {paramName} must have consecutive IDs. Expected ID {nextId}, but found {prefab.Id}.", paramName);
			}
		}
	}

	private static IEnumerable<PrefabSegment> SegmentsFromPrefabs(IEnumerable<KeyValuePair<ushort, Prefab>> prefabs)
		=> prefabs.OrderBy(item => item.Key).SelectMany(item => item.Value.Values);

	private bool IsLastPrefab(Prefab prefab)
		=> prefab.Id + prefab.Count >= SegmentCount + IdOffset;

	private bool WillBeLastPrefab(Prefab prefab)
		=> prefab.Id >= SegmentCount + IdOffset;

	private void RemoveIdFromBlocks(ushort id)
	{
		foreach (var prefab in _prefabs.Values)
		{
			ushort[] array = prefab.Blocks.Array.Array;

			for (int z = 0; z < prefab.Blocks.Size.Z; z++)
			{
				for (int y = 0; y < prefab.Blocks.Size.Y; y++)
				{
					for (int x = 0; x < prefab.Blocks.Size.X; x++)
					{
						int i = prefab.Blocks.Index(new int3(x, y, z));

						if (array[i] == id)
						{
							array[i] = 0;
						}
					}
				}
			}
		}
	}

	private bool CanAddIdToPrefab(ushort prefabId, int3 offset)
	{
		foreach (var prefab in _prefabs.Values)
		{
			ushort[] array = prefab.Blocks.Array.Array;

			for (int z = 0; z < prefab.Blocks.Size.Z; z++)
			{
				for (int y = 0; y < prefab.Blocks.Size.Y; y++)
				{
					for (int x = 0; x < prefab.Blocks.Size.X; x++)
					{
						int3 pos = new int3(x, y, z);
						int i = prefab.Blocks.Index(pos);

						if (array[i] == prefabId)
						{
							pos += offset;
							if (prefab.Blocks.InBounds(pos) && prefab.Blocks.GetBlockUnchecked(pos) != 0)
							{
								return false;
							}
						}
					}
				}
			}
		}

		return true;
	}

	private void AddIdToPrefab(ushort prefabId, int3 offset, ushort id)
	{
		foreach (var prefab in _prefabs.Values)
		{
			ushort[] array = prefab.Blocks.Array.Array;

			for (int z = 0; z < prefab.Blocks.Size.Z; z++)
			{
				for (int y = 0; y < prefab.Blocks.Size.Y; y++)
				{
					for (int x = 0; x < prefab.Blocks.Size.X; x++)
					{
						int3 pos = new int3(x, y, z);
						int i = prefab.Blocks.Index(pos);

						if (array[i] == prefabId)
						{
							prefab.Blocks.SetBlock(pos + offset, id);
						}
					}
				}
			}
		}
	}

	private void IncreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _segments.Count; i++)
		{
			PrefabSegment segment = _segments[i];

			if (segment.PrefabId >= index)
			{
				segment.PrefabId += amount;
			}
		}

		List<ushort> prefabsToChangeId = [];

		foreach (var (id, prefab) in _prefabs)
		{
			if (id >= index)
			{
				prefabsToChangeId.Add(id);
			}

			ushort[] array = prefab.Blocks.Array.Array;

			for (int z = 0; z < prefab.Blocks.Size.Z; z++)
			{
				for (int y = 0; y < prefab.Blocks.Size.Y; y++)
				{
					for (int x = 0; x < prefab.Blocks.Size.X; x++)
					{
						int i = prefab.Blocks.Index(new int3(x, y, z));

						if (array[i] >= index)
						{
							array[i] += amount;
						}
					}
				}
			}
		}

		foreach (ushort id in prefabsToChangeId.OrderByDescending(item => item))
		{
			bool removed = _prefabs.Remove(id, out var prefab);

			Debug.Assert(removed, "Prefab should have been removed.");
			Debug.Assert(prefab is not null, $"{nameof(prefab)} shouldn't be null.");

			ushort newId = (ushort)(id + amount);
			prefab.Id = newId;
			_prefabs[newId] = prefab;
		}
	}

	private void DecreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _segments.Count; i++)
		{
			PrefabSegment segment = _segments[i];

			if (segment.PrefabId >= index)
			{
				segment.PrefabId -= amount;
			}
		}

		List<ushort> prefabsToChangeId = [];

		foreach (var (id, prefab) in _prefabs)
		{
			if (id >= index)
			{
				prefabsToChangeId.Add(id);
			}

			ushort[] array = prefab.Blocks.Array.Array;

			for (int z = 0; z < prefab.Blocks.Size.Z; z++)
			{
				for (int y = 0; y < prefab.Blocks.Size.Y; y++)
				{
					for (int x = 0; x < prefab.Blocks.Size.X; x++)
					{
						int i = prefab.Blocks.Index(new int3(x, y, z));

						if (array[i] >= index)
						{
							array[i] -= amount;
						}
					}
				}
			}
		}

		foreach (ushort id in prefabsToChangeId.OrderBy(item => item))
		{
			bool removed = _prefabs.Remove(id, out var prefab);

			Debug.Assert(removed, "Prefab should have been removed.");
			Debug.Assert(prefab is not null, $"{nameof(prefab)} shouldn't be null.");

			ushort newId = (ushort)(id - amount);
			prefab.Id = newId;
			_prefabs[newId] = prefab;
		}
	}
}
