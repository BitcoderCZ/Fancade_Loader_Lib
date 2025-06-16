using FancadeLoaderLib.Common;
using FancadeLoaderLib.Editing;
using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Diagnostics;

using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Data;

public class FcDataWriter
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

    private readonly PrefabList _prefabs;
    private readonly Prefab _prefab;
    private readonly int3 _dataBlockPos;
    private readonly ushort[] _ids;

    public FcDataWriter(PrefabList prefabs, ushort targetPrefabId, int3 dataBlockPos, bool optimizedModel = true)
    {
        ThrowIfNull(prefabs);

        ThrowIfNegative(dataBlockPos.X);
        ThrowIfNegative(dataBlockPos.Y);
        ThrowIfNegative(dataBlockPos.Z);

        _prefabs = prefabs;
        _prefab = _prefabs.GetPrefab(targetPrefabId);
        _dataBlockPos = dataBlockPos;

        _ids = new ushort[MaxBlockValue];

        AddDataBlocks(_prefabs, _ids, optimizedModel);
    }

    public int DataBlockCount { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="dataPosInBlock"></param>
    /// <param name="prefabNameSuffix"></param>
    /// <returns>Id of the first data containing prefab or <see cref="ushort.MaxValue"/> if <paramref name="data"/> is empty.</returns>
    public ushort WriteData(ReadOnlySpan<byte> data, int3 dataPosInBlock, string prefabNameSuffix, out int writtenBlockCount)
    {
        if (data.IsEmpty)
        {
            writtenBlockCount = 0;
            return ushort.MaxValue;
        }

        int lenght6Bit = Maths.DivCeiling(data.Length * 8, 6);
        Span<byte> data6Bit = lenght6Bit < 1024 ? (stackalloc byte[1024])[..lenght6Bit] : new byte[lenght6Bit];
        BitUtils.Copy8To6Bit(data, data6Bit);

        var blockVoxels = BlockVoxelsGenerator.CreateScript(int2.One).First().Value;

        ushort? firstDataPrefabId = null;
        writtenBlockCount = 0;
        for (int i = 0; i < data6Bit.Length; i += MaxBlocksPerPrefab, DataBlockCount++, writtenBlockCount++)
        {
            Span<byte> blockData = data6Bit[i..Math.Min(i + MaxBlocksPerPrefab, data6Bit.Length)];

            Prefab dataPrefab = Prefab.CreateBlock(0, $"D{prefabNameSuffix}{DataBlockCount}");
            dataPrefab.Type = PrefabType.Script;
            dataPrefab.Collider = PrefabCollider.None;
            dataPrefab[int3.Zero].Voxels = blockVoxels;

            _prefabs.AddPrefab(dataPrefab);
            firstDataPrefabId ??= dataPrefab.Id;

            _prefab.Blocks.SetBlock(_dataBlockPos + new int3(DataBlockCount, 0, 0), dataPrefab.Id);

            WriteDataToPrefab(dataPrefab, blockData, dataPosInBlock, DataSize, _ids);
        }

        Debug.Assert(firstDataPrefabId is not null);

        return firstDataPrefabId.Value;
    }

    private static unsafe void AddDataBlocks(PrefabList prefabs, Span<ushort> ids, bool optimizedModel)
    {
        Debug.Assert(ids.Length >= MaxBlockValue);

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

    private static void WriteDataToPrefab(Prefab prefab, ReadOnlySpan<byte> data, int3 dataPos, int3 dataSize, ReadOnlySpan<ushort> ids)
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