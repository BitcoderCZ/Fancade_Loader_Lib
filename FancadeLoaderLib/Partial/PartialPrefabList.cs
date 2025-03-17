// <copyright file="PartialPrefabList.cs" company="BitcoderCZ">
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

#pragma warning disable CA1716
namespace FancadeLoaderLib.Partial;
#pragma warning restore CA1716

/// <summary>
/// <see cref="List{T}"/> wrapper for easier <see cref="PartialPrefab"/> manipulation.
/// </summary>
/// <remarks>
/// Ids are automatically changed when prefabs are inserter/removed.
/// <para>Allows for saving/loading.</para>
/// </remarks>
public partial class PartialPrefabList : ICloneable
{
	/// <summary>
	/// The id offset of this list, <see cref="RawGame.CurrentNumbStockPrefabs"/> by default.
	/// </summary>
	public ushort IdOffset = RawGame.CurrentNumbStockPrefabs;

	internal readonly Dictionary<ushort, PartialPrefab> _prefabs;
	internal readonly List<PartialPrefabSegment> _segments;

	public PartialPrefabList()
	{
		_prefabs = [];
		_segments = [];
	}

	public PartialPrefabList(int prefabCapacity, int segmentCapacity)
	{
		_prefabs = new(prefabCapacity);
		_segments = new(segmentCapacity);
	}

	public PartialPrefabList(IEnumerable<PartialPrefab> prefabs)
	{
		if (prefabs is null)
		{
			ThrowArgumentNullException(nameof(prefabs));
		}

		_prefabs = prefabs.ToDictionary(prefab => prefab.Id);
		ValidatePrefabs(_prefabs.Values, nameof(prefabs)); // validate using _prefabs.Values to avoid iterating over collection multiple times

		_segments = [.. SegmentsFromPrefabs(_prefabs)];

		IdOffset = _prefabs.Min(item => item.Key);
	}

	public PartialPrefabList(PartialPrefabList other, bool deepCopy)
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

	private PartialPrefabList(Dictionary<ushort, PartialPrefab> dict)
	{
		_prefabs = dict;
		_segments = [.. SegmentsFromPrefabs(_prefabs)];
	}

	public int PrefabCount => _prefabs.Count;

	public int SegmentCount => _segments.Count;

	public IEnumerable<PartialPrefab> Prefabs => _prefabs.Values;

	public IEnumerable<PartialPrefabSegment> Segments => _segments;

	public static PartialPrefabList Load(FcBinaryReader reader)
	{
		if (reader is null)
		{
			ThrowArgumentNullException(nameof(reader));
		}

		uint count = reader.ReadUInt32();
		ushort idOffset = reader.ReadUInt16();

		OldPartialPrefab[] rawPrefabs = new OldPartialPrefab[count];

		for (int i = 0; i < count; i++)
		{
			rawPrefabs[i] = OldPartialPrefab.Load(reader);
		}

		Dictionary<ushort, PartialPrefab> prefabs = [];

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
				prefabs.Add(id, PartialPrefab.FromRaw(id, rawPrefabs.Skip(startIndex).Take(i - startIndex)));

				i--; // incremented at the end of the loop
			}
			else
			{
				ushort id = (ushort)(i + idOffset);
				prefabs.Add(id, PartialPrefab.FromRaw(id, [rawPrefabs[i]]));
			}
		}

		return new PartialPrefabList(prefabs)
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

		foreach (var prefab in _prefabs.OrderBy(item => item.Key).SelectMany(item => item.Value.ToRaw()))
		{
			prefab.Save(writer);
		}
	}

	public PartialPrefab GetPrefab(ushort id)
		=> _prefabs[id];

	public bool TryGetPrefab(ushort id, [MaybeNullWhen(false)] out PartialPrefab value)
		=> _prefabs.TryGetValue(id, out value);

	public PartialPrefabSegment GetSegment(ushort id)
		=> _segments[id - IdOffset];

	public bool TryGetSegment(ushort id, [MaybeNullWhen(false)] out PartialPrefabSegment value)
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

	public void AddPrefab(PartialPrefab value)
	{
		if (value.Id != SegmentCount + IdOffset)
		{
			ThrowArgumentException($"{nameof(value)}.{nameof(value.Id)} ({value.Id}) must be equal to {nameof(SegmentCount)} + {nameof(IdOffset)}.", $"{nameof(value)}.{nameof(value.Id)}");
		}

		_prefabs.Add(value.Id, value);
		_segments.AddRange(value.Values);
	}

	public void InsertPrefab(PartialPrefab value)
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

		if (WillBeLastPrefab(prefab))
		{
			return true;
		}

		DecreaseAfter(id, (ushort)prefab.Count);

		return true;
	}

	public bool RemovePrefab(ushort id, [MaybeNullWhen(false)] out PartialPrefab prefab)
	{
		if (!_prefabs.Remove(id, out prefab))
		{
			return false;
		}

		_segments.RemoveRange(id - IdOffset, prefab.Count);

		if (WillBeLastPrefab(prefab))
		{
			return true;
		}

		DecreaseAfter(id, (ushort)prefab.Count);

		return true;
	}

	public void AddSegmentToPrefab(ushort id, PartialPrefabSegment value)
	{
		var prefab = _prefabs[id];

		ushort segmentId = (ushort)(prefab.Id + prefab.Count);

		if (IsLastPrefab(prefab))
		{
			prefab.Add(value.PosInPrefab, value);
			_segments.Add(value);
			return;
		}

		prefab.Add(value.PosInPrefab, value);

		IncreaseAfter(segmentId, 1);
		_segments.Insert(segmentId, value);
	}

	public bool TryAddSegmentToPrefab(ushort id, PartialPrefabSegment value)
	{
		if (!_prefabs.TryGetValue(id, out var prefab))
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
			return true;
		}

		IncreaseAfter(segmentId, 1);
		_segments.Insert(segmentId, value);

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

	public PartialPrefabList Clone(bool deepCopy)
		=> new PartialPrefabList(this, deepCopy);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PartialPrefabList(this, true);

	private static void ValidatePrefabs(IEnumerable<PartialPrefab> prefabs, string paramName)
	{
		int? nextId = null;

		foreach (var prefab in prefabs.OrderBy(prefab => prefab.Id))
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

	private static IEnumerable<PartialPrefabSegment> SegmentsFromPrefabs(IEnumerable<KeyValuePair<ushort, PartialPrefab>> prefabs)
		=> prefabs.OrderBy(item => item.Key).SelectMany(item => item.Value.Values);

	private bool IsLastPrefab(PartialPrefab prefab)
		=> prefab.Id + prefab.Count >= SegmentCount + IdOffset;

	private bool WillBeLastPrefab(PartialPrefab prefab)
		=> prefab.Id >= SegmentCount + IdOffset;

	private void IncreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _segments.Count; i++)
		{
			PartialPrefabSegment segment = _segments[i];

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
			PartialPrefabSegment segment = _segments[i];

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
