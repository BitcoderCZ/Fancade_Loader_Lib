// <copyright file="BlockVoxelTerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Editing.Scripting.Terminals;

public readonly struct BlockVoxelTerminal : ITerminal
{
	public readonly Block Block;

	public BlockVoxelTerminal(Block block)
	{
		Block = block;
	}

	public int3 BlockPosition => Block.Position;

	public int TerminalIndex { get; init; }

	public int3? VoxelPosition { get; init; }

	public WireType WireType { get; init; }
}
