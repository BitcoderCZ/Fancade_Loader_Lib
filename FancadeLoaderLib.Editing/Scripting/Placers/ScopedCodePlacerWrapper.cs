// <copyright file="ScopedCodePlacerWrapper.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;

namespace FancadeLoaderLib.Editing.Scripting.Placers;

public sealed class ScopedCodePlacerWrapper : IScopedCodePlacer
{
    private readonly ICodePlacer _placer;
    private readonly IScopedCodePlacer? _scoped;

    public ScopedCodePlacerWrapper(ICodePlacer placer)
    {
        _placer = placer;
        _scoped = _placer as IScopedCodePlacer;
    }

    public int CurrentCodeBlockBlocks => _scoped is null ? _placer.PlacedBlockCount : _scoped.CurrentCodeBlockBlocks;

    public int PlacedBlockCount => _placer.PlacedBlockCount;

    public Block PlaceBlock(BlockDef blockType)
        => _placer.PlaceBlock(blockType);

    public void Connect(ITerminal fromTerminal, ITerminal toTerminal)
        => _placer.Connect(fromTerminal, toTerminal);

    public void SetSetting(Block block, int settingIndex, object value)
        => _placer.SetSetting(block, settingIndex, value);

    public void EnterExpressionBlock()
        => _scoped?.EnterExpressionBlock();

    public void EnterHighlight()
        => _scoped?.EnterHighlight();

    public void EnterStatementBlock()
        => _scoped?.EnterStatementBlock();

    public void ExitExpressionBlock()
        => _scoped?.ExitExpressionBlock();

    public void ExitHightlight()
        => _scoped?.ExitHightlight();

    public void ExitStatementBlock()
        => _scoped?.ExitStatementBlock();
}
