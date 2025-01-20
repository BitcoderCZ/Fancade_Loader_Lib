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

	public int3 BlockPosition => Block.Position;

	public int TerminalIndex => Terminal.Index;

	public int3? VoxelPosition => Terminal.Position;
}
