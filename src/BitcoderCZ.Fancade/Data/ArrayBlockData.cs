// <copyright file="ArrayBlockData.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Partial;
using BitcoderCZ.Maths.Vectors;
using BitcoderCZ.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static BitcoderCZ.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Data;

/// <summary>
/// Represents the blocks inside of a prefab.
/// </summary>
public class ArrayBlockData : IBlockData
{
    /// <summary>
    /// The underlying array.
    /// </summary>
    public readonly Array3D<ushort> Array;

    private const int BlockSize = 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayBlockData"/> class.
    /// </summary>
    public ArrayBlockData()
    {
        Array = new Array3D<ushort>(int3.One * BlockSize);
        Size = int3.Zero;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayBlockData"/> class.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    public ArrayBlockData(int3 capacity)
    {
        Array = new Array3D<ushort>(capacity);
        Size = int3.Zero;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayBlockData"/> class.
    /// </summary>
    /// <param name="blocks">The blocks to set <see cref="Array"/> to, doesn't clone.</param>
    public ArrayBlockData(Array3D<ushort> blocks)
    {
        ThrowIfNull(blocks, nameof(blocks));

        Array = blocks;

        Size = Array.Size;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayBlockData"/> class.
    /// </summary>
    /// <param name="data">The <see cref="ArrayBlockData"/> to copy.</param>
    public ArrayBlockData(ArrayBlockData data)
    {
        ThrowIfNull(data, nameof(data));

        Array = data.Array.Clone();
        Size = data.Size;
    }

    /// <inheritdoc/>
    public int3 BoundsMin => int3.Zero;

    /// <inheritdoc/>
    public int3 BoundsMax => Size;

    /// <summary>
    /// Gets the size of the data.
    /// </summary>
    /// <value>Size of the data.</value>
    public int3 Size { get; private set; }

    /// <summary>
    /// Gets the size of the underlying array.
    /// </summary>
    /// <value>Size of the underlying array.</value>
    public int3 Capacity => Array.Size;

    /// <summary>
    /// Determines if the specified position is inside the bounds of this data.
    /// </summary>
    /// <param name="pos">The position to check.</param>
    /// <returns><see langword="true"/> if <paramref name="pos"/> is inside the bounds of this data; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool InBounds(int3 pos)
        => pos.InBounds(Size.X, Size.Y, Size.Z);

    /// <summary>
    /// Determines if the specified position is inside the bounds of this data.
    /// </summary>
    /// <param name="x">The x positition.</param>
    /// <param name="y">The y positition.</param>
    /// <param name="z">The z positition.</param>
    /// <returns><see langword="true"/> if the position is inside the bounds of this data; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool InBounds(int x, int y, int z)
        => new int3(x, y, z).InBounds(Size.X, Size.Y, Size.Z);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IBlockData.IsInBounds(int3 position)
        => InBounds(position);

    /// <summary>
    /// Converts an index into a position.
    /// </summary>
    /// <param name="index">The index to convert.</param>
    /// <returns>Position interpretation of <paramref name="index"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int3 Index(int index)
        => Array.Index(index);

    /// <summary>
    /// Converts <paramref name="pos"/> into an index.
    /// </summary>
    /// <param name="pos">The position to convert.</param>
    /// <returns>Index into <see cref="Array"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index(int3 pos)
        => Array.Index(pos);

    /// <summary>
    /// Converts a position into an index.
    /// </summary>
    /// <param name="x">The x positition.</param>
    /// <param name="y">The y positition.</param>
    /// <param name="z">The z positition.</param>
    /// <returns>Index into <see cref="Array"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index(int x, int y, int z)
        => Array.Index(new int3(x, y, z));

    #region SetPrefab

    /// <inheritdoc/>
    public void SetPrefab(int3 position, Prefab prefab)
    {
        ThrowIfNull(prefab, nameof(prefab));

        CheckLowerBounds(position, nameof(position));

        int3 size = prefab.Size;

        if (size == int3.Zero)
        {
            return;
        }

        EnsureSize(position + size);

        foreach (var (segment, id) in prefab.EnumerateWithId())
        {
            SetBlockInternal(position + segment.PosInPrefab, id);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPrefab(int3 position, PartialPrefab prefab)
    {
        ThrowIfNull(prefab, nameof(prefab));

        CheckLowerBounds(position, nameof(position));

        int3 size = prefab.Size;

        if (size == int3.Zero)
        {
            return;
        }

        EnsureSize(position + size);

        foreach (var (segment, id) in prefab.EnumerateWithId())
        {
            SetBlockInternal(position + segment.PosInPrefab, id);
        }
    }
    #endregion

    #region SetBlock

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlock(int3 position, ushort id)
    {
        CheckLowerBounds(position);
        EnsureSize(position + int3.One);
        SetBlockInternal(position, id);
    }

    /// <inheritdoc/>
    public void SetBlockInBounds(int3 position, ushort id)
    {
        CheckBounds(position);
        SetBlockInternal(position, id);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlockUnchecked(int3 position, ushort id)
        => SetBlockInternal(position, id);
    #endregion

    #region GetBlock

    /// <summary>
    /// Gets the block at the specified position.
    /// </summary>
    /// <param name="position">Position of the block.</param>
    /// <returns>The block at the specified position.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetBlockInBounds(int3 position)
    {
        CheckBounds(position);

        return Array.GetUnchecked(position);
    }

    /// <summary>
    /// Gets the block at the specified position without bounds checking.
    /// </summary>
    /// <param name="position">Position of the block.</param>
    /// <returns>The block at the specified position.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetBlockUnchecked(int3 position)
        => Array.GetUnchecked(position);

    /// <summary>
    /// Gets the block at the specified position or <c>0</c>, if <paramref name="position"/> is out of bounds.
    /// </summary>
    /// <param name="position">Position of the block.</param>
    /// <returns>The block at the specified position.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetBlock(int3 position)
        => InBounds(position) ? GetBlockUnchecked(position) : (ushort)0;

    /// <summary>
    /// Gets the block at the specified position or <see langword="null"/>, if <paramref name="position"/> is out of bounds.
    /// </summary>
    /// <param name="position">Position of the block.</param>
    /// <returns>The block at the specified position.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort? GetBlockOrNull(int3 position)
        => InBounds(position) ? GetBlockUnchecked(position) : null;
    #endregion

    /// <inheritdoc/>
    public IEnumerable<KeyValuePair<int3, ushort>> EnumerateNonEmptyBlocks()
    {
        var array = Array;
        var arr = array.Array;
        for (var i = 0; i < arr.Length; i++)
        {
            ushort block = arr[i];
            if (block != 0)
            {
                yield return new(array.Index(i), block);
            }
        }
    }

    /// <inheritdoc/>
    public void EnumerateNonEmptyBlocks<TAction>(ref TAction action)
        where TAction : IRefValueAction<ushort, int3>
    {
        var array = Array;
        var arr = array.Array;
        var actionLocal = action;
        for (var i = 0; i < arr.Length; i++)
        {
            ref ushort block = ref arr[i];
            if (block != 0)
            {
                actionLocal.Invoke(ref block, array.Index(i));
            }
        }

        action = actionLocal;
    }

    /// <inheritdoc/>
    public void EnumerateNonEmptyBlocksWithBreak<TFunc>(ref TFunc function)
        where TFunc : IRefValueFunc<ushort, int3, bool>
    {
        var array = Array;
        var arr = array.Array;
        var functionLocal = function;
        for (var i = 0; i < arr.Length; i++)
        {
            ref ushort block = ref arr[i];
            if (block != 0)
            {
                if (functionLocal.Invoke(ref block, array.Index(i)))
                {
                    function = functionLocal;
                    return;
                }
            }
        }

        function = functionLocal;
    }

    /// <inheritdoc/>
    public Array3D<ushort> ToArray3D(bool clone)
        => clone ? Array.Clone() : Array;

    /// <summary>
    /// Trims the size to the smallest size possible.
    /// </summary>
    /// <param name="resize">
    /// If <see langword="true"/>, the underlying array should will be resized;
    /// if <see langword="false"/>, only <see cref="Size"/> will get changed.
    /// </param>
    public void Trim(bool resize = true)
    {
        if (Size == int3.Zero)
        {
            return;
        }

        int maxX = int.MaxValue;
        int maxY = int.MaxValue;
        int maxZ = int.MaxValue;

        var scanPos = Size - int3.One;

        while (true)
        {
            if (maxX == int.MaxValue)
            {
                for (int y = 0; y <= scanPos.Y; y++)
                {
                    for (int z = 0; z <= scanPos.Z; z++)
                    {
                        var pos = new int3(scanPos.X, y, z);
                        Debug.Assert(InBounds(pos), $"{nameof(pos)} should be in bounds.");

                        if (Array.GetUnchecked(pos) != 0)
                        {
                            maxX = scanPos.X;
                            goto endX;
                        }
                    }
                }
            }

        endX:
            if (maxY == int.MaxValue)
            {
                for (int x = 0; x <= scanPos.X; x++)
                {
                    for (int z = 0; z <= scanPos.Z; z++)
                    {
                        var pos = new int3(x, scanPos.Y, z);
                        Debug.Assert(InBounds(pos), $"{nameof(pos)} should be in bounds.");

                        if (Array.GetUnchecked(pos) != 0)
                        {
                            maxY = scanPos.Y;
                            goto endY;
                        }
                    }
                }
            }

        endY:
            if (maxZ == int.MaxValue)
            {
                for (int x = 0; x <= scanPos.X; x++)
                {
                    for (int y = 0; y <= scanPos.Y; y++)
                    {
                        var pos = new int3(x, y, scanPos.Z);
                        Debug.Assert(InBounds(pos), $"{nameof(pos)} should be in bounds.");

                        if (Array.GetUnchecked(pos) != 0)
                        {
                            maxZ = scanPos.Z;
                            goto endZ;
                        }
                    }
                }
            }

        endZ:
            if (maxX != int.MaxValue && maxY != int.MaxValue && maxZ != int.MaxValue)
            {
                if (resize)
                {
                    Resize(new int3(maxX, maxY, maxZ) + int3.One, false);
                }
                else
                {
                    Size = new int3(maxX, maxY, maxZ) + int3.One;
                }

                return;
            }
            else if (scanPos == int3.Zero)
            {
                // no blocks
                if (resize)
                {
                    Resize(int3.Zero, false);
                }
                else
                {
                    Size = int3.Zero;
                }

                return;
            }

            scanPos = int3.Max(scanPos - new int3(maxX == int.MaxValue ? 1 : 0, maxY == int.MaxValue ? 1 : 0, maxZ == int.MaxValue ? 1 : 0), int3.Zero);
        }
    }

    /// <summary>
    /// Shifts and resizes the <see cref="ArrayBlockData"/>, so that it is either empty, or there are blocks on the 0 position of each of the axis.
    /// </summary>
    /// <param name="resize">
    /// If <see langword="true"/>, the underlying array should will be resized;
    /// if <see langword="false"/>, blocks will be shifted and <see cref="Size"/> will get change, but the underlying array will not get resized.
    /// </param>
    /// <param name="trimY">
    /// If <see langword="true"/>, the y axis will be trimmed (unless there are no blocks, a block will be at (x,0,x));
    /// if <see langword="false"/>, the y axis will not be trimmed.
    /// </param>
    public void TrimNegative(bool resize = true, bool trimY = false)
    {
        if (Size == int3.Zero)
        {
            return;
        }

        int minX = int.MinValue;
        int minY = int.MinValue;
        int minZ = int.MinValue;

        var scanPos = int3.Zero;

        while (true)
        {
            if (minX == int.MinValue)
            {
                for (int y = scanPos.Y; y < Size.Y; y++)
                {
                    for (int z = scanPos.Z; z < Size.Z; z++)
                    {
                        var pos = new int3(scanPos.X, y, z);
                        Debug.Assert(InBounds(pos), $"{nameof(pos)} should be in bounds.");

                        if (Array.GetUnchecked(pos) != 0)
                        {
                            minX = scanPos.X;
                            goto endX;
                        }
                    }
                }
            }

        endX:
            if (minY == int.MinValue)
            {
                for (int x = scanPos.X; x < Size.X; x++)
                {
                    for (int z = scanPos.Z; z < Size.Z; z++)
                    {
                        var pos = new int3(x, scanPos.Y, z);
                        Debug.Assert(InBounds(pos), $"{nameof(pos)} should be in bounds.");

                        if (Array.GetUnchecked(pos) != 0)
                        {
                            minY = scanPos.Y;
                            goto endY;
                        }
                    }
                }
            }

        endY:
            if (minZ == int.MinValue)
            {
                for (int x = scanPos.X; x < Size.X; x++)
                {
                    for (int y = scanPos.Y; y < Size.Y; y++)
                    {
                        var pos = new int3(x, y, scanPos.Z);
                        Debug.Assert(InBounds(pos), $"{nameof(pos)} should be in bounds.");

                        if (Array.GetUnchecked(pos) != 0)
                        {
                            minZ = scanPos.Z;
                            goto endZ;
                        }
                    }
                }
            }

        endZ:
            if (minX != int.MinValue && minY != int.MinValue && minZ != int.MinValue)
            {
                var minPos = new int3(minX, trimY ? minY : 0, minZ);

                if (minPos == int3.Zero)
                {
                    return; // can't move
                }

                if (resize)
                {
                    Move(-minPos, minPos);
                    Size -= minPos;
                    Resize(Size, false);
                }
                else
                {
                    Move(-minPos, minPos);
                    Size -= minPos;
                }

                return;
            }
            else if (scanPos.X == Size.X - 1)
            {
                // no blocks
                if (resize)
                {
                    Resize(int3.Zero, false);
                }
                else
                {
                    Size = int3.Zero;
                }

                return;
            }

            scanPos = int3.Min(scanPos + new int3(minX == int.MinValue ? 1 : 0, minY == int.MinValue ? 1 : 0, minZ == int.MinValue ? 1 : 0), Size - int3.One);
        }
    }

    /// <inheritdoc/>
    public void Clear(bool resize = false)
    {
        if (resize)
        {
            Array.Resize(int3.Zero);
        }
        else
        {
            Array.Clear();
        }

        Size = int3.Zero;
    }

    /// <inheritdoc/>
    public void WriteRegion(int3 destinationPosition, IReadOnly3DArray<ushort> value, int3 sourcePosition, int3 size)
    {
        var max = destinationPosition + size;
        if (max.X > Size.X || max.Y > size.Y || max.Z > Size.Z)
        {
            ThrowArgumentOutOfRangeException(nameof(destinationPosition));
        }

        value.CopyTo(sourcePosition, Array, destinationPosition, size);
    }

    /// <inheritdoc/>
    public void ReadRegion(int3 sourcePosition, Array3D<ushort> destination, int3 destinationPosition, int3 size)
    {
        var max = sourcePosition + size;
        if (max.X > Size.X || max.Y > size.Y || max.Z > Size.Z)
        {
            ThrowArgumentOutOfRangeException(nameof(sourcePosition));
        }

        Array.CopyTo(sourcePosition, destination, destinationPosition, size);
    }

    /// <inheritdoc/>
    public void CopyRegionTo(int3 sourcePosition, IBlockData destination, int3 destinationPosition, int3 size)
    {
        var max = sourcePosition + size;
        if (max.X > Size.X || max.Y > size.Y || max.Z > Size.Z)
        {
            ThrowArgumentOutOfRangeException(nameof(sourcePosition));
        }

        destination.WriteRegion(destinationPosition, Array, sourcePosition, size);
    }

    /// <summary>
    /// Moves the contents by a specified offset while ensuring the array size is sufficient.
    /// </summary>
    /// <param name="offset"><see cref="int3"/> representing the movement offset along the X, Y, and Z axes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any component of <paramref name="offset"/> is negative.</exception>
    public void Move(int3 offset)
    {
        if (offset.X < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(offset.X));
        }
        else if (offset.Y < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(offset.Y));
        }
        else if (offset.Z < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(offset.Z));
        }

        if (offset == int3.Zero)
        {
            return;
        }

        int3 oldSize = Size;

        EnsureSize(Size + offset);

        ushort[] arr = Array.Array;

        for (int z = oldSize.Z - 1; z >= 0; z--)
        {
            for (int y = oldSize.Y - 1; y >= 0; y--)
            {
                System.Array.Copy(arr, Index(0, y, z), arr, Index(new int3(0, y, z) + offset), oldSize.X);
            }
        }

        if (offset.X > 0)
        {
            for (int z = oldSize.Z - 1; z >= 0; z--)
            {
                for (int y = oldSize.Y - 1; y >= 0; y--)
                {
                    System.Array.Clear(arr, Index(0, y, z), offset.X);
                }
            }
        }

        if (offset.Y > 0)
        {
            for (int z = 0; z < oldSize.Z; z++)
            {
                int newZ = z + offset.Z;
                for (int y = 0; y < offset.Y; y++)
                {
                    System.Array.Clear(arr, Index(0, y, newZ), Size.X);
                }
            }
        }

        for (int z = 0; z < offset.Z; z++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                System.Array.Clear(arr, Index(0, y, z), Size.X);
            }
        }
    }

    /// <summary>
    /// Moves a region of blocks by a specified offset while ensuring the array size is sufficient.
    /// </summary>
    /// <param name="offset"><see cref="int3"/> representing the movement offset along the X, Y, and Z axes.</param>
    /// <param name="min">The start pos of the region to move, the end pos is <see cref="Size"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any component of <paramref name="offset"/> is negative.</exception>
    public void Move(int3 offset, int3 min)
    {
        if (min.X >= Size.X || min.Y >= Size.Y || min.Z >= Size.Z)
        {
            ThrowArgumentOutOfRangeException(nameof(min));
        }

        int3 dest = min + offset;

        if (dest.X < 0 || dest.Y < 0 || dest.Z < 0)
        {
            ThrowArgumentOutOfRangeException();
        }

        if (offset == int3.Zero)
        {
            return;
        }

        int3 moveSize = Size - min;

        EnsureSize(Size + offset);

        ushort[] arr = Array.Array;

        bool moveYZ = offset.Y != 0 || offset.Z != 0;

        int startY = offset.Y > 0 ? moveSize.Y - 1 : 0;
        int endY = offset.Y > 0 ? -1 : moveSize.Y;
        int stepY = offset.Y > 0 ? -1 : 1;

        int startZ = offset.Z > 0 ? moveSize.Z - 1 : 0;
        int endZ = offset.Z > 0 ? -1 : moveSize.Z;
        int stepZ = offset.Z > 0 ? -1 : 1;

        for (int z = startZ; z != endZ; z += stepZ)
        {
            for (int y = startY; y != endY; y += stepY)
            {
                var pos = new int3(0, y, z);
                int index = Index(pos + min);
                System.Array.Copy(arr, index, arr, Index(pos + dest), moveSize.X);

                if (moveYZ)
                {
                    System.Array.Clear(arr, index, moveSize.X);
                }
                else
                {
                    if (offset.X > 0)
                    {
                        System.Array.Clear(arr, index, Math.Min(offset.X, moveSize.X));
                    }
                    else
                    {
                        System.Array.Clear(arr, index + Math.Max(moveSize.X + offset.X, 0), Math.Min(-offset.X, moveSize.X));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Moves a region of blocks by a specified offset while ensuring the array size is sufficient.
    /// </summary>
    /// <param name="offset"><see cref="int3"/> representing the movement offset along the X, Y, and Z axes.</param>
    /// <param name="min">The inclusive start pos of the region to move.</param>
    /// <param name="max">The inclusive end pos of the region to move.</param>
    public void Move(int3 offset, int3 min, int3 max)
    {
        if (min.X > max.X || min.Y > max.Y || min.Z > max.Z)
        {
            ThrowArgumentOutOfRangeException(nameof(min));
        }

        int3 dest = min + offset;

        if (dest.X < 0 || dest.Y < 0 || dest.Z < 0)
        {
            ThrowArgumentOutOfRangeException();
        }

        if (offset == int3.Zero)
        {
            return;
        }

        int3 moveRegionSize = max - min + int3.One;

        EnsureSize(dest + moveRegionSize);

        ushort[] arr = Array.Array;

        bool moveYZ = offset.Y != 0 || offset.Z != 0;

        int startY = offset.Y > 0 ? moveRegionSize.Y - 1 : 0;
        int endY = offset.Y > 0 ? -1 : moveRegionSize.Y;
        int stepY = offset.Y > 0 ? -1 : 1;

        int startZ = offset.Z > 0 ? moveRegionSize.Z - 1 : 0;
        int endZ = offset.Z > 0 ? -1 : moveRegionSize.Z;
        int stepZ = offset.Z > 0 ? -1 : 1;

        for (int z = startZ; z != endZ; z += stepZ)
        {
            for (int y = startY; y != endY; y += stepY)
            {
                var pos = new int3(0, y, z);
                int srcIndex = Index(pos + min);
                int destIndex = Index(pos + dest);

                System.Array.Copy(arr, srcIndex, arr, destIndex, moveRegionSize.X);

                if (moveYZ)
                {
                    System.Array.Clear(arr, srcIndex, moveRegionSize.X);
                }
                else
                {
                    if (offset.X > 0)
                    {
                        System.Array.Clear(arr, srcIndex, Math.Min(offset.X, moveRegionSize.X));
                    }
                    else
                    {
                        System.Array.Clear(arr, srcIndex + Math.Max(moveRegionSize.X + offset.X, 0), Math.Min(-offset.X, moveRegionSize.X));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Ensures that the underlying array is at least the size of <paramref name="size"/>.
    /// </summary>
    /// <param name="size">The minimum size.</param>
    public void EnsureSize(int3 size)
    {
        size = int3.Max(Size, size);

        if (size != Size)
        {
            // only resize if actually needed
            if (size.X > Capacity.X || size.Y > Capacity.Y || size.Z > Capacity.Z)
            {
                Resize(size);
            }
            else
            {
                Size = size;
            }
        }
    }

    /// <inheritdoc/>
    public void ReserveRegion(int3 min, int3 max)
    {
        if ((min.X | min.Y | min.Z) < 0)
        {
            ThrowArgumentOutOfRangeException($"{nameof(min)} must be non-negative.", nameof(min));
        }

        if ((max.X | max.Y | max.Z) < 0)
        {
            ThrowArgumentOutOfRangeException($"{nameof(max)} must be non-negative.", nameof(max));
        }

        if (min.X > max.X || min.Y > max.Y || min.Z > max.Z)
        {
            ThrowArgumentOutOfRangeException($"{nameof(min)} must be less than {nameof(max)}.", nameof(min));
        }

        var newCapacity = int3.Max(Capacity, max + int3.One);

        if (newCapacity == Capacity)
        {
            return;
        }

        newCapacity = CeilToMultiple(newCapacity, BlockSize);

        Array.Resize(newCapacity);
    }

    /// <inheritdoc/>
    public IBlockData Clone()
        => new ArrayBlockData(this);

    #region Utils
    private static int CeilToMultiple(int value, int blockSize)
    {
        int mod = value % blockSize;
        return Math.Max(mod == 0 ? value : value + (blockSize - mod), blockSize);
    }

    private static int3 CeilToMultiple(int3 val, int blockSize)
        => new int3(CeilToMultiple(val.X, blockSize), CeilToMultiple(val.Y, blockSize), CeilToMultiple(val.Z, blockSize));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckLowerBounds(int3 position, [CallerArgumentExpression(nameof(position))] string argumentName = "")
    {
        if (position.X < 0 || position.Y < 0 || position.Z < 0)
        {
            ThrowArgumentOutOfRangeException(argumentName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckBounds(int3 position, [CallerArgumentExpression(nameof(position))] string argumentName = "")
    {
        if (!InBounds(position))
        {
            ThrowArgumentOutOfRangeException(argumentName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckUpperBounds(int3 position, [CallerArgumentExpression(nameof(position))] string argumentName = "")
    {
        if (position.X >= Size.X || position.Y >= Size.Y || position.Z >= Size.Z)
        {
            ThrowArgumentOutOfRangeException(argumentName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetBlockInternal(int3 position, ushort id)
        => Array.SetUnchecked(position, id);

    private void Resize(int3 size, bool useBlock = true)
    {
        Size = size;

        if (useBlock)
        {
            size = CeilToMultiple(size, BlockSize);
        }

        Array.Resize(size);
    }
    #endregion
}
