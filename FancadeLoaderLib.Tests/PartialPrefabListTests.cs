using FancadeLoaderLib.Partial;
using MathUtils.Vectors;
using System.Diagnostics;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Extensions;

namespace FancadeLoaderLib.Tests;

public class PartialPrefabListTests
{
    [Test]
    public async Task Constructor_WithCapacity_InitializesEmptyLists()
    {
        var prefabList = new PartialPrefabList(10, 20);

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

        var prefabList = new PartialPrefabList([prefab1, prefab2]);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(5);
        }

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.Prefabs).IsEquivalentTo([prefab1, prefab2], new PartialPrefabComparer());
            await Assert.That(prefabList.Segments).IsEquivalentTo(segments1.Concat(segments2));
        }
    }

    [Test]
    public async Task Constructor_WithInvalidCollection_ThrowsException()
    {
        using (Assert.Multiple())
        {
            await Assert.That(() => new PartialPrefabList([CreateDummyPrefab(0, 2), CreateDummyPrefab(3, 1)])).Throws<ArgumentException>();
            await Assert.That(() => new PartialPrefabList([CreateDummyPrefab(0, 3), CreateDummyPrefab(1, 1)])).Throws<ArgumentException>();
        }
    }

    [Test]
    public async Task AddPrefab_AppendsPrefabAndSegments()
    {
        var prefabList = new PartialPrefabList()
        {
            IdOffset = 1,
        };

        var segments = CreateDummySegments(1, 3).ToArray();
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
        var prefabList = new PartialPrefabList()
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
        var prefabList = new PartialPrefabList()
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
        var prefabList = new PartialPrefabList()
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
    public async Task UpdatePrefab_UpdatesPrefabAndSegments()
    {
        var prefabList = new PartialPrefabList()
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

        prefabList.UpdatePrefab(newPrefab);

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
    public async Task RemovePrefab_RemovesPrefabAndSegments()
    {
        var prefabList = new PartialPrefabList()
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
    public async Task AddSegmentToPrefab_AppendsSegment()
    {
        var prefabList = new PartialPrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreateDummyPrefab(1, 1);
        prefabList.AddPrefab(prefab);

        var newSegment = new PartialPrefabSegment(1, new byte3(1, 0, 0));
        prefabList.AddSegmentToPrefab(1, newSegment);

        using (Assert.Multiple())
        {
            await Assert.That(prefabList.PrefabCount).IsEqualTo(1);
            await Assert.That(prefabList.SegmentCount).IsEqualTo(2);
        }

        await Assert.That(prefabList.GetSegment(2)).IsEqualTo(newSegment);
    }

    [Test]
    public async Task AddSegmentToPrefab_UpdatesIds()
    {
        var prefabList = new PartialPrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 1);
        var prefab2 = CreateDummyPrefab(2, 2);

        prefabList.AddPrefab(prefab1);
        prefabList.AddPrefab(prefab2);

        var newSegment = new PartialPrefabSegment(1, new byte3(1, 0, 0));
        prefabList.AddSegmentToPrefab(1, newSegment);

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
    }

    [Test]
    public async Task RemoveSegmentFromPrefab_RemovesSegmentsAndUpdatesPrefab()
    {
        var prefabList = new PartialPrefabList()
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
    public async Task RemoveSegmentFromPrefab_UpdatesIds()
    {
        var prefabList = new PartialPrefabList()
        {
            IdOffset = 1,
        };

        var prefab1 = CreateDummyPrefab(1, 2);
        var prefab2 = CreateDummyPrefab(3, 2);

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
    }

    [Test]
    public async Task IdOffset_Increase_UpdatesIds()
    {
        var prefabList = new PartialPrefabList()
        {
            IdOffset = 5,
        };

        var prefab1 = CreateDummyPrefab(5, 2);
        var prefab2 = CreateDummyPrefab(7, 1);

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
    }

    [Test]
    public async Task IdOffset_Decrease_UpdatesIds()
    {
        var prefabList = new PartialPrefabList()
        {
            IdOffset = 15,
        };

        var prefab1 = CreateDummyPrefab(15, 2);
        var prefab2 = CreateDummyPrefab(17, 1);

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
    }

    [Test]
    public async Task SaveLoad_PersistsAndRestoresData()
    {
        var prefabList = new PartialPrefabList()
        {
            IdOffset = 25,
        };

        var prefab1 = CreateDummyPrefab(25, 2);
        var prefab2 = CreateDummyPrefab(27, 1);
        var prefab3 = CreateDummyPrefab(28, 3);

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

            PartialPrefabList loadedPartialPrefabList;
            using (var reader = new FcBinaryReader(ms))
            {
                loadedPartialPrefabList = PartialPrefabList.Load(reader);
            }

            using (Assert.Multiple())
            {
                await Assert.That(loadedPartialPrefabList.PrefabCount).IsEqualTo(prefabList.PrefabCount);
                await Assert.That(loadedPartialPrefabList.SegmentCount).IsEqualTo(prefabList.SegmentCount);
                await Assert.That(loadedPartialPrefabList.IdOffset).IsEqualTo(prefabList.IdOffset);
            }

            await Assert.That(loadedPartialPrefabList.Prefabs).IsEquivalentTo(prefabList.Prefabs, new PartialPrefabComparer());
            await Assert.That(loadedPartialPrefabList.Segments).IsEquivalentTo(prefabList.Segments, new PartialPrefabSegmentComparer());
        }
    }

    private static PartialPrefab CreateDummyPrefab(ushort id, IEnumerable<PartialPrefabSegment> segments)
        => new PartialPrefab(id, $"Prefab {id}", PrefabType.Normal, segments);

    private static PartialPrefab CreateDummyPrefab(ushort id, int prefabCount)
        => CreateDummyPrefab(id, CreateDummySegments(id, prefabCount));

    private static IEnumerable<PartialPrefabSegment> CreateDummySegments(ushort id, int count)
    {
        Debug.Assert(count < 4 * 4 * 4);

        int c = 0;
        for (int z = 0; z < 4; z++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    yield return new PartialPrefabSegment(id, new byte3(x, y, z));
                    if (++c >= count)
                    {
                        yield break;
                    }
                }
            }
        }
    }
}
