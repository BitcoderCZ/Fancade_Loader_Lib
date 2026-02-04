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
// TODO: rename unsafe to unchecked?
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
    void SetBlockUnsafe(int3 position, ushort id);

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
    ushort GetBlockUnsafe(int3 position);

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
    /// <see cref="SetBlockInBounds"/>, <see cref="SetBlockUnsafe"/>, <see cref="GetBlockInBounds"/> and <see cref="GetBlockUnsafe"/> are safe to call within this region after this call (with respect to bounds checking).
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