// <copyright file="BlockBuilder.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using FancadeLoaderLib.Editing.Utils;
using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace FancadeLoaderLib.Editing.Scripting;

public abstract class BlockBuilder
{
#pragma warning disable CA1002 // Do not expose generic lists
    protected List<BlockSegment> segments = [];
    protected List<Block> highlightedBlocks = [];
    protected List<ConnectionRecord> connections = [];
    protected List<SettingRecord> settings = [];
#pragma warning restore CA1002 // Do not expose generic lists

    public virtual void AddBlockSegments(IEnumerable<Block> blocks)
    {
        BlockSegment segment = new BlockSegment(blocks);

        segments.Add(segment);
    }

    public virtual void AddHighlightedBlock(Block block)
        => highlightedBlocks.Add(block);

    public virtual void Connect(ITerminal fromTerminal, ITerminal toTerminal)
        => connections.Add(new ConnectionRecord(fromTerminal, toTerminal));

    public void Connect(ITerminalStore fromStore, ITerminalStore toStore)
    {
        if (fromStore is NopTerminalStore or null || toStore is NopTerminalStore or null)
        {
            return;
        }

        if (toStore.In is null)
        {
            return;
        }

        foreach (var target in fromStore.Out)
        {
            Connect(target, toStore.In);
        }
    }

    public virtual void SetSetting(Block block, int settingIndex, object value)
        => settings.Add(new SettingRecord(block, settingIndex, value));

    public abstract object Build(int3 buildPos);

    public virtual void Clear()
    {
        segments.Clear();
        connections.Clear();
        settings.Clear();
    }

    protected Block[] PreBuild(int3 buildPos, bool sortByPos)
    {
        if (buildPos.X < 0 || buildPos.Y < 0 || buildPos.Z < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(buildPos), $"{nameof(buildPos)} must be >= 0");
        }
        else if (segments.Count == 0)
        {
            return [];
        }

        int totalBlockCount = highlightedBlocks.Count;
        int3[] segmentSizes = new int3[segments.Count];

        for (int i = 0; i < segments.Count; i++)
        {
            totalBlockCount += segments[i].Blocks.Length;
            segmentSizes[i] = segments[i].Size + new int3(2, 1, 2); // margin
        }

        int3[] segmentPositions = BinPacker.Compute(segmentSizes);

        Block[] blocks = new Block[totalBlockCount];

        int3 highlightedPos = buildPos;
        for (int i = 0; i < highlightedBlocks.Count; i++)
        {
            highlightedBlocks[i].Position = highlightedPos;
            highlightedPos.X += 3;
        }

        highlightedBlocks.CopyTo(blocks);

        int index = highlightedBlocks.Count;
        int3 off = highlightedBlocks.Count > 0 ? new int3(0, 0, 4) : int3.Zero;

        for (int i = 0; i < segments.Count; i++)
        {
            BlockSegment segment = segments[i];

            segment.Move(segmentPositions[i] + buildPos + off - segment.MinPos);

            segment.Blocks.CopyTo(blocks, index);
            index += segment.Blocks.Length;
        }

        if (sortByPos)
        {
            Array.Sort(blocks, (a, b) =>
            {
                int comp = a.Position.Z.CompareTo(b.Position.Z);
                return comp == 0 ? a.Position.X.CompareTo(b.Position.X) : comp;
            });
        }

        return blocks;
    }

    protected virtual int3 ChooseSubPos(int3 pos)
        => new int3(7, 3, 3);

    protected readonly record struct ConnectionRecord(ITerminal From, ITerminal To);

    protected readonly record struct SettingRecord(Block Block, int ValueIndex, object Value);

    protected class BlockSegment
    {
        public readonly ImmutableArray<Block> Blocks;

        public BlockSegment(IEnumerable<Block> blocks)
        {
            if (blocks is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(blocks));
            }

            Blocks = [.. blocks];
            if (Blocks.Length == 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(blocks), $"{nameof(blocks)} cannot be empty.");
            }

            CalculateMinMax();
        }

        public int3 MinPos { get; private set; }

        public int3 MaxPos { get; private set; }

        public int3 Size => MaxPos - MinPos + int3.One;

        public void Move(int3 move)
        {
            if (move == int3.Zero)
            {
                return;
            }

            for (int i = 0; i < Blocks.Length; i++)
            {
                Blocks[i].Position += move;
            }

            MinPos += move;
            MaxPos += move;
        }

        private void CalculateMinMax()
        {
            int3 min = new int3(int.MaxValue, int.MaxValue, int.MaxValue);
            int3 max = new int3(int.MinValue, int.MinValue, int.MinValue);

            for (int i = 0; i < Blocks.Length; i++)
            {
                BlockDef type = Blocks[i].Type;

                min = int3.Min(Blocks[i].Position, min);
                max = int3.Max(Blocks[i].Position + type.Size, max);
            }

            MinPos = min;
            MaxPos = max - int3.One;
        }
    }
}