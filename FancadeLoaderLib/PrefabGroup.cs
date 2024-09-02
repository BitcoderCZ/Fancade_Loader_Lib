﻿using FancadeLoaderLib.Exceptions;
using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace FancadeLoaderLib
{
    public class PrefabGroup : ICloneable, IDictionary<Vector3B, Prefab>
    {
        public ushort Id { get; private set; }
        public Vector3B Size { get; private set; }

        public InvalidGroupIdBehaviour InvalidGroupIdBehaviour { get; set; } = InvalidGroupIdBehaviour.ThrowException;

        public ICollection<Vector3B> Keys => prefabs.Keys;
        public ICollection<Prefab> Values => prefabs.Values;

        public int Count => prefabs.Count;

        public bool IsReadOnly => false;

        public Prefab this[Vector3B index]
        {
            get => prefabs[index];
            set => prefabs[index] = validate(value);
        }

        private readonly Dictionary<Vector3B, Prefab> prefabs;

        public PrefabGroup()
        {
            prefabs = new Dictionary<Vector3B, Prefab>();
            Size = Vector3B.Zero;
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
                else if (!prefab.IsInGourp)
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
                Size = Vector3B.Zero;
                prefabs = new Dictionary<Vector3B, Prefab>();
                return;
            }

            prefabs = collection.ToDictionary(prefab =>
            {
                // validate
                if (prefab.PosInGroup.X < 0 || prefab.PosInGroup.Y < 0 || prefab.PosInGroup.Z < 0)
                    throw new ArgumentOutOfRangeException(nameof(collection), $"{nameof(Prefab.PosInGroup)} cannot be negative.");
                else if (!prefab.IsInGourp)
                    throw new ArgumentException($"All prefabs in {nameof(collection)} must be in group", nameof(collection));
                else if (prefab.GroupId != Id)
                    throw new ArgumentException($"GroupId must be the same for all prefabs in {nameof(collection)}", nameof(collection));

                return prefab.PosInGroup;
            });

            findSize();
        }

        public PrefabGroup(PrefabGroup group)
        {
            prefabs = new Dictionary<Vector3B, Prefab>(group.prefabs);
            Size = group.Size;
        }

        public void SwapPositions(Vector3B posA, Vector3B posB)
        {
            Prefab a = this[posA];
            Prefab b = this[posB];

            a.PosInGroup = posB;
            b.PosInGroup = posA;

            this[posA] = b;
            this[posB] = a;
        }

        public void Add(Vector3B key, Prefab value)
        {
            prefabs.Add(key, value);

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

        public bool TryGetValue(Vector3B key, out Prefab value)
            => prefabs.TryGetValue(key, out value);

        public void Clear()
        {
            prefabs.Clear();

            Size = Vector3B.Zero;
        }

        void ICollection<KeyValuePair<Vector3B, Prefab>>.Add(KeyValuePair<Vector3B, Prefab> item)
        {
            ((ICollection<KeyValuePair<Vector3B, Prefab>>)prefabs).Add(item);

            Size = Vector3B.Max(Size, item.Key + Vector3B.One);
        }

        bool ICollection<KeyValuePair<Vector3B, Prefab>>.Contains(KeyValuePair<Vector3B, Prefab> item)
            => ((ICollection<KeyValuePair<Vector3B, Prefab>>)prefabs).Contains(item);

        void ICollection<KeyValuePair<Vector3B, Prefab>>.CopyTo(KeyValuePair<Vector3B, Prefab>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<Vector3B, Prefab>>)prefabs).CopyTo(array, arrayIndex);

        bool ICollection<KeyValuePair<Vector3B, Prefab>>.Remove(KeyValuePair<Vector3B, Prefab> item)
        {
            bool val = ((ICollection<KeyValuePair<Vector3B, Prefab>>)prefabs).Remove(item);

            if (val)
                findSize();

            return val;
        }

        public IEnumerator<KeyValuePair<Vector3B, Prefab>> GetEnumerator()
            => prefabs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => prefabs.GetEnumerator();

        public PrefabGroup Clone()
            => new PrefabGroup(this);
        object ICloneable.Clone()
            => new PrefabGroup(this);

        private void findSize()
        {
            Size = Vector3B.Zero;

            foreach (var (pos, _) in prefabs)
                Size = Vector3B.Max(Size, pos + Vector3B.One);
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