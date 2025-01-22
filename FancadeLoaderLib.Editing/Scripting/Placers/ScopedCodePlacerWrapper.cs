// <copyright file="ScopedCodePlacerWrapper.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;

namespace FancadeLoaderLib.Editing.Scripting.Placers;

internal sealed class ScopedCodePlacerWrapper : IScopedCodePlacer
{
	private readonly ICodePlacer _placer;

	public ScopedCodePlacerWrapper(ICodePlacer placer)
	{
		_placer = placer;
	}

	public Block PlaceBlock(BlockDef blockType)
		=> _placer.PlaceBlock(blockType);

	public void Connect(ITerminal from, ITerminal to)
		=> _placer.Connect(from, to);

	public void SetSetting(Block block, int settingIndex, object value)
		=> _placer.SetSetting(block, settingIndex, value);

	public void EnterExpressionBlock()
	{
	}

	public void EnterHighlight()
	{
	}

	public void EnterStatementBlock()
	{
	}

	public void ExitExpressionBlock()
	{
	}

	public void ExitHightlight()
	{
	}

	public void ExitStatementBlock()
	{
	}
}
