// <copyright file="Array3D.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib;

/// <summary>
/// Represents a 3D array.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public class Array3D<T> : IEnumerable<T>
{
    private int _layerSize;
    private T[] _array;

    /// <summary>
    /// Initializes a new instance of the <see cref="Array3D{T}"/> class.
    /// </summary>
    /// <param name="size">Size of the array.</param>
    public Array3D(int3 size)
    {
        if (size.X < 0 || size.Y < 0 || size.Z < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(size));
        }
        else if ((size.X == 1 || size.Y == 1 || size.Z == 1) && (size.X == 0 || size.Y == 0 || size.Z == 0))
        {
            ThrowArgumentOutOfRangeException(nameof(size));
        }

        Size = size;
        _layerSize = Size.X * Size.Y;

        _array = new T[Size.X * Size.Y * Size.Z];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Array3D{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection to contruct the array from.</param>
    /// <param name="size">Size of the array.</param>
    public Array3D(IEnumerable<T> collection, int3 size)
    {
        if (size.X < 0 || size.Y < 0 || size.Z < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(size));
        }
        else if ((size.X == 1 || size.Y == 1 || size.Z == 1) && (size.X == 0 || size.Y == 0 || size.Z == 0))
        {
            ThrowArgumentOutOfRangeException(nameof(size));
        }

        Size = size;
        _layerSize = Size.X * Size.Y;

        _array = [.. collection];

        if (Length != Size.X * Size.Y * Size.Z)
        {
            ThrowArgumentException($"{nameof(collection)}'s length must be equal to: {nameof(size)}.X * {nameof(size)}.Y * {nameof(size)}.Z ({Size.X * Size.Y * Size.Z}), but it was: {Length}.", nameof(size));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Array3D{T}"/> class.
    /// </summary>
    /// <param name="array">The array to contruct this <see cref="Array3D{T}"/> from.</param>
    /// <param name="size">Size of the array.</param>
    public Array3D(T[] array, int3 size)
    {
        ThrowIfNull(array, nameof(array));

        if (size.X < 0 || size.Y < 0 || size.Z < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(size));
        }
        else if ((size.X == 1 || size.Y == 1 || size.Z == 1) && (size.X == 0 || size.Y == 0 || size.Z == 0))
        {
            ThrowArgumentOutOfRangeException(nameof(size));
        }

        Size = size;
        _layerSize = Size.X * Size.Y;

        _array = array;

        if (Length != Size.X * Size.Y * Size.Z)
        {
            ThrowArgumentException($"{nameof(array)}.Length must be equal to: {nameof(size)}.X * {nameof(size)}.Y * {nameof(size)}.Z ({Size.X * Size.Y * Size.Z}), but it was: {Length}.", nameof(size));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Array3D{T}"/> class.
    /// </summary>
    /// <param name="array">The <see cref="Array3D{T}"/> to copy.</param>
    public Array3D(Array3D<T> array)
    {
        ThrowIfNull(array, nameof(array));

        Size = array.Size;
        _layerSize = array._layerSize;

        _array = (T[])array._array.Clone();
    }

    /// <summary>
    /// Gets the underlying array.
    /// </summary>
    /// <value>The underlying array.</value>
#pragma warning disable CA1819 // Properties should not return arrays
    public T[] Array => _array;
#pragma warning restore CA1819

    /// <summary>
    /// Gets the size of this array.
    /// </summary>
    /// <value>The size of this array.</value>
    public int3 Size { get; private set; }

    /// <summary>
    /// Gets the total length of this array.
    /// </summary>
    /// <value>The total length of this array.</value>
    public int Length => _array.Length;

    /// <summary>
    /// Gets or sets the item at the specified index.
    /// </summary>
    /// <param name="index">Index of the item.</param>
    /// <returns>Item at the specified index.</returns>
    public T this[int index]
    {
        get => _array[index];
        set => _array[index] = value;
    }

    /// <summary>
    /// Gets or sets the item at the specified index.
    /// </summary>
    /// <param name="x">X index of the item.</param>
    /// <param name="y">Y index of the item.</param>
    /// <param name="z">Z index of the item.</param>
    /// <returns>Item at the specified index.</returns>
    public T this[int x, int y, int z]
    {
        get => Get(new int3(x, y, z));
        set => Set(new int3(x, y, z), value);
    }

    /// <summary>
    /// Gets or sets the item at the specified position.
    /// </summary>
    /// <param name="pos">Position of the item.</param>
    /// <returns>Item at the specified position.</returns>
    public T this[int3 pos]
    {
        get => Get(pos);
        set => Set(pos, value);
    }

    /// <summary>
    /// Determines if the specified position is inside the bounds of this array.
    /// </summary>
    /// <param name="pos">The position to check.</param>
    /// <returns><see langword="true"/> if <paramref name="pos"/> is inside the bounds of this array; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool InBounds(int3 pos)
        => pos.InBounds(Size.X, Size.Y, Size.Z);

    /// <summary>
    /// Converts <paramref name="pos"/> into an index.
    /// </summary>
    /// <param name="pos">The position to convert.</param>
    /// <returns>Index into <see cref="_array"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index(int3 pos)
        => pos.X + (pos.Y * Size.X) + (pos.Z * _layerSize);

    /// <summary>
    /// Converts an index into a position.
    /// </summary>
    /// <param name="index">The index to convert.</param>
    /// <returns>Position interpretation of <paramref name="index"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int3 Index(int index)
    {
        int remaining = index % _layerSize;
        return new int3(remaining % Size.X, remaining / Size.X, index / _layerSize);
    }

    /// <summary>
    /// Gets the item at the specified position.
    /// </summary>
    /// <param name="pos">Position of the item.</param>
    /// <returns>Item at the specified position.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(int3 pos)
    {
        if (!InBounds(pos))
        {
            ThrowArgumentOutOfRangeException(nameof(pos));
        }

        return _array[Index(pos)];
    }

    /// <summary>
    /// Gets the item at the specified position without bounds checking.
    /// </summary>
    /// <param name="pos">Position of the item.</param>
    /// <returns>Item at the specified position.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetUnchecked(int3 pos)
        => _array[Index(pos)];

    /// <summary>
    /// Sets the item at the specified position.
    /// </summary>
    /// <param name="pos">Position of the item.</param>
    /// <param name="value">The value to set at the specified position.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int3 pos, T value)
    {
        if (!InBounds(pos))
        {
            ThrowArgumentOutOfRangeException(nameof(pos));
        }

        _array[Index(pos)] = value;
    }

    /// <summary>
    /// Sets the item at the specified position without bounds checking.
    /// </summary>
    /// <param name="pos">Position of the item.</param>
    /// <param name="value">The value to set at the specified position.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUnchecked(int3 pos, T value)
        => _array[Index(pos)] = value;

    /// <summary>
    /// Changes the size of this array.
    /// </summary>
    /// <param name="newSize">The new size.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="newSize"/> is negative.</exception>
    public void Resize(int3 newSize)
    {
        if (newSize.X < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(newSize.X));
        }
        else if (newSize.Y < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(newSize.Y));
        }
        else if (newSize.Z < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(newSize.Z));
        }
        else if (newSize == int3.Zero)
        {
            _array = [];

            Size = int3.Zero;
            _layerSize = 0;

            return;
        }
        else if (newSize == Size)
        {
            return; // same length
        }
        else if ((newSize.X == 1 || newSize.Y == 1 || newSize.Z == 1) && (newSize.X == 0 || newSize.Y == 0 || newSize.Z == 0))
        {
            ThrowArgumentOutOfRangeException(nameof(newSize));
        }

        T[] newArray = new T[newSize.X * newSize.Y * newSize.Z];
        int newLayerSize = newSize.X * newSize.Y;
        int minX = Math.Min(Size.X, newSize.X);
        for (int z = 0; z < newSize.Z; z++)
        {
            for (int y = 0; y < newSize.Y; y++)
            {
                int3 pos = new int3(0, y, z);

                if (InBounds(pos) && InBoundsNew(y, z))
                {
                    System.Array.Copy(_array, Index(pos), newArray, IndexNew(0, y, z), minX);
                }
            }
        }

        _array = newArray;

        Size = newSize;
        _layerSize = Size.X * Size.Y;

        int IndexNew(int x, int y, int z)
        {
            return x + (y * newSize.X) + (z * newLayerSize);
        }

        bool InBoundsNew(int y, int z)
        {
            return y >= 0 && y < newSize.Y && z >= 0 && z < newSize.Z;
        }
    }

    /// <summary>
    /// Sets the elements of the array to the default value of each element type.
    /// </summary>
    public void Clear()
        => System.Array.Clear(_array, 0, _array.Length);

    /// <summary>
    /// Creates a copy of this array.
    /// </summary>
    /// <returns>A copy of this array.</returns>
    public Array3D<T> Clone()
        => new Array3D<T>(this);

    /// <inheritdoc/>
    public IEnumerator GetEnumerator()
        => _array.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => ((IEnumerable<T>)_array).GetEnumerator();
}
