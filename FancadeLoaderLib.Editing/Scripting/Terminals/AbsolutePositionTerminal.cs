// <copyright file="AbsolutePositionTerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib.Editing.Scripting.Terminals;

#pragma warning disable CA1815 // Override equals and operator equals on value types
public readonly struct AbsolutePositionTerminal : ITerminal
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
	public AbsolutePositionTerminal(int3 blockPosition)
	{
		BlockPosition = blockPosition;
	}

	public int3 BlockPosition { get; }

	public int TerminalIndex { get; init; }

	public int3? VoxelPosition { get; init; }

	public WireType WireType { get; init; }
}
