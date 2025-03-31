using FancadeLoaderLib.Partial;
using MathUtils.Vectors;
using System.Diagnostics;
using TUnit.Assertions.AssertConditions.Throws;
using static FancadeLoaderLib.Tests.Common.PartialPrefabGenerator;

namespace FancadeLoaderLib.Tests;

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
}
