﻿using BitcoderCZ.Fancade.Partial;
using BitcoderCZ.Maths.Vectors;
using TUnit.Assertions.AssertConditions.Throws;
using static BitcoderCZ.Fancade.Tests.Common.PartialPrefabGenerator;

namespace BitcoderCZ.Fancade.Tests;

public class PartialPrefabTests
{
    [Test]
    public async Task Constructor_NameNotTooLong_DoesNotThrow()
    {
        string name = new string('a', 255);

        PartialPrefab prefab = CreatePrefab(1, 1, name);

        await Assert.That(prefab.Name).IsEqualTo(name);
    }

    [Test]
    public async Task Constructor_NameTooLong_Throws()
    {
        string name = new string('a', 256);

        await Assert.That(() => CreatePrefab(1, 1, name)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Name_ValueNotTooLong_DoesNotThrow()
    {
        string name = new string('a', 255);

        PartialPrefab prefab = CreatePrefab(1, 1, "abc");

        prefab.Name = name;

        await Assert.That(prefab.Name).IsEqualTo(name);
    }

    [Test]
    public async Task Name_ValueTooLong_Throws()
    {
        string name = new string('a', 256);

        PartialPrefab prefab = CreatePrefab(1, 1, "abc");

        await Assert.That(() => prefab.Name = name).Throws<ArgumentException>();
    }

    [Test]
    [Arguments(Prefab.MaxSize - 1, 0, 0)]
    [Arguments(0, Prefab.MaxSize - 1, 0)]
    [Arguments(0, 0, Prefab.MaxSize - 1)]
    public async Task Add_PosInBounds_DoesNotThrow(int x, int y, int z)
    {
        PartialPrefab prefab = CreatePrefab(1, 1);

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
        PartialPrefab prefab = CreatePrefab(1, 1);

        await Assert.That(() => prefab.Add(new PartialPrefabSegment(1, new int3(x, y, z)))).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Remove_ShiftsSegments()
    {
        PartialPrefab prefab = CreatePrefab(1, 2);

        bool removed = prefab.Remove(new int3(0, 0, 0), out _, out var shift);

        await Assert.That(removed).IsTrue();

        await Assert.That(shift).IsEqualTo(new int3(1, 0, 0));

        using (Assert.Multiple())
        {
            await Assert.That(prefab.Size).IsEqualTo(int3.One);
            await Assert.That(prefab.ContainsKey(new int3(0, 0, 0))).IsTrue();
            await Assert.That(prefab.ContainsKey(new int3(1, 0, 0))).IsFalse();
            await Assert.That(prefab[new int3(0, 0, 0)].PosInPrefab).IsEqualTo(new int3(0, 0, 0));
        }
    }
}
