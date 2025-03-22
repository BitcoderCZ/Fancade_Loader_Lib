using MathUtils.Vectors;
using System.Diagnostics;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Extensions;

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
        var segments1 = CreateDummySegments(0, 2).ToList();
        var segments2 = CreateDummySegments(2, 3).ToList();

        var prefab1 = CreateDummyPrefab(0, segments1);
        var prefab2 = CreateDummyPrefab(2, segments2);

        var prefabList = new PrefabList([prefab1, prefab2]);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(5);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.Prefabs).IsEquivalentTo([prefab1, prefab2], new PrefabComparer());
            await Assert.That(prefabList.Segments).IsEquivalentTo(segments1.Concat(segments2));
        }
    }

    [Test]
    public async Task Constructor_WithInvalidCollection_ThrowsException()
    {
        using (Assert.Multiple())
        {
            await Assert.That(() => new PrefabList([CreateDummyPrefab(0, 2), CreateDummyPrefab(3, 1)])).Throws<ArgumentException>();
            await Assert.That(() => new PrefabList([CreateDummyPrefab(0, 3), CreateDummyPrefab(1, 1)])).Throws<ArgumentException>();
        }
    }

    [Test]
    public async Task AddPrefab_AppendsPrefabAndSegments()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var segments = CreateDummySegments(1, 3).ToList();
        var prefab = CreateDummyPrefab(1, segments);
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

        var prefab = CreateDummyPrefab(0, 3);
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

        var prefab1 = CreateDummyPrefab(1, 1);
        var prefab2 = CreateDummyPrefab(2, 1);

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

        var prefab1 = CreateDummyPrefab(1, 1);
        var prefab3 = CreateDummyPrefab(2, 1);
        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab3);

        var prefab2 = CreateDummyPrefab(2, 1);
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

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab3 = CreateDummyPrefab(3, 2);

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

        var prefab2 = CreateDummyPrefab(3, 2);
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
    public async Task UpdatePrefab_UpdatesPrefabAndSegments()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab2 = CreateDummyPrefab(3, 2);
        var prefab3 = CreateDummyPrefab(5, 2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        var newPrefab = CreateDummyPrefab(3, 3);

        prefabList.UpdatePrefab(newPrefab, false);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(3);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(7);
        }

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
    public async Task UpdatePrefab_AddsIds()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab2 = CreateDummyPrefab(3, 2);
        var prefab3 = CreateDummyPrefab(5, 2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);
        blocks.SetPrefab(new int3(0, 0, 2), prefab3);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        var newPrefab = CreateDummyPrefab(3, 3);

        prefabList.UpdatePrefab(newPrefab, false);

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
    public async Task UpdatePrefab_RemovesIds()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab2 = CreateDummyPrefab(3, 3);
        var prefab3 = CreateDummyPrefab(6, 2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);
        blocks.SetPrefab(new int3(0, 0, 2), prefab3);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        var newPrefab = CreateDummyPrefab(3, 2);

        prefabList.UpdatePrefab(newPrefab, false);

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
    public async Task UpdatePrefab_WithObstruction_ThrowsException()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab2 = CreateDummyPrefab(3, 3);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(2, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var newPrefab = CreateDummyPrefab(1, 3);

        await Assert.That(() => prefabList.UpdatePrefab(newPrefab, false)).Throws<InvalidOperationException>();

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
    public async Task UpdatePrefab_WithObstruction_OverwriteTrue_DoesNotThrow()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab2 = CreateDummyPrefab(3, 3);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(2, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var newPrefab = CreateDummyPrefab(1, 3);

        prefabList.UpdatePrefab(newPrefab, true);

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
    public async Task RemovePrefab_RemovesPrefabAndSegments()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab2 = CreateDummyPrefab(3, 1);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        bool removed = prefabList.RemovePrefab(1);

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
    public async Task RemovePrefab_RemovesAndUpdatesBlockIds()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab2 = CreateDummyPrefab(3, 1);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        bool removed = prefabList.RemovePrefab(1);

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
    public async Task RemovePrefabFromBLocks_RemovesIds()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab2 = CreateDummyPrefab(3, 2);
        var prefab3 = CreateDummyPrefab(5, 2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);
        blocks.SetPrefab(new int3(0, 0, 2), prefab3);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        bool removed = prefabList.RemovePrefabFromBLocks(prefab2.Id);

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
    public async Task RemovePrefabFromBLocks_WhenNotContained_ReturnsFalse()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab2 = CreateDummyPrefab(3, 2);
        var prefab3 = CreateDummyPrefab(5, 2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab3);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);
        prefabList.AddPrefab(prefab3);

        bool removed = prefabList.RemovePrefabFromBLocks(prefab2.Id);

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
    public async Task AddSegmentToPrefab_AppendsSegment()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreateDummyPrefab(1, 1);

        var newSegment = new PrefabSegment(1, new byte3(1, 0, 0));

        prefabList.AddPrefab(prefab);
        prefabList.AddSegmentToPrefab(1, newSegment, false);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(1);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(2);
        }

        await Assert.That(prefabList.GetSegment(2)).IsEqualTo(newSegment);
    }

    [Test]
    public async Task AddSegmentToPrefab_UpdatesIdsAndShiftBlockIds()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 1);
        var prefab2 = CreateDummyPrefab(2, 2);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var newPrefab = new PrefabSegment(1, new byte3(1, 0, 0));
        prefabList.AddSegmentToPrefab(1, newPrefab, false);

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
    public async Task AddSegmentToPrefab_WithObstruction_ThrowsException()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreateDummyPrefab(1, 1);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);
        blocks.SetBlock(new int3(1, 0, 0), 5);

        prefabList.AddPrefab(prefab);

        var newSegment = new PrefabSegment(1, new byte3(1, 0, 0));
        await Assert.That(() => prefabList.AddSegmentToPrefab(1, newSegment, false)).Throws<InvalidOperationException>();

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Count).IsEqualTo(1);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)5);
        }
    }

    [Test]
    public async Task AddSegmentToPrefab_WithObstruction_OverwriteTrue_DoesNotThrow()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreateDummyPrefab(1, 1);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);
        blocks.SetBlock(new int3(1, 0, 0), 5);

        prefabList.AddPrefab(prefab);

        var newSegment = new PrefabSegment(1, new byte3(1, 0, 0));

        prefabList.AddSegmentToPrefab(1, newSegment, true);

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Count).IsEqualTo(2);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
        }
    }

    [Test]
    public async Task AddSegmentToPrefab_OverwritesPrefabsCorrectly()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 1);
        var prefab2 = CreateDummyPrefab(2, 2);

        var blocks = prefab1.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(1, 0, 0), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var newSegment = new PrefabSegment(1, new byte3(1, 0, 0));
        prefabList.AddSegmentToPrefab(1, newSegment, true);

        using (Assert.Multiple())
        {
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
            await Assert.That(blocks.GetBlock(new int3(2, 0, 0))).IsEqualTo((ushort)0);
        }
    }

    [Test]
    public async Task TryAddSegmentToPrefab_WithObstruction_ReturnsFalse()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreateDummyPrefab(1, 1);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);
        blocks.SetBlock(new int3(1, 0, 0), 5);

        prefabList.AddPrefab(prefab);

        var newSegment = new PrefabSegment(1, new byte3(1, 0, 0));

        bool added = prefabList.TryAddSegmentToPrefab(1, newSegment, false);

        await Assert.That(added).IsFalse();

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Count).IsEqualTo(1);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)5);
        }
    }

    [Test]
    public async Task TryAddSegmentToPrefab_WithObstruction_OverwriteTrue_ReturnsTrue()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreateDummyPrefab(1, 1);

        var blocks = prefab.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab);
        blocks.SetBlock(new int3(1, 0, 0), 5);

        prefabList.AddPrefab(prefab);

        var newSegment = new PrefabSegment(1, new byte3(1, 0, 0));

        bool added = prefabList.TryAddSegmentToPrefab(1, newSegment, true);

        await Assert.That(added).IsTrue();

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Count).IsEqualTo(2);
            await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
            await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
        }
    }

    [Test]
    public async Task RemoveSegmentFromPrefab_RemovesSegmentAndUpdatesPrefabs()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreateDummyPrefab(1, 2);
        prefabList.AddPrefab(prefab);

        bool removed = prefabList.RemoveSegmentFromPrefab(1, new byte3(1, 0, 0));

        await Assert.That(removed).IsTrue();

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Count).IsEqualTo(1);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(1);
            await Assert.That(prefabList.GetSegment(1)).IsEqualTo(prefab[byte3.Zero]);
        }
    }

    [Test]
    public async Task RemoveSegmentFromPrefab_UpdatesIdsAndShiftBlockIds()
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab2 = CreateDummyPrefab(3, 2);

        var blocks = prefab2.Blocks;
        blocks.SetPrefab(new int3(0, 0, 0), prefab1);
        blocks.SetPrefab(new int3(0, 0, 1), prefab2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        bool removed = prefabList.RemoveSegmentFromPrefab(1, new byte3(1, 0, 0));

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

        var prefab1 = CreateDummyPrefab(5, 2);
        var prefab2 = CreateDummyPrefab(7, 1);

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

        var prefab1 = CreateDummyPrefab(15, 2);
        var prefab2 = CreateDummyPrefab(17, 1);

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

        var prefab1 = CreateDummyPrefab(25, 2);
        var prefab2 = CreateDummyPrefab(27, 1);
        var prefab3 = CreateDummyPrefab(28, 3);

        var segment = prefab3[new byte3(1, 0, 0)];
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

            await Assert.That(loadedPrefabList.Prefabs).IsEquivalentTo(prefabList.Prefabs, new PrefabComparer());
            await Assert.That(loadedPrefabList.Segments).IsEquivalentTo(prefabList.Segments, new PrefabSegmentComparer());
        }
    }

    private static Prefab CreateDummyPrefab(ushort id, IEnumerable<PrefabSegment> segments)
        => new Prefab(id, $"Prefab {id}", PrefabCollider.Box, PrefabType.Normal, FcColorUtils.DefaultBackgroundColor, true, null, null, null, segments);

    private static Prefab CreateDummyPrefab(ushort id, int prefabCount)
        => CreateDummyPrefab(id, CreateDummySegments(id, prefabCount));

    private static IEnumerable<PrefabSegment> CreateDummySegments(ushort id, int count)
    {
        Debug.Assert(count < 4 * 4 * 4);

        int c = 0;
        for (int z = 0; z < 4; z++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    yield return new PrefabSegment(id, new byte3(x, y, z));
                    if (++c >= count)
                    {
                        yield break;
                    }
                }
            }
        }
    }
}
