using FancadeLoaderLib.Exceptions;
using FancadeLoaderLib.Tests.Common;
using MathUtils.Vectors;
using TUnit.Assertions.AssertConditions.Throws;
using static FancadeLoaderLib.Tests.Common.PrefabGenerator;

namespace FancadeLoaderLib.Tests;

public class PrefabListTests
{
    [Test]
    public async Task Constructor_WithCapacity_InitializesEmptyLists()
    {
        var prefabList = new PrefabList(10, 20);

        await Assert.That(prefabList.PrefabCount).IsEqualTo(0);
        await Assert.That(prefabList.SegmentCount).IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_WithCollection_InitializesPrefabsAndSegments()
    {
        var segments1 = CreateSegments(0, 2).ToList();
        var segments2 = CreateSegments(2, 3).ToList();

        var prefab1 = CreatePrefab(0, segments1);
        var prefab2 = CreatePrefab(2, segments2);

        var prefabList = new PrefabList([prefab1, prefab2]);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(5);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.Prefabs).IsEquivalentTo([prefab1, prefab2], PrefabComparer.Instance);
            await Assert.That(prefabList.Segments).IsEquivalentTo(segments1.Order(PrefabSegmentPositionComparer.Instance).Concat(segments2.Order(PrefabSegmentPositionComparer.Instance)));
        }
    }

    [Test]
    public async Task Constructor_WithInvalidCollection_ThrowsException()
    {
        using (Assert.Multiple())
        {
            await Assert.That(() => new PrefabList([CreatePrefab(0, 2), CreatePrefab(3, 1)])).Throws<ArgumentException>();
            await Assert.That(() => new PrefabList([CreatePrefab(0, 3), CreatePrefab(1, 1)])).Throws<ArgumentException>();
        }
    }

    [Test]
    public async Task AddPrefab_AppendsPrefabAndSegments()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var segments = CreateSegments(1, 3).ToList();
        var prefab = CreatePrefab(1, segments);
        prefabList.AddPrefab(prefab);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(1);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(3);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetPrefab(1)).IsEqualTo(prefab);
            foreach (var (segment, segmentId) in prefab.EnumerateWithId())
            {
                await Assert.That(prefabList.GetSegment(segmentId)).IsEqualTo(segment);
            }
        }
    }

    [Test]
    public async Task AddPrefab_UpdatesPrefabId()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(0, 3);
        prefabList.AddPrefab(prefab);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(1);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(3);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetPrefab(1)).IsEqualTo(prefab);
            await Assert.That(prefab.Id).IsEqualTo((ushort)1);
        }
    }

    [Test]
    public async Task InsertPrefab_LastPrefabCondition_WorksLikeAddPrefab()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 1);
        var prefab2 = CreatePrefab(2, 1);

        prefabList.AddPrefab(prefab1);
        prefabList.InsertPrefab(prefab2);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(2);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetPrefab(1)).IsEqualTo(prefab1);
            await Assert.That(prefabList.GetPrefab(2)).IsEqualTo(prefab2);
        }
    }

    [Test]
    public async Task InsertPrefab_InsertsPrefabAtCorrectPosition()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 1);
        var prefab3 = CreatePrefab(2, 1);
        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab3);

        var prefab2 = CreatePrefab(2, 1);
        prefabList.InsertPrefab(prefab2);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(3);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(3);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetPrefab(1)).IsEqualTo(prefab1);
            await Assert.That(prefabList.GetPrefab(2)).IsEqualTo(prefab2);
            await Assert.That(prefabList.GetPrefab(3)).IsEqualTo(prefab3);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefab1.Id).IsEqualTo((ushort)1);
            await Assert.That(prefab2.Id).IsEqualTo((ushort)2);
            await Assert.That(prefab3.Id).IsEqualTo((ushort)3);
        }
    }

    [Test]
    public async Task InsertPrefab_ShiftsBlockIds()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab3 = CreatePrefab(3, 2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 2), prefab3);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab3);

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 2))).IsEqualTo((ushort)3);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 2))).IsEqualTo((ushort)4);
        }

        var prefab2 = CreatePrefab(3, 2);
        prefabList.InsertPrefab(prefab2);

        blocks.SetPrefab(new int3(0, 0, 1), prefab2);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(3);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(6);
        }

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)3);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 1))).IsEqualTo((ushort)4);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 2))).IsEqualTo((ushort)5);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 2))).IsEqualTo((ushort)6);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task UpdatePrefab_UpdatesPrefabAndSegments(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab2 = CreatePrefab(3, 2);
        var prefab3 = CreatePrefab(5, 2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        var newPrefab = CreatePrefab(3, 3);

        var prevPrefab = prefabList.UpdatePrefab(newPrefab, false, cache ? new BlockInstancesCache(prefabList.Prefabs, newPrefab.Id) : null);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(3);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(7);
        }

        await Assert.That(prevPrefab).IsEqualTo(prefab2);

        using (Assert.Multiple())
        {
            await Assert.That(prefab1.Id).IsEqualTo((ushort)1);
            await Assert.That(newPrefab.Id).IsEqualTo((ushort)3);
            await Assert.That(prefab3.Id).IsEqualTo((ushort)6);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetPrefab(3)).IsEqualTo(newPrefab);

            foreach (var (segment, segmentId) in newPrefab.EnumerateWithId())
            {
                await Assert.That(prefabList.GetSegment(segmentId)).IsEqualTo(segment);
            }
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task UpdatePrefab_NotLastSegment_UpdatesPrefabAndSegments(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab2 = CreatePrefab(3, [new int3(0, 0, 0), new int3(0, 0, 1)]);
        var prefab3 = CreatePrefab(5, 2);

        var blocks = prefab1.Blocks;

        blocks.SetPrefab(new int3(0, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        var newPrefab = CreatePrefab(3, [new int3(0, 0, 0), new int3(0, 1, 0), new int3(0, 0, 1)]);

        var prevPrefab = prefabList.UpdatePrefab(newPrefab, false, cache ? new BlockInstancesCache(prefabList.Prefabs, newPrefab.Id) : null);

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)3);
            await Assert.That(blocks.GetBlock(new int3(0, 1, 0))).IsEqualTo((ushort)4);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)5);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetSegment(blocks.GetBlock(new int3(0, 0, 0))).PosInPrefab).IsEqualTo(new int3(0, 0, 0));
            await Assert.That(prefabList.GetSegment(blocks.GetBlock(new int3(0, 1, 0))).PosInPrefab).IsEqualTo(new int3(0, 1, 0));
            await Assert.That(prefabList.GetSegment(blocks.GetBlock(new int3(0, 0, 1))).PosInPrefab).IsEqualTo(new int3(0, 0, 1));
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(3);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(7);
        }

        await Assert.That(prevPrefab).IsEqualTo(prefab2);

        using (Assert.Multiple())
        {
            await Assert.That(prefab1.Id).IsEqualTo((ushort)1);
            await Assert.That(newPrefab.Id).IsEqualTo((ushort)3);
            await Assert.That(prefab3.Id).IsEqualTo((ushort)6);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetPrefab(3)).IsEqualTo(newPrefab);

            foreach (var (segment, segmentId) in newPrefab.EnumerateWithId())
            {
                await Assert.That(prefabList.GetSegment(segmentId)).IsEqualTo(segment);
            }
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task UpdatePrefab_AddsIds(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab2 = CreatePrefab(3, 2);
        var prefab3 = CreatePrefab(5, 2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);
        blocks.SetPrefab(new int3(0, 0, 2), prefab3);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        var newPrefab = CreatePrefab(3, 3);

        prefabList.UpdatePrefab(newPrefab, false, cache ? new BlockInstancesCache(prefabList.Prefabs, newPrefab.Id) : null);

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);

            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)3);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 1))).IsEqualTo((ushort)4);
            await Assert.That(blocks.GetBlock(new int3(2, 0, 1))).IsEqualTo((ushort)5);

            await Assert.That(blocks.GetBlock(new int3(0, 0, 2))).IsEqualTo((ushort)6);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 2))).IsEqualTo((ushort)7);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task UpdatePrefab_RemovesIds(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab2 = CreatePrefab(3, 3);
        var prefab3 = CreatePrefab(6, 2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);
        blocks.SetPrefab(new int3(0, 0, 2), prefab3);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        var newPrefab = CreatePrefab(3, 2);

        prefabList.UpdatePrefab(newPrefab, false, cache ? new BlockInstancesCache(prefabList.Prefabs, newPrefab.Id) : null);

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);

            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)3);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 1))).IsEqualTo((ushort)4);
            await Assert.That(blocks.GetBlock(new int3(2, 0, 1))).IsEqualTo((ushort)0);

            await Assert.That(blocks.GetBlock(new int3(0, 0, 2))).IsEqualTo((ushort)5);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 2))).IsEqualTo((ushort)6);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task UpdatePrefab_WithObstruction_ThrowsException(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab2 = CreatePrefab(3, 3);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(2, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var newPrefab = CreatePrefab(1, 3);

        await Assert.That(() => prefabList.UpdatePrefab(newPrefab, false, cache ? new BlockInstancesCache(prefabList.Prefabs, newPrefab.Id) : null)).Throws<BlockObstructedException>();

        using (Assert.Multiple())
        {
            await Assert.That(prefab1.Id).IsEqualTo((ushort)1);
            await Assert.That(prefab2.Id).IsEqualTo((ushort)3);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetPrefab(1)).IsEqualTo(prefab1);
            await Assert.That(prefabList.GetPrefab(3)).IsEqualTo(prefab2);
        }

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);

            await Assert.That(blocks.GetBlock(new int3(2, 0, 0))).IsEqualTo((ushort)3);
            await Assert.That(blocks.GetBlock(new int3(3, 0, 0))).IsEqualTo((ushort)4);
            await Assert.That(blocks.GetBlock(new int3(4, 0, 0))).IsEqualTo((ushort)5);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task UpdatePrefab_WithObstruction_OverwriteTrue_DoesNotThrow(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab2 = CreatePrefab(3, 3);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(2, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var newPrefab = CreatePrefab(1, 3);

        prefabList.UpdatePrefab(newPrefab, true, cache ? new BlockInstancesCache(prefabList.Prefabs, newPrefab.Id) : null);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetPrefab(1)).IsEqualTo(newPrefab);
            await Assert.That(prefabList.GetPrefab(4)).IsEqualTo(prefab2);
        }

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
            await Assert.That(blocks.GetBlock(new int3(2, 0, 0))).IsEqualTo((ushort)3);

            await Assert.That(blocks.GetBlock(new int3(3, 0, 0))).IsEqualTo((ushort)0);
            await Assert.That(blocks.GetBlock(new int3(4, 0, 0))).IsEqualTo((ushort)0);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemovePrefab_RemovesPrefabAndSegments(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab2 = CreatePrefab(3, 1);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        bool removed = prefabList.RemovePrefab(1, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(removed).IsTrue();

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(1);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(1);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.ContainsPrefab(3)).IsFalse();
            await Assert.That(prefab2.Id).IsEqualTo((ushort)1);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemovePrefab_RemovesAndUpdatesBlockIds(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab2 = CreatePrefab(3, 1);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        bool removed = prefabList.RemovePrefab(1, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(removed).IsTrue();

        await Assert.That(prefab2.Id).IsEqualTo((ushort)1);

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)0);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)0);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)1);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemovePrefabFromBLocks_RemovesIds(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab2 = CreatePrefab(3, 2);
        var prefab3 = CreatePrefab(5, 2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);
        blocks.SetPrefab(new int3(0, 0, 2), prefab3);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        bool removed = prefabList.RemovePrefabFromBLocks(prefab2.Id, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab2.Id) : null);

        await Assert.That(removed).IsTrue();

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);

            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)0);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 1))).IsEqualTo((ushort)0);

            await Assert.That(blocks.GetBlock(new int3(0, 0, 2))).IsEqualTo((ushort)5);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 2))).IsEqualTo((ushort)6);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemovePrefabFromBLocks_WhenNotContained_ReturnsFalse(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab2 = CreatePrefab(3, 2);
        var prefab3 = CreatePrefab(5, 2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab3);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        bool removed = prefabList.RemovePrefabFromBLocks(prefab2.Id, cache ? new BlockInstancesCache(prefabList.Prefabs, prefab2.Id) : null);

        await Assert.That(removed).IsFalse();

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);

            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)5);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 1))).IsEqualTo((ushort)6);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task CanAddSegmentToPrefab_WithObstruction_ReturnsFalse(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);
        blocks.SetBlock(new int3(1, 0, 0), 5);

        prefabList.AddPrefab(prefab);

        var newSegment = new int3(1, 0, 0);

        bool added = prefabList.CanAddSegmentToPrefab(1, newSegment, false, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(added).IsFalse();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task CanAddSegmentToPrefab_WithObstruction_OverwriteTrue_ReturnsTrue(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);
        blocks.SetBlock(new int3(1, 0, 0), 5);

        prefabList.AddPrefab(prefab);

        var newSegment = new int3(1, 0, 0);

        bool added = prefabList.CanAddSegmentToPrefab(1, newSegment, true, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(added).IsTrue();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task AddSegmentToPrefab_AppendsSegment(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);

        var newSegment = new PrefabSegment(1, new int3(1, 0, 0));

        prefabList.AddPrefab(prefab);
        prefabList.AddSegmentToPrefab(1, newSegment, false, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(1);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(2);
        }

        await Assert.That(prefabList.GetSegment(2)).IsEqualTo(newSegment);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task AddSegmentToPrefab_UpdatesIdsAndShiftBlockIds(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 1);
        var prefab2 = CreatePrefab(2, 2);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var newPrefab = new PrefabSegment(1, new int3(1, 0, 0));
        prefabList.AddSegmentToPrefab(1, newPrefab, false, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(4);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefab1.Id).IsEqualTo((ushort)1);
            await Assert.That(prefab2.Id).IsEqualTo((ushort)3);
        }

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)3);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 1))).IsEqualTo((ushort)4);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task AddSegmentToPrefab_NotLastSegment_UpdatesIdsAndShiftBlockIds(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, [new int3(0, 0, 0), new int3(0, 0, 1)]);
        var prefab2 = CreatePrefab(3, 2);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 2), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var newPrefab = new PrefabSegment(1, new int3(0, 1, 0));
        prefabList.AddSegmentToPrefab(1, newPrefab, false, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(5);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefab1.Id).IsEqualTo((ushort)1);
            await Assert.That(prefab2.Id).IsEqualTo((ushort)4);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetSegment(blocks.GetBlock(new int3(0, 0, 0))).PosInPrefab).IsEqualTo(new int3(0, 0, 0));
            await Assert.That(prefabList.GetSegment(blocks.GetBlock(new int3(0, 1, 0))).PosInPrefab).IsEqualTo(new int3(0, 1, 0));
            await Assert.That(prefabList.GetSegment(blocks.GetBlock(new int3(0, 0, 1))).PosInPrefab).IsEqualTo(new int3(0, 0, 1));
        }

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(0, 1, 0))).IsEqualTo((ushort)2);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)3);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 2))).IsEqualTo((ushort)4);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 2))).IsEqualTo((ushort)5);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task AddSegmentToPrefab_WithObstruction_ThrowsException(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);
        blocks.SetBlock(new int3(1, 0, 0), 5);

        prefabList.AddPrefab(prefab);

        var newSegment = new PrefabSegment(1, new int3(1, 0, 0));
        await Assert.That(() => prefabList.AddSegmentToPrefab(1, newSegment, false, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null)).Throws<BlockObstructedException>();

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Count).IsEqualTo(1);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)5);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task AddSegmentToPrefab_WithObstruction_OverwriteTrue_DoesNotThrow(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);
        blocks.SetBlock(new int3(1, 0, 0), 5);

        prefabList.AddPrefab(prefab);

        var newSegment = new PrefabSegment(1, new int3(1, 0, 0));

        prefabList.AddSegmentToPrefab(1, newSegment, true, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Count).IsEqualTo(2);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task AddSegmentToPrefab_OverwritesPrefabsCorrectly(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 1);
        var prefab2 = CreatePrefab(2, 2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(1, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var newSegment = new PrefabSegment(1, new int3(1, 0, 0));
        prefabList.AddSegmentToPrefab(1, newSegment, true, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
            await Assert.That(blocks.GetBlock(new int3(2, 0, 0))).IsEqualTo((ushort)0);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TryAddSegmentToPrefab_NotLastSegment_UpdatesIdsAndShiftBlockIds(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, [new int3(0, 0, 0), new int3(0, 0, 1)]);
        var prefab2 = CreatePrefab(3, 2);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 2), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var newPrefab = new PrefabSegment(1, new int3(0, 1, 0));
        bool added = prefabList.TryAddSegmentToPrefab(1, newPrefab, false, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(added).IsTrue();

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(5);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefab1.Id).IsEqualTo((ushort)1);
            await Assert.That(prefab2.Id).IsEqualTo((ushort)4);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetSegment(blocks.GetBlock(new int3(0, 0, 0))).PosInPrefab).IsEqualTo(new int3(0, 0, 0));
            await Assert.That(prefabList.GetSegment(blocks.GetBlock(new int3(0, 1, 0))).PosInPrefab).IsEqualTo(new int3(0, 1, 0));
            await Assert.That(prefabList.GetSegment(blocks.GetBlock(new int3(0, 0, 1))).PosInPrefab).IsEqualTo(new int3(0, 0, 1));
        }

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(0, 1, 0))).IsEqualTo((ushort)2);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)3);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 2))).IsEqualTo((ushort)4);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 2))).IsEqualTo((ushort)5);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TryAddSegmentToPrefab_WithObstruction_ReturnsFalse(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);
        blocks.SetBlock(new int3(1, 0, 0), 5);

        prefabList.AddPrefab(prefab);

        var newSegment = new PrefabSegment(1, new int3(1, 0, 0));

        bool added = prefabList.TryAddSegmentToPrefab(1, newSegment, false, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(added).IsFalse();

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Count).IsEqualTo(1);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)5);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TryAddSegmentToPrefab_WithObstruction_OverwriteTrue_ReturnsTrue(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 1);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);
        blocks.SetBlock(new int3(1, 0, 0), 5);

        prefabList.AddPrefab(prefab);

        var newSegment = new PrefabSegment(1, new int3(1, 0, 0));

        bool added = prefabList.TryAddSegmentToPrefab(1, newSegment, true, cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(added).IsTrue();

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Count).IsEqualTo(2);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemoveSegmentFromPrefab_RemovesSegmentAndUpdatesPrefabs(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 2);
        prefabList.AddPrefab(prefab);

        bool removed = prefabList.RemoveSegmentFromPrefab(1, new int3(1, 0, 0), cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(removed).IsTrue();

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Count).IsEqualTo(1);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(1);
            await Assert.That(prefabList.GetSegment(1)).IsEqualTo(prefab[int3.Zero]);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemoveSegmentFromPrefab_UpdatesIdsAndShiftBlockIds(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreatePrefab(1, 2);
        var prefab2 = CreatePrefab(3, 2);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        bool removed = prefabList.RemoveSegmentFromPrefab(1, new int3(1, 0, 0), cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(removed).IsTrue();

        using (Assert.Multiple())
        {
            await Assert.That(prefab1.Id).IsEqualTo((ushort)1);
            await Assert.That(prefab2.Id).IsEqualTo((ushort)2);

            await Assert.That(prefab1.Count).IsEqualTo(1);
            await Assert.That(prefab2.Count).IsEqualTo(2);
        }

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)0);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)2);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 1))).IsEqualTo((ushort)3);
        }
    }

    [Test]
    public async Task IdOffset_Increase_UpdatesIds()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 5,
        };

        var prefab1 = CreatePrefab(5, 2);
        var prefab2 = CreatePrefab(7, 1);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        prefabList.IdOffset += 5;

        using (Assert.Multiple())
        {
            await Assert.That(prefab1.Id).IsEqualTo((ushort)10);
            await Assert.That(prefab2.Id).IsEqualTo((ushort)12);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetPrefab(10)).IsEqualTo(prefab1);
            await Assert.That(prefabList.GetPrefab(12)).IsEqualTo(prefab2);
        }

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)10);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)11);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)12);
        }
    }

    [Test]
    public async Task IdOffset_Decrease_UpdatesIds()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 15,
        };

        var prefab1 = CreatePrefab(15, 2);
        var prefab2 = CreatePrefab(17, 1);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        prefabList.IdOffset -= 5;

        using (Assert.Multiple())
        {
            await Assert.That(prefab1.Id).IsEqualTo((ushort)10);
            await Assert.That(prefab2.Id).IsEqualTo((ushort)12);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.GetPrefab(10)).IsEqualTo(prefab1);
            await Assert.That(prefabList.GetPrefab(12)).IsEqualTo(prefab2);
        }

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)10);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)11);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)12);
        }
    }

    [Test]
    public async Task SaveLoad_PersistsAndRestoresData()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 25,
        };

        var prefab1 = CreatePrefab(25, 2);
        var prefab2 = CreatePrefab(27, 1);
        var prefab3 = CreatePrefab(28, 3);

        var segment = prefab3[new int3(1, 0, 0)];
        segment.Voxels = new Voxel[8 * 8 * 8];
        segment.Voxels[0].Colors[0] = (byte)FcColor.Brown;
        segment.Voxels[0].Attribs[0] = true;

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        using (var ms = new MemoryStream())
        {
            using (var writer = new FcBinaryWriter(ms, true))
            {
                prefabList.Save(writer);
            }

            ms.Position = 0;

            PrefabList loadedPrefabList;
            using (var reader = new FcBinaryReader(ms))
            {
                loadedPrefabList = PrefabList.Load(reader);
            }

            using (Assert.Multiple())
            {
                await Assert.That(loadedPrefabList.PrefabCount).IsEqualTo(prefabList.PrefabCount);
                await Assert.That(loadedPrefabList.SegmentCount).IsEqualTo(prefabList.SegmentCount);
                await Assert.That(loadedPrefabList.IdOffset).IsEqualTo(prefabList.IdOffset);
            }

            await Assert.That(loadedPrefabList.Prefabs).IsEquivalentTo(prefabList.Prefabs, PrefabComparer.Instance);
            await Assert.That(loadedPrefabList.Segments).IsEquivalentTo(prefabList.Segments, PrefabSegmentComparer.Instance);
        }
    }
}
