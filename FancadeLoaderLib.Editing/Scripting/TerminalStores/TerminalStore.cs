// <copyright file="TerminalStore.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using System;

namespace FancadeLoaderLib.Editing.Scripting.TerminalStores;

public readonly struct TerminalStore : ITerminalStore
{
	private readonly ITerminal[] _out;

	public TerminalStore(Block block)
		: this(block, block.Type.Before, block, block.Type.After)
	{
		if (block.Type.BlockType != BlockType.Active)
		{
			throw new ArgumentException($"{nameof(block)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(BlockType)}.{nameof(BlockType.Active)}.", nameof(block));
		}
	}

	public TerminalStore(Block @in, TerminalDef inTerminal, Block @out, TerminalDef outTerminal)
	{
		In = new BlockTerminal(@in, inTerminal);
		_out = [new BlockTerminal(@out, outTerminal)];
	}

	public TerminalStore(Block @in, string inTerminalName, Block @out, string outTerminalName)
	{
		In = new BlockTerminal(@in, @in.Type[inTerminalName]);
		_out = [new BlockTerminal(@out, @out.Type[outTerminalName])];
	}

	public TerminalStore(ITerminal @in, ITerminal[] @out)
	{
		In = @in;
		_out = @out;
	}

	public ITerminal In { get; }

	public ReadOnlySpan<ITerminal> Out => _out;

	public static TerminalStore CIn(Block block)
		=> block.Type.BlockType == BlockType.Active
			? CIn(block, block.Type.Before)
			: throw new ArgumentException($"{nameof(block)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(BlockType)}.{nameof(BlockType.Active)}.", nameof(block));

	public static TerminalStore CIn(Block block, TerminalDef terminal)
		=> new TerminalStore(new BlockTerminal(block, terminal), [NopTerminal.Instance]);

	public static TerminalStore COut(Block block) => block.Type.BlockType == BlockType.Active
			? COut(block, block.Type.After)
			: throw new ArgumentException($"{nameof(block)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(BlockType)}.{nameof(BlockType.Active)}.", nameof(block));

	public static TerminalStore COut(Block block, TerminalDef terminal)
		=> new TerminalStore(NopTerminal.Instance, [new BlockTerminal(block, terminal)]);
}
