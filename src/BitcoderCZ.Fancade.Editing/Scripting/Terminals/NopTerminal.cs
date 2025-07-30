// <copyright file="NopTerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Editing.Scripting.Terminals;

/// <summary>
/// A terminal that doesn't connect to anything.
/// </summary>
public sealed class NopTerminal : ITerminal
{
    /// <summary>
    /// The <see cref="NopTerminal"/> instance.
    /// </summary>
    public static readonly NopTerminal Instance = new NopTerminal();

    private NopTerminal()
    {
    }

    /// <inheritdoc/>
    public int3 BlockPosition => new int3(-1, -1, -1);

    /// <inheritdoc/>
    public int TerminalIndex => -1;

    /// <inheritdoc/>
    public int3? VoxelPosition => new int3(-1, -1, -1);

    /// <inheritdoc/>
    public SignalType SignalType => SignalType.Error;
}
