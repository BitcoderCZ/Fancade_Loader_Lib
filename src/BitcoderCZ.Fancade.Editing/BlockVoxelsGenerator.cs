// <copyright file="BlockVoxelsGenerator.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing;

/// <summary>
/// A helper for generating the <see cref="Voxel"/>s for prefabs.
/// </summary>
public sealed class BlockVoxelsGenerator
{
    private readonly Dictionary<int3, Voxels> _blocks = [];

    private BlockVoxelsGenerator()
    {
    }

    private delegate void LoopDelegate(Voxels.Ref voxel);

    /// <summary>
    /// Generates the voxels for a script block.
    /// </summary>
    /// <remarks>
    /// Size in voxels is (<paramref name="sizeInBlocks"/>.x * 8 - 1, 3, <paramref name="sizeInBlocks"/>.y * 8 - 1).
    /// </remarks>
    /// <param name="sizeInBlocks">The X and Z size of the prefab in blocks.</param>
    /// <returns>The generated voxels.</returns>
#if NET8_0_OR_GREATER
    public static IEnumerable<KeyValuePair<int3, Voxels>> CreateScript(int2 sizeInBlocks)
#else
    public static unsafe IEnumerable<KeyValuePair<int3, Voxels>> CreateScript(int2 sizeInBlocks)
#endif
    {
        if (sizeInBlocks.X < 1 || sizeInBlocks.Y < 1)
        {
            ThrowArgumentOutOfRangeException(nameof(sizeInBlocks));
        }

        int3 sizeInVoxels = new int3((sizeInBlocks.X * 8) - 1, 3, (sizeInBlocks.Y * 8) - 1);

        BlockVoxelsGenerator generator = new BlockVoxelsGenerator();

        generator.Fill(int3.Zero, sizeInVoxels, FcColor.Black);

        generator.Loop(new int3(1, 2, 1), new int3(sizeInVoxels.X - 1, 3, sizeInVoxels.Z - 1), (Voxels.Ref voxel) =>
        {
            voxel.SetColor(2, FcColor.Gray4);
        });

        generator.GetVoxelRef(new int3(sizeInVoxels.X - 1, 2, 0)).SetColor(2, FcColor.Gray4);
        generator.GetVoxelRef(new int3(0, 2, sizeInVoxels.Z - 1)).SetColor(2, FcColor.Gray4);

        generator.Loop(new int3(1, 2, sizeInVoxels.Z - 1), new int3(sizeInVoxels.X, 3, sizeInVoxels.Z), (Voxels.Ref voxel) =>
        {
            voxel.SetColor(2, FcColor.Gray3);
        });

        generator.Loop(new int3(sizeInVoxels.X - 1, 2, 1), new int3(sizeInVoxels.X, 3, sizeInVoxels.Z), (Voxels.Ref voxel) =>
        {
            voxel.SetColor(2, FcColor.Gray3);
        });

        return generator._blocks;
    }

    private Voxels GetBlock(int3 pos)
    {
        if (!_blocks.TryGetValue(pos, out Voxels voxels))
        {
            voxels = new Voxels();
            _blocks.Add(pos, voxels);
        }

        return voxels;
    }

    private Voxels.Ref GetVoxelRef(int3 pos)
    {
        int3 blockPos = pos / 8;
        int3 voxelBlockPos = blockPos * 8;
        int3 inBlockPos = pos - voxelBlockPos;

        return GetBlock(blockPos).GetVoxelRef(inBlockPos);
    }

    private unsafe void Fill(int3 from, int3 to, FcColor color)
    {
        Voxel voxel = default;
        byte colByte = (byte)color;
        voxel.Colors[0] = colByte;
        voxel.Colors[1] = colByte;
        voxel.Colors[2] = colByte;
        voxel.Colors[3] = colByte;
        voxel.Colors[4] = colByte;
        voxel.Colors[5] = colByte;

        Fill(from, to, voxel);
    }

    private void Fill(int3 from, int3 to, Voxel voxel)
    {
        if (from.X > to.X)
        {
            int temp = from.X;
            from.X = to.X;
            to.X = temp;
        }

        if (from.Y > to.Y)
        {
            int temp = from.Y;
            from.Y = to.Y;
            to.Y = temp;
        }

        if (from.X > to.X)
        {
            int temp = from.Z;
            from.Z = to.Z;
            to.Z = temp;
        }

        int3 fromBlock = from / 8;
        int3 toBlock = to / 8;

        for (int bz = fromBlock.Z; bz <= toBlock.Z; bz++)
        {
            for (int by = fromBlock.Y; by <= toBlock.Y; by++)
            {
                for (int bx = 0; bx <= toBlock.X; bx++)
                {
                    int3 blockPos = new int3(bx, by, bz);
                    int3 voxelPos = blockPos * 8;
                    Voxels block = GetBlock(blockPos);

                    int3 min = int3.Max(int3.Zero, from - voxelPos);
                    int3 max = int3.Min(new int3(8, 8, 8), to - voxelPos);

                    for (int z = min.Z; z < max.Z; z++)
                    {
                        for (int y = min.Y; y < max.Y; y++)
                        {
                            for (int x = min.X; x < max.X; x++)
                            {
                                block[new int3(x, y, z)] = voxel;
                            }
                        }
                    }
                }
            }
        }
    }

    private void Loop(int3 from, int3 to, LoopDelegate action)
    {
        if (from.X > to.X)
        {
            int temp = from.X;
            from.X = to.X;
            to.X = temp;
        }

        if (from.Y > to.Y)
        {
            int temp = from.Y;
            from.Y = to.Y;
            to.Y = temp;
        }

        if (from.X > to.X)
        {
            int temp = from.Z;
            from.Z = to.Z;
            to.Z = temp;
        }

        int3 fromBlock = from / 8;
        int3 toBlock = to / 8;

        for (int bz = fromBlock.Z; bz <= toBlock.Z; bz++)
        {
            for (int by = fromBlock.Y; by <= toBlock.Y; by++)
            {
                for (int bx = 0; bx <= toBlock.X; bx++)
                {
                    int3 blockPos = new int3(bx, by, bz);
                    int3 voxelPos = blockPos * 8;
                    Voxels block = GetBlock(blockPos);

                    int3 min = int3.Max(int3.Zero, from - voxelPos);
                    int3 max = int3.Min(new int3(8, 8, 8), to - voxelPos);

                    for (int z = min.Z; z < max.Z; z++)
                    {
                        for (int y = min.Y; y < max.Y; y++)
                        {
                            for (int x = min.X; x < max.X; x++)
                            {
                                action(block.GetVoxelRef(new(x, y, z)));
                            }
                        }
                    }
                }
            }
        }
    }
}