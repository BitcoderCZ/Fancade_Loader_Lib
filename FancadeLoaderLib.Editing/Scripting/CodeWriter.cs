// <copyright file="CodeWriter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Placers;
using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using System;

namespace FancadeLoaderLib.Editing.Scripting;

public sealed class CodeWriter
{
	private readonly IScopedCodePlacer _codePlacer;

	public CodeWriter(ICodePlacer codePlacer)
	{
		_codePlacer = codePlacer is IScopedCodePlacer scoped ? scoped : new ScopedCodePlacerWrapper(codePlacer);
	}

	public ITerminal PlaceLiteral(object value)
	{
		var block = _codePlacer.PlaceBlock(StockBlocks.Values.ValueByType(value));

		if (value is not bool)
		{
			_codePlacer.SetSetting(block, 0, value);
		}

		return new BlockTerminal(block, block.Type.Terminals[0]);
	}

	public ITerminalStore SetVariable(string name, object value)
	{
		if (value is null)
		{
			throw new ArgumentNullException(nameof(value));
		}

		var wireType = WireTypeUtils.FromType(value.GetType());

		var block = _codePlacer.PlaceBlock(StockBlocks.Variables.SetVariableByType(wireType));

		_codePlacer.SetSetting(block, 0, name);

		using (_codePlacer.ExpressionBlock())
		{
			var valueTerminal = PlaceLiteral(value);

			_codePlacer.Connect(valueTerminal, new BlockTerminal(block, "Value"));
		}

		return new TerminalStore(block);
	}

	public ITerminalStore SetVariable(string name, ITerminal terminal)
	{
		var block = _codePlacer.PlaceBlock(StockBlocks.Variables.SetVariableByType(terminal.WireType));

		_codePlacer.Connect(terminal, new BlockTerminal(block, "Value"));

		_codePlacer.SetSetting(block, 0, name);

		return new TerminalStore(block);
	}

	#region Blocks
	public IDisposable StatementBlock()
		=> _codePlacer.StatementBlock();

	public IDisposable ExpressionBlock()
		=> _codePlacer.ExpressionBlock();

	public IDisposable HighlightBlock()
		=> _codePlacer.HighlightBlock();
	#endregion
}
