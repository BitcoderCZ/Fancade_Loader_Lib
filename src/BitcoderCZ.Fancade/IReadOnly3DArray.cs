// <copyright file="IReadOnly3DArray.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade;

/// <summary>
/// Represents a read-only 3D array.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public interface IReadOnly3DArray<T> : IEnumerable<T>
{
    /// <summary>
    /// Gets the underlying data.
    /// </summary>
    /// <value>The underlying data.</value>
    ReadOnlySpan<T> Data { get; }

    /// <summary>
    /// Gets the size of this array.
    /// </summary>
    /// <value>The size of this array.</value>
    int3 Size { get; }

    /// <summary>
    /// Gets the total length of this array.
    /// </summary>
    /// <value>The total length of this array.</value>
    virtual int Length => Data.Length;

    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    /// <param name="index">Index of the item.</param>
    /// <returns>Item at the specified index.</returns>
    T this[int index] { get; }

    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    /// <param name="x">X index of the item.</param>
    /// <param name="y">Y index of the item.</param>
    /// <param name="z">Z index of the item.</param>
    /// <returns>Item at the specified index.</returns>
    T this[int x, int y, int z] { get; }

    /// <summary>
    /// Gets the item at the specified position.
    /// </summary>
    /// <param name="pos">Position of the item.</param>
    /// <returns>Item at the specified position.</returns>
    T this[int3 pos] { get; }

    /// <summary>
    /// Determines if the specified position is inside the bounds of this array.
    /// </summary>
    /// <param name="pos">The position to check.</param>
    /// <returns><see langword="true"/> if <paramref name="pos"/> is inside the bounds of this array; otherwise, <see langword="false"/>.</returns>
    bool InBounds(int3 pos);

    /// <summary>
    /// Converts <paramref name="pos"/> into an index.
    /// </summary>
    /// <param name="pos">The position to convert.</param>
    /// <returns>Index into <see cref="Data"/>.</returns>
    int Index(int3 pos);

    /// <summary>
    /// Converts an index into a position.
    /// </summary>
    /// <param name="index">The index to convert.</param>
    /// <returns>Position interpretation of <paramref name="index"/>.</returns>
    int3 Index(int index);

    /// <summary>
    /// Gets the item at the specified position.
    /// </summary>
    /// <param name="pos">Position of the item.</param>
    /// <returns>Item at the specified position.</returns>
    T Get(int3 pos);

    /// <summary>
    /// Gets the item at the specified position without bounds checking.
    /// </summary>
    /// <param name="pos">Position of the item.</param>
    /// <returns>Item at the specified position.</returns>
    T GetUnchecked(int3 pos);

    /// <summary>
    /// Copies a region from this <see cref="IReadOnly3DArray{T}"/> to the specified destination <see cref="Array3D{T}"/>, starting at the origin of both arrays.
    /// </summary>
    /// <param name="destination">The destination <see cref="Array3D{T}"/> to copy data into.</param>
    /// <param name="size">The size of the region to copy, in elements, along each axis.</param>
    /// <remarks>
    /// This method is equivalent to calling <see cref="CopyTo(int3, Array3D{T}, int3, int3)"/> with zero source and destination positions.
    /// </remarks>
    virtual void CopyTo(Array3D<T> destination, int3 size)
        => CopyTo(int3.Zero, destination, int3.Zero, size);

    /// <summary>
    /// Copies a region from this <see cref="IReadOnly3DArray{T}"/> to the specified destination <see cref="Array3D{T}"/>, starting at the origin of both arrays.
    /// </summary>
    /// <param name="sourcePosition">The starting position of the region to copy from this array.</param>
    /// <param name="destination">The destination <see cref="Array3D{T}"/> to copy data into.</param>
    /// <param name="destinationPosition">The starting position of the region to copy into the destination array.</param>
    /// <param name="size">The size of the region to copy, in elements, along each axis.</param>
    void CopyTo(int3 sourcePosition, Array3D<T> destination, int3 destinationPosition, int3 size);
}