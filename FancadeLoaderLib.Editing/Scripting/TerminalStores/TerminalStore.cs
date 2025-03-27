// <copyright file="TerminalStore.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting.Terminals;
using System;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing.Scripting.TerminalStores;

/// <summary>
/// A basic <see cref="ITerminalStore"/> implementation.
/// </summary>
public readonly struct TerminalStore : ITerminalStore
{
    private readonly ITerminal[] _out;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalStore"/> struct.
    /// </summary>
    /// <param name="block">The <see cref="Block"/> to create this <see cref="TerminalStore"/> from, must be <see cref="BlockType.Active"/>.</param>
    public TerminalStore(Block block)
        : this(block, block.Type.Before, block, block.Type.After)
    {
        if (block.Type.BlockType != BlockType.Active)
        {
            ThrowArgumentException($"{nameof(block)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(BlockType)}.{nameof(BlockType.Active)}.", nameof(block));
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
    /// Creates a <see cref="TerminalStore"/> only for the block's input.
    /// </summary>
    /// <param name="block">The block to create this <see cref="TerminalStore"/> for, must be <see cref="BlockType.Active"/>.</param>
    /// <returns>The created <see cref="TerminalStore"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="block"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Throws when <paramref name="block"/>'s type is not <see cref="BlockType.Active"/>.</exception>
    public static TerminalStore CreateIn(Block block)
        => block is null
            ? throw new ArgumentNullException(nameof(block))
            : block.Type.BlockType == BlockType.Active
            ? CreateIn(block, block.Type.Before)
            : throw new ArgumentException($"{nameof(block)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(BlockType)}.{nameof(BlockType.Active)}.", nameof(block));

    public static TerminalStore CreateIn(Block block, TerminalDef terminal)
        => new TerminalStore(new BlockTerminal(block, terminal), []);

    public static TerminalStore CreateOut(Block block)
        => block is null
            ? throw new ArgumentNullException(nameof(block))
            : block.Type.BlockType == BlockType.Active
            ? CreateOut(block, block.Type.After)
            : throw new ArgumentException($"{nameof(block)}.{nameof(Block.Type)}.{nameof(BlockDef.BlockType)} must be {nameof(BlockType)}.{nameof(BlockType.Active)}.", nameof(block));

    public static TerminalStore CreateOut(Block block, TerminalDef terminal)
        => new TerminalStore(NopTerminal.Instance, [new BlockTerminal(block, terminal)]);
}
