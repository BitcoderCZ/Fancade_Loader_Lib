using FancadeLoaderLib.Partial;
using MathUtils.Vectors;
using System.Diagnostics;
using TUnit.Assertions.AssertConditions.Throws;

namespace FancadeLoaderLib.Tests;

public class PartialPrefabListTests
{
	[Test]
	public async Task Constructor_WithCapacity_InitializesEmptyLists()
	{
		var prefabList = new PartialPrefabList(10, 20);

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

		var prefabList = new PartialPrefabList([group1, group2]);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GroupCount).IsEqualTo(2);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(5);
		}

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.Groups).IsEquivalentTo([group1, group2], new PartialPrefabGroupComparer());
			await Assert.That(prefabList.Prefabs).IsEquivalentTo(group1Prefabs.Concat(group2Prefabs));
		}
	}

	[Test]
	public async Task Constructor_WithInvalidCollection_ThrowsException()
	{
		using (Assert.Multiple())
		{
			await Assert.That(() => new PartialPrefabList([CreateDummyPrefabGroup(0, 2), CreateDummyPrefabGroup(3, 1)])).Throws<ArgumentException>();
			await Assert.That(() => new PartialPrefabList([CreateDummyPrefabGroup(0, 3), CreateDummyPrefabGroup(1, 1)])).Throws<ArgumentException>();
		}
	}

	[Test]
	public async Task AddGroup_AppendsGroupAndUpdatesPartialPrefabList()
	{
		var prefabList = new PartialPrefabList();
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
		var prefabList = new PartialPrefabList();
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
		var prefabList = new PartialPrefabList();
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
	public async Task RemoveGroup_RemovesGroupAndUpdatesPartialPrefabList()
	{
		var prefabList = new PartialPrefabList();
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
	public async Task AddPrefabToGroup_AppendsPrefabAndUpdatesGroupAndPartialPrefabList()
	{
		var prefabList = new PartialPrefabList();
		prefabList.IdOffset = 0;

		var group = CreateDummyPrefabGroup(0, 1);
		prefabList.AddGroup(group);

		var newPrefab = new PartialPrefab(0, new byte3(1, 0, 0));
		prefabList.AddPrefabToGroup(0, newPrefab);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GroupCount).IsEqualTo(1);
			await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
		}

		await Assert.That(prefabList.GetPrefab(1)).IsEqualTo(newPrefab);
	}

	[Test]
	public async Task AddPrefabToGroup_UpdatesIds()
	{
		var prefabList = new PartialPrefabList();
		prefabList.IdOffset = 0;

		var group1 = CreateDummyPrefabGroup(0, 1);
		prefabList.AddGroup(group1);

		var group2 = CreateDummyPrefabGroup(1, 2);
		prefabList.AddGroup(group2);

		var newPrefab = new PartialPrefab(0, new byte3(1, 0, 0));
		prefabList.AddPrefabToGroup(0, newPrefab);

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
	}

	[Test]
	public async Task RemovePrefabFromGroup_ShouldRemovePrefabAndUpdatePartialPrefabList()
	{
		var prefabList = new PartialPrefabList();
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
		var prefabList = new PartialPrefabList();
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
	public async Task RemovePrefabFromGroup_UpdatesIds()
	{
		var prefabList = new PartialPrefabList();
		prefabList.IdOffset = 1;

		var group1 = CreateDummyPrefabGroup(1, 2);
		prefabList.AddGroup(group1);

		var group2 = CreateDummyPrefabGroup(3, 2);
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
	}

	[Test]
	public async Task SaveLoad_ShouldPersistAndRestoreData()
	{
		var prefabList = new PartialPrefabList();
		prefabList.IdOffset = 25;

		var group1 = CreateDummyPrefabGroup(25, 2);
		prefabList.AddGroup(group1);

		var group2 = CreateDummyPrefabGroup(27, 1);
		prefabList.AddGroup(group2);

		var group3 = CreateDummyPrefabGroup(28, 3);
		prefabList.AddGroup(group3);

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
				await Assert.That(loadedPartialPrefabList.GroupCount).IsEqualTo(prefabList.GroupCount);
				await Assert.That(loadedPartialPrefabList.PrefabCount).IsEqualTo(prefabList.PrefabCount);
				await Assert.That(loadedPartialPrefabList.IdOffset).IsEqualTo(prefabList.IdOffset);
			}

			await Assert.That(loadedPartialPrefabList.Groups).IsEquivalentTo(prefabList.Groups, new PartialPrefabGroupComparer());
			await Assert.That(loadedPartialPrefabList.Prefabs).IsEquivalentTo(prefabList.Prefabs, new PartialPrefabComparer());
		}
	}

	private static PartialPrefabGroup CreateDummyPrefabGroup(ushort id, IEnumerable<PartialPrefab> prefabs)
		=> new PartialPrefabGroup(id, $"Group {id}", PrefabType.Normal, prefabs);

	private static PartialPrefabGroup CreateDummyPrefabGroup(ushort id, int prefabCount)
		=> CreateDummyPrefabGroup(id, CreateDummyPrefabs(id, prefabCount));

	private static IEnumerable<PartialPrefab> CreateDummyPrefabs(ushort id, int count)
	{
		Debug.Assert(count < 4 * 4 * 4);

		int c = 0;
		for (int z = 0; z < 4; z++)
		{
			for (int y = 0; y < 4; y++)
			{
				for (int x = 0; x < 4; x++)
				{
					yield return new PartialPrefab(id, new byte3(x, y, z));
					if (++c >= count)
					{
						yield break;
					}
				}
			}
		}
	}
}
