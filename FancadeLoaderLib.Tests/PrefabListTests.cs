using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Tests;

[TestFixture]
public class PrefabListTests
{
	[Test]
	public void Constructor_WithCapacity_InitializesEmptyLists()
	{
		var prefabList = new PrefabList(10, 20);

		Assert.That(prefabList.GroupCount, Is.EqualTo(0));
		Assert.That(prefabList.PrefabCount, Is.EqualTo(0));
	}

	[Test]
	public void Constructor_WithCollection_InitializesGroupsAndPrefabs()
	{
		var group1Prefabs = CreateDummyPrefabs(0, 2).ToList();
		var group1 = CreateDummyPrefabGroup(0, group1Prefabs);

		var group2Prefabs = CreateDummyPrefabs(2, 3).ToList();
		var group2 = CreateDummyPrefabGroup(2, group2Prefabs);

		var prefabList = new PrefabList([group1, group2]);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(prefabList.GroupCount, Is.EqualTo(2));
			Assert.That(prefabList.PrefabCount, Is.EqualTo(5));
		}

		using (Assert.EnterMultipleScope())
		{
			Assert.That(prefabList.Groups.ToList(), Is.EqualTo(new[] { group1, group2 }).AsCollection);
			Assert.That(prefabList.Prefabs.ToList(), Is.EqualTo(group1Prefabs.Concat(group2Prefabs).ToList()).AsCollection);
		}
	}

	[Test]
	public void Constructor_WithInvalidCollection_ThrowsException()
	{
		using (Assert.EnterMultipleScope())
		{
			Assert.Throws<ArgumentException>(() => new PrefabList([CreateDummyPrefabGroup(0, 2), CreateDummyPrefabGroup(3, 1)]));
			Assert.Throws<ArgumentException>(() => new PrefabList([CreateDummyPrefabGroup(0, 3), CreateDummyPrefabGroup(1, 1)]));
		}
	}

	[Test]
	public void AddGroup_AppendsGroupAndUpdatesPrefabList()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var groupPrefabs = CreateDummyPrefabs(0, 3).ToList();
		var group = CreateDummyPrefabGroup(0, groupPrefabs);
		prefabList.AddGroup(group);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(prefabList.GroupCount, Is.EqualTo(1));
			Assert.That(prefabList.PrefabCount, Is.EqualTo(3));
		}

		using (Assert.EnterMultipleScope())
		{
			Assert.That(prefabList.GetGroup(0), Is.EqualTo(group));
			Assert.That(prefabList.GetPrefab(0), Is.EqualTo(groupPrefabs[0]));
		}
	}

	[Test]
	public void InsertGroup_LastGroupCondition_WorksLikeAddGroup()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group1 = CreateDummyPrefabGroup(0, 1);
		prefabList.AddGroup(group1);

		var group2 = CreateDummyPrefabGroup(1, 1);
		prefabList.InsertGroup(group2);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(prefabList.GroupCount, Is.EqualTo(2));
			Assert.That(prefabList.PrefabCount, Is.EqualTo(2));
		}

		Assert.That(prefabList.GetGroup(1), Is.EqualTo(group2));
	}

	[Test]
	public void InsertGroup_InsertsGroupAtCorrectPosition()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group1 = CreateDummyPrefabGroup(0, 1);
		var group3 = CreateDummyPrefabGroup(1, 1);
		prefabList.AddGroup(group1);
		prefabList.AddGroup(group3);

		var group2 = CreateDummyPrefabGroup(1, 1);
		prefabList.InsertGroup(group2);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(prefabList.GroupCount, Is.EqualTo(3));
			Assert.That(prefabList.PrefabCount, Is.EqualTo(3));
		}

		using (Assert.EnterMultipleScope())
		{
			Assert.That(prefabList.GetGroup(0), Is.EqualTo(group1));
			Assert.That(prefabList.GetGroup(1), Is.EqualTo(group2));
			Assert.That(prefabList.GetGroup(2), Is.EqualTo(group3));
		}

		using (Assert.EnterMultipleScope())
		{
			Assert.That(group1.Id, Is.EqualTo(0));
			Assert.That(group2.Id, Is.EqualTo(1));
			Assert.That(group3.Id, Is.EqualTo(2));
		}
	}

	[Test]
	public void InsertGroup_ShiftsBlockIds()
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

		using (Assert.EnterMultipleScope())
		{
			Assert.That(blocks.GetBlock(new int3(0, 0, 0)), Is.EqualTo(0));
			Assert.That(blocks.GetBlock(new int3(1, 0, 0)), Is.EqualTo(1));
			Assert.That(blocks.GetBlock(new int3(0, 0, 2)), Is.EqualTo(2));
			Assert.That(blocks.GetBlock(new int3(1, 0, 2)), Is.EqualTo(3));
		}

		var group2 = CreateDummyPrefabGroup(2, 2);
		prefabList.InsertGroup(group2);

		blocks.SetGroup(new int3(0, 0, 1), group2);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(prefabList.GroupCount, Is.EqualTo(3));
			Assert.That(prefabList.PrefabCount, Is.EqualTo(6));
		}

		using (Assert.EnterMultipleScope())
		{
			Assert.That(blocks.GetBlock(new int3(0, 0, 0)), Is.EqualTo(0));
			Assert.That(blocks.GetBlock(new int3(1, 0, 0)), Is.EqualTo(1));
			Assert.That(blocks.GetBlock(new int3(0, 0, 1)), Is.EqualTo(2));
			Assert.That(blocks.GetBlock(new int3(1, 0, 1)), Is.EqualTo(3));
			Assert.That(blocks.GetBlock(new int3(0, 0, 2)), Is.EqualTo(4));
			Assert.That(blocks.GetBlock(new int3(1, 0, 2)), Is.EqualTo(5));
		}
	}

	[Test]
	public void RemoveGroup_RemovesGroupAndUpdatesPrefabList()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group1 = CreateDummyPrefabGroup(0, 2);
		prefabList.AddGroup(group1);

		var group2 = CreateDummyPrefabGroup(2, 1);
		prefabList.AddGroup(group2);

		bool removed = prefabList.RemoveGroup(0);

		Assert.That(removed, Is.True);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(prefabList.GroupCount, Is.EqualTo(1));
			Assert.That(prefabList.PrefabCount, Is.EqualTo(1));
		}

		Assert.Throws<KeyNotFoundException>(() => prefabList.GetGroup(2));
		Assert.That(group2.Id, Is.EqualTo(0));
	}

	[Test]
	public void RemoveGroup_RemovesAndUpdatesBlockIds()
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

		Assert.That(removed, Is.True);

		Assert.That(group2.Id, Is.EqualTo(1));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(blocks.GetBlock(new int3(0, 0, 0)), Is.EqualTo(0));
			Assert.That(blocks.GetBlock(new int3(1, 0, 0)), Is.EqualTo(0));
			Assert.That(blocks.GetBlock(new int3(0, 0, 1)), Is.EqualTo(1));
		}
	}

	[Test]
	public void AddPrefabToGroup_AppendsPrefabAndUpdatesGroupAndPrefabList()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group = CreateDummyPrefabGroup(0, 1);
		prefabList.AddGroup(group);

		var newPrefab = new Prefab(0, new byte3(1, 0, 0));
		prefabList.AddPrefabToGroup(0, newPrefab, false);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(prefabList.GroupCount, Is.EqualTo(1));
			Assert.That(prefabList.PrefabCount, Is.EqualTo(2));
		}

		Assert.That(prefabList.GetPrefab(1), Is.EqualTo(newPrefab));
	}

	[Test]
	public void AddPrefabToGroup_UpdatesIdsAndShiftBlockIds()
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

		using (Assert.EnterMultipleScope())
		{
			Assert.That(prefabList.GroupCount, Is.EqualTo(2));
			Assert.That(prefabList.PrefabCount, Is.EqualTo(4));
		}

		using (Assert.EnterMultipleScope())
		{
			Assert.That(group1.Id, Is.EqualTo(0));
			Assert.That(group2.Id, Is.EqualTo(2));
		}

		using (Assert.EnterMultipleScope())
		{
			Assert.That(blocks.GetBlock(new int3(0, 0, 0)), Is.EqualTo(0));
			Assert.That(blocks.GetBlock(new int3(1, 0, 0)), Is.EqualTo(1));
			Assert.That(blocks.GetBlock(new int3(0, 0, 1)), Is.EqualTo(2));
			Assert.That(blocks.GetBlock(new int3(1, 0, 1)), Is.EqualTo(3));
		}
	}

	[Test]
	public void AddPrefabToGroup_WithObstruction_ThrowsException()
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

		using (Assert.EnterMultipleScope())
		{
			Assert.That(group.Count, Is.EqualTo(1));
			Assert.That(blocks.GetBlock(new int3(0, 0, 0)), Is.EqualTo(1));
			Assert.That(blocks.GetBlock(new int3(1, 0, 0)), Is.EqualTo(5));
		}
	}

	[Test]
	public void AddPrefabToGroup_WithObstruction_OverwriteTrue_DoesNotThrow()
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

		using (Assert.EnterMultipleScope())
		{
			Assert.That(group.Count, Is.EqualTo(2));
			Assert.That(blocks.GetBlock(new int3(0, 0, 0)), Is.EqualTo(1));
			Assert.That(blocks.GetBlock(new int3(1, 0, 0)), Is.EqualTo(2));
		}
	}

	[Test]
	public void TryAddPrefabToGroup_WithObstruction_ReturnsFalse()
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

		Assert.That(added, Is.False);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(group.Count, Is.EqualTo(1));
			Assert.That(blocks.GetBlock(new int3(0, 0, 0)), Is.EqualTo(1));
			Assert.That(blocks.GetBlock(new int3(1, 0, 0)), Is.EqualTo(5));
		}
	}

	[Test]
	public void TryAddPrefabToGroup_WithObstruction_OverwriteTrue_ReturnsTrue()
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

		Assert.That(added, Is.True);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(group.Count, Is.EqualTo(2));
			Assert.That(blocks.GetBlock(new int3(0, 0, 0)), Is.EqualTo(1));
			Assert.That(blocks.GetBlock(new int3(1, 0, 0)), Is.EqualTo(2));
		}
	}

	[Test]
	public void RemovePrefabFromGroup_ShouldRemovePrefabAndUpdatePrefabList()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group = CreateDummyPrefabGroup(0, 2);
		prefabList.AddGroup(group);

		bool removed = prefabList.RemovePrefabFromGroup(0, new byte3(1, 0, 0));

		Assert.That(removed, Is.True);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(group.Count, Is.EqualTo(1));
			Assert.That(prefabList.PrefabCount, Is.EqualTo(1));
			Assert.That(prefabList.GetPrefab(0), Is.EqualTo(group[byte3.Zero]));
		}
	}

	[Test]
	public void RemovePrefabFromGroup_RemoveOrigin_ReturnsFalse()
	{
		var prefabList = new PrefabList();
		prefabList.IdOffset = 0;

		var group = CreateDummyPrefabGroup(0, 2);
		prefabList.AddGroup(group);

		bool removed = prefabList.RemovePrefabFromGroup(0, new byte3(0, 0, 0));

		Assert.That(removed, Is.False);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(group.Count, Is.EqualTo(2));
			Assert.That(prefabList.PrefabCount, Is.EqualTo(2));
		}
	}

	[Test]
	public void RemovePrefabFromGroup_UpdatesIdsAndShiftBlockIds()
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

		Assert.That(removed, Is.True);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(group1.Id, Is.EqualTo(1));
			Assert.That(group2.Id, Is.EqualTo(2));

			Assert.That(group1.Count, Is.EqualTo(1));
			Assert.That(group2.Count, Is.EqualTo(2));
		}

		using (Assert.EnterMultipleScope())
		{
			Assert.That(blocks.GetBlock(new int3(0, 0, 0)), Is.EqualTo(1));
			Assert.That(blocks.GetBlock(new int3(1, 0, 0)), Is.EqualTo(0));
			Assert.That(blocks.GetBlock(new int3(0, 0, 1)), Is.EqualTo(2));
			Assert.That(blocks.GetBlock(new int3(1, 0, 1)), Is.EqualTo(3));
		}
	}

	[Test]
	public void SaveLoad_ShouldPersistAndRestoreData()
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(loadedPrefabList.GroupCount, Is.EqualTo(prefabList.GroupCount));
				Assert.That(loadedPrefabList.PrefabCount, Is.EqualTo(prefabList.PrefabCount));
				Assert.That(loadedPrefabList.IdOffset, Is.EqualTo(prefabList.IdOffset));
			}

			Assert.That(loadedPrefabList.Groups, Is.EquivalentTo(prefabList.Groups).Using(new PrefabGroupComparer()));
			Assert.That(loadedPrefabList.Prefabs, Is.EquivalentTo(prefabList.Prefabs).Using(new PrefabComparer()));
		}
	}

	private static PrefabGroup CreateDummyPrefabGroup(ushort id, IEnumerable<Prefab> prefabs)
		=> new PrefabGroup(id, string.Empty, PrefabCollider.Box, PrefabType.Normal, FcColorUtils.DefaultBackgroundColor, true, null, null, null, prefabs);

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
