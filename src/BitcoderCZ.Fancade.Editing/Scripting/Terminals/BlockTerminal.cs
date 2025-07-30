// <copyright file="BlockTerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing.Scripting.Terminals;

/// <summary>
/// A <see cref="ITerminal"/> that connects to an <see cref="Editing.Block"/>, getting the terminal info from a <see cref="TerminalDef"/>.
/// </summary>
public readonly struct BlockTerminal : ITerminal
{
    /// <summary>
    /// The block this <see cref="BlockTerminal"/> is on.
    /// </summary>
    public readonly Block Block;

    /// <summary>
    /// The <see cref="TerminalDef"/> that defines this terminal.
    /// </summary>
    public readonly TerminalDef Terminal;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockTerminal"/> struct.
    /// </summary>
    /// <param name="block">The block the <see cref="BlockTerminal"/> is on.</param>
    /// <param name="terminal">The <see cref="TerminalDef"/> that defines the terminal.</param>
    public BlockTerminal(Block block, TerminalDef terminal)
    {
        Block = block;
        Terminal = terminal;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockTerminal"/> struct.
    /// </summary>
    /// <param name="block">The block the <see cref="BlockTerminal"/> is on.</param>
    /// <param name="terminalName">The name of the terminal.</param>
    public BlockTerminal(Block block, string terminalName)
    {
        ThrowIfNull(block, nameof(block));

        Block = block;
        Terminal = block.Type[terminalName];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockTerminal"/> struct.
    /// </summary>
    /// <param name="block">The block the <see cref="BlockTerminal"/> is on.</param>
    /// <param name="terminalIndex">The index of the terminal.</param>
    public BlockTerminal(Block block, int terminalIndex)
    {
        ThrowIfNull(block, nameof(block));

        Block = block;
        Terminal = block.Type.Terminals[terminalIndex];
    }

    /// <inheritdoc/>
    public int3 BlockPosition => Block.Position;

    /// <inheritdoc/>
    public int TerminalIndex => Terminal.Index;

    /// <inheritdoc/>
    public int3? VoxelPosition => Terminal.Position;

    /// <inheritdoc/>
    public SignalType SignalType => Terminal.SignalType;
}
