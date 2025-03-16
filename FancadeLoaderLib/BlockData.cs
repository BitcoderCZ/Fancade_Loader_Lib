// <copyright file="BlockData.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FancadeLoaderLib;

/// <summary>
/// Represents the blocks inside of a prefab.
/// </summary>
public class BlockData
{
	/// <summary>
	/// The underlying array.
	/// </summary>
	public readonly Array3D<ushort> Array;

	private const int BlockSize = 8;

	/// <summary>
	/// Initializes a new instance of the <see cref="BlockData"/> class.
	/// </summary>
	public BlockData()
	{
		Array = new Array3D<ushort>(int3.One * BlockSize);
		Size = int3.Zero;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BlockData"/> class.
	/// </summary>
	/// <param name="capacity">The initial capacity.</param>
	public BlockData(int3 capacity)
	{
		Array = new Array3D<ushort>(capacity);
		Size = int3.Zero;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BlockData"/> class.
	/// </summary>
	/// <param name="blocks">The blocks to set <see cref="Array"/> to, doesn't clone.</param>
	public BlockData(Array3D<ushort> blocks)
	{
		if (blocks is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(blocks));
		}

		Array = blocks;

		Size = Array.Size;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BlockData"/> class.
	/// </summary>
	/// <param name="data">The <see cref="BlockData"/> to copy.</param>
	public BlockData(BlockData data)
	{
		if (data is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(data));
		}

		Array = data.Array.Clone();
		Size = data.Size;
	}

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
	/// <param name="x">The x postition.</param>
	/// <param name="y">The y postition.</param>
	/// <param name="z">The z postition.</param>
	/// <returns><see langword="true"/> if the position is inside the bounds of this data; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool InBounds(int x, int y, int z)
		=> new int3(x, y, z).InBounds(Size.X, Size.Y, Size.Z);

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
	/// <param name="x">The x postition.</param>
	/// <param name="y">The y postition.</param>
	/// <param name="z">The z postition.</param>
	/// <returns>Index into <see cref="Array"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Index(int x, int y, int z)
		=> Array.Index(new int3(x, y, z));

	#region SetGroup

	/// <summary>
	/// "Places" a prefab group at the specified postion.
	/// </summary>
	/// <param name="pos">The postition to place the group at.</param>
	/// <param name="group">The group to place.</param>
	public void SetGroup(int3 pos, PrefabGroup group)
	{
		if (group is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(group));
		}

		CheckLowerBounds(pos, nameof(pos));

		byte3 size = group.Size;

		if (size == byte3.Zero)
		{
			return;
		}

		EnsureSize(pos + size);

		foreach (var (prefab, id) in group.EnumerateWithId())
		{
			SetBlockInternal(pos + prefab.PosInGroup, id);
		}
	}

	/// <summary>
	/// "Places" a partial prefab group at the specified postion.
	/// </summary>
	/// <param name="pos">The postition to place the group at.</param>
	/// <param name="group">The group to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGroup(int3 pos, PartialPrefabGroup group)
	{
		if (group is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(group));
		}

		CheckLowerBounds(pos, nameof(pos));

		byte3 size = group.Size;

		if (size == byte3.Zero)
		{
			return;
		}

		EnsureSize(pos + size);

		foreach (var (prefab, id) in group.EnumerateWithId())
		{
			SetBlockInternal(pos + prefab.PosInGroup, id);
		}
	}
	#endregion

	#region SetBlock

	/// <summary>
	/// "Places" a single block at the specified postion.
	/// </summary>
	/// <param name="pos">The postition to place the group at.</param>
	/// <param name="id">Id of the block to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlock(int3 pos, ushort id)
	{
		CheckLowerBounds(pos, nameof(pos));
		EnsureSize(pos + int3.One);
		SetBlockInternal(pos, id);
	}

	/// <summary>
	/// "Places" a single block at the specified postion without resizing the underlying array or bounds checking.
	/// </summary>
	/// <param name="pos">The postition to place the group at.</param>
	/// <param name="id">Id of the block to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlockUnchecked(int3 pos, ushort id)
		=> SetBlockInternal(pos, id);
	#endregion

	#region GetBlock

	/// <summary>
	/// Gets the block at the specified position.
	/// </summary>
	/// <param name="pos">Position of the block.</param>
	/// <returns>The block at the specified position.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ushort GetBlock(int3 pos)
	{
		CheckBounds(pos, nameof(pos));

		return Array.GetUnchecked(pos);
	}

	/// <summary>
	/// Gets the block at the specified position without bounds checking.
	/// </summary>
	/// <param name="pos">Position of the block.</param>
	/// <returns>The block at the specified position.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ushort GetBlockUnchecked(int3 pos)
		=> Array.GetUnchecked(pos);
	#endregion

	/// <summary>
	/// Trims the size to the smallest size possible.
	/// </summary>
	/// <param name="shrink">If the underlying array should also be resized.</param>
	public void Trim(bool shrink = true)
	{
		if (Size == int3.Zero)
		{
			return;
		}

		int maxX = int.MaxValue;
		int maxY = int.MaxValue;
		int maxZ = int.MaxValue;

		int3 scanPos = Size - int3.One;

		while (true)
		{
			if (maxX == int.MaxValue)
			{
				for (int y = 0; y <= scanPos.Y; y++)
				{
					for (int z = 0; z <= scanPos.Z; z++)
					{
						int3 pos = new int3(scanPos.X, y, z);
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
						int3 pos = new int3(x, scanPos.Y, z);
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
						int3 pos = new int3(x, y, scanPos.Z);
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
				if (shrink)
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
				if (shrink)
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

	public void TrimNegative(bool shrink = true, bool trimY = false)
	{
		if (Size == int3.Zero)
		{
			return;
		}

		int minX = int.MinValue;
		int minY = int.MinValue;
		int minZ = int.MinValue;

		int3 scanPos = int3.Zero;

		while (true)
		{
			if (minX == int.MinValue)
			{
				for (int y = scanPos.Y; y < Size.Y; y++)
				{
					for (int z = scanPos.Z; z < Size.Z; z++)
					{
						int3 pos = new int3(scanPos.X, y, z);
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
						int3 pos = new int3(x, scanPos.Y, z);
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
						int3 pos = new int3(x, y, scanPos.Z);
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
				int3 minPos = new int3(minX, trimY ? minY : 0, minZ);

				if (minPos == int3.Zero)
				{
					return; // can't move
				}

				if (shrink)
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
				if (shrink)
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

	/// <summary>
	/// Clears all block data, resetting the size to zero. 
	/// </summary>
	/// <param name="shrink">If the underlying array should also be resized to zero.</param>
	public void Clear(bool shrink = false)
	{
		if (shrink)
		{
			Array.Resize(int3.Zero);
		}
		else
		{
			Array.Clear();
		}

		Size = int3.Zero;
	}

	/// <summary>
	/// Moves the contents by a specified offset while ensuring the array size is sufficient.
	/// </summary>
	/// <param name="move"><see cref="int3"/> representing the movement offset along the X, Y, and Z axes.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when any component of <paramref name="move"/> is negative.</exception>
	public void Move(int3 move)
	{
		if (move.X < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(nameof(move.X));
		}
		else if (move.Y < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(nameof(move.Y));
		}
		else if (move.Z < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(nameof(move.Z));
		}

		if ((move.X | move.Y | move.Z) == 0)
		{
			return; // move by 0
		}

		int3 oldSize = Size;

		EnsureSize(Size + move);

		ushort[] arr = Array.Array;

		for (int z = oldSize.Z - 1; z >= 0; z--)
		{
			for (int y = oldSize.Y - 1; y >= 0; y--)
			{
				System.Array.Copy(arr, Index(0, y, z), arr, Index(new int3(0, y, z) + move), oldSize.X);
			}
		}

		if (move.X > 0)
		{
			for (int z = oldSize.Z - 1; z >= 0; z--)
			{
				for (int y = oldSize.Y - 1; y >= 0; y--)
				{
					System.Array.Clear(arr, Index(0, y, z), move.X);
				}
			}
		}

		if (move.Y > 0)
		{
			for (int z = 0; z < oldSize.Z; z++)
			{
				int newZ = z + move.Z;
				for (int y = 0; y < move.Y; y++)
				{
					System.Array.Clear(arr, Index(0, y, newZ), Size.X);
				}
			}
		}

		for (int z = 0; z < move.Z; z++)
		{
			for (int y = 0; y < Size.Y; y++)
			{
				System.Array.Clear(arr, Index(0, y, z), Size.X);
			}
		}
	}

	public void Move(int3 move, int3 startPos)
	{
		if (startPos.X >= Size.X || startPos.Y >= Size.Y || startPos.Z >= Size.Z)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(nameof(startPos));
		}

		int3 dest = startPos + move;

		if (dest.X < 0 || dest.Y < 0 || dest.Z < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}

		int3 moveSize = Size - startPos;

		EnsureSize(Size + move);

		ushort[] arr = Array.Array;

		bool moveYZ = move.Y != 0 || move.Z != 0;

		int startY = (move.Y > 0) ? moveSize.Y - 1 : 0;
		int endY = (move.Y > 0) ? -1 : moveSize.Y;
		int stepY = (move.Y > 0) ? -1 : 1;

		int startZ = (move.Z > 0) ? moveSize.Z - 1 : 0;
		int endZ = (move.Z > 0) ? -1 : moveSize.Z;
		int stepZ = (move.Z > 0) ? -1 : 1;

		for (int z = startZ; z != endZ; z += stepZ)
		{
			for (int y = startY; y != endY; y += stepY)
			{
				int3 pos = new int3(0, y, z);
				int index = Index(pos + startPos);
				System.Array.Copy(arr, index, arr, Index(pos + dest), moveSize.X);

				if (moveYZ)
				{
					System.Array.Clear(arr, index, moveSize.X);
				}
				else
				{
					if (move.X > 0)
					{
						System.Array.Clear(arr, index, Math.Min(move.X, moveSize.X));
					}
					else
					{
						System.Array.Clear(arr, index + Math.Max(moveSize.X + move.X, 0), Math.Min(-move.X, moveSize.X));
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

	/// <summary>
	/// Creates a copy of this <see cref="BlockData"/>.
	/// </summary>
	/// <returns>A copy of this <see cref="BlockData"/>.</returns>
	public BlockData Clone()
		=> new BlockData(this);

	#region Utils
	private static int CeilToMultiple(int numb, int blockSize)
	{
		int mod = numb % blockSize;
		return Math.Max(mod == 0 ? numb : numb + (blockSize - mod), blockSize);
	}

	private static int3 CeilToMultiple(int3 val, int blockSize)
		=> new int3(CeilToMultiple(val.X, blockSize), CeilToMultiple(val.Y, blockSize), CeilToMultiple(val.Z, blockSize));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void CheckLowerBounds(int3 pos, string argumentName)
	{
		if (pos.X < 0 || pos.Y < 0 || pos.Z < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(argumentName);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CheckBounds(int3 pos, string argumentName)
	{
		if (!InBounds(pos))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(argumentName);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CheckUpperBounds(int3 pos, string argumentName)
	{
		if (pos.X >= Size.X || pos.Y >= Size.Y || pos.Z >= Size.Z)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(argumentName);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetBlockInternal(int3 pos, ushort id)
		=> Array.SetUnchecked(pos, id);

	private void Resize(int3 size, bool useBlock = true)
	{
		if (useBlock)
		{
			Array.Resize(CeilToMultiple(size, BlockSize));
		}
		else
		{
			Array.Resize(size);
		}

		Size = size;
	}
	#endregion
}
