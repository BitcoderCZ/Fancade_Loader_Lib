using BitcoderCZ.Fancade.Tests.Common;
using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;
using static BitcoderCZ.Fancade.Tests.Common.PrefabGenerator;

namespace BitcoderCZ.Fancade.Editing.Tests;

public class PrefabListUtilsTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemoveEmptySegmentsFromPrefab_DoesNotModifyNonEmptySegments(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, 4, initVoxels: true);

        prefabList.AddPrefab(prefab);

        foreach (var segment in prefab.Values)
        {
            Debug.Assert(segment.Voxels is not null);
            segment.Voxels[0] = new Voxel(FcColor.Blue, false);
        }

        var prefabClone = prefab.Clone(true);

        prefabList.RemoveEmptySegmentsFromPrefab(prefab.Id, cache: cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(prefab).IsEqualTo(prefabClone, PrefabComparer.Instance);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemovesEmptySegments_RemovesEmptyVoxels(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, [new int3(0, 0, 0), new int3(1, 0, 0), new int3(0, 0, 1), new int3(1, 0, 1)], initVoxels: true);

        prefabList.AddPrefab(prefab);

        prefabList.RemoveEmptySegmentsFromPrefab(prefab.Id, cache: cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(prefab.Size).IsEqualTo(int3.One);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RemoveEmptySegments_KeepsNonEmptySegments(bool cache)
    {
        var prefabList = new PrefabList()
        {
            IdOffset = 1,
        };

        var prefab = CreatePrefab(1, [new int3(0, 0, 0), new int3(1, 0, 0), new int3(0, 0, 1), new int3(1, 0, 1), new int3(0, 0, 2), new int3(1, 0, 2)], initVoxels: true);

        var seg1 = prefab[new int3(0, 0, 2)];
        seg1.Voxels![0] = new Voxel(FcColor.Blue, false);

        var seg1Clone = seg1.Clone();

        var seg2 = prefab[new int3(1, 0, 2)];
        seg2.Voxels![1] = new Voxel(FcColor.Green, true);

        var seg2Clone = seg2.Clone();

        prefabList.AddPrefab(prefab);

        prefabList.RemoveEmptySegmentsFromPrefab(prefab.Id, cache: cache ? new BlockInstancesCache(prefabList.Prefabs, 1) : null);

        await Assert.That(prefab.Size).IsEqualTo(new int3(2, 1, 1));

        using (Assert.Multiple())
        {
            await Assert.That(seg1.PosInPrefab).IsEqualTo(new int3(0, 0, 0));
            await Assert.That(seg2.PosInPrefab).IsEqualTo(new int3(1, 0, 0));
        }

        using (Assert.Multiple())
        {
            await Assert.That(seg1.Voxels).IsEquivalentTo(seg1Clone.Voxels);
            await Assert.That(seg2.Voxels).IsEquivalentTo(seg2Clone.Voxels);
        }
    }
}
