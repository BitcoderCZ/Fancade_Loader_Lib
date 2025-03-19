// <copyright file="BlockVoxelTerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib.Editing.Scripting.Terminals;

#pragma warning disable CA1815 // Override equals and operator equals on value types
public readonly struct BlockVoxelTerminal : ITerminal
#pragma warning restore CA1815 // Override equals and operator equals on value types
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
