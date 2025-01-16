// <copyright file="BlockData.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using MathUtils.Vectors;
using System;
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
		Array = new Array3D<ushort>(0, 0, 0);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BlockData"/> class.
	/// </summary>
	/// <param name="blocks">The blocks to set <see cref="Array"/> to, doesn't clone.</param>
	public BlockData(Array3D<ushort> blocks)
	{
		Array = blocks;

		Size = new int3(Array.LengthX, Array.LengthY, Array.LengthZ);
		Trim();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BlockData"/> class.
	/// </summary>
	/// <param name="data">The <see cref="BlockData"/> to copy.</param>
	public BlockData(BlockData data)
	{
		Array = data.Array.Clone();
		Size = data.Size;
	}

	/// <summary>
	/// Gets the size of the data.
	/// </summary>
	/// <value>Size of the data.</value>
	public int3 Size { get; private set; }

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
		=> Array.Index(x, y, z);

	#region SetGroup

	/// <summary>
	/// "Places" a prefab group at the specified postion.
	/// </summary>
	/// <param name="pos">The postition to place the group at.</param>
	/// <param name="group">The group to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGroup(int3 pos, PrefabGroup group)
		=> SetGroup(pos.X, pos.Y, pos.Z, group);

	/// <summary>
	/// "Places" a prefab group at the specified postion.
	/// </summary>
	/// <param name="x">The x postition to place the group at.</param>
	/// <param name="y">The y postition to place the group at.</param>
	/// <param name="z">The z postition to place the group at.</param>
	/// <param name="group">The group to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGroup(int x, int y, int z, PrefabGroup group)
	{
		ushort id = group.Id;
		byte3 size = group.Size;

		EnsureSize(x + (size.X - 1), y + (size.Y - 1), z + (size.Z - 1));

		for (byte zIndex = 0; zIndex < size.Z; zIndex++)
		{
			for (byte yIndex = 0; yIndex < size.Y; yIndex++)
			{
				for (byte xIndex = 0; xIndex < size.X; xIndex++)
				{
					byte3 pos = new byte3(xIndex, yIndex, zIndex);
					if (!group.ContainsKey(pos))
					{
						continue;
					}

					SetBlockInternal(x + xIndex, y + yIndex, z + zIndex, id);
					id++;
				}
			}
		}
	}

	/// <summary>
	/// "Places" a partial prefab group at the specified postion.
	/// </summary>
	/// <param name="pos">The postition to place the group at.</param>
	/// <param name="group">The group to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGroup(int3 pos, PartialPrefabGroup group)
		=> SetGroup(pos.X, pos.Y, pos.Z, group);

	/// <summary>
	/// "Places" a partial prefab group at the specified postion.
	/// </summary>
	/// <param name="x">The x postition to place the group at.</param>
	/// <param name="y">The y postition to place the group at.</param>
	/// <param name="z">The z postition to place the group at.</param>
	/// <param name="group">The group to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGroup(int x, int y, int z, PartialPrefabGroup group)
	{
		ushort id = group.Id;
		byte3 size = group.Size;

		EnsureSize(x + (size.X - 1), y + (size.Y - 1), z + (size.Z - 1));

		for (byte zIndex = 0; zIndex < size.Z; zIndex++)
		{
			for (byte yIndex = 0; yIndex < size.Y; yIndex++)
			{
				for (byte xIndex = 0; xIndex < size.X; xIndex++)
				{
					byte3 pos = new byte3(xIndex, yIndex, zIndex);
					if (!group.ContainsKey(pos))
					{
						continue;
					}

					SetBlockInternal(x + xIndex, y + yIndex, z + zIndex, id);
					id++;
				}
			}
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
		CheckBounds(pos);

		if (id != 0)
		{
			EnsureSize(pos); // not placing "air"
		}

		SetBlockInternal(pos.X, pos.Y, pos.Z, id);
	}

	/// <summary>
	/// "Places" a single block at the specified index.
	/// </summary>
	/// <param name="index">The index to place the group at.</param>
	/// <param name="id">Id of the block to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlock(int index, ushort id)
	{
		int3 pos = Index(index);
		SetBlock(pos.X, pos.Y, pos.Z, id);
	}

	/// <summary>
	/// "Places" a single block at the specified index.
	/// </summary>
	/// <param name="x">The x postition to place the block at.</param>
	/// <param name="y">The y postition to place the block at.</param>
	/// <param name="z">The z postition to place the block at.</param>
	/// <param name="id">Id of the block to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlock(int x, int y, int z, ushort id)
	{
		CheckBounds(new int3(x, y, z), "x, y, z");

		if (id != 0)
		{
			EnsureSize(x, y, z); // not placing "air"
		}

		SetBlockInternal(x, y, z, id);
	}

	/// <summary>
	/// "Places" a single block at the specified postion without resizing the underlying array or bounds checking.
	/// </summary>
	/// <param name="pos">The postition to place the group at.</param>
	/// <param name="id">Id of the block to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlockUnchecked(int3 pos, ushort id)
		=> SetBlockUnchecked(pos.X, pos.Y, pos.Z, id);

	/// <summary>
	/// "Places" a single block at the specified index without resizing the underlying array or bounds checking.
	/// </summary>
	/// <param name="index">The index to place the block at.</param>
	/// <param name="id">Id of the block to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlockUnchecked(int index, ushort id)
	{
		int3 pos = Index(index);
		SetBlockUnchecked(pos.X, pos.Y, pos.Z, id);
	}

	/// <summary>
	/// "Places" a single block at the specified index without resizing the underlying array or bounds checking.
	/// </summary>
	/// <param name="x">The x postition to place the block at.</param>
	/// <param name="y">The y postition to place the block at.</param>
	/// <param name="z">The z postition to place the block at.</param>
	/// <param name="id">Id of the block to place.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlockUnchecked(int x, int y, int z, ushort id)
		=> SetBlockInternal(x, y, z, id);
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
		CheckBounds(pos);

		return Array[pos.X, pos.Y, pos.Z];
	}

	/// <summary>
	/// Gets the block at the specified position.
	/// </summary>
	/// <param name="x">The x postition of the block.</param>
	/// <param name="y">The y postition of the block.</param>
	/// <param name="z">The z postition of the block.</param>
	/// <returns>The block at the specified position.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ushort GetBlock(int x, int y, int z)
	{
		CheckBounds(new int3(x, y, z), "x, y, z");

		return Array[x, y, z];
	}

	/// <summary>
	/// Gets the block at the specified position without bounds checking.
	/// </summary>
	/// <param name="pos">Position of the block.</param>
	/// <returns>The block at the specified position.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ushort GetBlockUnchecked(int3 pos)
	{
		CheckBounds(pos);

		return Array[pos.X, pos.Y, pos.Z];
	}

	/// <summary>
	/// Gets the block at the specified position without bounds checking.
	/// </summary>
	/// <param name="x">The x postition of the block.</param>
	/// <param name="y">The y postition of the block.</param>
	/// <param name="z">The z postition of the block.</param>
	/// <returns>The block at the specified position.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ushort GetBlockUnchecked(int x, int y, int z)
	{
		CheckBounds(new int3(x, y, z), "x, y, z");

		return Array[x, y, z];
	}
	#endregion

	/// <summary>
	/// Trims the size to the smallest size possible.
	/// </summary>
	public void Trim()
	{
		if (Size == int3.Zero)
		{
			return;
		}

		int maxX = int.MaxValue;
		int maxY = int.MaxValue;
		int maxZ = int.MaxValue;

		int3 scanPos = Size;

		while (true)
		{
			if (maxX == int.MaxValue)
			{
				for (int y = 0; y < scanPos.Y; y++)
				{
					for (int z = 0; z < scanPos.Z; z++)
					{
						if (Array[Index(scanPos.X - 1, y, z)] != 0)
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
				for (int x = 0; x < scanPos.X; x++)
				{
					for (int z = 0; z < scanPos.Z; z++)
					{
						if (Array[Index(x, scanPos.Y - 1, z)] != 0)
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
				for (int x = 0; x < scanPos.X; x++)
				{
					for (int y = 0; y < scanPos.Y; y++)
					{
						if (Array[Index(x, y, scanPos.Z - 1)] != 0)
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
				Resize(new int3(maxX + 1, maxY + 1, maxZ + 1), false);
				return;
			}
			else if (scanPos.X == 1 && scanPos.Y == 1 && scanPos.Z == 1)
			{
				// no blocks
				Resize(int3.Zero, false);
				return;
			}

			scanPos = new int3(Math.Max(1, scanPos.X - 1), Math.Max(1, scanPos.Y - 1), Math.Max(1, scanPos.Z - 1));
		}
	}

	/// <summary>
	/// Ensures that the underlying array is at least the size of <paramref name="size"/>.
	/// </summary>
	/// <param name="size">The minimum size.</param>
	public void EnsureSize(int3 size)
		=> EnsureSize(size.X, size.Y, size.Z);

	/// <summary>
	/// Ensures that the underlying array is at least the specified size.
	/// </summary>
	/// <param name="sizeX">The minimum x size.</param>
	/// <param name="sizeY">The minimum y size.</param>
	/// <param name="sizeZ">The minimum z size.</param>
	public void EnsureSize(int sizeX, int sizeY, int sizeZ)
	{
		int3 size = Size;
		if (sizeX >= size.X)
		{
			size.X = sizeX + 1;
		}

		if (sizeY >= size.Y)
		{
			size.Y = sizeY + 1;
		}

		if (sizeZ >= size.Z)
		{
			size.Z = sizeZ + 1;
		}

		if (size != Size)
		{
			// only resize if actually needed
			if (size.X > Array.LengthX || size.Y > Array.LengthY || size.Z > Array.LengthZ)
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CheckBounds(int3 pos, [CallerArgumentExpression(nameof(pos))] string? argumentName = null)
	{
		if (!InBounds(pos))
		{
			throw new ArgumentOutOfRangeException(argumentName);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetBlockInternal(int x, int y, int z, ushort id)
		=> Array[x, y, z] = id;

#pragma warning disable SA1204 // Static elements should appear before instance elements
	private static int CeilToMultiple(int numb, int blockSize)
#pragma warning restore SA1204
	{
		int mod = numb % blockSize;
		return Math.Max(mod == 0 ? numb : numb + (blockSize - mod), blockSize);
	}

	private void Resize(int3 size, bool useBlock = true)
	{
		if (useBlock)
		{
			Array.Resize(
				CeilToMultiple(size.X, BlockSize),
				CeilToMultiple(size.Y, BlockSize),
				CeilToMultiple(size.Z, BlockSize));
		}
		else
		{
			Array.Resize(size.X, size.Y, size.Z);
		}

		Size = size;
	}
	#endregion
}
