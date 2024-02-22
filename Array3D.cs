using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
        public class Array3D<T> : IEnumerable<T>
        {
            private T[] array;

            public int LengthX { get; private set; }
            public int LengthY { get; private set; }
            public int LengthZ { get; private set; }
            public int Length => array.Length;
            public int Count => array.Length;
            private int size2;

            public bool IsSynchronized => true;

            public object SyncRoot => syncRoot;

            public bool IsReadOnly => false;

            private static object syncRoot = new object();

            public T this[int i]
            {
                get => array[i];
                set => array[i] = value;
            }
            public T this[int x, int y, int z]
            {
                get => Get(x, y, z);
                set => Set(x, y, z, value);
            }

            public Array3D(int sizeX, int sizeY, int sizeZ)
            {
                LengthX = sizeX;
                LengthY = sizeY;
                LengthZ = sizeZ;
                size2 = sizeX * sizeY;

                array = new T[sizeX * sizeY * sizeZ];
            }
            public Array3D(IEnumerable<T> collection, int sizeX, int sizeY, int sizeZ)
            {
                LengthX = sizeX;
                LengthY = sizeY;
                LengthZ = sizeZ;
                size2 = sizeX * sizeY;

                array = collection.ToArray();
                if (Length != LengthX * LengthY * LengthZ)
                    throw new ArgumentException($"collection length must sizeX * sizeY * sizeZ", "collection");
            }
            public Array3D(T[] _array, int sizeX, int sizeY, int sizeZ)
            {
                LengthX = sizeX;
                LengthY = sizeY;
                LengthZ = sizeZ;
                size2 = sizeX * sizeY;

                array = (T[])_array.Clone();
                if (Length != LengthX * LengthY * LengthZ)
                    throw new ArgumentException($"collection length must sizeX * sizeY * sizeZ", "collection");
            }

            public bool InBounds(Vector3I pos)
                => InBounds(pos.X, pos.Y, pos.Z);
            public bool InBounds(int x, int y, int z)
                => x >= 0 && x < LengthX && y >= 0 && y < LengthY && z >= 0 && z < LengthZ;

            public int Index(Vector3I pos)
                => Index(pos.X, pos.Y, pos.Z);
            public int Index(int x, int y, int z)
                => x + y * LengthX + z * size2;

            public T Get(int x, int y, int z)
                => array[Index(x, y, z)];

            public void Set(int x, int y, int z, T value)
                => array[Index(x, y, z)] = value;

            public void Resize(int newX, int newY, int newZ)
            {
                T[] newArray = new T[newX * newY * newZ];

                int newSize2 = newX * newY;

                Parallel.For(0, newX, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, x =>
                {
                    for (int y = 0; y < newX; y++)
                        for (int z = 0; z < newZ; z++)
                            if (InBounds(x, y, z) && inBounds(x, y, z))
                                newArray[index(x, y, z)] = array[Index(x, y, z)];
                });

                array = newArray;

                LengthX = newX;
                LengthY = newY;
                LengthZ = newZ;
                size2 = LengthX * LengthY;

                int index(int x, int y, int z)
                    => x + y * newX + z * newSize2;
                bool inBounds(int x, int y, int z)
                    => x >= 0 && x < newX && y >= 0 && y < newY && z >= 0 && z < newZ;
            }

            public IEnumerator GetEnumerator()
                => array.GetEnumerator();

            public void CopyTo(Array array, int index)
                => array.CopyTo(array, index);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => ((IEnumerable<T>)array).GetEnumerator();
        }
}
