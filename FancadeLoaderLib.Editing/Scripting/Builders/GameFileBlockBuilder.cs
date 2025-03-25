// <copyright file="GameFileBlockBuilder.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing.Scripting.Builders;

public sealed class GameFileBlockBuilder : BlockBuilder
{
    public readonly Game? InGame;

    public readonly string? PrefabName;
    public readonly PrefabType? PrefabType;

    public readonly ushort? PrefabId;

    public GameFileBlockBuilder(Game? inGame, string prefabName, PrefabType prefabType)
    {
        InGame = inGame;

        CreateNewPrefab = true;

        PrefabName = prefabName;
        PrefabType = prefabType;
    }

    public GameFileBlockBuilder(Game inGame, ushort prefabId)
    {
        ThrowIfNull(inGame, nameof(inGame));

        InGame = inGame;

        CreateNewPrefab = false;

        PrefabId = prefabId;
    }

    [MemberNotNullWhen(true, nameof(PrefabName), nameof(PrefabType))]
    [MemberNotNullWhen(false, nameof(PrefabId))]
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
                ushort id = RawGame.CurrentNumbStockPrefabs;

                while (id < game.Prefabs.SegmentCount - RawGame.CurrentNumbStockPrefabs && game.Prefabs.TryGetPrefab(id, out var item) && item.Type == FancadeLoaderLib.PrefabType.Level)
                {
                    id += (ushort)item.Count;
                }

                prefab = Prefab.CreateLevel(id, PrefabName);
                game.Prefabs.InsertPrefab(prefab);
            }
            else
            {
                prefab = Prefab.CreateBlock((ushort)(game.Prefabs.SegmentCount + RawGame.CurrentNumbStockPrefabs), PrefabName);
                prefab.Type = (PrefabType)PrefabType;
                prefab[int3.Zero].Voxels = BlockVoxelsGenerator.CreateScript(int2.One).First().Value;

                game.Prefabs.AddPrefab(prefab);
            }
        }
        else
        {
            prefab = game.Prefabs.GetPrefab((ushort)PrefabId);
        }

        Block[] blocks = PreBuild(buildPos, false);

        PartialPrefabList stockPrefabs = StockBlocks.PrefabList;

        Dictionary<ushort, PartialPrefab> prefabCache = [];

        for (int i = 0; i < blocks.Length; i++)
        {
            Block block = blocks[i];
            if (!prefabCache.TryGetValue(block.Type.Prefab.Id, out var stockPrefab))
            {
                stockPrefab = stockPrefabs.GetPrefab(block.Type.Prefab.Id);
                prefabCache.Add(block.Type.Prefab.Id, stockPrefab);
            }

            prefab.Blocks.SetPrefab(block.Position, stockPrefab);
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
