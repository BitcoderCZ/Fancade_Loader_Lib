using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FancadeLoaderLib
{
    /// <summary>
    /// <see cref="List{T}"/> wrapper for easier <see cref="Prefab"/> handeling
    /// </summary>
    public class PrefabList : IList<Prefab>
    {
        public int IdOffset = RawGame.CurrentNumbStockPrefabs;

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

        public void Add(Prefab item)
            => list.Add(item);
        public void AddRange(IEnumerable<Prefab> collection)
            => list.AddRange(collection);

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
            list.Insert(index, item);
            increaseAfter(index, 1);
        }

        public void InsertRange(int index, IEnumerable<Prefab> collection)
        {
            list.InsertRange(index, collection);
            int count = collection.Count();
            increaseAfter(index + count - 1, (ushort)count);
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

        private void increaseAfter(int index, ushort amount)
        {
            index += IdOffset;

            for (int i = index; i < list.Count; i++)
            {
                Prefab prefab = this[i];

                if (prefab.IsInGourp)
                    prefab.GroupId += amount;

                if (!(prefab.Blocks is null))
                {
                    ushort[] array = prefab.Blocks.Array.Array;

                    for (int j = 0; j < array.Length; j++)
                        if (array[j] > index)
                            array[j] += amount;
                }
            }
        }

        private void decreaseAfter(int index, ushort amount)
        {
            index += IdOffset;

            for (int i = index; i < list.Count; i++)
            {
                Prefab prefab = this[i];

                if (prefab.IsInGourp)
                    prefab.GroupId -= amount;

                if (!(prefab.Blocks is null))
                {
                    ushort[] array = prefab.Blocks.Array.Array;

                    for (int j = 0; j < array.Length; j++)
                        if (array[j] > index)
                            array[j] -= amount;
                }
            }
        }
    }
}
