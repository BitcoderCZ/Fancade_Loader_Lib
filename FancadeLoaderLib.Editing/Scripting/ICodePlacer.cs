// <copyright file="ICodePlacer.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;

namespace FancadeLoaderLib.Editing.Scripting;

public interface ICodePlacer
{
	int PlacedBlockCount { get; }

	Block PlaceBlock(BlockDef blockType);

	void Connect(ITerminal from, ITerminal to);

	void SetSetting(Block block, int settingIndex, object value);
}

public static class ICodePlacerUtils
{
	public static void Connect(this ICodePlacer placer, ITerminalStore from, ITerminalStore to)
	{
		if (from is NopTerminalStore || to is NopTerminalStore)
		{
			return;
		}

		foreach (var target in from.Out)
		{
			placer.Connect(target, to.In);
		}
	}

	public static void Connect(this ICodePlacer placer, ITerminalStore from, ITerminal to)
	{
		if (from is NopTerminalStore || to is NopTerminal)
		{
			return;
		}

		foreach (var target in from.Out)
		{
			placer.Connect(target, to);
		}
	}

	public static void Connect(this ICodePlacer placer, ITerminal from, ITerminalStore to)
	{
		if (from is NopTerminal || to is NopTerminalStore)
		{
			return;
		}

		placer.Connect(from, to.In);
	}
}