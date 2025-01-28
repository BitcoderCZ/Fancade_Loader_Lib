// <copyright file="BlockTerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;

namespace FancadeLoaderLib.Editing.Scripting.Terminals;

#pragma warning disable CA1815 // Override equals and operator equals on value types
public readonly struct BlockTerminal : ITerminal
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
	public readonly Block Block;
	public readonly TerminalDef Terminal;

	public BlockTerminal(Block block, TerminalDef terminal)
	{
		Block = block;
		Terminal = terminal;
	}

	public BlockTerminal(Block block, string terminalName)
	{
		if (block is null)
		{
			throw new ArgumentNullException(nameof(block));
		}

		Block = block;
		Terminal = block.Type[terminalName];
	}

	public BlockTerminal(Block block, int terminalIndex)
	{
		if (block is null)
		{
			throw new ArgumentNullException(nameof(block));
		}

		Block = block;
		Terminal = block.Type.Terminals[terminalIndex];
	}

	public int3 BlockPosition => Block.Position;

	public int TerminalIndex => Terminal.Index;

	public int3? VoxelPosition => Terminal.Position;

	public WireType WireType => Terminal.WireType;
}
