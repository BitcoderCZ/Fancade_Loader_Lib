using FancadeLoaderLib.Raw;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FancadeLoaderLib.Partial
{
    /// <summary>
    /// <see cref="PrefabList"/> for <see cref="PartialPrefab"/>.
    /// </summary>
    public class PartialPrefabList : IList<PartialPrefab>, ICloneable
    {
        public ushort IdOffset = RawGame.CurrentNumbStockPrefabs;

        private List<PartialPrefab> list;

        public PartialPrefab this[int index]
        {
            get => list[index];
            set => list[index] = value;
        }

        public int Count => list.Count;

        public bool IsReadOnly => false;

        public PartialPrefabList()
        {
            list = new List<PartialPrefab>();
        }
        public PartialPrefabList(int capacity)
        {
            list = new List<PartialPrefab>(capacity);
        }
        public PartialPrefabList(IEnumerable<PartialPrefab> collection)
        {
            list = new List<PartialPrefab>(collection);
        }
        public PartialPrefabList(PartialPrefabList list, bool deepCopy)
        {
            if (deepCopy)
                this.list = new List<PartialPrefab>(list.Select(prefab => prefab.Clone()));
            else
                this.list = new List<PartialPrefab>(list);
        }
        private PartialPrefabList(List<PartialPrefab> list)
        {
            this.list = list;
        }

        public void Save(FcBinaryWriter writer)
        {
            writer.WriteUInt32((uint)Count);
            writer.WriteUInt16(IdOffset);

            for (int i = 0; i < Count; i++)
                this[i].Save(writer);
        }
        public static PartialPrefabList Load(FcBinaryReader reader)
        {
            uint count = reader.ReadUInt32();
            ushort idOffset = reader.ReadUInt16();

            List<PartialPrefab> list = new List<PartialPrefab>((int)count);

            for (int i = 0; i < count; i++)
                list.Add(PartialPrefab.Load(reader));

            return new PartialPrefabList(list)
            {
                IdOffset = idOffset,
            };
        }

        public void Add(PartialPrefab item)
            => list.Add(item);
        public void AddRange(IEnumerable<PartialPrefab> collection)
            => list.AddRange(collection);
        public void AddGroup(PartialPrefabGroup group)
        {
            group.Id = (ushort)list.Count;
            list.AddRange(group.EnumerateInIdOrder());
        }

        public void Clear()
            => list.Clear();

        public bool Contains(PartialPrefab item)
            => list.Contains(item);

        public void CopyTo(PartialPrefab[] array, int arrayIndex)
            => list.CopyTo(array, arrayIndex);
        public void CopyTo(PartialPrefab[] array)
            => list.CopyTo(array);
        public void CopyTo(int index, PartialPrefab[] array, int arrayIndex, int count)
            => list.CopyTo(index, array, arrayIndex, count);

        public bool Exists(Predicate<PartialPrefab> match)
            => list.Exists(match);

        public PartialPrefab Find(Predicate<PartialPrefab> match)
            => list.Find(match);
        public List<PartialPrefab> FindAll(Predicate<PartialPrefab> match)
            => list.FindAll(match);

        public int FindIndex(int startIndex, int count, Predicate<PartialPrefab> match)
            => list.FindIndex(startIndex, count, match);
        public int FindIndex(int startIndex, Predicate<PartialPrefab> match)
            => list.FindIndex(startIndex, match);
        public int FindIndex(Predicate<PartialPrefab> match)
            => list.FindIndex(match);

        public PartialPrefab FindLast(Predicate<PartialPrefab> match)
            => list.FindLast(match);

        public int FindLastIndex(int startIndex, int count, Predicate<PartialPrefab> match)
            => list.FindLastIndex(startIndex, count, match);
        public int FindLastIndex(int startIndex, Predicate<PartialPrefab> match)
            => list.FindLastIndex(startIndex, match);
        public int FindLastIndex(Predicate<PartialPrefab> match)
            => list.FindLastIndex(match);

        public void Insert(int index, PartialPrefab item)
        {
            increaseAfter(index, 1);
            list.Insert(index, item);
        }

        public void InsertRange(int index, IEnumerable<PartialPrefab> collection)
        {
            int count = collection.Count();
            increaseAfter(index, (ushort)count);
            list.InsertRange(index, collection);
        }

        public void InsertGroup(int index, PartialPrefabGroup group)
        {
            increaseAfter(index, (ushort)group.Count);
            group.Id = (ushort)index;
            list.InsertRange(index, group.EnumerateInIdOrder());
        }

        public int LastIndexOf(PartialPrefab item)
            => list.LastIndexOf(item);

        public int LastIndexOf(PartialPrefab item, int index)
            => list.LastIndexOf(item, index);

        public int LastIndexOf(PartialPrefab item, int index, int count)
            => list.LastIndexOf(item, index, count);

        public int IndexOf(PartialPrefab item)
            => list.IndexOf(item);

        public bool Remove(PartialPrefab item)
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

        public bool TrueForAll(Predicate<PartialPrefab> match)
            => list.TrueForAll(match);

        public PartialPrefab[] ToArray()
            => list.ToArray();

        public IEnumerator<PartialPrefab> GetEnumerator()
            => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => list.GetEnumerator();

        public PartialPrefabList Clone(bool deepCopy)
            => new PartialPrefabList(this, deepCopy);
        object ICloneable.Clone()
            => new PartialPrefabList(this, true);

        private void increaseAfter(int index, ushort amount)
        {
            index += IdOffset;

            for (int i = 0; i < list.Count; i++)
            {
                PartialPrefab prefab = this[i];

                if (prefab.IsInGroup && prefab.GroupId >= index)
                    prefab.GroupId += amount;
            }
        }

        private void decreaseAfter(int index, ushort amount)
        {
            index += IdOffset;

            for (int i = 0; i < list.Count; i++)
            {
                PartialPrefab prefab = this[i];

                if (prefab.IsInGroup && prefab.GroupId >= index)
                    prefab.GroupId -= amount;
            }
        }
    }
}
