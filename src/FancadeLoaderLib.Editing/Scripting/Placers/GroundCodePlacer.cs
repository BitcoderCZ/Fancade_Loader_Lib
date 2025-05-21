// <copyright file="GroundCodePlacer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Utils;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing.Scripting.Placers;

/// <summary>
/// A <see cref="IScopedCodePlacer"/> that places blocks on the ground, attempting to place them as a human would.
/// </summary>
public sealed class GroundCodePlacer : IScopedCodePlacer
{
    private readonly Stack<StatementCodeBlock> _statements = new();
    private readonly Stack<ExpressionCodeBlock> _expressions = new();

    private readonly BlockBuilder _builder;

    private bool _inHighlight = false;

    private int _blockXOffset = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroundCodePlacer"/> class.
    /// </summary>
    /// <param name="builder">The <see cref="BlockBuilder"/> to use.</param>
    public GroundCodePlacer(BlockBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc/>
    public int CurrentCodeBlockBlocks => _expressions.Count != 0 ? _expressions.Peek().BlockCount : _statements.Peek().BlockCount;

    /// <inheritdoc/>
    public int PlacedBlockCount { get; private set; }

    /// <summary>
    /// Gets the horizontal space between blocks of code (minimum value and default is 3).
    /// </summary>
    /// <value>Horizontal space between blocks of code.</value>
    public int BlockXOffset
    {
        get => _blockXOffset;
        init
        {
            if (value < 3)
            {
                ThrowArgumentOutOfRangeException(nameof(value), $"{nameof(BlockXOffset)} must be greater than 2.");
            }

            _blockXOffset = value;
        }
    }

    /// <inheritdoc/>
    public Block PlaceBlock(BlockDef blockType)
    {
        Block block;

        if (_inHighlight)
        {
            block = new Block(blockType, int3.Zero);
            _builder.AddHighlightedBlock(block);
        }
        else
        {
            block = _expressions.Count != 0 ? _expressions.Peek().PlaceBlock(blockType) : _statements.Peek().PlaceBlock(blockType);
        }

        PlacedBlockCount++;

        return block;
    }

    /// <inheritdoc/>
    public void Connect(ITerminal fromTerminal, ITerminal toTerminal)
        => _builder.Connect(fromTerminal, toTerminal);

    /// <inheritdoc/>
    public void SetSetting(Block block, int settingIndex, object value)
        => _builder.SetSetting(block, settingIndex, value);

    /// <inheritdoc/>
    public void EnterStatementBlock()
    {
        Debug.Assert(_expressions.Count == 0, "Cannot enter a statement block while being in an expression block");

        if (_statements.Count == 0)
        {
            _statements.Push(CreateFunction());
        }
        else
        {
            _statements.Push(_statements.Peek().CreateStatementChild());
        }
    }

    /// <inheritdoc/>
    public void ExitStatementBlock()
    {
        Debug.Assert(_expressions.Count == 0, "Cannot exit a statement block while being in an expression block");

        StatementCodeBlock statement = _statements.Pop();

        if (_statements.Count == 0 && statement.AllBlocks.Any())
        {
            // end of function
            LayerStack.Process(statement, BlockXOffset);

            _builder.AddBlockSegments(statement.AllBlocks);
        }
    }

    /// <inheritdoc/>
    public void EnterExpressionBlock()
    {
        Debug.Assert(_statements.Count > 0, "Cannot enter an expression block without being in a statement block");

        if (_expressions.Count == 0)
        {
            _expressions.Push(_statements.Peek().CreateExpressionChild());
        }
        else
        {
            _expressions.Push(_expressions.Peek().CreateExpressionChild());
        }
    }

    /// <inheritdoc/>
    public void ExitExpressionBlock()
    {
        Debug.Assert(_statements.Count > 0, "Cannot exit an expression block without being in a statement block");

        ExpressionCodeBlock expression = _expressions.Pop();

        if (_statements.Count == 1)
        {
            // TODO: get's hit but code still works fine, figure out why it's here and fix or remove
            //Debug.Assert(expression.Parent?.Parent is not null, "Parent of parent shouldn't be null.");

            // if this is child of the top statement, process and assign x and y position to blocks
            LayerStack.Process(expression, BlockXOffset);
        }
    }

    /// <inheritdoc/>
    public void EnterHighlight()
        => _inHighlight = true;

    /// <inheritdoc/>
    public void ExitHightlight()
        => _inHighlight = false;

    private StatementCodeBlock CreateFunction()
        => new StatementCodeBlock(this, 0);

    private static class LayerStack
    {
        public static void Process(CodeBlock block, int blockXOffset)
        {
            ThrowIfNull(block, nameof(block));

            MultiValueDictionary<int, CodeBlock> xToBlocks = block.AllChildren.ToMultiValueDictionary(child => child.LayerPos, child => child);

            xToBlocks.Add(block.LayerPos, block);

            foreach (var (_, list) in xToBlocks)
            {
                // sort in descending order - blocks start and zero and go down
                list.Sort((a, b) => b.StartZ.CompareTo(a.StartZ));

                List<int> positions = [];

                MultiValueDictionary<int, CodeBlock> layers = [];

                foreach (var item in list)
                {
                    if (item.BlockCount == 0)
                    {
                        continue;
                    }

                    int yPos = -1;

                    for (int j = 0; j < positions.Count; j++)
                    {
                        if (positions[j] > item.StartZ)
                        {
                            positions[j] = item.CurrentPos;
                            yPos = j;
                            break;
                        }
                    }

                    if (yPos == -1)
                    {
                        yPos = positions.Count;
                        positions.Add(item.CurrentPos);
                    }

                    layers.Add(yPos, item);
                }

                (int YPos, int Height)[] layerToInfo = new (int YPos, int Height)[layers.Count];

                foreach (var (layer, layerList) in layers)
                {
                    int maxHeight = layerList.Max(block => block.Height);

                    if (layer == 0)
                    {
                        layerToInfo[layer] = (0, maxHeight);
                    }
                    else
                    {
                        var (lastY, lastheight) = layerToInfo[layer - 1];
                        layerToInfo[layer] = (lastY + lastheight, maxHeight);
                    }
                }

                foreach (var (layer, layerList) in layers)
                {
                    int yPos = layerToInfo[layer].YPos;

                    foreach (CodeBlock cb in layerList)
                    {
                        foreach (var itemBlock in cb.Blocks)
                        {
                            itemBlock.Position = itemBlock.Position with { X = blockXOffset * cb.LayerPos, Y = yPos };
                        }
                    }
                }
            }
        }
    }

    private abstract class CodeBlock
    {
        public readonly List<Block> Blocks = [];

        public readonly CodeBlock? Parent;

        public List<CodeBlock> Children = [];

        protected readonly GroundCodePlacer Placer;

        protected BlockDef? lastPlacedBlockType;

        protected CodeBlock(GroundCodePlacer placer, int pos, CodeBlock parent, int layerOffset)
        {
            ThrowIfNull(parent, nameof(parent));

            Debug.Assert(layerOffset == 1 || layerOffset == -1, $"{nameof(layerOffset)} == 1 || {nameof(layerOffset)} == -1, Value: '{layerOffset}'");

            Placer = placer;
            StartPos = pos;
            CurrentPos = StartPos;
            HasParent = true;
            Parent = parent;
            LayerOffsetFromParent = layerOffset;
        }

        protected CodeBlock(GroundCodePlacer placer, int pos)
        {
            Placer = placer;
            StartPos = pos;
            CurrentPos = StartPos;
            HasParent = false;
        }

        public int StartPos { get; private set; }

        public int CurrentPos { get; protected set; }

        public int Height { get; protected set; }

        /// <summary>
        /// Gets the blocks of this <see cref="CodeBlock"/> and it's children.
        /// </summary>
        /// <value>Blocks of this <see cref="CodeBlock"/> and it's children.</value>
        public IEnumerable<Block> AllBlocks
            => Blocks
            .Concat(Children.SelectMany(child => child.AllBlocks));

        public int BlockCount => Blocks.Count;

        public IEnumerable<CodeBlock> AllChildren
            => Children
            .Concat(Children.SelectMany(child => child.AllChildren));

        [MemberNotNullWhen(true, nameof(Parent), nameof(LayerOffsetFromParent))]
        public bool HasParent { get; private set; }

        public int LayerPos => HasParent ? Parent.LayerPos + LayerOffsetFromParent.Value : 0;

        public int StartZ => StartPos + BlockZOffset;

        protected abstract int BlockZOffset { get; }

        protected int? LayerOffsetFromParent { get; private set; }

        public abstract StatementCodeBlock CreateStatementChild();

        public abstract ExpressionCodeBlock CreateExpressionChild();

        public Block PlaceBlock(BlockDef blockType)
        {
            ThrowIfNull(blockType, nameof(blockType));

            if (BlockCount != 0)
            {
                CurrentPos -= blockType.Size.Z + BlockZOffset;
            }
            else
            {
                CurrentPos -= blockType.Size.Z - 1;
            }

            Height = Math.Max(Height, blockType.Size.Y);

            lastPlacedBlockType = blockType;

            Block block = new Block(blockType, new int3(0, 0, CurrentPos));
            Blocks.Add(block);
            return block;
        }

        protected int CalculatePosOfNextChild()
            => CurrentPos + (lastPlacedBlockType is null ? 0 : lastPlacedBlockType.Size.Z - 1);

        protected void IncrementXOffsetOfStatementParent()
        {
            CodeBlock? current = this;

            while (current is not null)
            {
                if (current is StatementCodeBlock)
                {
                    current.LayerOffsetFromParent++;
                    return;
                }

                current = current.Parent;
            }
        }
    }

    private sealed class StatementCodeBlock : CodeBlock
    {
        public StatementCodeBlock(GroundCodePlacer placer, int pos)
            : base(placer, pos)
        {
        }

        public StatementCodeBlock(GroundCodePlacer placer, int pos, CodeBlock parent, int layerOffset)
            : base(placer, pos, parent, layerOffset)
        {
        }

        protected override int BlockZOffset => 2;

        public override StatementCodeBlock CreateStatementChild()
        {
            StatementCodeBlock child = new StatementCodeBlock(Placer, CalculatePosOfNextChild(), this, 1);
            Children.Add(child);
            return child;
        }

        public override ExpressionCodeBlock CreateExpressionChild()
        {
            int lp = LayerPos;

            // is to the right of top statement
            if (lp > 0)
            {
                // if there is no space between this and the statement on the left, move self to the right
                if (Parent is not null && lp <= Parent.LayerPos + 1)
                {
                    IncrementXOffsetOfStatementParent();
                }
            }

            ExpressionCodeBlock child = new ExpressionCodeBlock(Placer, CalculatePosOfNextChild(), this, -1);
            Children.Add(child);
            return child;
        }
    }

    private sealed class ExpressionCodeBlock : CodeBlock
    {
        public ExpressionCodeBlock(GroundCodePlacer placer, int pos, CodeBlock parent, int layerOffset)
            : base(placer, pos, parent, layerOffset)
        {
        }

        protected override int BlockZOffset => 0;

        public override StatementCodeBlock CreateStatementChild()
            => throw new InvalidOperationException();

        public override ExpressionCodeBlock CreateExpressionChild()
        {
            int lp = LayerPos;

            // is to the right of top statement
            if (lp > 0)
            {
                StatementCodeBlock? parent = GetStatementParentOfParent();

                // if there is no space between this and the statement on the left, move self to the right
                if (parent is not null && lp <= parent.LayerPos + 1)
                {
                    IncrementXOffsetOfStatementParent();
                }
            }

            ExpressionCodeBlock child = new ExpressionCodeBlock(Placer, CalculatePosOfNextChild(), this, -1);
            Children.Add(child);
            return child;

            // returns the statement to the left of this expression block
            StatementCodeBlock? GetStatementParentOfParent()
            {
                int statementParentCount = 0;
                CodeBlock? current = this;

                while (current is not null)
                {
                    current = current.Parent;
                    if (current is StatementCodeBlock sb)
                    {
                        // if 1 - the statement to the right
                        // if 2 - the statement to the left
                        if (++statementParentCount >= 2)
                        {
                            return sb;
                        }
                    }
                }

                return null;
            }
        }
    }
}
