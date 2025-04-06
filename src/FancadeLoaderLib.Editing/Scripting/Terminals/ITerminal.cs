// <copyright file="ITerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib.Editing.Scripting.Terminals;

/// <summary>
/// Represents a terminal of a block.
/// </summary>
public interface ITerminal
{
    /// <summary>
    /// Gets the position of the block the terminal is on.
    /// </summary>
    /// <value>The position of the block the terminal is on.</value>
    int3 BlockPosition { get; }

    /// <summary>
    /// Gets the index of the terminal.
    /// </summary>
    /// <value>The index of the terminal.</value>
    int TerminalIndex { get; }

    /// <summary>
    /// Gets the voxel position of the terminal relative to <see cref="BlockPosition"/>.
    /// </summary>
    /// <value>The voxel position of the terminal.</value>
    int3? VoxelPosition { get; }

    /// <summary>
    /// Gets the <see cref="FancadeLoaderLib.SignalType"/> of the terminal.
    /// </summary>
    /// <value>The <see cref="FancadeLoaderLib.SignalType"/> of the terminal.</value>
    SignalType SignalType { get; }
}
