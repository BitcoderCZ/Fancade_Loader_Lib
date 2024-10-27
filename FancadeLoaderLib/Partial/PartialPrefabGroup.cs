using FancadeLoaderLib.Exceptions;
using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FancadeLoaderLib.Partial
{
    /// <summary>
    /// <see cref="PrefabGroup"/> for <see cref="PartialPrefab"/>, usefull for when just the dimensions of groups are needed.
    /// </summary>
    public class PartialPrefabGroup : IDictionary<Vector3B, PartialPrefab>, ICloneable
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
        public Vector3B Size { get; private set; }

        public InvalidGroupIdBehaviour InvalidGroupIdBehaviour { get; set; } = InvalidGroupIdBehaviour.ThrowException;

        public ICollection<Vector3B> Keys => prefabs.Keys;
        public ICollection<PartialPrefab> Values => prefabs.Values;

        public int Count => prefabs.Count;

        public bool IsReadOnly => false;

        public PartialPrefab this[Vector3B index]
        {
            get => prefabs[index];
            set => prefabs[index] = validate(value);
        }

        private readonly Dictionary<Vector3B, PartialPrefab> prefabs;

        public PartialPrefabGroup()
        {
            prefabs = new Dictionary<Vector3B, PartialPrefab>();
            Size = Vector3B.Zero;
        }

        public PartialPrefabGroup(IEnumerable<PartialPrefab> collection)
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

        public PartialPrefabGroup(IEnumerable<PartialPrefab> collection, ushort id)
        {
            if (id == ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(id), $"{nameof(id)} cannot be {ushort.MaxValue}");

            Id = id;

            if (!collection.Any())
            {
                Size = Vector3B.Zero;
                prefabs = new Dictionary<Vector3B, PartialPrefab>();
                return;
            }

            prefabs = collection.ToDictionary(prefab =>
            {
                // validate
                if (prefab.PosInGroup.X < 0 || prefab.PosInGroup.Y < 0 || prefab.PosInGroup.Z < 0)
                    throw new ArgumentOutOfRangeException(nameof(collection), $"{nameof(PartialPrefab.PosInGroup)} cannot be negative.");
                else if (!prefab.IsInGroup)
                    throw new ArgumentException($"All prefabs in {nameof(collection)} must be in group", nameof(collection));
                else if (prefab.GroupId != Id)
                    throw new ArgumentException($"GroupId must be the same for all prefabs in {nameof(collection)}", nameof(collection));

                return prefab.PosInGroup;
            });

            findSize();
        }

        public PartialPrefabGroup(PartialPrefabGroup group, bool deepCopy)
        {
            if (deepCopy)
                prefabs = new Dictionary<Vector3B, PartialPrefab>(group.prefabs.Select(item => new KeyValuePair<Vector3B, PartialPrefab>(item.Key, item.Value.Clone())));
            else
                prefabs = new Dictionary<Vector3B, PartialPrefab>(group.prefabs);

            Size = group.Size;
        }

        public void SwapPositions(Vector3B posA, Vector3B posB)
        {
            TryGetValue(posA, out PartialPrefab? a);
            TryGetValue(posB, out PartialPrefab? b);

            if (!(a is null))
                a.PosInGroup = posB;
            if (!(b is null))
                b.PosInGroup = posA;

            this[posA] = b;
            this[posB] = a;
        }

        public void Add(Vector3B key, PartialPrefab value)
        {
            prefabs.Add(key, validate(value));

            Size = Vector3B.Max(Size, key + Vector3B.One);
        }

        public bool ContainsKey(Vector3B key)
            => prefabs.ContainsKey(key);

        public bool Remove(Vector3B key)
        {
            bool val = prefabs.Remove(key);

            if (val)
                findSize();

            return val;
        }

        public bool TryGetValue(Vector3B key, out PartialPrefab value)
            => prefabs.TryGetValue(key, out value);

        public void Clear()
        {
            prefabs.Clear();

            Size = Vector3B.Zero;
        }

        void ICollection<KeyValuePair<Vector3B, PartialPrefab>>.Add(KeyValuePair<Vector3B, PartialPrefab> item)
        {
            PartialPrefab res = validate(item.Value);
            if (!ReferenceEquals(item.Value, res))
                item = new KeyValuePair<Vector3B, PartialPrefab>(item.Key, res);

            ((ICollection<KeyValuePair<Vector3B, PartialPrefab>>)prefabs).Add(item);

            Size = Vector3B.Max(Size, item.Key + Vector3B.One);
        }

        bool ICollection<KeyValuePair<Vector3B, PartialPrefab>>.Contains(KeyValuePair<Vector3B, PartialPrefab> item)
            => ((ICollection<KeyValuePair<Vector3B, PartialPrefab>>)prefabs).Contains(item);

        void ICollection<KeyValuePair<Vector3B, PartialPrefab>>.CopyTo(KeyValuePair<Vector3B, PartialPrefab>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<Vector3B, PartialPrefab>>)prefabs).CopyTo(array, arrayIndex);

        bool ICollection<KeyValuePair<Vector3B, PartialPrefab>>.Remove(KeyValuePair<Vector3B, PartialPrefab> item)
        {
            bool val = ((ICollection<KeyValuePair<Vector3B, PartialPrefab>>)prefabs).Remove(item);

            if (val)
                findSize();

            return val;
        }

        public IEnumerator<KeyValuePair<Vector3B, PartialPrefab>> GetEnumerator()
            => prefabs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => prefabs.GetEnumerator();

        public IEnumerable<PartialPrefab> EnumerateInIdOrder()
        {
            for (byte z = 0; z < Size.Z; z++)
            {
                for (byte y = 0; y < Size.Y; y++)
                {
                    for (byte x = 0; x < Size.X; x++)
                    {
                        if (prefabs.TryGetValue(new Vector3B(x, y, z), out var prefab))
                            yield return prefab;
                    }
                }
            }
        }

        public PartialPrefabGroup Clone(bool deepCopy)
            => new PartialPrefabGroup(this, deepCopy);
        object ICloneable.Clone()
            => new PartialPrefabGroup(this, true);

        private void findSize()
        {
            Size = Vector3B.Zero;

            foreach (var (pos, _) in prefabs)
                Size = Vector3B.Max(Size, pos + Vector3B.One);
        }

        private PartialPrefab validate(PartialPrefab prefab)
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
}
