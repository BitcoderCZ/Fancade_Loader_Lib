using FancadeLoaderLib.Tests.Common;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FancadeLoaderLib.Tests.Common.PrefabGenerator;

namespace FancadeLoaderLib.Editing.Tests;

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
}
