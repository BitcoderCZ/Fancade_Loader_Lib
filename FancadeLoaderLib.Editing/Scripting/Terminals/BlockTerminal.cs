// <copyright file="BlockTerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib.Editing.Scripting.Terminals;

public readonly struct BlockTerminal : ITerminal
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
		Block = block;
		Terminal = block.Type[terminalName];
	}

	public BlockTerminal(Block block, int terminalIndex)
	{
		Block = block;
		Terminal = block.Type.Terminals[terminalIndex];
	}

	public int3 BlockPosition => Block.Position;

	public int TerminalIndex => Terminal.Index;

	public int3? VoxelPosition => Terminal.Position;

	public WireType WireType => Terminal.WireType;
}
