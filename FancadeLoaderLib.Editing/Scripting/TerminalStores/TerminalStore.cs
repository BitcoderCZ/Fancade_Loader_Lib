// <copyright file="TerminalStore.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using System;

namespace FancadeLoaderLib.Editing.Scripting.TerminalStores;

#pragma warning disable CA1815 // Override equals and operator equals on value types
public readonly struct TerminalStore : ITerminalStore
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
	private readonly ITerminal[] _out;

	public TerminalStore(Block block)
#pragma warning disable CA1062 // Validate arguments of public methods
		: this(block, block.Type.Before, block, block.Type.After)
#pragma warning restore CA1062 // Validate arguments of public methods
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
		if (@in is null)
		{
			throw new ArgumentNullException(nameof(@in));
		}

		if (@out is null)
		{
			throw new ArgumentNullException(nameof(@out));
		}

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
		=> block is null
			? throw new ArgumentNullException(nameof(block))
			: block.Type.BlockType == BlockType.Active
			? CIn(block, block.Type.Before)
			: throw new ArgumentException($"{nameof(block)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(BlockType)}.{nameof(BlockType.Active)}.", nameof(block));

	public static TerminalStore CIn(Block block, TerminalDef terminal)
		=> new TerminalStore(new BlockTerminal(block, terminal), [NopTerminal.Instance]);

	public static TerminalStore COut(Block block)
		=> block is null
			? throw new ArgumentNullException(nameof(block))
			: block.Type.BlockType == BlockType.Active
			? COut(block, block.Type.After)
			: throw new ArgumentException($"{nameof(block)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(BlockType)}.{nameof(BlockType.Active)}.", nameof(block));

	public static TerminalStore COut(Block block, TerminalDef terminal)
		=> new TerminalStore(NopTerminal.Instance, [new BlockTerminal(block, terminal)]);
}
