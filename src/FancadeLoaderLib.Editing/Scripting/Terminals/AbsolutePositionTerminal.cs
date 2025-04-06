// <copyright file="AbsolutePositionTerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib.Editing.Scripting.Terminals;

/// <summary>
/// A <see cref="ITerminal"/> that connects to an absolute position.
/// </summary>
public readonly struct AbsolutePositionTerminal : ITerminal
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbsolutePositionTerminal"/> struct.
    /// </summary>
    /// <param name="blockPosition">The position of the block this terminal is on.</param>
    public AbsolutePositionTerminal(int3 blockPosition)
    {
        BlockPosition = blockPosition;
    }

    /// <inheritdoc/>
    public int3 BlockPosition { get; }

    /// <inheritdoc/>
    public int TerminalIndex { get; init; }

    /// <inheritdoc/>
    public int3? VoxelPosition { get; init; }

    /// <inheritdoc/>
    public SignalType SignalType { get; init; }
}
