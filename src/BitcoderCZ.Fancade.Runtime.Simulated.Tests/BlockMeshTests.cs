using BitcoderCZ.Maths.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BitcoderCZ.Fancade.Tests.Common.PrefabGenerator;

namespace BitcoderCZ.Fancade.Runtime.Simulated.Tests;

public class BlockMeshTests
{
    [Test]
    public async Task BlocksDoNotCombine1()
    {
        var level = Prefab.CreateLevel(0, "A");
        var prefabGlue = CreatePrefabGlue(0);
        var prefabNotGlue = CreatePrefabNotGlue(0);

        var prefabs = new PrefabList();
        prefabs.AddPrefab(level);
        prefabs.AddPrefab(prefabGlue);
        prefabs.AddPrefab(prefabNotGlue);

        level.Blocks.SetPrefab(new int3(0, 0, 0), prefabGlue);
        level.Blocks.SetPrefab(new int3(1, 0, 0), prefabNotGlue);

        var blockMesh = GameMeshInfo.Create(prefabs, level.Id).GetBlockMesh(level.Id);
        await Assert.That(blockMesh.MeshCount).IsEqualTo(2);
    }

    [Test]
    public async Task BlocksDoNotCombine2()
    {
        var level = Prefab.CreateLevel(0, "A");
        var prefabGlue = CreatePrefabGlue(0);
        var prefabNotGlue = CreatePrefabNotGlue(0);

        var prefabs = new PrefabList();
        prefabs.AddPrefab(level);
        prefabs.AddPrefab(prefabGlue);
        prefabs.AddPrefab(prefabNotGlue);

        level.Blocks.SetPrefab(new int3(0, 0, 0), prefabNotGlue);
        level.Blocks.SetPrefab(new int3(1, 0, 0), prefabGlue);

        var blockMesh = GameMeshInfo.Create(prefabs, level.Id).GetBlockMesh(level.Id);
        await Assert.That(blockMesh.MeshCount).IsEqualTo(2);
    }

    [Test]
    public async Task BlocksDoNotCombine3()
    {
        var level = Prefab.CreateLevel(0, "A");
        var prefabNotGlue = CreatePrefabNotGlue(0);

        var prefabs = new PrefabList();
        prefabs.AddPrefab(level);
        prefabs.AddPrefab(prefabNotGlue);

        level.Blocks.SetPrefab(new int3(0, 0, 0), prefabNotGlue);
        level.Blocks.SetPrefab(new int3(1, 0, 0), prefabNotGlue);

        var blockMesh = GameMeshInfo.Create(prefabs, level.Id).GetBlockMesh(level.Id);
        await Assert.That(blockMesh.MeshCount).IsEqualTo(2);
    }

    [Test]
    public async Task BlocksDoCombine()
    {
        var level = Prefab.CreateLevel(0, "A");
        var prefabGlue = CreatePrefabGlue(0);

        var prefabs = new PrefabList();
        prefabs.AddPrefab(level);
        prefabs.AddPrefab(prefabGlue);

        level.Blocks.SetPrefab(new int3(0, 0, 0), prefabGlue);
        level.Blocks.SetPrefab(new int3(1, 0, 0), prefabGlue);

        var blockMesh = GameMeshInfo.Create(prefabs, level.Id).GetBlockMesh(level.Id);
        await Assert.That(blockMesh.MeshCount).IsEqualTo(1);
    }

    private static Prefab CreatePrefabGlue(ushort id)
    {
        var prefab = CreatePrefab(id, 1, initVoxels: true);

        var voxels = prefab[int3.Zero].Voxels;
        for (int z = 0; z < Voxels.Size; z++)
        {
            for (int y = 0; y < Voxels.Size; y++)
            {
                for (int x = 0; x < Voxels.Size; x++)
                {
                    voxels[new int3(x, y, z)] = new Voxel(FcColor.Black, false);
                }
            }
        }

        return prefab;
    }

    private Prefab CreatePrefabNotGlue(ushort id)
    {
        var prefab = CreatePrefab(id, 1, initVoxels: true);

        var voxels = prefab[int3.Zero].Voxels;
        for (int z = 0; z < Voxels.Size; z++)
        {
            for (int y = 0; y < Voxels.Size; y++)
            {
                for (int x = 0; x < Voxels.Size; x++)
                {
                    Voxel voxel = new Voxel(FcColor.Black, false);
                    if (x == 7)
                    {
                        voxel.Attribs[0] = true;
                    }else if (x == 0)
                    {
                        voxel.Attribs[1] = true;
                    }

                    if (y == 7)
                    {
                        voxel.Attribs[2] = true;
                    }
                    else if (y == 0)
                    {
                        voxel.Attribs[3] = true;
                    }

                    if (z == 7)
                    {
                        voxel.Attribs[4] = true;
                    }
                    else if (z == 0)
                    {
                        voxel.Attribs[5] = true;
                    }

                    voxels[new int3(x, y, z)] = voxel;
                }
            }
        }

        return prefab;
    }
}
