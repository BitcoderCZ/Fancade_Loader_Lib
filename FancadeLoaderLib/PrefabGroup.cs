using FancadeLoaderLib.Exceptions;
using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FancadeLoaderLib
{
	public class PrefabGroup : IDictionary<byte3, Prefab>, ICloneable
	{
		private ushort id;
		public ushort Id
		{
			get => id;
			set
			{
				foreach (var prefab in Values)
					prefab.GroupId = value;

				id = value;
			}
		}
		public byte3 Size { get; private set; }

		public InvalidGroupIdBehaviour InvalidGroupIdBehaviour { get; set; } = InvalidGroupIdBehaviour.ThrowException;

		public ICollection<byte3> Keys => prefabs.Keys;
		public ICollection<Prefab> Values => prefabs.Values;

		public int Count => prefabs.Count;

		public bool IsReadOnly => false;

		public Prefab this[byte3 index]
		{
			get => prefabs[index];
			set => prefabs[index] = validate(value);
		}

		private readonly Dictionary<byte3, Prefab> prefabs;

		public PrefabGroup()
		{
			prefabs = new Dictionary<byte3, Prefab>();
			Size = byte3.Zero;
		}

		public PrefabGroup(IEnumerable<Prefab> collection)
		{
			if (!collection.Any())
				throw new ArgumentNullException(nameof(collection), $"{nameof(collection)} cannot be empty.");

			ushort? id = null;

			prefabs = collection.ToDictionary(prefab =>
			{
				// validate
				if (prefab.PosInGroup.X < 0 || prefab.PosInGroup.Y < 0 || prefab.PosInGroup.Z < 0)
					throw new ArgumentOutOfRangeException(nameof(collection), $"{nameof(Prefab.PosInGroup)} cannot be negative.");
				else if (!prefab.IsInGroup)
					throw new ArgumentException($"All prefabs in {nameof(collection)} must be in group", nameof(collection));
				else if (id != null && prefab.GroupId != id)
					throw new ArgumentException($"GroupId must be the same for all prefabs in {nameof(collection)}", nameof(collection));

				id = prefab.GroupId;

				return prefab.PosInGroup;
			});

			Id = id!.Value;

			findSize();
		}

		public PrefabGroup(IEnumerable<Prefab> collection, ushort id)
		{
			if (id == ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(id), $"{nameof(id)} cannot be {ushort.MaxValue}");

			Id = id;

			if (!collection.Any())
			{
				Size = byte3.Zero;
				prefabs = new Dictionary<byte3, Prefab>();
				return;
			}

			prefabs = collection.ToDictionary(prefab =>
			{
				// validate
				if (prefab.PosInGroup.X < 0 || prefab.PosInGroup.Y < 0 || prefab.PosInGroup.Z < 0)
					throw new ArgumentOutOfRangeException(nameof(collection), $"{nameof(Prefab.PosInGroup)} cannot be negative.");
				else if (!prefab.IsInGroup)
					throw new ArgumentException($"All prefabs in {nameof(collection)} must be in group", nameof(collection));
				else if (prefab.GroupId != Id)
					throw new ArgumentException($"GroupId must be the same for all prefabs in {nameof(collection)}", nameof(collection));

				return prefab.PosInGroup;
			});

			findSize();
		}

		public PrefabGroup(PrefabGroup group, bool deepCopy)
		{
			if (deepCopy)
				prefabs = new Dictionary<byte3, Prefab>(group.prefabs.Select(item => new KeyValuePair<byte3, Prefab>(item.Key, item.Value.Clone())));
			else
				prefabs = new Dictionary<byte3, Prefab>(group.prefabs);
			Size = group.Size;
		}

		public void SwapPositions(byte3 posA, byte3 posB)
		{
			TryGetValue(posA, out Prefab? a);
			TryGetValue(posB, out Prefab? b);

			if (!(a is null))
				a.PosInGroup = posB;
			if (!(b is null))
				b.PosInGroup = posA;

			this[posA] = b;
			this[posB] = a;
		}

		public void Add(byte3 key, Prefab value)
		{
			prefabs.Add(key, validate(value));

			Size = byte3.Max(Size, key + byte3.One);
		}

		public bool ContainsKey(byte3 key)
			=> prefabs.ContainsKey(key);

		public bool Remove(byte3 key)
		{
			bool val = prefabs.Remove(key);

			if (val)
				findSize();

			return val;
		}

		public bool TryGetValue(byte3 key, out Prefab value)
			=> prefabs.TryGetValue(key, out value);

		public void Clear()
		{
			prefabs.Clear();

			Size = byte3.Zero;
		}

		void ICollection<KeyValuePair<byte3, Prefab>>.Add(KeyValuePair<byte3, Prefab> item)
		{
			Prefab res = validate(item.Value);
			if (!ReferenceEquals(item.Value, res))
				item = new KeyValuePair<byte3, Prefab>(item.Key, res);

			((ICollection<KeyValuePair<byte3, Prefab>>)prefabs).Add(item);

			Size = byte3.Max(Size, item.Key + byte3.One);
		}

		bool ICollection<KeyValuePair<byte3, Prefab>>.Contains(KeyValuePair<byte3, Prefab> item)
			=> ((ICollection<KeyValuePair<byte3, Prefab>>)prefabs).Contains(item);

		void ICollection<KeyValuePair<byte3, Prefab>>.CopyTo(KeyValuePair<byte3, Prefab>[] array, int arrayIndex)
			=> ((ICollection<KeyValuePair<byte3, Prefab>>)prefabs).CopyTo(array, arrayIndex);

		bool ICollection<KeyValuePair<byte3, Prefab>>.Remove(KeyValuePair<byte3, Prefab> item)
		{
			bool val = ((ICollection<KeyValuePair<byte3, Prefab>>)prefabs).Remove(item);

			if (val)
				findSize();

			return val;
		}

		public IEnumerator<KeyValuePair<byte3, Prefab>> GetEnumerator()
			=> prefabs.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> prefabs.GetEnumerator();

		public IEnumerable<Prefab> EnumerateInIdOrder()
		{
			for (byte z = 0; z < Size.Z; z++)
			{
				for (byte y = 0; y < Size.Y; y++)
				{
					for (byte x = 0; x < Size.X; x++)
					{
						if (prefabs.TryGetValue(new byte3(x, y, z), out var prefab))
							yield return prefab;
					}
				}
			}
		}

		public PrefabGroup Clone(bool deepCopy)
			=> new PrefabGroup(this, deepCopy);
		object ICloneable.Clone()
			=> new PrefabGroup(this, true);

		private void findSize()
		{
			Size = byte3.Zero;

			foreach (var (pos, _) in prefabs)
				Size = byte3.Max(Size, pos + byte3.One);
		}

		private Prefab validate(Prefab prefab)
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
							Prefab newPrefab = prefab.Clone();
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

	public enum InvalidGroupIdBehaviour
	{
		ThrowException,
		ChangeGroupId,
		CloneAndChangeGroupId,
	}
}
