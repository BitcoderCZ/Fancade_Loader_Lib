﻿using FancadeLoaderLib.Partial;
using MathUtils.Vectors;
using System.Diagnostics;
using TUnit.Assertions.AssertConditions.Throws;

namespace FancadeLoaderLib.Tests;

public class PartialPrefabTests
{
    [Test]
    public async Task Constructor_NameNotTooLong_DoesNotThrow()
    {
        string name = new string('a', 255);

        PartialPrefab prefab = CreatePrefab(1, name, 1);

        await Assert.That(prefab.Name).IsEqualTo(name);
    }

    [Test]
    public async Task Constructor_NameTooLong_Throws()
    {
        string name = new string('a', 256);

        await Assert.That(() => CreatePrefab(1, name, 1)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Name_ValueNotTooLong_DoesNotThrow()
    {
        string name = new string('a', 255);

        PartialPrefab prefab = CreatePrefab(1, "abc", 1);

        prefab.Name = name;

        await Assert.That(prefab.Name).IsEqualTo(name);
    }

    [Test]
    public async Task Name_ValueTooLong_Throws()
    {
        string name = new string('a', 256);

        PartialPrefab prefab = CreatePrefab(1, "abc", 1);

        await Assert.That(() => prefab.Name = name).Throws<ArgumentException>();
    }

    [Test]
    [Arguments(Prefab.MaxSize - 1, 0, 0)]
    [Arguments(0, Prefab.MaxSize - 1, 0)]
    [Arguments(0, 0, Prefab.MaxSize - 1)]
    public async Task Add_PosInBounds_DoesNotThrow(int x, int y, int z)
    {
        PartialPrefab prefab = CreatePrefab(1, "abc", 1);

        prefab.Add(new PartialPrefabSegment(1, new int3(x, y, z)));

        await Assert.That(prefab.Count).IsEqualTo(2);
    }

    [Test]
    [Arguments(-1, 0, 0)]
    [Arguments(0, -1, 0)]
    [Arguments(0, 0, -1)]
    [Arguments(Prefab.MaxSize, 0, 0)]
    [Arguments(0, Prefab.MaxSize, 0)]
    [Arguments(0, 0, Prefab.MaxSize)]
    public async Task Add_PosOutOfBounds_Throws(int x, int y, int z)
    {
        PartialPrefab prefab = CreatePrefab(1, "abc", 1);

        await Assert.That(() => prefab.Add(new PartialPrefabSegment(1, new int3(x, y, z)))).Throws<ArgumentOutOfRangeException>();
    }

    private static PartialPrefab CreatePrefab(ushort id, string name, IEnumerable<PartialPrefabSegment> segments)
        => new PartialPrefab(id, name, PrefabType.Normal, segments);

    private static PartialPrefab CreatePrefab(ushort id, string name, int segmentCount)
        => CreatePrefab(id, name, CreateSegments(id, segmentCount));

    private static IEnumerable<PartialPrefabSegment> CreateSegments(ushort id, int count)
    {
        Debug.Assert(count < 4 * 4 * 4);

        int c = 0;
        for (int z = 0; z < 4; z++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    yield return new PartialPrefabSegment(id, new int3(x, y, z));
                    if (++c >= count)
                    {
                        yield break;
                    }
                }
            }
        }
    }
}
