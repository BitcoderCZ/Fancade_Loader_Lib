// <copyright file="EntryPoint.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using System.Runtime.CompilerServices;

namespace BitcoderCZ.Fancade.Runtime;

/// <summary>
/// A fancade script entry point.
/// </summary>
public readonly struct EntryPoint
{
    /// <summary>
    /// Index of the environmen.
    /// </summary>
    public readonly int EnvironmentIndex;

    /// <summary>
    /// Position of the block.
    /// </summary>
    public readonly int3 BlockPos;

    /// <summary>
    /// Position of the terminal.
    /// </summary>
    public readonly byte3 TerminalPos;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntryPoint"/> struct.
    /// </summary>
    /// <param name="environmentIndex">Index of the environmen.</param>
    /// <param name="blockPos">Position of the block.</param>
    /// <param name="terminalPos">Position of the terminal.</param>
    public EntryPoint(int environmentIndex, int3 blockPos, byte3 terminalPos)
    {
        EnvironmentIndex = environmentIndex;
        BlockPos = blockPos;
        TerminalPos = terminalPos;
    }

    /// <summary>
    /// Deconstructs the <see cref="EntryPoint"/>.
    /// </summary>
    /// <param name="environmentIndex">Index of the environmen.</param>
    /// <param name="blockPos">Position of the block.</param>
    /// <param name="terminalPos">Position of the terminal.</param>
    public void Deconstruct(out int environmentIndex, out int3 blockPos, out byte3 terminalPos)
    {
        environmentIndex = EnvironmentIndex;
        blockPos = BlockPos;
        terminalPos = TerminalPos;
    }
}