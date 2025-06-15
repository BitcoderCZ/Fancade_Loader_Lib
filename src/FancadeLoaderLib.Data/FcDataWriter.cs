using FancadeLoaderLib.Common;
using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Data;

public static class FcDataWriter
{
    public const int MaxBlocksPerPrefab = 19683; // 3^9, should be power of 3
    private const int MaxBlockValue = 64;

    public static readonly int3 DataSize = CalculateDataSize(MaxBlocksPerPrefab);
    public static readonly int3 DataSizeWithBase = DataSize + int3.UnitY;

    private static readonly ImmutableArray<byte> DataBlockColors =
    [
        (byte)FcColor.Purple,
            (byte)FcColor.Pink,
            (byte)FcColor.Blue,
            (byte)FcColor.Green,
            (byte)FcColor.Yellow,
            (byte)FcColor.Orange,
            (byte)FcColor.Red,
            (byte)FcColor.LightBrown,
        ];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="prefabs"></param>
    /// <param name="level"></param>
    /// <param name="data"></param>
    /// <param name="dataPos"></param>
    /// <param name="dataPosInBlock"></param>
    /// <param name="ids">Ids of the data prefabs, initialized by <see cref="AddDataBlocks(PrefabList, Span{ushort}, bool)"/>.</param>
    /// <param name="prefabNameSuffix"></param>
    /// <param name="dataBlockCount"></param>
    public static void WriteData(PrefabList prefabs, Prefab level, Span<byte> data, int3 dataPos, int3 dataPosInBlock, Span<ushort> ids, string prefabNameSuffix, out int dataBlockCount)
    {
        Debug.Assert(dataPos.X >= 0 && dataPos.Y >= 0 && dataPos.Z >= 0);
        Debug.Assert(ids.Length >= MaxBlockValue);

        if (data.IsEmpty)
        {
            dataBlockCount = 0;
            return;
        }

        int lenght6Bit = Maths.DivCeiling(data.Length * 8, 6);
        Span<byte> data6Bit = lenght6Bit < 1024 ? stackalloc byte[lenght6Bit] : new byte[lenght6Bit];
        BitUtils.Copy8To6Bit(data, data6Bit);

        var blockVoxels = BlockVoxelsGenerator.CreateScript(int2.One).First().Value;

        dataBlockCount = 0;
        for (int i = 0; i < data6Bit.Length; i += MaxBlocksPerPrefab, dataBlockCount++)
        {
            Span<byte> blockData = data6Bit[i..Math.Min(i + MaxBlocksPerPrefab, data6Bit.Length)];

            Prefab dataBlock = Prefab.CreateBlock(0, $"D{prefabNameSuffix}{dataBlockCount}");
            dataBlock.Type = PrefabType.Script;
            dataBlock.Collider = PrefabCollider.None;
            dataBlock[int3.Zero].Voxels = blockVoxels;

            prefabs.AddPrefab(dataBlock);
            level.Blocks.SetBlock(dataPos + new int3(dataBlockCount, 0, 0), (ushort)(RawGame.CurrentNumbStockPrefabs + prefabs.SegmentCount - 1)); // place the data block at 0, 0, 0

            WriteDataToPrefab(dataBlock, blockData, dataPosInBlock, DataSize, ids);
        }
    }

    public static unsafe void AddDataBlocks(PrefabList prefabs, Span<ushort> ids, bool optimizedModel)
    {
        ThrowIfLessThan(ids.Length, MaxBlockValue);

        int index = 0;

        Span<byte> colors = stackalloc byte[3];

        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 8; z++)
            {
                if (index >= ids.Length)
                {
                    return;
                }

                colors[0] = DataBlockColors[x];
                colors[1] = DataBlockColors[0];
                colors[2] = DataBlockColors[z];

                int3 size = new int3(x == 0 ? 8 : x, 8, z == 0 ? 8 : z);

                string blockName = $"D{index}_{size.X}x{size.Y}x{size.Z}";

                var existingPrefab = prefabs.FirstOrDefault(prefab => prefab.Name == blockName);
                if (existingPrefab is not null)
                {
                    ids[index++] = existingPrefab.Id;
                    continue;
                }

                Prefab block = Prefab.CreateBlock(0, blockName);
                var voxels = block[int3.Zero].Voxels!;

                prefabs.AddPrefab(block);

                ids[index++] = block.Id;

                if (optimizedModel)
                {
                    if (size.X == 1)
                    {
                        voxels[0].Attribs[0] = true;
                    }
                    else
                    {
                        voxels[0].Colors[0] = colors[0];
                    }

                    voxels[0].Colors[2] = colors[1];
                    if (size.Z > 1)
                    {
                        voxels[0].Colors[4] = colors[2];
                    }

                    for (int i = 1; i < size.X; i++)
                    {
                        int voxelIndex = PrefabSegment.IndexVoxels(new(i, 0, 0));
                        voxels[voxelIndex].Colors[1] = colors[0];
                        if (i == size.X - 1)
                        {
                            voxels[voxelIndex].Attribs[0] = true;
                        }
                        else
                        {
                            voxels[voxelIndex].Colors[0] = colors[0];
                        }
                    }

                    for (int i = 1; i < size.Z; i++)
                    {
                        int voxelIndex = PrefabSegment.IndexVoxels(new(0, 0, i));
                        voxels[voxelIndex].Colors[5] = colors[2];
                        voxels[voxelIndex].Attribs[0] = true;
                        if (i == size.Z - 1)
                        {
                            voxels[voxelIndex].Attribs[4] = true;
                        }
                        else
                        {
                            voxels[voxelIndex].Colors[4] = colors[2];
                        }
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        int voxelIndex = PrefabSegment.IndexVoxels(new(0, i, 0));
                        if (i != 0)
                        {
                            voxels[voxelIndex].Attribs[0] = true;
                            voxels[voxelIndex].Colors[3] = colors[1];
                        }

                        if (i != 7)
                        {
                            voxels[voxelIndex].Colors[2] = colors[1];
                        }
                    }
                }
                else
                {
                    Voxel voxel = default;

                    voxel.Colors[0] = colors[0];
                    voxel.Colors[1] = colors[0];
                    voxel.Colors[2] = colors[1];
                    voxel.Colors[3] = colors[1];
                    voxel.Colors[4] = colors[2];
                    voxel.Colors[5] = colors[2];

                    for (int vz = 0; vz < size.Z; vz++)
                    {
                        for (int vy = 0; vy < size.Y; vy++)
                        {
                            for (int vx = 0; vx < size.X; vx++)
                            {
                                voxels[PrefabSegment.IndexVoxels(new(vx, vy, vz))] = voxel;
                            }
                        }
                    }
                }
            }
        }
    }

    private static void WriteDataToPrefab(Prefab prefab, Span<byte> data, int3 dataPos, int3 dataSize, Span<ushort> ids)
    {
        prefab.Blocks.EnsureSize(dataPos + dataSize + int3.UnitY);

        // base platform
        for (int z = 0; z < dataSize.Z; z++)
        {
            for (int x = 0; x < dataSize.X; x++)
            {
                prefab.Blocks.SetBlockUnchecked(dataPos + new int3(x, 0, z), 1);
            }
        }

        int index = 0;
        for (int y = 0; y < dataSize.Y; y++)
        {
            for (int z = 0; z < dataSize.Z; z++)
            {
                for (int x = 0; x < dataSize.X; x++)
                {
                    prefab.Blocks.SetBlockUnchecked(dataPos + new int3(x, y + 1, z), ids[data[index++]]);

                    if (index >= data.Length)
                    {
                        return;
                    }
                }
            }
        }
    }

    private static int3 CalculateDataSize(int length, int? forceX = null, int? forceY = null, int? forceZ = null)
    {
        if (!forceX.HasValue && !forceY.HasValue && !forceZ.HasValue)
        {
            int sqrt = (int)MathF.Ceiling(MathF.Pow(length, 1f / 3f));
            // Adjust for floating point imprecision
            int sqrtMinusOne = sqrt - 1;
            if (sqrtMinusOne * sqrtMinusOne * sqrtMinusOne >= length)
            {
                sqrt = sqrtMinusOne;
            }
        }
        else if (forceX.HasValue && forceY.HasValue && forceZ.HasValue)
        {
            return new int3(forceX.Value, forceY.Value, forceZ.Value);
        }

        int x = forceX ?? 1;
        int y = forceY ?? 1;
        int z = forceZ ?? 1;

        while (x * y * z < length)
        {
            if (!forceX.HasValue && x <= y && x <= z)
            {
                x++;
            }
            else if (!forceY.HasValue && y <= z)
            {
                y++;
            }
            else if (!forceZ.HasValue)
            {
                z++;
            }
        }

        return new int3(x, y, z);
    }
}