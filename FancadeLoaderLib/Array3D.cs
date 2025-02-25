// <copyright file="Array3D.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
	/// <param name="sizeX">X size of the array.</param>
	/// <param name="sizeY">Y size of the array.</param>
	/// <param name="sizeZ">Z size of the array.</param>
	public Array3D(int sizeX, int sizeY, int sizeZ)
	{
		LengthX = sizeX;
		LengthY = sizeY;
		LengthZ = sizeZ;
		_layerSize = sizeX * sizeY;

		_array = new T[sizeX * sizeY * sizeZ];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Array3D{T}"/> class.
	/// </summary>
	/// <param name="collection">The collection to contruct the array from.</param>
	/// <param name="sizeX">X size of the array.</param>
	/// <param name="sizeY">Y size of the array.</param>
	/// <param name="sizeZ">Z size of the array.</param>
	public Array3D(IEnumerable<T> collection, int sizeX, int sizeY, int sizeZ)
	{
		LengthX = sizeX;
		LengthY = sizeY;
		LengthZ = sizeZ;
		_layerSize = sizeX * sizeY;

		_array = [.. collection];
		if (Length != LengthX * LengthY * LengthZ)
		{
			throw new ArgumentException($"{nameof(collection)} length must be equal to: sizeX * sizeY * sizeZ ({LengthX * LengthY * LengthZ}), but it is: {Length}", nameof(collection));
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Array3D{T}"/> class.
	/// </summary>
	/// <param name="array">The array to contruct this <see cref="Array3D{T}"/> from.</param>
	/// <param name="sizeX">X size of the array.</param>
	/// <param name="sizeY">Y size of the array.</param>
	/// <param name="sizeZ">Z size of the array.</param>
	public Array3D(T[] array, int sizeX, int sizeY, int sizeZ)
	{
		LengthX = sizeX;
		LengthY = sizeY;
		LengthZ = sizeZ;
		_layerSize = sizeX * sizeY;

		_array = array;
		if (Length != LengthX * LengthY * LengthZ)
		{
			throw new ArgumentException($"{nameof(array)}.Length must be equal to: sizeX * sizeY * sizeZ ({LengthX * LengthY * LengthZ}), but it is: {Length}", nameof(array));
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Array3D{T}"/> class.
	/// </summary>
	/// <param name="array">The <see cref="Array3D{T}"/> to copy.</param>
	public Array3D(Array3D<T> array)
	{
		if (array is null)
		{
			throw new ArgumentNullException(nameof(array));
		}

		LengthX = array.LengthX;
		LengthY = array.LengthY;
		LengthZ = array.LengthZ;
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
	/// Gets the X size of this array.
	/// </summary>
	/// <value>The X size of this array.</value>
	public int LengthX { get; private set; }

	/// <summary>
	/// Gets the Y size of this array.
	/// </summary>
	/// <value>The Y size of this array.</value>
	public int LengthY { get; private set; }

	/// <summary>
	/// Gets the Z size of this array.
	/// </summary>
	/// <value>The Z size of this array.</value>
	public int LengthZ { get; private set; }

	/// <summary>
	/// Gets the size of this array.
	/// </summary>
	/// <value>The size of this array.</value>
	public int3 Size => new int3(LengthX, LengthY, LengthZ);

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
		get => Get(x, y, z);
		set => Set(x, y, z, value);
	}

	/// <summary>
	/// Determines if the specified position is inside the bounds of this array.
	/// </summary>
	/// <param name="pos">The position to check.</param>
	/// <returns><see langword="true"/> if <paramref name="pos"/> is inside the bounds of this array; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool InBounds(int3 pos)
		=> InBounds(pos.X, pos.Y, pos.Z);

	/// <summary>
	/// Determines if the specified position is inside the bounds of this array.
	/// </summary>
	/// <param name="x">The x postition.</param>
	/// <param name="y">The y postition.</param>
	/// <param name="z">The z postition.</param>
	/// <returns><see langword="true"/> if the position is inside the bounds of this array; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool InBounds(int x, int y, int z)
		=> x >= 0 && x < LengthX && y >= 0 && y < LengthY && z >= 0 && z < LengthZ;

	/// <summary>
	/// Converts <paramref name="pos"/> into an index.
	/// </summary>
	/// <param name="pos">The position to convert.</param>
	/// <returns>Index into <see cref="_array"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Index(int3 pos)
		=> Index(pos.X, pos.Y, pos.Z);

	/// <summary>
	/// Converts a position into an index.
	/// </summary>
	/// <param name="x">The x postition.</param>
	/// <param name="y">The y postition.</param>
	/// <param name="z">The z postition.</param>
	/// <returns>Index into <see cref="_array"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Index(int x, int y, int z)
		=> x + (y * LengthX) + (z * _layerSize);

	/// <summary>
	/// Converts an index into a position.
	/// </summary>
	/// <param name="index">The index to convert.</param>
	/// <returns>Position interpretation of <paramref name="index"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int3 Index(int index)
	{
		int remaining = index % _layerSize;
		return new int3(remaining % LengthX, remaining / LengthX, index / _layerSize);
	}

	/// <summary>
	/// Gets the item at the specified index.
	/// </summary>
	/// <param name="x">X index of the item.</param>
	/// <param name="y">Y index of the item.</param>
	/// <param name="z">Z index of the item.</param>
	/// <returns>Item at the specified index.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Get(int x, int y, int z)
		=> _array[Index(x, y, z)];

	/// <summary>
	/// Sets the item at the specified index.
	/// </summary>
	/// <param name="x">X index of the item.</param>
	/// <param name="y">Y index of the item.</param>
	/// <param name="z">Z index of the item.</param>
	/// <param name="value">The value to set at the specified position.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Set(int x, int y, int z, T value)
		=> _array[Index(x, y, z)] = value;

	/// <summary>
	/// Changes the size of this array.
	/// </summary>
	/// <param name="newX">The new x size.</param>
	/// <param name="newY">The new y size.</param>
	/// <param name="newZ">The new z size.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="newX"/>, <paramref name="newY"/> or <paramref name="newZ"/> are negative.</exception>
	public void Resize(int newX, int newY, int newZ)
	{
		if (newX < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(newX));
		}
		else if (newY < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(newY));
		}
		else if (newZ < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(newZ));
		}
		else if (newX == 0 || newY == 0 || newZ == 0)
		{
			_array = [];

			LengthX = 0;
			LengthY = 0;
			LengthZ = 0;
			_layerSize = 0;

			return;
		}
		else if (newX == LengthX && newY == LengthY && newZ == LengthZ)
		{
			return; // same length
		}

		T[] newArray = new T[newX * newY * newZ];

		int newSize2 = newX * newY;

		int minX = Math.Min(LengthX, newX);

		for (int z = 0; z < newZ; z++)
		{
			for (int y = 0; y < newY; y++)
			{
				if (InBounds(0, y, z) && InBoundsNew(y, z))
				{
					System.Array.Copy(_array, Index(0, y, z), newArray, IndexNew(0, y, z), minX);
				}
			}
		}

		_array = newArray;

		LengthX = newX;
		LengthY = newY;
		LengthZ = newZ;
		_layerSize = LengthX * LengthY;

		int IndexNew(int x, int y, int z)
		{
			return x + (y * newX) + (z * newSize2);
		}

		bool InBoundsNew(int y, int z)
		{
			return y >= 0 && y < newY && z >= 0 && z < newZ;
		}
	}

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
