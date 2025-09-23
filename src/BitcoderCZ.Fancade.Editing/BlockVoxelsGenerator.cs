// <copyright file="BlockVoxelsGenerator.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using System.Runtime.CompilerServices;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing;

/// <summary>
/// A helper for generating the <see cref="Voxel"/>s for prefabs.
/// </summary>
public sealed class BlockVoxelsGenerator
{
    private readonly Dictionary<int3, Voxel[]> _blocks = [];

    private BlockVoxelsGenerator()
    {
    }

    private delegate void LoopDelegate(ref Voxel voxel);

    /// <summary>
    /// Generates the voxels for a script block.
    /// </summary>
    /// <remarks>
    /// Size in voxels is (<paramref name="sizeInBlocks"/>.x * 8 - 1, 3, <paramref name="sizeInBlocks"/>.y * 8 - 1).
    /// </remarks>
    /// <param name="sizeInBlocks">The X and Z size of the prefab in blocks.</param>
    /// <returns>The generated voxels.</returns>
    public static IEnumerable<KeyValuePair<int3, Voxel[]>> CreateScript(int2 sizeInBlocks)
        => CreateScript(sizeInBlocks, ScriptColorStyle.DefaultExecute);

    /// <summary>
    /// Generates the voxels for a script block.
    /// </summary>
    /// <remarks>
    /// Size in voxels is (<paramref name="sizeInBlocks"/>.x * 8 - 1, 3, <paramref name="sizeInBlocks"/>.y * 8 - 1).
    /// </remarks>
    /// <param name="sizeInBlocks">The X and Z size of the prefab in blocks.</param>
    /// <param name="colors">Colors of the script block.</param>
    /// <returns>The generated voxels.</returns>
    public static IEnumerable<KeyValuePair<int3, Voxel[]>> CreateScript(int2 sizeInBlocks, ScriptColorStyle colors)
    {
        if (sizeInBlocks.X < 1 || sizeInBlocks.Y < 1)
        {
            ThrowArgumentOutOfRangeException(nameof(sizeInBlocks));
        }

        int3 sizeInVoxels = new int3((sizeInBlocks.X * 8) - 1, 3, (sizeInBlocks.Y * 8) - 1);

        byte middleColor = (byte)colors.MiddleColor;
        byte borderColor = (byte)colors.BorderColor;

        BlockVoxelsGenerator generator = new BlockVoxelsGenerator();

        generator.Fill(int3.Zero, sizeInVoxels, colors.MainColor);

        unsafe
        {
            generator.Loop(new int3(1, 2, 1), new int3(sizeInVoxels.X - 1, 3, sizeInVoxels.Z - 1), (ref Voxel voxel) =>
            {
                voxel.Colors[2] = middleColor;
            });

            generator.GetVoxel(new int3(sizeInVoxels.X - 1, 2, 0)).Colors[2] = middleColor;
            generator.GetVoxel(new int3(0, 2, sizeInVoxels.Z - 1)).Colors[2] = middleColor;

            generator.Loop(new int3(1, 2, sizeInVoxels.Z - 1), new int3(sizeInVoxels.X, 3, sizeInVoxels.Z), (ref Voxel voxel) =>
            {
                voxel.Colors[2] = borderColor;
            });

            generator.Loop(new int3(sizeInVoxels.X - 1, 2, 1), new int3(sizeInVoxels.X, 3, sizeInVoxels.Z), (ref Voxel voxel) =>
            {
                voxel.Colors[2] = borderColor;
            });
        }

        return generator._blocks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Index(int3 pos)
        => Index(pos.X, pos.Y, pos.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Index(int x, int y, int z)
        => x + (y * 8) + (z * 8 * 8);

    private Voxel[] GetBlock(int3 pos)
    {
        if (!_blocks.TryGetValue(pos, out Voxel[]? voxels))
        {
            voxels = new Voxel[PrefabSegment.NumbVoxels];
            _blocks.Add(pos, voxels);
        }

        return voxels;
    }

    private ref Voxel GetVoxel(int3 pos)
    {
        int3 blockPos = pos / 8;
        int3 voxelBlockPos = blockPos * 8;
        int3 inBlockPos = pos - voxelBlockPos;

        return ref GetBlock(blockPos)[Index(inBlockPos)];
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
                    Voxel[] block = GetBlock(blockPos);

                    int3 min = int3.Max(int3.Zero, from - voxelPos);
                    int3 max = int3.Min(new int3(8, 8, 8), to - voxelPos);

                    for (int z = min.Z; z < max.Z; z++)
                    {
                        for (int y = min.Y; y < max.Y; y++)
                        {
                            for (int x = min.X; x < max.X; x++)
                            {
                                block[Index(x, y, z)] = voxel;
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
                    Voxel[] block = GetBlock(blockPos);

                    int3 min = int3.Max(int3.Zero, from - voxelPos);
                    int3 max = int3.Min(new int3(8, 8, 8), to - voxelPos);

                    for (int z = min.Z; z < max.Z; z++)
                    {
                        for (int y = min.Y; y < max.Y; y++)
                        {
                            for (int x = min.X; x < max.X; x++)
                            {
                                action(ref block[Index(x, y, z)]);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Describes the colors of a standard script block.
    /// </summary>
    public readonly struct ScriptColorStyle
    {
        /// <summary>
        /// The style used by default scripts with before/after wires.
        /// </summary>
        public static readonly ScriptColorStyle DefaultExecute = new ScriptColorStyle(FcColor.Black, FcColor.Gray4, FcColor.Gray3);

        /// <summary>
        /// The style used by default scripts without before/after wires.
        /// </summary>
        public static readonly ScriptColorStyle Default = new ScriptColorStyle(FcColor.Gray4, FcColor.Gray3, FcColor.Gray2);

        /// <summary>
        /// The style used by control scripts.
        /// </summary>
        public static readonly ScriptColorStyle Control = new ScriptColorStyle(FcColor.DarkYellow, FcColor.Yellow, FcColor.LightYellow);

        /// <summary>
        /// The style used by number scripts.
        /// </summary>
        public static readonly ScriptColorStyle Number = new ScriptColorStyle(FcColor.DarkBlue, FcColor.Blue, FcColor.LightBlue);

        /// <summary>
        /// The style used by vector scripts.
        /// </summary>
        public static readonly ScriptColorStyle Vector = new ScriptColorStyle(FcColor.DarkGreen, FcColor.Green, FcColor.LightGreen);

        /// <summary>
        /// The style used by rotation scripts.
        /// </summary>
        public static readonly ScriptColorStyle Rotation = new ScriptColorStyle(FcColor.DarkOrange, FcColor.Orange, FcColor.LightOrange);

        /// <summary>
        /// The style used by truth scripts.
        /// </summary>
        public static readonly ScriptColorStyle Truth = new ScriptColorStyle(FcColor.DarkRed, FcColor.Red, FcColor.LightRed);

        /// <summary>
        /// The style used by object scripts.
        /// </summary>
        public static readonly ScriptColorStyle Object = new ScriptColorStyle(FcColor.DarkPink, FcColor.Pink, FcColor.LightPink);

        /// <summary>
        /// The style used by constraint scripts.
        /// </summary>
        public static readonly ScriptColorStyle Constraint = new ScriptColorStyle(FcColor.Gray2, FcColor.Gray1, FcColor.White);

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptColorStyle"/> struct.
        /// </summary>
        /// <param name="mainColor">Main color of the prefab.</param>
        /// <param name="middleColor">Color in the middle of the prefab.</param>
        /// <param name="borderColor">Color of the top and right borders.</param>
        public ScriptColorStyle(FcColor mainColor, FcColor middleColor, FcColor borderColor)
        {
            MainColor = mainColor;
            MiddleColor = middleColor;
            BorderColor = borderColor;
        }

        /// <summary>
        /// Gets the main color of the prefab.
        /// </summary>
        /// <value>Main color of the prefab.</value>
        public readonly FcColor MainColor { get; }

        /// <summary>
        /// Gets the color in the middle of the prefab.
        /// </summary>
        /// <value>Color in the middle of the prefab.</value>
        public readonly FcColor MiddleColor { get; }

        /// <summary>
        /// Gets the color of the top and right borders.
        /// </summary>
        /// <value>Color of the top and right borders.</value>
        public readonly FcColor BorderColor { get; }
    }
}
