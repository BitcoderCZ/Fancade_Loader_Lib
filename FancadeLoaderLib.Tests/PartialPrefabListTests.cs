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
			IdOffset = 0,
		};

		var segments = CreateDummySegments(0, 3).ToList();
		var prefab = CreateDummyPrefab(0, segments);
		prefabList.AddPrefab(prefab);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.PrefabCount).IsEqualTo(1);
			await Assert.That(prefabList.SegmentCount).IsEqualTo(3);
		}

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GetPrefab(0)).IsEqualTo(prefab);
			await Assert.That(prefabList.GetSegment(0)).IsEqualTo(segments[0]);
		}
	}

	[Test]
	public async Task InsertPrefab_LastPrefabCondition_WorksLikeAddPrefab()
	{
		var prefabList = new PartialPrefabList()
		{
			IdOffset = 0,
		};

		var prefab1 = CreateDummyPrefab(0, 1);
		var prefab2 = CreateDummyPrefab(1, 1);

		prefabList.AddPrefab(prefab1);
		prefabList.InsertPrefab(prefab2);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
			await Assert.That(prefabList.SegmentCount).IsEqualTo(2);
		}

		await Assert.That(prefabList.GetPrefab(1)).IsEqualTo(prefab2);
	}

	[Test]
	public async Task InsertPrefab_InsertsPrefabAtCorrectPosition()
	{
		var prefabList = new PartialPrefabList()
		{
			IdOffset = 0,
		};

		var prefab1 = CreateDummyPrefab(0, 1);
		var prefab3 = CreateDummyPrefab(1, 1);
		prefabList.AddPrefab(prefab1);
		prefabList.AddPrefab(prefab3);

		var prefab2 = CreateDummyPrefab(1, 1);
		prefabList.InsertPrefab(prefab2);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.PrefabCount).IsEqualTo(3);
			await Assert.That(prefabList.SegmentCount).IsEqualTo(3);
		}

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.GetPrefab(0)).IsEqualTo(prefab1);
			await Assert.That(prefabList.GetPrefab(1)).IsEqualTo(prefab2);
			await Assert.That(prefabList.GetPrefab(2)).IsEqualTo(prefab3);
		}

		using (Assert.Multiple())
		{
			await Assert.That(prefab1.Id).IsEqualTo((ushort)0);
			await Assert.That(prefab2.Id).IsEqualTo((ushort)1);
			await Assert.That(prefab3.Id).IsEqualTo((ushort)2);
		}
	}

	[Test]
	public async Task RemovePrefab_RemovesPrefabAndSegments()
	{
		var prefabList = new PartialPrefabList()
		{
			IdOffset = 0,
		};

		var prefab1 = CreateDummyPrefab(0, 2);
		var prefab2 = CreateDummyPrefab(2, 1);

		prefabList.AddPrefab(prefab1);
		prefabList.AddPrefab(prefab2);

		bool removed = prefabList.RemovePrefab(0);

		await Assert.That(removed).IsTrue();

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.PrefabCount).IsEqualTo(1);
			await Assert.That(prefabList.SegmentCount).IsEqualTo(1);
		}

		await Assert.That(() => prefabList.GetPrefab(2)).Throws<KeyNotFoundException>();
		await Assert.That(prefab2.Id).IsEqualTo((ushort)0);
	}

	[Test]
	public async Task AddSegmentToPrefab_AppendsPrefabAndSegments()
	{
		var prefabList = new PartialPrefabList()
		{
			IdOffset = 0,
		};

		var prefab = CreateDummyPrefab(0, 1);
		prefabList.AddPrefab(prefab);

		var newSegment = new PartialPrefabSegment(0, new byte3(1, 0, 0));
		prefabList.AddSegmentToPrefab(0, newSegment);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.PrefabCount).IsEqualTo(1);
			await Assert.That(prefabList.SegmentCount).IsEqualTo(2);
		}

		await Assert.That(prefabList.GetSegment(1)).IsEqualTo(newSegment);
	}

	[Test]
	public async Task AddSegmentToPrefab_UpdatesIds()
	{
		var prefabList = new PartialPrefabList()
		{
			IdOffset = 0,
		};

		var prefab1 = CreateDummyPrefab(0, 1);
		var prefab2 = CreateDummyPrefab(1, 2);

		prefabList.AddPrefab(prefab1);
		prefabList.AddPrefab(prefab2);

		var newSegment = new PartialPrefabSegment(0, new byte3(1, 0, 0));
		prefabList.AddSegmentToPrefab(0, newSegment);

		using (Assert.Multiple())
		{
			await Assert.That(prefabList.PrefabCount).IsEqualTo(2);
			await Assert.That(prefabList.SegmentCount).IsEqualTo(4);
		}

		using (Assert.Multiple())
		{
			await Assert.That(prefab1.Id).IsEqualTo((ushort)0);
			await Assert.That(prefab2.Id).IsEqualTo((ushort)2);
		}
	}

	[Test]
	public async Task RemoveSegmentFromPrefab_RemovesSegmentsAndUpdatesPrefab()
	{
		var prefabList = new PartialPrefabList()
		{
			IdOffset = 0,
		};

		var prefab = CreateDummyPrefab(0, 2);
		prefabList.AddPrefab(prefab);

		bool removed = prefabList.RemoveSegmentFromPrefab(0, new byte3(1, 0, 0));

		await Assert.That(removed).IsTrue();

		using (Assert.Multiple())
		{
			await Assert.That(prefab.Count).IsEqualTo(1);
			await Assert.That(prefabList.SegmentCount).IsEqualTo(1);
			await Assert.That(prefabList.GetSegment(0)).IsEqualTo(prefab[byte3.Zero]);
		}
	}

	[Test]
	public async Task RemoveSegmentFromPrefab_RemoveOrigin_ReturnsFalse()
	{
		var prefabList = new PartialPrefabList()
		{
			IdOffset = 0,
		};

		var prefab = CreateDummyPrefab(0, 2);
		prefabList.AddPrefab(prefab);

		bool removed = prefabList.RemoveSegmentFromPrefab(0, new byte3(0, 0, 0));

		await Assert.That(removed).IsFalse();

		using (Assert.Multiple())
		{
			await Assert.That(prefab.Count).IsEqualTo(2);
			await Assert.That(prefabList.SegmentCount).IsEqualTo(2);
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
