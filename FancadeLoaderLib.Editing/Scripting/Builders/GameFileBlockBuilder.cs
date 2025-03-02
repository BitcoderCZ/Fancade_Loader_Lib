// <copyright file="GameFileBlockBuilder.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace FancadeLoaderLib.Editing.Scripting.Builders;

public sealed class GameFileBlockBuilder : BlockBuilder
{
	public readonly Game? InGame;

	public readonly string? PrefabName;
	public readonly PrefabType? PrefabType;

	public readonly ushort? PrefabIndex;

	public GameFileBlockBuilder(Game? inGame, string prefabName, PrefabType prefabType)
	{
		InGame = inGame;

		CreateNewPrefab = true;

		PrefabName = prefabName;
		PrefabType = prefabType;
	}

	public GameFileBlockBuilder(Game inGame, ushort prefabIndex)
	{
		if (inGame is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(inGame));
		}

		InGame = inGame;

		CreateNewPrefab = false;

		PrefabIndex = prefabIndex;
	}

	[MemberNotNullWhen(true, nameof(PrefabName), nameof(PrefabType))]
	[MemberNotNullWhen(false, nameof(PrefabIndex))]
	public bool CreateNewPrefab { get; private set; }

#if NET5_0_OR_GREATER
	public override Game Build(int3 buildPos)
#else
	public override object Build(int3 buildPos)
#endif
	{
		Game game = InGame ?? new Game("FanScript");

		Prefab prefab;
		if (CreateNewPrefab)
		{
			if (PrefabType == FancadeLoaderLib.PrefabType.Level)
			{
				prefab = Prefab.CreateLevel(PrefabName);

				int index = 0;

				while (index < game.Prefabs.Count && game.Prefabs[index].Type == FancadeLoaderLib.PrefabType.Level)
				{
					index++;
				}

				game.Prefabs.Insert(index, prefab);
			}
			else
			{
				prefab = Prefab.CreateBlock(PrefabName);
				prefab.Type = (PrefabType)PrefabType;
				prefab.Voxels = BlockVoxelsGenerator.CreateScript(int2.One).First().Value;

				game.Prefabs.Add(prefab);
			}
		}
		else
		{
			prefab = game.Prefabs[(int)PrefabIndex];
		}

		Block[] blocks = PreBuild(buildPos, false);

		PartialPrefabList stockPrefabs = StockBlocks.PrefabList;

		Dictionary<ushort, PartialPrefabGroup> groupCache = [];

		for (int i = 0; i < blocks.Length; i++)
		{
			Block block = blocks[i];
			if (block.Type.IsGroup)
			{
				if (!groupCache.TryGetValue(block.Type.Prefab.Id, out var group))
				{
					group = stockPrefabs.GetGroup(block.Type.Prefab.Id);
					groupCache.Add(block.Type.Prefab.Id, group);
				}

				prefab.Blocks.SetGroup(block.Position, group);
			}
			else
			{
				prefab.Blocks.SetBlock(block.Position, block.Type.Prefab.Id);
			}
		}

		for (int i = 0; i < settings.Count; i++)
		{
			SettingRecord set = settings[i];
			prefab.Settings.Add(new PrefabSetting()
			{
				Index = (byte)set.ValueIndex,
				Type = set.Value switch
				{
					byte => SettingType.Byte,
					ushort => SettingType.Ushort,
					float => SettingType.Float,
					float3 => SettingType.Vec3,
					Rotation => SettingType.Vec3,
					string => SettingType.String,
					_ => throw new InvalidDataException($"Unsupported type of value: '{set.Value.GetType()}'."),
				},
				Position = (ushort3)set.Block.Position,
				Value = set.Value is Rotation rot ? rot.Value : set.Value,
			});
		}

		for (int i = 0; i < connections.Count; i++)
		{
			ConnectionRecord con = connections[i];
			prefab.Connections.Add(new Connection()
			{
				From = (ushort3)con.From.BlockPosition,
				FromVoxel = (ushort3)(con.From.VoxelPosition ?? ChooseSubPos(con.From.BlockPosition)),
				To = (ushort3)con.To.BlockPosition,
				ToVoxel = (ushort3)(con.To.VoxelPosition ?? ChooseSubPos(con.To.BlockPosition)),
			});
		}

		return game;
	}
}
