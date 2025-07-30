// <copyright file="BlockVoxelTerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Editing.Scripting.Terminals;

/// <summary>
/// A <see cref="ITerminal"/> that connects to an <see cref="Editing.Block"/>.
/// </summary>
public readonly struct BlockVoxelTerminal : ITerminal
{
    /// <summary>
    /// The block this <see cref="BlockTerminal"/> is on.
    /// </summary>
    public readonly Block Block;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockVoxelTerminal"/> struct.
    /// </summary>
    /// <param name="block">The block the <see cref="BlockVoxelTerminal"/> is on.</param>
    public BlockVoxelTerminal(Block block)
    {
        Block = block;
    }

    /// <inheritdoc/>
    public int3 BlockPosition => Block.Position;

    /// <inheritdoc/>
    public int TerminalIndex { get; init; }

    /// <inheritdoc/>
    public int3? VoxelPosition { get; init; }

    /// <inheritdoc/>
    public SignalType SignalType { get; init; }
}
