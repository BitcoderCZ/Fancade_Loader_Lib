using MathUtils.Vectors;
using System.Diagnostics;
using TUnit.Assertions.AssertConditions.Throws;

namespace FancadeLoaderLib.Tests;

public class PrefabListTests
{
	[Test]
	public async Task Constructor_WithCapacity_InitializesEmptyLists()
	{
		var prefabList = new PrefabList(10, 20);

		await Assert.That(prefabList.GroupCount).IsEqualTo(0);
		await Assert.That(prefabList.PrefabCount).IsEqualTo(0);
	}

	[Test]
	public async Task Constructor_WithCollection_InitializesGroupsAndPrefabs()
	{
		var group1Prefabs = CreateDummyPrefabs(0, 2).ToList();
		var group1 = CreateDummyPrefabGroup(0, group1Prefabs);

		var group2Prefabs = CreateDummyPrefabs(2, 3).ToList();
		var group2 = CreateDummyPrefabGroup(2, group2Prefabs);

		var prefabList = new PrefabList([group1, group2]);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GroupCount).IsEqualTo(2);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(5);
		}

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.Groups).IsEquivalentTo([group1, group2], new PrefabGroupComparer());
			await Assert.That(prefabList.Prefabs).IsEquivalentTo(group1Prefabs.Concat(group2Prefabs));
		}
	}

	[Test]
	public async Task Constructor_WithInvalidCollection_ThrowsException()
	{
		using (Assert.Multiple())
		{
			await Assert.That(() => new PrefabList([CreateDummyPrefabGroup(0, 2), CreateDummyPrefabGroup(3, 1)])).Throws<ArgumentException>();
			await Assert.That(() => new PrefabList([CreateDummyPrefabGroup(0, 3), CreateDummyPrefabGroup(1, 1)])).Throws<ArgumentException>();
		}
	}

	[Test]
	public async Task AddGroup_AppendsGroupAndUpdatesPrefabList()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var groupPrefabs = CreateDummyPrefabs(0, 3).ToList();
		var group = CreateDummyPrefabGroup(0, groupPrefabs);
		prefabList.AddGroup(group);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GroupCount).IsEqualTo(1);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(3);
		}

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GetGroup(0)).IsEqualTo(group);
			await Assert.That(prefabList.GetPrefab(0)).IsEqualTo(groupPrefabs[0]);
		}
	}

	[Test]
	public async Task InsertGroup_LastGroupCondition_WorksLikeAddGroup()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group1 = CreateDummyPrefabGroup(0, 1);
		prefabList.AddGroup(group1);

		var group2 = CreateDummyPrefabGroup(1, 1);
		prefabList.InsertGroup(group2);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GroupCount).IsEqualTo(2);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
		}

		await Assert.That(prefabList.GetGroup(1)).IsEqualTo(group2);
	}

	[Test]
	public async Task InsertGroup_InsertsGroupAtCorrectPosition()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group1 = CreateDummyPrefabGroup(0, 1);
		var group3 = CreateDummyPrefabGroup(1, 1);
		prefabList.AddGroup(group1);
		prefabList.AddGroup(group3);

		var group2 = CreateDummyPrefabGroup(1, 1);
		prefabList.InsertGroup(group2);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GroupCount).IsEqualTo(3);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(3);
		}

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GetGroup(0)).IsEqualTo(group1);
			await Assert.That(prefabList.GetGroup(1)).IsEqualTo(group2);
			await Assert.That(prefabList.GetGroup(2)).IsEqualTo(group3);
		}

		using (Assert.Multiple())
		{
			await Assert.That(group1.Id).IsEqualTo((ushort)0);
			await Assert.That(group2.Id).IsEqualTo((ushort)1);
			await Assert.That(group3.Id).IsEqualTo((ushort)2);
		}
	}

	[Test]
	public async Task InsertGroup_ShiftsBlockIds()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group1 = CreateDummyPrefabGroup(0, 2);
		var group3 = CreateDummyPrefabGroup(2, 2);
		var blocks = group1.Blocks;
		blocks.SetGroup(new int3(0, 0, 0), group1);
		blocks.SetGroup(new int3(0, 0, 2), group3);
		prefabList.AddGroup(group1);
		prefabList.AddGroup(group3);

		using (Assert.Multiple())
		{
			await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)0);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)1);
			await Assert.That(blocks.GetBlock(new int3(0, 0, 2))).IsEqualTo((ushort)2);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 2))).IsEqualTo((ushort)3);
		}

		var group2 = CreateDummyPrefabGroup(2, 2);
		prefabList.InsertGroup(group2);

		blocks.SetGroup(new int3(0, 0, 1), group2);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GroupCount).IsEqualTo(3);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(6);
		}

		using (Assert.Multiple())
		{
			await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)0);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)1);
			await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)2);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 1))).IsEqualTo((ushort)3);
			await Assert.That(blocks.GetBlock(new int3(0, 0, 2))).IsEqualTo((ushort)4);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 2))).IsEqualTo((ushort)5);
		}
	}

	[Test]
	public async Task RemoveGroup_RemovesGroupAndUpdatesPrefabList()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group1 = CreateDummyPrefabGroup(0, 2);
		prefabList.AddGroup(group1);

		var group2 = CreateDummyPrefabGroup(2, 1);
		prefabList.AddGroup(group2);

		bool removed = prefabList.RemoveGroup(0);

		await Assert.That(removed).IsTrue();

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GroupCount).IsEqualTo(1);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(1);
		}

		await Assert.That(() => prefabList.GetGroup(2)).Throws<KeyNotFoundException>();
		await Assert.That(group2.Id).IsEqualTo((ushort)0);
	}

	[Test]
	public async Task RemoveGroup_RemovesAndUpdatesBlockIds()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 1;

		var group1 = CreateDummyPrefabGroup(1, 2);
		prefabList.AddGroup(group1);

		var group2 = CreateDummyPrefabGroup(3, 1);
		var blocks = group2.Blocks;
		blocks.SetGroup(new int3(0, 0, 0), group1);
		blocks.SetGroup(new int3(0, 0, 1), group2);
		prefabList.AddGroup(group2);

		bool removed = prefabList.RemoveGroup(1);

		await Assert.That(removed).IsTrue();

		await Assert.That(group2.Id).IsEqualTo((ushort)1);

		using (Assert.Multiple())
		{
			await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)0);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)0);
			await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)1);
		}
	}

	[Test]
	public async Task AddPrefabToGroup_AppendsPrefabAndUpdatesGroupAndPrefabList()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group = CreateDummyPrefabGroup(0, 1);
		prefabList.AddGroup(group);

		var newPrefab = new Prefab(0, new byte3(1, 0, 0));
		prefabList.AddPrefabToGroup(0, newPrefab, false);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GroupCount).IsEqualTo(1);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
		}

		await Assert.That(prefabList.GetPrefab(1)).IsEqualTo(newPrefab);
	}

	[Test]
	public async Task AddPrefabToGroup_UpdatesIdsAndShiftBlockIds()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group1 = CreateDummyPrefabGroup(0, 1);
		prefabList.AddGroup(group1);

		var group2 = CreateDummyPrefabGroup(1, 2);
		var blocks = group2.Blocks;
		blocks.SetGroup(new int3(0, 0, 0), group1);
		blocks.SetGroup(new int3(0, 0, 1), group2);
		prefabList.AddGroup(group2);

		var newPrefab = new Prefab(0, new byte3(1, 0, 0));
		prefabList.AddPrefabToGroup(0, newPrefab, false);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GroupCount).IsEqualTo(2);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(4);
		}

		using (Assert.Multiple())
		{
			await Assert.That(group1.Id).IsEqualTo((ushort)0);
			await Assert.That(group2.Id).IsEqualTo((ushort)2);
		}

		using (Assert.Multiple())
		{
			await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)0);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)1);
			await Assert.That(blocks.GetBlock(new int3(0, 0, 1))).IsEqualTo((ushort)2);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 1))).IsEqualTo((ushort)3);
		}
	}

	[Test]
	public async Task AddPrefabToGroup_WithObstruction_ThrowsException()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 1;

		var group = CreateDummyPrefabGroup(1, 1);
		var blocks = group.Blocks;
		blocks.SetGroup(new int3(0, 0, 0), group);
		blocks.SetBlock(new int3(1, 0, 0), 5);
		prefabList.AddGroup(group);

		var newPrefab = new Prefab(1, new byte3(1, 0, 0));
		Assert.Throws<InvalidOperationException>(() => prefabList.AddPrefabToGroup(1, newPrefab, false));

		using (Assert.Multiple())
		{
			await Assert.That(group.Count).IsEqualTo(1);
			await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)5);
		}
	}

	[Test]
	public async Task AddPrefabToGroup_WithObstruction_OverwriteTrue_DoesNotThrow()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 1;

		var group = CreateDummyPrefabGroup(1, 1);
		var blocks = group.Blocks;
		blocks.SetGroup(new int3(0, 0, 0), group);
		blocks.SetBlock(new int3(1, 0, 0), 5);
		prefabList.AddGroup(group);

		var newPrefab = new Prefab(1, new byte3(1, 0, 0));

		prefabList.AddPrefabToGroup(1, newPrefab, true);

		using (Assert.Multiple())
		{
			await Assert.That(group.Count).IsEqualTo(2);
			await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
		}
	}

	[Test]
	public async Task TryAddPrefabToGroup_WithObstruction_ReturnsFalse()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 1;

		var group = CreateDummyPrefabGroup(1, 1);
		var blocks = group.Blocks;
		blocks.SetGroup(new int3(0, 0, 0), group);
		blocks.SetBlock(new int3(1, 0, 0), 5);
		prefabList.AddGroup(group);

		var newPrefab = new Prefab(1, new byte3(1, 0, 0));

		bool added = prefabList.TryAddPrefabToGroup(1, newPrefab, false);

		await Assert.That(added).IsFalse();

		using (Assert.Multiple())
		{
			await Assert.That(group.Count).IsEqualTo(1);
			await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)5);
		}
	}

	[Test]
	public async Task TryAddPrefabToGroup_WithObstruction_OverwriteTrue_ReturnsTrue()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 1;

		var group = CreateDummyPrefabGroup(1, 1);
		var blocks = group.Blocks;
		blocks.SetGroup(new int3(0, 0, 0), group);
		blocks.SetBlock(new int3(1, 0, 0), 5);
		prefabList.AddGroup(group);

		var newPrefab = new Prefab(1, new byte3(1, 0, 0));

		bool added = prefabList.TryAddPrefabToGroup(1, newPrefab, true);

		await Assert.That(added).IsTrue();

		using (Assert.Multiple())
		{
			await Assert.That(group.Count).IsEqualTo(2);
			await Assert.That(blocks.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
			await Assert.That(blocks.GetBlock(new int3(1, 0, 0))).IsEqualTo((ushort)2);
		}
	}

	[Test]
	public async Task RemovePrefabFromGroup_ShouldRemovePrefabAndUpdatePrefabList()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group = CreateDummyPrefabGroup(0, 2);
		prefabList.AddGroup(group);

		bool removed = prefabList.RemovePrefabFromGroup(0, new byte3(1, 0, 0));

		await Assert.That(removed).IsTrue();

		using (Assert.Multiple())
		{
			await Assert.That(group.Count).IsEqualTo(1);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(1);
			await Assert.That(prefabList.GetPrefab(0)).IsEqualTo(group[byte3.Zero]);
		}
	}

	[Test]
	public async Task RemovePrefabFromGroup_RemoveOrigin_ReturnsFalse()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group = CreateDummyPrefabGroup(0, 2);
		prefabList.AddGroup(group);

		bool removed = prefabList.RemovePrefabFromGroup(0, new byte3(0, 0, 0));

		await Assert.That(removed).IsFalse();

		using (Assert.Multiple())
		{
			await Assert.That(group.Count).IsEqualTo(2);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
		}
	}

	[Test]
	public async Task RemovePrefabFromGroup_UpdatesIdsAndShiftBlockIds()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 1;

		var group1 = CreateDummyPrefabGroup(1, 2);
		prefabList.AddGroup(group1);

		var group2 = CreateDummyPrefabGroup(3, 2);
		var blocks = group2.Blocks;
		blocks.SetGroup(new int3(0, 0, 0), group1);
		blocks.SetGroup(new int3(0, 0, 1), group2);
		prefabList.AddGroup(group2);

		bool removed = prefabList.RemovePrefabFromGroup(1, new byte3(1, 0, 0));

		await Assert.That(removed).IsTrue();

		using (Assert.Multiple())
		{
			await Assert.That(group1.Id).IsEqualTo((ushort)1);
			await Assert.That(group2.Id).IsEqualTo((ushort)2);

			await Assert.That(group1.Count).IsEqualTo(1);
			await Assert.That(group2.Count).IsEqualTo(2);
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
	public async Task SaveLoad_ShouldPersistAndRestoreData()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 25;

		var group1 = CreateDummyPrefabGroup(25, 2);
		prefabList.AddGroup(group1);

		var group2 = CreateDummyPrefabGroup(27, 1);
		prefabList.AddGroup(group2);

		var group3 = CreateDummyPrefabGroup(28, 3);
		var prefab = group3[new byte3(1, 0, 0)];
		prefab.Voxels = new Voxel[8 * 8 * 8];
		prefab.Voxels[0].Colors[0] = (byte)FcColor.Brown;
		prefab.Voxels[0].Attribs[0] = true;
		prefabList.AddGroup(group3);

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
				await Assert.That(loadedPrefabList.GroupCount).IsEqualTo(prefabList.GroupCount);
				await Assert.That(loadedPrefabList.PrefabCount).IsEqualTo(prefabList.PrefabCount);
				await Assert.That(loadedPrefabList.IdOffset).IsEqualTo(prefabList.IdOffset);
			}

			await Assert.That(loadedPrefabList.Groups).IsEquivalentTo(prefabList.Groups, new PrefabGroupComparer());
			await Assert.That(loadedPrefabList.Prefabs).IsEquivalentTo(prefabList.Prefabs, new PrefabComparer());
		}
	}

	private static PrefabGroup CreateDummyPrefabGroup(ushort id, IEnumerable<Prefab> prefabs)
		=> new PrefabGroup(id, $"Group {id}", PrefabCollider.Box, PrefabType.Normal, FcColorUtils.DefaultBackgroundColor, true, null, null, null, prefabs);

	private static PrefabGroup CreateDummyPrefabGroup(ushort id, int prefabCount)
		=> CreateDummyPrefabGroup(id, CreateDummyPrefabs(id, prefabCount));

	private static IEnumerable<Prefab> CreateDummyPrefabs(ushort id, int count)
	{
		Debug.Assert(count < 4 * 4 * 4);

		int c = 0;
		for (int z = 0; z < 4; z++)
		{
			for (int y = 0; y < 4; y++)
			{
				for (int x = 0; x < 4; x++)
				{
					yield return new Prefab(id, new byte3(x, y, z));
					if (++c >= count)
					{
						yield break;
					}
				}
			}
		}
	}
}
