// <copyright file="ScopedCodePlacerWrapper.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Terminals;

namespace BitcoderCZ.Fancade.Editing.Scripting.Placers;

/// <summary>
/// A wrapper over a <see cref="ICodePlacer"/> that implements <see cref="IScopedCodePlacer"/>.
/// </summary>
public sealed class ScopedCodePlacerWrapper : IScopedCodePlacer
{
    private readonly ICodePlacer _placer;
    private readonly IScopedCodePlacer? _scoped;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedCodePlacerWrapper"/> class.
    /// </summary>
    /// <param name="placer">The <see cref="ICodePlacer"/> to wrap.</param>
    public ScopedCodePlacerWrapper(ICodePlacer placer)
    {
        _placer = placer;
        _scoped = _placer as IScopedCodePlacer;
    }

    /// <inheritdoc/>
    public BlockBuilder Builder => _placer.Builder;

    /// <inheritdoc/>
    public int CurrentCodeBlockBlocks => _scoped is null ? _placer.PlacedBlockCount : _scoped.CurrentCodeBlockBlocks;

    /// <inheritdoc/>
    public int PlacedBlockCount => _placer.PlacedBlockCount;

    /// <inheritdoc/>
    public Block PlaceBlock(BlockDef blockType)
        => _placer.PlaceBlock(blockType);

    /// <inheritdoc/>
    public void Connect(ITerminal fromTerminal, ITerminal toTerminal)
        => _placer.Connect(fromTerminal, toTerminal);

    /// <inheritdoc/>
    public void SetSetting(Block block, int settingIndex, object value)
        => _placer.SetSetting(block, settingIndex, value);

    /// <inheritdoc/>
    public void EnterExpressionBlock()
        => _scoped?.EnterExpressionBlock();

    /// <inheritdoc/>
    public void EnterHighlight()
        => _scoped?.EnterHighlight();

    /// <inheritdoc/>
    public void EnterStatementBlock()
        => _scoped?.EnterStatementBlock();

    /// <inheritdoc/>
    public void ExitExpressionBlock()
        => _scoped?.ExitExpressionBlock();

    /// <inheritdoc/>
    public void ExitHightlight()
        => _scoped?.ExitHightlight();

    /// <inheritdoc/>
    public void ExitStatementBlock()
        => _scoped?.ExitStatementBlock();

    /// <inheritdoc/>
    public void Flush()
        => _placer.Flush();
}
