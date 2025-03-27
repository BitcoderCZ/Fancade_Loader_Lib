// <copyright file="BlockBuilder.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using FancadeLoaderLib.Editing.Utils;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing.Scripting;

/// <summary>
/// A class to abstract away the placing and connecting of blocks.
/// </summary>
public abstract class BlockBuilder
{
    /// <summary>
    /// The <see cref="BlockSegment"/>s to place.
    /// </summary>
    protected List<BlockSegment> segments = [];

    /// <summary>
    /// The highlighted <see cref="Block"/>s to place.
    /// </summary>
    protected List<Block> highlightedBlocks = [];

    /// <summary>
    /// The <see cref="ConnectionRecord"/>s to connect.
    /// </summary>
    protected List<ConnectionRecord> connections = [];

    /// <summary>
    /// The <see cref="SettingRecord"/>s to set.
    /// </summary>
    protected List<SettingRecord> settings = [];

    /// <summary>
    /// Adds blocks to the <see cref="BlockBuilder"/>.
    /// </summary>
    /// <param name="blocks">The blocks to add.</param>
    public virtual void AddBlockSegments(IEnumerable<Block> blocks)
    {
        BlockSegment segment = new BlockSegment(blocks);

        segments.Add(segment);
    }

    /// <summary>
    /// Adds a "highlighted" block to the <see cref="BlockBuilder"/>.
    /// </summary>
    /// <remarks>
    /// The block will be placed in a position that is easily visible.
    /// </remarks>
    /// <param name="block">The block to add.</param>
    public virtual void AddHighlightedBlock(Block block)
        => highlightedBlocks.Add(block);

    /// <summary>
    /// Connects 2 <see cref="ITerminal"/>s together.
    /// </summary>
    /// <param name="from">The first <see cref="ITerminal"/>.</param>
    /// <param name="to">The second <see cref="ITerminal"/>.</param>
    public virtual void Connect(ITerminal from, ITerminal to)
        => connections.Add(new ConnectionRecord(from, to));

    /// <summary>
    /// Connects 2 <see cref="ITerminalStore"/>s together.
    /// </summary>
    /// <param name="from">The first <see cref="ITerminalStore"/>.</param>
    /// <param name="to">The second <see cref="ITerminalStore"/>.</param>
    public void Connect(ITerminalStore from, ITerminalStore to)
    {
        if (from is NopTerminalStore or null || to is NopTerminalStore or null)
        {
            return;
        }

        if (to.In is null)
        {
            return;
        }

        foreach (var target in from.Out)
        {
            Connect(target, to.In);
        }
    }

    /// <summary>
    /// Adds a setting to a block.
    /// </summary>
    /// <param name="block">The block to add the setting to.</param>
    /// <param name="settingIndex">Index of the setting.</param>
    /// <param name="value">The setting value.</param>
    public virtual void SetSetting(Block block, int settingIndex, object value)
        => settings.Add(new SettingRecord(block, settingIndex, value));

    /// <summary>
    /// Builds the blocks, connections and settings into an output.
    /// </summary>
    /// <param name="buildPos">The position at which blocks should be placed.</param>
    /// <returns>An object representing fancade game, or from which a game can be created.</returns>
    public abstract object Build(int3 buildPos);

    /// <summary>
    /// Removes all blocks, connections and settings from the <see cref="BlockBuilder"/>.
    /// </summary>
    public virtual void Clear()
    {
        segments.Clear();
        highlightedBlocks.Clear();
        connections.Clear();
        settings.Clear();
    }

    /// <summary>
    /// Combines <see cref="segments"/> and <see cref="highlightedBlocks"/> into a <see cref="Block"/> array.
    /// </summary>
    /// <param name="buildPos">The position at which blocks should be placed.</param>
    /// <param name="sortByPos">If the blocks should be sorted by their Z and X position.</param>
    /// <returns>The created <see cref="Block"/> array.</returns>
    protected Block[] PreBuild(int3 buildPos, bool sortByPos)
    {
        if (buildPos.X < 0 || buildPos.Y < 0 || buildPos.Z < 0)
        {
            ThrowArgumentOutOfRangeException(nameof(buildPos), $"{nameof(buildPos)} must be >= 0");
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

    /// <summary>
    /// Choose the voxel position of the therminal for a given block position.
    /// </summary>
    /// <param name="pos">Position of the block the terminal is on.</param>
    /// <returns>The voxel position of the terminal.</returns>
    protected virtual int3 ChooseTerminalVoxelPos(int3 pos)
        => new int3(7, 3, 3);

    /// <summary>
    /// Represents a connection between 2 terminals.
    /// </summary>
    /// <param name="From">The first terminal.</param>
    /// <param name="To">The second terminal.</param>
    protected readonly record struct ConnectionRecord(ITerminal From, ITerminal To);

    /// <summary>
    /// Represents a setting of a block.
    /// </summary>
    /// <param name="Block">The block to apply the setting to.</param>
    /// <param name="SettingIndex">Index of the setting.</param>
    /// <param name="Value">The setting value.</param>
    protected readonly record struct SettingRecord(Block Block, int SettingIndex, object Value);

    /// <summary>
    /// Represents a segment of blocks.
    /// </summary>
    protected sealed class BlockSegment
    {
        /// <summary>
        /// The blocks in this segment.
        /// </summary>
        public readonly ImmutableArray<Block> Blocks;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockSegment"/> class.
        /// </summary>
        /// <param name="blocks">The blocks to be placed in this segment.</param>
        public BlockSegment(IEnumerable<Block> blocks)
        {
            ThrowIfNull(blocks, nameof(blocks));

            Blocks = [.. blocks];
            if (Blocks.Length == 0)
            {
                ThrowArgumentOutOfRangeException(nameof(blocks), $"{nameof(blocks)} cannot be empty.");
            }

            CalculateMinMax();
        }

        /// <summary>
        /// Gets the minimum position of the blocks in this segment.
        /// </summary>
        /// <value>The minimum position of the blocks in this segment.</value>
        public int3 MinPos { get; private set; }

        /// <summary>
        /// Gets the maximum position of the blocks in this segment.
        /// </summary>
        /// <value>The maximum position of the blocks in this segment.</value>
        public int3 MaxPos { get; private set; }

        /// <summary>
        /// Gets the size of this segment.
        /// </summary>
        /// <value>The size of this segment.</value>
        public int3 Size => MaxPos - MinPos + int3.One;

        /// <summary>
        /// Moves this segment.
        /// </summary>
        /// <param name="move"><see cref="int3"/> representing the movement offset along the X, Y, and Z axes.</param>
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