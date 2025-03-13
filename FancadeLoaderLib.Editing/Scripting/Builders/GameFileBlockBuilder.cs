// <copyright file="GameFileBlockBuilder.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using FancadeLoaderLib.Raw;
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

	public readonly ushort? GroupId;

	public GameFileBlockBuilder(Game? inGame, string prefabName, PrefabType prefabType)
	{
		InGame = inGame;

		CreateNewPrefab = true;

		PrefabName = prefabName;
		PrefabType = prefabType;
	}

	public GameFileBlockBuilder(Game inGame, ushort groupId)
	{
		if (inGame is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(inGame));
		}

		InGame = inGame;

		CreateNewPrefab = false;

		GroupId = groupId;
	}

	[MemberNotNullWhen(true, nameof(PrefabName), nameof(PrefabType))]
	[MemberNotNullWhen(false, nameof(GroupId))]
	public bool CreateNewPrefab { get; private set; }

#if NET5_0_OR_GREATER
	public override Game Build(int3 buildPos)
#else
	public override object Build(int3 buildPos)
#endif
	{
		Game game = InGame ?? new Game("FanScript");

		PrefabGroup group;
		if (CreateNewPrefab)
		{
			if (PrefabType == FancadeLoaderLib.PrefabType.Level)
			{
				ushort id = RawGame.CurrentNumbStockPrefabs;

				while (id < game.Prefabs.PrefabCount - RawGame.CurrentNumbStockPrefabs && game.Prefabs.TryGetGroup(id, out var item) && item.Type == FancadeLoaderLib.PrefabType.Level)
				{
					id += (ushort)item.Count;
				}

				group = PrefabGroup.CreateLevel(id, PrefabName);
				game.Prefabs.InsertGroup(group);
			}
			else
			{
				group = PrefabGroup.CreateBlock((ushort)(game.Prefabs.PrefabCount + RawGame.CurrentNumbStockPrefabs), PrefabName);
				group.Type = (PrefabType)PrefabType;
				group[byte3.Zero].Voxels = BlockVoxelsGenerator.CreateScript(int2.One).First().Value;

				game.Prefabs.AddGroup(group);
			}
		}
		else
		{
			group = game.Prefabs.GetGroup((ushort)GroupId);
		}

		Block[] blocks = PreBuild(buildPos, false);

		PartialPrefabList stockPrefabs = StockBlocks.PrefabList;

		Dictionary<ushort, PartialPrefabGroup> groupCache = [];

		for (int i = 0; i < blocks.Length; i++)
		{
			Block block = blocks[i];
			if (block.Type.IsGroup)
			{
				if (!groupCache.TryGetValue(block.Type.Prefab.Id, out var stockGroup))
				{
					stockGroup = stockPrefabs.GetGroup(block.Type.Prefab.Id);
					groupCache.Add(block.Type.Prefab.Id, stockGroup);
				}

				group.Blocks.SetGroup(block.Position, stockGroup);
			}
			else
			{
				group.Blocks.SetBlock(block.Position, block.Type.Prefab.Id);
			}
		}

		for (int i = 0; i < settings.Count; i++)
		{
			SettingRecord set = settings[i];
			group.Settings.Add(new PrefabSetting()
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
			group.Connections.Add(new Connection()
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
