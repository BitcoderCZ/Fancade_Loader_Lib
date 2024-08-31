using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public class Array3D<T> : IEnumerable<T>
    {
        public T[] Array { get; private set; }

        public int LengthX { get; private set; }
        public int LengthY { get; private set; }
        public int LengthZ { get; private set; }
        public int Length => Array.Length;
        public int Count => Array.Length;
        private int size2;

        public bool IsSynchronized => true;

        public object SyncRoot => syncRoot;

        public bool IsReadOnly => false;

        private static object syncRoot = new object();

        public T this[int i]
        {
            get => Array[i];
            set => Array[i] = value;
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

            Array = new T[sizeX * sizeY * sizeZ];
        }
        public Array3D(IEnumerable<T> collection, int sizeX, int sizeY, int sizeZ)
        {
            LengthX = sizeX;
            LengthY = sizeY;
            LengthZ = sizeZ;
            size2 = sizeX * sizeY;

            Array = collection.ToArray();
            if (Length != LengthX * LengthY * LengthZ)
                throw new ArgumentException($"{nameof(collection)} length must be equal to: sizeX * sizeY * sizeZ ({LengthX * LengthY * LengthZ}), but it is: {Length}", "collection");
        }
        public Array3D(T[] array, int sizeX, int sizeY, int sizeZ)
        {
            LengthX = sizeX;
            LengthY = sizeY;
            LengthZ = sizeZ;
            size2 = sizeX * sizeY;

            Array = array;
            if (Length != LengthX * LengthY * LengthZ)
                throw new ArgumentException($"{nameof(array)}.Length must be equal to: sizeX * sizeY * sizeZ ({LengthX * LengthY * LengthZ}), but it is: {Length}", "collection");
        }
        public Array3D(Array3D<T> _array)
        {
            LengthX = _array.LengthX;
            LengthY = _array.LengthY;
            LengthZ = _array.LengthZ;
            size2 = _array.size2;

            Array = (T[])_array.Array.Clone();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InBounds(Vector3I pos)
            => InBounds(pos.X, pos.Y, pos.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InBounds(int x, int y, int z)
            => x >= 0 && x < LengthX && y >= 0 && y < LengthY && z >= 0 && z < LengthZ;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(Vector3I pos)
            => Index(pos.X, pos.Y, pos.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(int x, int y, int z)
            => x + y * LengthX + z * size2;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3I Index(int index)
        {
            int remaining = index % size2;
            return new Vector3I(remaining % LengthX, remaining / LengthX, index / size2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(int x, int y, int z)
            => Array[Index(x, y, z)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int x, int y, int z, T value)
            => Array[Index(x, y, z)] = value;

        public void Resize(int newX, int newY, int newZ)
        {
            T[] newArray = new T[newX * newY * newZ];

            int newSize2 = newX * newY;

            Parallel.For(0, newX, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, x =>
            {
                for (int y = 0; y < newX; y++)
                    for (int z = 0; z < newZ; z++)
                        if (InBounds(x, y, z) && inBounds(x, y, z))
                            newArray[index(x, y, z)] = Array[Index(x, y, z)];
            });

            Array = newArray;

            LengthX = newX;
            LengthY = newY;
            LengthZ = newZ;
            size2 = LengthX * LengthY;

            int index(int x, int y, int z)
                => x + y * newX + z * newSize2;
            bool inBounds(int x, int y, int z)
                => x >= 0 && x < newX && y >= 0 && y < newY && z >= 0 && z < newZ;
        }

        public Array3D<T> Clone()
            => new Array3D<T>(this);

        public void CopyTo(Array array, int index)
            => array.CopyTo(array, index);

        public IEnumerator GetEnumerator()
            => Array.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => ((IEnumerable<T>)Array).GetEnumerator();
    }
}
