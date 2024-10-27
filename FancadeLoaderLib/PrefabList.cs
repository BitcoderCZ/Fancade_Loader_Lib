using FancadeLoaderLib.Raw;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FancadeLoaderLib
{
    /// <summary>
    /// <see cref="List{T}"/> wrapper for easier <see cref="Prefab"/> handeling.
    /// Also allows for saving/loading.
    /// </summary>
    public class PrefabList : IList<Prefab>, ICloneable
    {
        public ushort IdOffset = RawGame.CurrentNumbStockPrefabs;

        private List<Prefab> list;

        public Prefab this[int index]
        {
            get => list[index];
            set => list[index] = value;
        }

        public int Count => list.Count;

        public bool IsReadOnly => false;

        public PrefabList()
        {
            list = new List<Prefab>();
        }
        public PrefabList(int capacity)
        {
            list = new List<Prefab>(capacity);
        }
        public PrefabList(IEnumerable<Prefab> collection)
        {
            list = new List<Prefab>(collection);
        }
        public PrefabList(PrefabList list, bool deepCopy)
        {
            if (deepCopy)
                this.list = new List<Prefab>(list.Select(prefab => prefab.Clone()));
            else
                this.list = new List<Prefab>(list);
        }
        private PrefabList(List<Prefab> list)
        {
            this.list = list;
        }

        public void Save(FcBinaryWriter writer)
        {
            writer.WriteUInt32((uint)Count);
            writer.WriteUInt16(IdOffset);

            for (int i = 0; i < Count; i++)
                this[i].ToRaw(false).Save(writer);
        }
        public static PrefabList Load(FcBinaryReader reader)
        {
            uint count = reader.ReadUInt32();
            ushort idOffset = reader.ReadUInt16();

            List<Prefab> list = new List<Prefab>((int)count);

            for (int i = 0; i < count; i++)
                list.Add(Prefab.FromRaw(RawPrefab.Load(reader), ushort.MaxValue, 0, false));

            return new PrefabList(list)
            {
                IdOffset = idOffset,
            };
        }

        public void Add(Prefab item)
            => list.Add(item);
        public void AddRange(IEnumerable<Prefab> collection)
            => list.AddRange(collection);
        public void AddGroup(PrefabGroup group)
        {
            group.Id = (ushort)list.Count;
            list.AddRange(group.EnumerateInIdOrder());
        }

        public void Clear()
            => list.Clear();

        public bool Contains(Prefab item)
            => list.Contains(item);

        public void CopyTo(Prefab[] array, int arrayIndex)
            => list.CopyTo(array, arrayIndex);
        public void CopyTo(Prefab[] array)
            => list.CopyTo(array);
        public void CopyTo(int index, Prefab[] array, int arrayIndex, int count)
            => list.CopyTo(index, array, arrayIndex, count);

        public bool Exists(Predicate<Prefab> match)
            => list.Exists(match);

        public Prefab Find(Predicate<Prefab> match)
            => list.Find(match);
        public List<Prefab> FindAll(Predicate<Prefab> match)
            => list.FindAll(match);

        public int FindIndex(int startIndex, int count, Predicate<Prefab> match)
            => list.FindIndex(startIndex, count, match);
        public int FindIndex(int startIndex, Predicate<Prefab> match)
            => list.FindIndex(startIndex, match);
        public int FindIndex(Predicate<Prefab> match)
            => list.FindIndex(match);

        public Prefab FindLast(Predicate<Prefab> match)
            => list.FindLast(match);

        public int FindLastIndex(int startIndex, int count, Predicate<Prefab> match)
            => list.FindLastIndex(startIndex, count, match);
        public int FindLastIndex(int startIndex, Predicate<Prefab> match)
            => list.FindLastIndex(startIndex, match);
        public int FindLastIndex(Predicate<Prefab> match)
            => list.FindLastIndex(match);

        public void Insert(int index, Prefab item)
        {
            increaseAfter(index, 1);
            list.Insert(index, item);
        }

        public void InsertRange(int index, IEnumerable<Prefab> collection)
        {
            int count = collection.Count();
            increaseAfter(index, (ushort)count);
            list.InsertRange(index, collection);
        }

        public void InsertGroup(int index, PrefabGroup group)
        {
            increaseAfter(index, (ushort)group.Count);
            group.Id = (ushort)index;
            list.InsertRange(index, group.EnumerateInIdOrder());
        }

        public int LastIndexOf(Prefab item)
            => list.LastIndexOf(item);

        public int LastIndexOf(Prefab item, int index)
            => list.LastIndexOf(item, index);

        public int LastIndexOf(Prefab item, int index, int count)
            => list.LastIndexOf(item, index, count);

        public int IndexOf(Prefab item)
            => list.IndexOf(item);

        public bool Remove(Prefab item)
        {
            int index = list.IndexOf(item);
            if (index < 0) return false;
            else
            {
                RemoveAt(index);
                return true;
            }
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
            decreaseAfter(index - 1, 1);
        }

        public void RemoveRange(int index, int count)
        {
            list.RemoveRange(index, count);
            decreaseAfter(index - 1, (ushort)count);
        }

        public bool TrueForAll(Predicate<Prefab> match)
            => list.TrueForAll(match);

        public Prefab[] ToArray()
            => list.ToArray();

        public IEnumerator<Prefab> GetEnumerator()
            => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => list.GetEnumerator();

        public PrefabList Clone(bool deepCopy)
            => new PrefabList(this, deepCopy);
        object ICloneable.Clone()
            => new PrefabList(this, true);

        private void increaseAfter(int index, ushort amount)
        {
            index += IdOffset;

            for (int i = 0; i < list.Count; i++)
            {
                Prefab prefab = this[i];

                if (prefab.IsInGroup && prefab.GroupId >= index)
                    prefab.GroupId += amount;

                if (!(prefab.Blocks is null))
                {
                    ushort[] array = prefab.Blocks.Array.Array;

                    for (int j = 0; j < array.Length; j++)
                        if (array[j] >= index)
                            array[j] += amount;
                }
            }
        }

        private void decreaseAfter(int index, ushort amount)
        {
            index += IdOffset;

            for (int i = 0; i < list.Count; i++)
            {
                Prefab prefab = this[i];

                if (prefab.IsInGroup && prefab.GroupId >= index)
                    prefab.GroupId -= amount;

                if (!(prefab.Blocks is null))
                {
                    ushort[] array = prefab.Blocks.Array.Array;

                    for (int j = 0; j < array.Length; j++)
                        if (array[j] >= index)
                            array[j] -= amount;
                }
            }
        }
    }
}
