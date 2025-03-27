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
    /// Gets the position of the block this terminal is on.
    /// </summary>
    /// <value>The position of the block this terminal is on.</value>
    int3 BlockPosition { get; }

    /// <summary>
    /// Gets the index of this terminal.
    /// </summary>
    /// <value>The index of this terminal.</value>
    int TerminalIndex { get; }

    /// <summary>
    /// Gets the voxel position of this terminal relative to <see cref="BlockPosition"/>.
    /// </summary>
    /// <value>The voxel position of this terminal</value>
    int3? VoxelPosition { get; }

    /// <summary>
    /// Gets the <see cref="Editing.WireType"/> of this terminal.
    /// </summary>
    /// <value>The <see cref="Editing.WireType"/> of this terminal.</value>
    WireType WireType { get; }
}
