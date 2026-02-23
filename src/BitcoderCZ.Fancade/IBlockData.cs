// <copyright file="IBlockData.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Partial;
using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade;

// ContinuousBlockData // backed by Array3D; for small levels, fast access (todo: implement allowing negative xz positons by storing int3 offset and adding it to position parameters)
// ChunkedBlockData, very large levels, slower access (dictionary)
// HybridBlockData // x lower chunks are continuous, other are in dictionary; large levels, fast access for lower x chunks, otherwise dictionary

/// <summary>
/// Represents a mutable 3D block data container.
/// </summary>
/// <remarks>
/// Negative XZ positions are allowed, but negative Y positions are not.
/// </remarks>
public interface IBlockData
{
    /// <summary>
    /// Gets the minimum inclusive bounds of the block data.
    /// </summary>
    /// <value>Minimum inclusive bounds of the block data.</value>
    int3 BoundsMin { get; }

    /// <summary>
    /// Gets the maximum inclusive bounds of the block data.
    /// </summary>
    /// <value>The maximum inclusive bounds of the block data.</value>
    int3 BoundsMax { get; }

    /// <summary>
    /// Gets the size of the block data.
    /// </summary>
    /// <remarks>
    /// Derived from <see cref="BoundsMin"/> and <see cref="BoundsMax"/>.
    /// </remarks>
    /// <value>Size of the block data.</value>
    virtual int3 Size => BoundsMax - BoundsMin + int3.One;

    /// <summary>
    /// Gets whether a position is within the bounds of the <see cref="IBlockData"/>.
    /// </summary>
    /// <param name="position">The position to test.</param>
    /// <returns><see langword="true"/> if <paramref name="position"/> is in bounds; otherwise, <see langword="false"/>.</returns>
    bool IsInBounds(int3 position);

    /// <summary>
    /// "Places" a prefab at the specified position.
    /// </summary>
    /// <remarks>
    /// Resized the underlying data storage if necessary.
    /// </remarks>
    /// <param name="position">The positition to place the prefab at.</param>
    /// <param name="prefab">The prefab to place.</param>
    void SetPrefab(int3 position, Prefab prefab);

    /// <summary>
    /// "Places" a partial prefab at the specified position.
    /// </summary>
    /// <remarks>
    /// Resized the underlying data storage if necessary.
    /// </remarks>
    /// <param name="position">The positition to place the prefab at.</param>
    /// <param name="prefab">The prefab to place.</param>
    void SetPrefab(int3 position, PartialPrefab prefab);

    /// <summary>
    /// "Places" a single block at the specified position.
    /// </summary>
    /// <remarks>
    /// Resized the underlying data storage if necessary.
    /// </remarks>
    /// <param name="position">The positition to place the block at.</param>
    /// <param name="id">Id of the block to place.</param>
    void SetBlock(int3 position, ushort id);

    /// <summary>
    /// "Places" a single block at the specified position.
    /// </summary>
    /// <remarks>
    /// The position must be within bounds.
    /// </remarks>
    /// <param name="position">The positition to place the block at.</param>
    /// <param name="id">Id of the block to place.</param>
    void SetBlockInBounds(int3 position, ushort id);

    /// <summary>
    /// "Places" a single block at the specified position without resizing the underlying data storage or bounds checking.
    /// </summary>
    /// <param name="position">The positition to place the block at.</param>
    /// <param name="id">Id of the block to place.</param>
    void SetBlockUnchecked(int3 position, ushort id);

    /// <summary>
    /// Writes the <see cref="IReadOnly3DArray{T}"/> to the <see cref="IBlockData"/>.
    /// </summary>
    /// <param name="destinationPosition">Position to write the <see cref="IReadOnly3DArray{T}"/> at.</param>
    /// <param name="value">The <see cref="IReadOnly3DArray{T}"/> to write.</param>
    /// <param name="sourcePosition">The starting position of the region to copy from <paramref name="value"/>.</param>
    /// <param name="size">The size of the region to copy, in elements, along each axis.</param>
    void WriteRegion(int3 destinationPosition, IReadOnly3DArray<ushort> value, int3 sourcePosition, int3 size);

    /// <summary>
    /// Writes the <see cref="IReadOnly3DArray{T}"/> to the <see cref="IBlockData"/>.
    /// </summary>
    /// <param name="destinationPosition">Position to write the <see cref="IReadOnly3DArray{T}"/> at.</param>
    /// <param name="value">The <see cref="IReadOnly3DArray{T}"/> to write.</param>
    virtual void WriteRegion(int3 destinationPosition, IReadOnly3DArray<ushort> value)
        => WriteRegion(destinationPosition, value, int3.Zero, value.Size);

    /// <summary>
    /// Gets the block at the specified position.
    /// </summary>
    /// <param name="position">Position of the block.</param>
    /// <returns>Id of the block at the specified position.</returns>
    ushort GetBlock(int3 position);

    /// <summary>
    /// Gets the block at the specified position.
    /// </summary>
    /// <remarks>
    /// The position must be within bounds.
    /// </remarks>
    /// <param name="position">Position of the block.</param>
    /// <returns>Id of the block at the specified position.</returns>
    ushort GetBlockInBounds(int3 position);

    /// <summary>
    /// Gets the block at the specified position without performing bounds checking.
    /// </summary>
    /// <param name="position">Position of the block.</param>
    /// <returns>Id of the block at the specified position.</returns>
    ushort GetBlockUnchecked(int3 position);

    // todo: span overload

    /// <summary>
    /// Writes a region of the <see cref="IBlockData"/> to the <see cref="Array3D{T}"/>.
    /// </summary>
    /// <param name="sourcePosition">Position to start reading at.</param>
    /// <param name="destination">The <see cref="Array3D{T}"/> to write to.</param>
    /// <param name="destinationPosition">Position to start writing at.</param>
    /// <param name="size">Size of the region to copy.</param>
    void ReadRegion(int3 sourcePosition, Array3D<ushort> destination, int3 destinationPosition, int3 size);

    /// <summary>
    /// Writes a region of the <see cref="IBlockData"/> to the <see cref="Array3D{T}"/>.
    /// </summary>
    /// <param name="destination">The <see cref="Array3D{T}"/> to write to.</param>
    /// <param name="size">Size of the region to copy.</param>
    virtual void ReadRegion(Array3D<ushort> destination, int3 size)
        => ReadRegion(BoundsMin, destination, int3.Zero, size);

    /// <summary>
    /// Copies a region of the <see cref="IBlockData"/> to another <see cref="IBlockData"/>.
    /// </summary>
    /// <param name="sourcePosition">Position to start reading at.</param>
    /// <param name="destination">The <see cref="IBlockData"/> to write to.</param>
    /// <param name="destinationPosition">Position to start writing at.</param>
    /// <param name="size">Size of the region to copy.</param>
    void CopyRegionTo(int3 sourcePosition, IBlockData destination, int3 destinationPosition, int3 size);

    /// <summary>
    /// Copies a region of the <see cref="IBlockData"/> to another <see cref="IBlockData"/>.
    /// </summary>
    /// <param name="destination">The <see cref="IBlockData"/> to write to.</param>
    /// <param name="destinationPosition">Position to start writing at.</param>
    /// <param name="size">Size of the region to copy.</param>
    virtual void CopyRegionTo(IBlockData destination, int3 destinationPosition, int3 size)
        => CopyRegionTo(BoundsMin, destination, destinationPosition, size);

    /// <summary>
    /// Enumerates all blocks, ignoring empty/air.
    /// </summary>
    /// <returns>An IEnumerable that returns all non empty blocks.</returns>
    IEnumerable<KeyValuePair<int3, ushort>> EnumerateNonEmptyBlocks();

    /// <summary>
    /// Enumerates all blocks, ignoring empty/air.
    /// </summary>
    /// <typeparam name="TAction">Type of the action.</typeparam>
    /// <param name="action">An action to execute at every non air positon.</param>
    void EnumerateNonEmptyBlocks<TAction>(TAction action)
        where TAction : IRefValueAction<ushort, int3>;

    /// <summary>
    /// Materializes the contents of this <see cref="IBlockData"/> as an <see cref="Array3D{T}"/>.
    /// </summary>
    /// <param name="clone">
    /// When <see langword="true"/>, the returned <see cref="Array3D{T}"/> always owns its own copy of the data.
    /// When <see langword="false"/> and the underlying storage is already an <see cref="Array3D{T}"/>,
    /// the implementation may return it directly without allocating or copying.
    /// </param>
    /// <returns>
    /// An <see cref="Array3D{T}"/> containing the blocks represented by this <see cref="IBlockData"/>.
    /// </returns>
    virtual Array3D<ushort> ToArray3D(bool clone)
    {
        var array = new Array3D<ushort>(Size);
        ReadRegion(array, array.Size);
        return array;
    }

    /// <summary>
    /// Trims the size to the smallest size possible.
    /// </summary>
    /// <param name="resize">
    /// If <see langword="true"/>, the underlying data storage should will be resized;
    /// if <see langword="false"/>, only <see cref="BoundsMax"/> will get changed.
    /// </param>
    void Trim(bool resize = true);

    /// <summary>
    /// Shifts and resizes the <see cref="IBlockData"/>, so that it is either empty, or there are blocks on the 0 position of each of the axis.
    /// </summary>
    /// <param name="resize">
    /// If <see langword="true"/>, the underlying data storage should will be resized;
    /// if <see langword="false"/>, blocks will be shifted and <see cref="Size"/> will get change, but the underlying data storage will not get resized.
    /// </param>
    /// <param name="trimY">
    /// If <see langword="true"/>, the y axis will be trimmed (unless there are no blocks, a block will be at (x,0,z));
    /// if <see langword="false"/>, the y axis will not be trimmed.
    /// </param>
    void TrimNegative(bool resize = true, bool trimY = false);

    /// <summary>
    /// Clears all block data, resetting the size to zero. 
    /// </summary>
    /// <param name="resize">
    /// If <see langword="true"/>, the underlying data storage will get resized to zero;
    /// if <see langword="false"/>, <see cref="BoundsMin"/> and <see cref="BoundsMax"/> will get changed and the underlying data storage will get cleared.
    /// </param>
    void Clear(bool resize = false);

    /// <summary>
    /// Moves all block data by the specified offset.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="BoundsMin"/> and <see cref="BoundsMax"/> as the source region.
    /// </remarks>
    /// <param name="offset">The offset to apply.</param>
    void Move(int3 offset);

    /// <summary>
    /// Moves all block data by the specified offset.
    /// </summary>
    /// <remarks>
    /// Uses <paramref name="min"/> and <see cref="BoundsMax"/> as the source region.
    /// </remarks>
    /// <param name="offset">The offset to apply.</param>
    /// <param name="min">The minimum bounds (inclusive) of the region to move.</param>
    void Move(int3 offset, int3 min);

    /// <summary>
    /// Moves a region of block data by the specified offset.
    /// </summary>
    /// <param name="offset">The offset to apply.</param>
    /// <param name="min">The minimum (inclusive) bounds of the region to move.</param>
    /// <param name="max">The maximum (inclusive) bounds of the region to move.</param>
    void Move(int3 offset, int3 min, int3 max);

    /// <summary>
    /// Reserves storage so that the specified region can be accessed safely.
    /// </summary>
    /// <remarks>
    /// <see cref="SetBlockInBounds"/>, <see cref="SetBlockUnchecked"/>, <see cref="GetBlockInBounds"/> and <see cref="GetBlockUnchecked"/> are safe to call within this region after this call (with respect to bounds checking).
    /// </remarks>
    /// <param name="min">The minimum bounds (inclusive) of the region.</param>
    /// <param name="max">The maximum bounds (inclusive) of the region.</param>
    void ReserveRegion(int3 min, int3 max);

    /// <summary>
    /// Creates a deep copy of this <see cref="IBlockData"/> instance.
    /// </summary>
    /// <returns>A cloned instance of the block data.</returns>
    IBlockData Clone();
}