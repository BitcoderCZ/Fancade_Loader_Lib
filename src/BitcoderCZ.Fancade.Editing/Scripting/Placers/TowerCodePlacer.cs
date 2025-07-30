// <copyright file="TowerCodePlacer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Terminals;
using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing.Scripting.Placers;

/// <summary>
/// A <see cref="IScopedCodePlacer"/> that places blocks in towers.
/// </summary>
public sealed class TowerCodePlacer : IScopedCodePlacer
{
    private readonly List<Block> _blocks = new List<Block>(256);

    private readonly BlockBuilder _builder;
    private int _maxHeight = 20;
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Clarity.")]
    private bool _inHighlight = false;
    private int _statementDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="TowerCodePlacer"/> class.
    /// </summary>
    /// <param name="builder">The <see cref="BlockBuilder"/> to use.</param>
    public TowerCodePlacer(BlockBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc/>
    public BlockBuilder Builder => _builder;

    /// <inheritdoc/>
    public int CurrentCodeBlockBlocks => _blocks.Count;

    /// <inheritdoc/>
    public int PlacedBlockCount => _blocks.Count;

    /// <summary>
    /// Gets or sets the maximum height of the towers.
    /// </summary>
    /// <value>The maximum height of the towers, once it is reached a new tower is started.</value>
    public int MaxHeight
    {
        get => _maxHeight;
        set
        {
            if (value < 1)
            {
                ThrowArgumentOutOfRangeException(nameof(value), $"{nameof(MaxHeight)} must be larger than 0.");
            }

            _maxHeight = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the towers will be placed in a square.
    /// </summary>
    /// <value>
    /// If <see langword="true"/>, the towers will be placed in a square;
    /// if <see langword="false"/>, the towers will be placed in a line.
    /// </value>
    public bool SquarePlacement { get; set; } = true;

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
            block = new Block(blockType, int3.Zero);
            _blocks.Add(block);
        }

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
        => _statementDepth++;

    /// <inheritdoc/>
    public void ExitStatementBlock()
    {
        const int move = 4;

        _statementDepth--;

        if (_statementDepth < 0)
        {
            ThrowInvalidOperationException("Exited a statement block without being in one.");
        }

        if (_statementDepth <= 0 && _blocks.Count > 0)
        {
            // https://stackoverflow.com/a/17974
            int width = (_blocks.Count + MaxHeight - 1) / MaxHeight;

            if (SquarePlacement)
            {
                width = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(width)));
            }

            width *= move;

            int3 bPos = int3.Zero;

            for (int i = 0; i < _blocks.Count; i++)
            {
                _blocks[i].Position = bPos;
                bPos.Y++;

                if (bPos.Y > MaxHeight)
                {
                    bPos.Y = 0;
                    bPos.X += move;

                    if (bPos.X >= width)
                    {
                        bPos.X = 0;
                        bPos.Z += move;
                    }
                }
            }

            _builder.AddBlockSegments(_blocks);

            _blocks.Clear();
        }
    }

    /// <inheritdoc/>
    public void EnterExpressionBlock()
    {
    }

    /// <inheritdoc/>
    public void ExitExpressionBlock()
    {
    }

    /// <inheritdoc/>
    public void EnterHighlight()
        => _inHighlight = true;

    /// <inheritdoc/>
    public void ExitHightlight()
        => _inHighlight = false;

    /// <inheritdoc/>
    public void Flush()
    {
        while (_statementDepth > 0)
        {
            ExitStatementBlock();
        }
    }
}
