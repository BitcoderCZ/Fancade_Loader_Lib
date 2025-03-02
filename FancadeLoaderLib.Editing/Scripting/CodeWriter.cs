// <copyright file="CodeWriter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Placers;
using FancadeLoaderLib.Editing.Scripting.Settings;
using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using FancadeLoaderLib.Editing.Scripting.Utils;
using FancadeLoaderLib.Editing.Utils;
using FancadeLoaderLib.Utils;
using System;
using System.Collections.Generic;

namespace FancadeLoaderLib.Editing.Scripting;

public sealed class CodeWriter
{
	private readonly IScopedCodePlacer _codePlacer;

	private readonly Dictionary<string, BreakBlockCache> _vectorBreakCache = [];
	private readonly Dictionary<string, BreakBlockCache> _rotationBreakCache = [];

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

	public ITerminal PlaceVariable(string name, WireType type)
	{
		var block = _codePlacer.PlaceBlock(StockBlocks.Variables.GetVariableByType(type));

		_codePlacer.SetSetting(block, 0, name);

		return new BlockTerminal(block, block.Type.Terminals[0]);
	}

	public void PlaceComment(string text)
	{
		var span = text.AsSpan();

		foreach (var lineRange in StringExtensions.SplitByMaxLength(span, FancadeConstants.MaxCommentLength))
		{
			Block block = _codePlacer.PlaceBlock(StockBlocks.Values.Comment);
			_codePlacer.SetSetting(block, 0, new string(span[lineRange]));
		}
	}

	public ITerminalStore SetVariable(string name, object value)
	{
		if (value is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(value));
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
		if (terminal is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(terminal));
		}

		var block = _codePlacer.PlaceBlock(StockBlocks.Variables.SetVariableByType(terminal.WireType));

		_codePlacer.Connect(terminal, new BlockTerminal(block, "Value"));

		_codePlacer.SetSetting(block, 0, name);

		return new TerminalStore(block);
	}

	public ITerminalStore SetVariable(string name, Func<ITerminal> getTerminalFunc, WireType terminalType)
	{
		if (getTerminalFunc is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(getTerminalFunc));
		}

		var block = _codePlacer.PlaceBlock(StockBlocks.Variables.SetVariableByType(terminalType));

		_codePlacer.Connect(getTerminalFunc(), new BlockTerminal(block, "Value"));

		_codePlacer.SetSetting(block, 0, name);

		return new TerminalStore(block);
	}

	public ITerminalStore SetListRange<T>(string variableName, ReadOnlySpan<T> values, int startIndex)
		where T : notnull
	{
		var wireType = WireTypeUtils.FromType(typeof(T));

		TerminalConnector connector = new TerminalConnector(_codePlacer.Connect);

		ITerminal? lastElementTerminal = null;

		for (int i = 0; i < values.Length; i++)
		{
			if (i == 0 && startIndex == 0)
			{
				connector.Add(SetVariable(variableName, values[i]));
			}
			else
			{
				Block setBlock = _codePlacer.PlaceBlock(StockBlocks.Variables.SetPtrByType(wireType));

				connector.Add(new TerminalStore(setBlock));

				using (ExpressionBlock())
				{
					Block listBlock = _codePlacer.PlaceBlock(StockBlocks.Variables.ListByType(wireType));

					_codePlacer.Connect(TerminalStore.COut(listBlock, listBlock.Type["Element"]), TerminalStore.CIn(setBlock, setBlock.Type["Variable"]));

					using (ExpressionBlock())
					{
						lastElementTerminal ??= PlaceVariable(variableName, wireType);

						_codePlacer.Connect(lastElementTerminal, TerminalStore.CIn(listBlock, listBlock.Type["Variable"]));

						lastElementTerminal = new BlockTerminal(listBlock, "Element");

						_codePlacer.Connect(i == 0 ? PlaceLiteral((float)startIndex) : PlaceLiteral(1f), TerminalStore.CIn(listBlock, listBlock.Type["Index"]));
					}

					_codePlacer.Connect(PlaceLiteral(values[i]), TerminalStore.CIn(setBlock, setBlock.Type["Value"]));
				}
			}
		}

		return connector.Store;
	}

	public ITerminalStore SetListRange<T>(string variableName, ReadOnlySpan<T> values, string startIndexVariableName)
		where T : notnull
	{
		var wireType = WireTypeUtils.FromType(typeof(T));

		TerminalConnector connector = new TerminalConnector(_codePlacer.Connect);

		ITerminal? lastElementTerminal = null;

		for (int i = 0; i < values.Length; i++)
		{
			Block setBlock = _codePlacer.PlaceBlock(StockBlocks.Variables.SetPtrByType(wireType));

			connector.Add(new TerminalStore(setBlock));

			using (ExpressionBlock())
			{
				Block listBlock = _codePlacer.PlaceBlock(StockBlocks.Variables.ListByType(wireType));

				_codePlacer.Connect(TerminalStore.COut(listBlock, listBlock.Type["Element"]), TerminalStore.CIn(setBlock, setBlock.Type["Variable"]));

				using (ExpressionBlock())
				{
					lastElementTerminal ??= PlaceVariable(variableName, wireType);

					_codePlacer.Connect(lastElementTerminal, TerminalStore.CIn(listBlock, listBlock.Type["Variable"]));

					lastElementTerminal = new BlockTerminal(listBlock, "Element");

					_codePlacer.Connect(i == 0 ? PlaceVariable(startIndexVariableName, WireType.Float) : PlaceLiteral(1f), TerminalStore.CIn(listBlock, listBlock.Type["Index"]));
				}

				_codePlacer.Connect(PlaceLiteral(values[i]), TerminalStore.CIn(setBlock, setBlock.Type["Value"]));
			}
		}

		return connector.Store;
	}

	public (ITerminal X, ITerminal Y, ITerminal Z) BreakVector(string variableName, WireType type, bool cache = true)
	{
		type = type.ToNotPointer();

		switch (type)
		{
			case WireType.Vec3:
			case WireType.Rot:
				break;
			default:
				ThrowHelper.ThrowInvalidEnumArgumentException(nameof(type), (int)type, typeof(WireType));
		}

		Block? block;

		if (cache)
		{
			BreakBlockCache blockCache = (type == WireType.Vec3 ? _vectorBreakCache : _rotationBreakCache)
				.AddIfAbsent(variableName, new BreakBlockCache(null, FancadeConstants.MaxWireSplits));

			if (!blockCache.TryGet(out block))
			{
				block = _codePlacer.PlaceBlock(type == WireType.Vec3 ? StockBlocks.Math.Break_Vector : StockBlocks.Math.Break_Rotation);
				blockCache.SetNewBlock(block);

				using (ExpressionBlock())
				{
					ITerminal variableTerminal = PlaceVariable(variableName, type);
					_codePlacer.Connect(variableTerminal, new BlockTerminal(block, 3));
				}
			}
		}
		else
		{
			block = _codePlacer.PlaceBlock(StockBlocks.Math.BreakByType(type));
		}

		using (ExpressionBlock())
		{
			ITerminal terminal = PlaceVariable(variableName, type);
			_codePlacer.Connect(terminal, new BlockTerminal(block, 3));
		}

		return (
			new BlockTerminal(block, 2),
			new BlockTerminal(block, 1),
			new BlockTerminal(block, 0)
		);
	}

	public (ITerminalStore Store, ITerminal TrueTerminal, ITerminal FalseTerminal) PlaceIf(Func<ITerminal> getConditionTerminalFunc)
	{
		if (getConditionTerminalFunc is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(getConditionTerminalFunc));
		}

		Block block = _codePlacer.PlaceBlock(StockBlocks.Control.If);

		ITerminal condition = getConditionTerminalFunc();

		_codePlacer.Connect(condition, new BlockTerminal(block, "Condition"));

		return (new TerminalStore(block), new BlockTerminal(block, "True"), new BlockTerminal(block, "False"));
	}

	public (ITerminalStore Store, ITerminal OnPlayTerminal) PlacePlaySensor()
	{
		Block block = _codePlacer.PlaceBlock(StockBlocks.Control.PlaySensor);

		return (new TerminalStore(block), new BlockTerminal(block, "On Play"));
	}

	public (ITerminalStore Store, ITerminal AfterPhysicsTerminal) PlaceLateUpdate()
	{
		Block block = _codePlacer.PlaceBlock(StockBlocks.Control.LateUpdate);

		return (new TerminalStore(block), new BlockTerminal(block, "After Physics"));
	}

	public (ITerminalStore Store, ITerminal OnScreenshotTerminal) PlaceBoxArtSensor()
	{
		Block block = _codePlacer.PlaceBlock(StockBlocks.Control.BoxArtSensor);

		return (new TerminalStore(block), new BlockTerminal(block, "On Screenshot"));
	}

	public (ITerminalStore Store, ITerminal TouchedTerminal, ITerminal ScreenXTerminal, ITerminal ScreenYTerminal) PlaceTouchSensor(TouchState touchState, int touchFinger)
	{
		if (touchFinger < 0 || touchFinger > FancadeConstants.TouchSensorMaxFinger)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(nameof(touchFinger));
		}

		Block block = _codePlacer.PlaceBlock(StockBlocks.Control.TouchSensor);

		_codePlacer.SetSetting(block, 0, (byte)touchState);
		_codePlacer.SetSetting(block, 1, (byte)touchFinger);

		return (new TerminalStore(block), new BlockTerminal(block, "Touched"), new BlockTerminal(block, "Screen X"), new BlockTerminal(block, "Screen Y"));
	}

	public (ITerminalStore Store, ITerminal SwipedTerminal, ITerminal DirectionTerminal) PlaceSwipeSensor()
	{
		Block block = _codePlacer.PlaceBlock(StockBlocks.Control.SwipeSensor);

		return (new TerminalStore(block), new BlockTerminal(block, "Swiped"), new BlockTerminal(block, "Direction"));
	}

	public (ITerminalStore Store, ITerminal ButtonTerminal) PlaceButton(ButtonType buttonType)
	{
		Block block = _codePlacer.PlaceBlock(StockBlocks.Control.Button);

		_codePlacer.SetSetting(block, 0, (byte)buttonType);

		return (new TerminalStore(block), new BlockTerminal(block, "Button"));
	}

	public (ITerminalStore Store, ITerminal CollidedTerminal, ITerminal SecondObjectTerminal, ITerminal ImpulseTerminal, ITerminal NormalTerminal) PlaceCollision(Func<ITerminal> getFirstObjectTerminalFunc)
	{
		if (getFirstObjectTerminalFunc is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(getFirstObjectTerminalFunc));
		}

		Block block = _codePlacer.PlaceBlock(StockBlocks.Control.Collision);

		_codePlacer.Connect(getFirstObjectTerminalFunc(), new BlockTerminal(block, "1st Object"));

		return (new TerminalStore(block), new BlockTerminal(block, "Collided"), new BlockTerminal(block, "2nd Object"), new BlockTerminal(block, "Impulse"), new BlockTerminal(block, "Normal"));
	}

	public (ITerminalStore Store, ITerminal DoTerminal, ITerminal CounterTerminal) PlaceLoop(Func<ITerminal> getStartTerminalFunc, Func<ITerminal> getStopTerminalFunc)
	{
		if (getStartTerminalFunc is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(getStartTerminalFunc));
		}

		if (getStopTerminalFunc is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(getStopTerminalFunc));
		}

		Block block = _codePlacer.PlaceBlock(StockBlocks.Control.Loop);

		_codePlacer.Connect(getStartTerminalFunc(), new BlockTerminal(block, "Start"));
		_codePlacer.Connect(getStopTerminalFunc(), new BlockTerminal(block, "Stop"));

		return (new TerminalStore(block), new BlockTerminal(block, "Do"), new BlockTerminal(block, "Counter"));
	}

	public ITerminalStore PlaceIncrementNumber(Func<ITerminal> getVariableTerminalFunc)
	{
		if (getVariableTerminalFunc is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(getVariableTerminalFunc));
		}

		Block block = _codePlacer.PlaceBlock(StockBlocks.Variables.IncrementNumber);

		_codePlacer.Connect(getVariableTerminalFunc(), new BlockTerminal(block, "Variable"));

		return new TerminalStore(block);
	}

	public ITerminalStore PlaceDecrementNumber(Func<ITerminal> getVariableTerminalFunc)
	{
		if (getVariableTerminalFunc is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(getVariableTerminalFunc));
		}

		Block block = _codePlacer.PlaceBlock(StockBlocks.Variables.DecrementNumber);

		_codePlacer.Connect(getVariableTerminalFunc(), new BlockTerminal(block, "Variable"));

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
