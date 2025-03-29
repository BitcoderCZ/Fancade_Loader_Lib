using FancadeLoaderLib.Exceptions;
using FancadeLoaderLib.Tests.Common;
using MathUtils.Vectors;
using System.Diagnostics;
using TUnit.Assertions.AssertConditions.Throws;

namespace FancadeLoaderLib.Editing.Tests;

public class PrefabUtilsTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Fill_PosOutOfBounds_DoesNothing(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);
        prefabList.AddPrefab(prefab);

        var prefabClone = prefab.Clone(true);

        var voxel = new Voxel(FcColor.Blue, false);

        prefab.Fill(new int3(-10, -10, -10), new int3(-1, -1, -1), voxel, true, true, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab.Id) : null);

        await Assert.That(prefab).IsEqualTo(prefabClone, new PrefabComparer());
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Fill_FillVoxels(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);
        prefabList.AddPrefab(prefab);

        var voxel = new Voxel(FcColor.Blue, false);

        prefab.Fill(new int3(1, 1, 1), new int3(6, 6, 6), voxel, true, false, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab.Id) : null);

        using (Assert.Multiple())
        {
            await AssertVoxelRegion(prefab, new int3(1, 1, 1), new int3(6, 6, 6), voxel);
            await AssertVoxelRegionInverted(prefab, new int3(1, 1, 1), new int3(6, 6, 6), default);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Fill_FillsMultipleSegments(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1, initVoxels: false);
        prefabList.AddPrefab(prefab);

        var voxel = new Voxel(FcColor.Blue, false);

        prefab.Fill(new int3(1, 1, 1), new int3(14, 14, 14), voxel, true, false, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab.Id) : null);

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Size).IsEqualTo(new int3(2, 2, 2));

            for (int z = 0; z < 2; z++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        await Assert.That(prefab.ContainsKey(new int3(x, y, z))).IsTrue();
                    }
                }
            }
        }

        using (Assert.Multiple())
        {
            await AssertVoxelRegion(prefab, new int3(1, 1, 1), new int3(14, 14, 14), voxel);
            await AssertVoxelRegionInverted(prefab, new int3(1, 1, 1), new int3(14, 14, 14), default);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Fill_OverwriteVoxelsTrue_OverwritesVoxels(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);
        prefabList.AddPrefab(prefab);

        var voxels = prefab[int3.Zero].Voxels!;
        voxels[1] = new Voxel(FcColor.Black, true);

        var voxel = new Voxel(FcColor.Blue, false);

        prefab.Fill(new int3(0, 0, 0), new int3(1, 0, 0), voxel, true, false, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab.Id) : null);

        using (Assert.Multiple())
        {
            await AssertVoxelRegion(prefab, new int3(0, 0, 0), new int3(1, 0, 0), voxel);
            await AssertVoxelRegionInverted(prefab, new int3(0, 0, 0), new int3(1, 0, 0), default);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Fill_OverwriteVoxelsFalse_DoesNotOverwriteVoxels(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);
        prefabList.AddPrefab(prefab);

        var voxels = prefab[int3.Zero].Voxels!;

        var voxel1 = new Voxel(FcColor.Black, true);
        var voxel2 = new Voxel(FcColor.Blue, false);

        voxels[1] = voxel1;

        prefab.Fill(new int3(0, 0, 0), new int3(1, 0, 0), voxel2, false, false, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab.Id) : null);

        using (Assert.Multiple())
        {
            await Assert.That(voxels[0]).IsEqualTo(voxel2);
            await Assert.That(voxels[1]).IsEqualTo(voxel1);
            await AssertVoxelRegionInverted(prefab, new int3(0, 0, 0), new int3(1, 0, 0), default);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Fill_OverwriteBlocksFalse_Throws(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 1);
        var prefab2 = CreatePrefab(2, 1);

        var blocks = prefab1.Blocks;

        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(1, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var voxel = new Voxel(FcColor.Black, true);

        await Assert.That(() => prefab1.Fill(new int3(7, 0, 0), new int3(8, 0, 0), voxel, true, false, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab1.Id) : null)).Throws<BlockObstructedException>();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Fill_OverwriteBlocksTrue_DoesNotThrow(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 1);
        var prefab2 = CreatePrefab(2, 1);

        var blocks = prefab1.Blocks;

        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(1, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var voxel = new Voxel(FcColor.Black, true);

        prefab1.Fill(new int3(7, 0, 0), new int3(8, 0, 0), voxel, true, true, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab1.Id) : null);

        await Assert.That(prefab1.Size).IsEqualTo(new int3(2, 1, 1));
        await Assert.That(prefab2.Id).IsEqualTo((ushort)3);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TryFill_PosOutOfBounds_DoesNothing(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);
        prefabList.AddPrefab(prefab);

        var prefabClone = prefab.Clone(true);

        var voxel = new Voxel(FcColor.Blue, false);

        bool filled = prefab.TryFill(new int3(-10, -10, -10), new int3(-1, -1, -1), voxel, true, true, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab.Id) : null);

        await Assert.That(filled).IsTrue();

        await Assert.That(prefab).IsEqualTo(prefabClone, new PrefabComparer());
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TryFill_OverwriteBlocksFalse_ReturnsFalse(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 1);
        var prefab2 = CreatePrefab(2, 1);

        var blocks = prefab1.Blocks;

        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(1, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var voxel = new Voxel(FcColor.Black, true);

        bool filled = prefab1.TryFill(new int3(7, 0, 0), new int3(8, 0, 0), voxel, true, false, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab1.Id) : null);

        await Assert.That(filled).IsFalse();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TryFill_OverwriteBlocksTrue_ReturnsTrue(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 1);
        var prefab2 = CreatePrefab(2, 1);

        var blocks = prefab1.Blocks;

        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(1, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var voxel = new Voxel(FcColor.Black, true);

        bool filled = prefab1.TryFill(new int3(7, 0, 0), new int3(8, 0, 0), voxel, true, true, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab1.Id) : null);

        await Assert.That(filled).IsTrue();

        await Assert.That(prefab1.Size).IsEqualTo(new int3(2, 1, 1));
        await Assert.That(prefab2.Id).IsEqualTo((ushort)3);
    }

    [Test]
    public async Task FillColor_NoVoxels_DoesNothing()
    {
        var prefab = CreatePrefab(1, 1);
        var prefabClone = prefab.Clone(true);

        prefab.FillColor(new int3(0, 0, 0), int3.One * 8 * Prefab.MaxSize - 1, 0, FcColor.Blue);

        await Assert.That(prefab).IsEqualTo(prefabClone, new PrefabComparer());
    }

    [Test]
    public async Task FillColor_FillsColor()
    {
        var prefab = CreatePrefab(1, 1);

        var voxels = prefab[int3.Zero].Voxels!;

        Voxel voxel = new Voxel(FcColor.White, false);
        voxels[1] = voxel;
        voxels[2] = voxel;

        prefab.FillColor(new int3(0, 0, 0), int3.One * 8 * Prefab.MaxSize - 1, 2, FcColor.Black);

        using (Assert.Multiple())
        {
            for (int i = 0; i < 8 * 8 * 8; i++)
            {
                if (i is 1 or 2)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        await Assert.That(voxels[i].Colors[j]).IsEqualTo((byte)(j is 2 ? FcColor.Black : FcColor.White));
                    }
                }
                else
                {
                    await Assert.That(voxels[i]).IsEqualTo(default);
                }
            }
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task EnsureSegmentVoxels_AddsSegments(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);

        prefabList.AddPrefab(prefab);

        bool added = prefab.EnsureSegmentVoxels(new int3(0, 0, 0), new int3(1, 1, 1), true, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab.Id) : null);

        await Assert.That(added).IsTrue();

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Size).IsEqualTo(new int3(2, 2, 2));

            for (int z = 0; z < 2; z++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        await Assert.That(prefab.ContainsKey(new int3(x, y, z))).IsTrue();
                        await Assert.That(prefab[new int3(x, y, z)].Voxels).IsNotNull();
                    }
                }
            }
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task EnsureSegmentVoxels_AddsVoxels(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1, initVoxels: false);

        prefabList.AddPrefab(prefab);

        bool added = prefab.EnsureSegmentVoxels(new int3(0, 0, 0), new int3(0, 0, 0), true, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab.Id) : null);

        await Assert.That(added).IsFalse();

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Size).IsEqualTo(new int3(1, 1, 1));

            await Assert.That(prefab.ContainsKey(new int3(0, 0, 0))).IsTrue();
            await Assert.That(prefab[new int3(0, 0, 0)].Voxels).IsNotNull();
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task EnsureSegmentVoxels_OverwriteBlocksFalse_Throws(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 1);
        var prefab2 = CreatePrefab(1, 1);

        var blocks = prefab1.Blocks;

        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(1, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        await Assert.That(() => prefab1.EnsureSegmentVoxels(new int3(0, 0, 0), new int3(1, 0, 0), false, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab1.Id) : null)).Throws<BlockObstructedException>();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task EnsureSegmentVoxels_OverwriteBlocksTrue_DoesNotThrow(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 1);
        var prefab2 = CreatePrefab(2, 1);

        var blocks = prefab1.Blocks;

        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(1, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        bool added = prefab1.EnsureSegmentVoxels(new int3(0, 0, 0), new int3(1, 0, 0), true, prefabList, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab1.Id) : null);

        await Assert.That(added).IsTrue();

        await Assert.That(prefab1.Size).IsEqualTo(new int3(2, 1, 1));
        await Assert.That(prefab2.Id).IsEqualTo((ushort)3);
    }

    [Test]
    [Arguments(-1, -1, -1, 0, 0, 0)]
    [Arguments(int.MaxValue, int.MaxValue, int.MaxValue, Prefab.MaxSize - 1, Prefab.MaxSize - 1, Prefab.MaxSize - 1)]
    public async Task ClampSegmentToPrefab_Clamps(int x, int y, int z, int expectedX, int expectedY, int expectedZ)
        => await Assert.That(PrefabUtils.ClampSegmentToPrefab(new int3(x, y, z))).IsEqualTo(new int3(expectedX, expectedY, expectedZ));

    [Test]
    [Arguments(-1, -1, -1, 0, 0, 0)]
    [Arguments(int.MaxValue, int.MaxValue, int.MaxValue, (8 * Prefab.MaxSize) - 1, (8 * Prefab.MaxSize) - 1, (8 * Prefab.MaxSize) - 1)]
    public async Task ClampVoxelToPrefab(int x, int y, int z, int expectedX, int expectedY, int expectedZ)
        => await Assert.That(PrefabUtils.ClampVoxelToPrefab(new int3(x, y, z))).IsEqualTo(new int3(expectedX, expectedY, expectedZ));

    [Test]
    [Arguments(-1, -1, -1, 0, 0, 0)]
    [Arguments(int.MaxValue, int.MaxValue, int.MaxValue, 7, 7, 7)]
    public async Task ClampVoxelToSegment_Clamps(int x, int y, int z, int expectedX, int expectedY, int expectedZ)
        => await Assert.That(PrefabUtils.ClampVoxelToSegment(new int3(x, y, z))).IsEqualTo(new int3(expectedX, expectedY, expectedZ));

    [Test]
    [Arguments(0, 0, 0, 0, 0, 0)]
    [Arguments(7, 7, 7, 0, 0, 0)]
    [Arguments(8, 8, 8, 1, 1, 1)]
    [Arguments(15, 15, 15, 1, 1, 1)]
    [Arguments(16, 16, 16, 2, 2, 2)]
    public async Task VoxelToSegment_Clamps(int x, int y, int z, int expectedX, int expectedY, int expectedZ)
        => await Assert.That(PrefabUtils.VoxelToSegment(new int3(x, y, z))).IsEqualTo(new int3(expectedX, expectedY, expectedZ));

    private static async Task AssertVoxelRegion(Prefab prefab, int3 fromVoxel, int3 toVoxel, Voxel expected)
    {
        Debug.Assert(fromVoxel.InBounds(8 * Prefab.MaxSize, 8 * Prefab.MaxSize, 8 * Prefab.MaxSize), $"{nameof(fromVoxel)} should be in bounds.");
        Debug.Assert(toVoxel.InBounds(8 * Prefab.MaxSize, 8 * Prefab.MaxSize, 8 * Prefab.MaxSize), $"{nameof(fromVoxel)} should be in bounds.");
        Debug.Assert(fromVoxel.X <= toVoxel.X && fromVoxel.Y <= toVoxel.Y && fromVoxel.Z <= toVoxel.Z, $"{nameof(fromVoxel)} should be smaller than or equal to {nameof(toVoxel)}.");

        for (int z = fromVoxel.Z; z <= toVoxel.Z; z++)
        {
            for (int y = fromVoxel.Y; y <= toVoxel.Y; y++)
            {
                for (int x = fromVoxel.X; x <= toVoxel.X; x++)
                {
                    int3 pos = new int3(x, y, z);
                    if (prefab.TryGetValue(PrefabUtils.VoxelToSegment(pos), out var segment) && segment.Voxels is not null)
                    {
                        await Assert.That(segment.Voxels[PrefabSegment.IndexVoxels(pos % 8)]).IsEqualTo(expected);
                    }
                    else if (expected != default)
                    {
                        Assert.Fail($"{nameof(prefab)} should contain a segment at {PrefabUtils.VoxelToSegment(pos)}.");
                    }
                }
            }
        }
    }

    private static async Task AssertVoxelRegionInverted(Prefab prefab, int3 fromVoxel, int3 toVoxel, Voxel expected)
    {
        Debug.Assert(fromVoxel.InBounds(8 * Prefab.MaxSize, 8 * Prefab.MaxSize, 8 * Prefab.MaxSize), $"{nameof(fromVoxel)} should be in bounds.");
        Debug.Assert(toVoxel.InBounds(8 * Prefab.MaxSize, 8 * Prefab.MaxSize, 8 * Prefab.MaxSize), $"{nameof(fromVoxel)} should be in bounds.");
        Debug.Assert(fromVoxel.X <= toVoxel.X && fromVoxel.Y <= toVoxel.Y && fromVoxel.Z <= toVoxel.Z, $"{nameof(fromVoxel)} should be smaller than or equal to {nameof(toVoxel)}.");

        for (int z = 0; z < 8 * Prefab.MaxSize; z++)
        {
            for (int y = 0; y < 8 * Prefab.MaxSize; y++)
            {
                for (int x = 0; x < 8 * Prefab.MaxSize; x++)
                {
                    if ((x < fromVoxel.X || x > toVoxel.X) && (y < fromVoxel.Y || y > toVoxel.Y) && (z < fromVoxel.Z || z > toVoxel.Z))
                    {
                        int3 pos = new int3(x, y, z);
                        if (prefab.TryGetValue(PrefabUtils.VoxelToSegment(pos), out var segment) && segment.Voxels is not null)
                        {
                            await Assert.That(segment.Voxels[PrefabSegment.IndexVoxels(pos % 8)]).IsEqualTo(expected);
                        }
                        else if (expected != default)
                        {
                            Assert.Fail($"{nameof(prefab)} should contain a segment at {PrefabUtils.VoxelToSegment(pos)}.");
                        }
                    }
                }
            }
        }
    }

    private static Prefab CreatePrefab(ushort id, IEnumerable<PrefabSegment> segments)
        => new Prefab(id, $"Prefab {id}", PrefabCollider.Box, PrefabType.Normal, FcColorUtils.DefaultBackgroundColor, true, null, null, null, segments);

    private static Prefab CreatePrefab(ushort id, int segmentCount, bool initVoxels = true)
        => CreatePrefab(id, CreateSegments(id, segmentCount, initVoxels));

    private static IEnumerable<PrefabSegment> CreateSegments(ushort id, int count, bool initVoxels)
    {
        Debug.Assert(count < 4 * 4 * 4);

        int c = 0;
        for (int z = 0; z < 4; z++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    yield return new PrefabSegment(id, new int3(x, y, z), initVoxels ? new Voxel[8 * 8 * 8] : null);
                    if (++c >= count)
                    {
                        yield break;
                    }
                }
            }
        }
    }
}
