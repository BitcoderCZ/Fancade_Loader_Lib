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

/// <summary>
/// <see cref="BlockBuilder"/> that creates the output as <see cref="Game"/> object.
/// </summary>
public sealed class GameFileBlockBuilder : BlockBuilder
{
    /// <summary>
    /// The game to write the output to.
    /// </summary>
    public readonly Game? InGame;

    /// <summary>
    /// Name of the prefab to add.
    /// </summary>
    public readonly string? PrefabName;

    /// <summary>
    /// Type of the prefab to add.
    /// </summary>
    public readonly PrefabType? PrefabType;

    /// <summary>
    /// Id of the prefab to place the output in.
    /// </summary>
    public readonly ushort? PrefabId;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameFileBlockBuilder"/> class.
    /// Places the output into a new prefab.
    /// </summary>
    /// <param name="inGame">The game to write the output to.</param>
    /// <param name="prefabName">Name of the prefab to add.</param>
    /// <param name="prefabType">Type of the prefab to add.</param>
    public GameFileBlockBuilder(Game? inGame, string prefabName, PrefabType prefabType)
    {
        InGame = inGame;

        CreateNewPrefab = true;

        PrefabName = prefabName;
        PrefabType = prefabType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameFileBlockBuilder"/> class.
    /// Places the output into an existing prefab.
    /// </summary>
    /// <param name="inGame">The game to write the output to.</param>
    /// <param name="prefabId">Id of the prefab to place the output in.</param>
    public GameFileBlockBuilder(Game inGame, ushort prefabId)
    {
        ThrowIfNull(inGame, nameof(inGame));

        InGame = inGame;

        CreateNewPrefab = false;

        PrefabId = prefabId;
    }

    /// <summary>
    /// Gets a value indicating whether the output should be placed into a new prefab.
    /// </summary>
    /// <value><see langword="true"/> if the output will be placed into a new prefab; otherwise, <see langword="false"/>.</value>
    [MemberNotNullWhen(true, nameof(PrefabName), nameof(PrefabType))]
    [MemberNotNullWhen(false, nameof(PrefabId))]
    public bool CreateNewPrefab { get; private set; }

    /// <summary>
    /// Builds the blocks, connections and settings into a <see cref="Game"/> object.
    /// </summary>
    /// <param name="buildPos">The position at which blocks should be placed.</param>
    /// <returns>The created <see cref="Game"/> object or <see cref="InGame"/> if it is not <see langword="null"/>.</returns>
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

        PartialPrefabList stockPrefabs = StockBlocks.PartialPrefabList;

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
                Index = (byte)set.SettingIndex,
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
                FromVoxel = (ushort3)(con.From.VoxelPosition ?? ChooseTerminalVoxelPos(con.From.BlockPosition)),
                To = (ushort3)con.To.BlockPosition,
                ToVoxel = (ushort3)(con.To.VoxelPosition ?? ChooseTerminalVoxelPos(con.To.BlockPosition)),
            });
        }

        return game;
    }
}
