// <copyright file="TerminalStore.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing.Scripting.Terminals;
using System;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing.Scripting.TerminalStores;

/// <summary>
/// A basic <see cref="ITerminalStore"/> implementation.
/// </summary>
public readonly struct TerminalStore : ITerminalStore
{
    private readonly ITerminal[] _out;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalStore"/> struct.
    /// </summary>
    /// <param name="block">The <see cref="Block"/> to create this <see cref="TerminalStore"/> from, must be <see cref="ScriptBlockType.Active"/>.</param>
    public TerminalStore(Block block)
        : this(block, block.Type.Before, block, block.Type.After)
    {
        if (block.Type.BlockType != ScriptBlockType.Active)
        {
            ThrowArgumentException($"{nameof(block)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(ScriptBlockType)}.{nameof(ScriptBlockType.Active)}.", nameof(block));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalStore"/> struct.
    /// </summary>
    /// <param name="in">The input block.</param>
    /// <param name="inTerminal">The input terminal.</param>
    /// <param name="out">The output block.</param>
    /// <param name="outTerminal">The output terminal.</param>
    public TerminalStore(Block @in, TerminalDef inTerminal, Block @out, TerminalDef outTerminal)
    {
        In = new BlockTerminal(@in, inTerminal);
        _out = [new BlockTerminal(@out, outTerminal)];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalStore"/> struct.
    /// </summary>
    /// <param name="in">The input block.</param>
    /// <param name="inTerminalName">Name of the input terminal.</param>
    /// <param name="out">The output block.</param>
    /// <param name="outTerminalName">Name of the output terminal.</param>
    public TerminalStore(Block @in, string inTerminalName, Block @out, string outTerminalName)
    {
        ThrowIfNull(@in, nameof(@in));
        ThrowIfNull(@out, nameof(@out));

        In = new BlockTerminal(@in, @in.Type[inTerminalName]);
        _out = [new BlockTerminal(@out, @out.Type[outTerminalName])];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalStore"/> struct.
    /// </summary>
    /// <param name="in">The input terminal.</param>
    /// <param name="out">The output terminals.</param>
    public TerminalStore(ITerminal @in, ITerminal[] @out)
    {
        In = @in;
        _out = @out;
    }

    /// <inheritdoc/>
    public ITerminal In { get; }

    /// <inheritdoc/>
    public ReadOnlySpan<ITerminal> Out => _out;

    /// <summary>
    /// Creates an input-only <see cref="TerminalStore"/>.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="BlockDef.Before"/> for the terminal.
    /// </remarks>
    /// <param name="block">The input block, must be <see cref="ScriptBlockType.Active"/>.</param>
    /// <returns>The created <see cref="TerminalStore"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="block"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Throws when <paramref name="block"/>'s type is not <see cref="ScriptBlockType.Active"/>.</exception>
    public static TerminalStore CreateIn(Block block)
        => block is null
            ? throw new ArgumentNullException(nameof(block))
            : block.Type.BlockType == ScriptBlockType.Active
            ? CreateIn(block, block.Type.Before)
            : throw new ArgumentException($"{nameof(block)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(ScriptBlockType)}.{nameof(ScriptBlockType.Active)}.", nameof(block));

    /// <summary>
    /// Creates an input-only <see cref="TerminalStore"/>.
    /// </summary>
    /// <param name="block">The input block.</param>
    /// <param name="terminal">The input terminal.</param>
    /// <returns>The created <see cref="TerminalStore"/>.</returns>
    public static TerminalStore CreateIn(Block block, TerminalDef terminal)
        => new TerminalStore(new BlockTerminal(block, terminal), []);

    /// <summary>
    /// Creates an output-only <see cref="TerminalStore"/>.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="BlockDef.After"/> for the terminal.
    /// </remarks>
    /// <param name="block">The output block, must be <see cref="ScriptBlockType.Active"/>.</param>
    /// <returns>The created <see cref="TerminalStore"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="block"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Throws when <paramref name="block"/>'s type is not <see cref="ScriptBlockType.Active"/>.</exception>
    public static TerminalStore CreateOut(Block block)
        => block is null
            ? throw new ArgumentNullException(nameof(block))
            : block.Type.BlockType == ScriptBlockType.Active
            ? CreateOut(block, block.Type.After)
            : throw new ArgumentException($"{nameof(block)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(ScriptBlockType)}.{nameof(ScriptBlockType.Active)}.", nameof(block));

    /// <summary>
    /// Creates an output-only <see cref="TerminalStore"/>.
    /// </summary>
    /// <param name="block">The output block.</param>
    /// <param name="terminal">The output terminal.</param>
    /// <returns>The created <see cref="TerminalStore"/>.</returns>
    public static TerminalStore CreateOut(Block block, TerminalDef terminal)
        => new TerminalStore(NopTerminal.Instance, [new BlockTerminal(block, terminal)]);
}
