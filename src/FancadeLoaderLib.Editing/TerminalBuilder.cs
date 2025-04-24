// <copyright file="TerminalBuilder.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace FancadeLoaderLib.Editing;

/// <summary>
/// A helper for creating terminals.
/// </summary>
public readonly struct TerminalBuilder
{
    /// <summary>
    /// An empty <see cref="TerminalBuilder"/>.
    /// </summary>
    public static readonly TerminalBuilder Empty = new TerminalBuilder(0);

    private readonly List<TerminalModel> _terminals = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalBuilder"/> struct.
    /// </summary>
    /// <param name="inititalCapacity">The initial capacity of the <see cref="TerminalBuilder"/>.</param>
    public TerminalBuilder(int inititalCapacity)
    {
        _terminals = new List<TerminalModel>(inititalCapacity);
    }

    /// <summary>
    /// Creates a new <see cref="TerminalBuilder"/>.
    /// </summary>
    /// <returns>The new <see cref="TerminalBuilder"/>.</returns>
    public static TerminalBuilder Create()
        => new TerminalBuilder(8);

    /// <summary>
    /// Adds a terminal.
    /// </summary>
    /// <param name="signalType">Signal type of the terminal.</param>
    /// <param name="type">Type of the terminal.</param>
    /// <returns>This instance after the add operation has completed.</returns>
    public TerminalBuilder Add(SignalType signalType, TerminalType type)
        => Add(signalType, type, null);

    /// <summary>
    /// Adds a terminal.
    /// </summary>
    /// <param name="signalType">Signal type of the terminal.</param>
    /// <param name="type">Type of the terminal.</param>
    /// <param name="name">Name of the terminal.</param>
    /// <returns>This instance after the add operation has completed.</returns>
    public TerminalBuilder Add(SignalType signalType, TerminalType type, string? name)
    {
        _terminals.Add(new TerminalModel(signalType, type, name));

        return this;
    }

    /// <summary>
    /// Builds this <see cref="TerminalBuilder"/>.
    /// </summary>
    /// <param name="blockSize">Size of the block the terminals belong to, determines their positions.</param>
    /// <param name="blockType">Type of the block the terminals belong to.</param>
    /// <returns>The built terminals.</returns>
    public ImmutableArray<TerminalDef> Build(int3 blockSize, ScriptBlockType blockType)
    {
        TerminalDef[] terminals = new TerminalDef[_terminals.Count];

        int off = blockType == ScriptBlockType.Active ? 1 : 0;

        int countIn = 0;
        int countOut = 0;

        // count in and out terminals
        for (int i = off; i < _terminals.Count - off; i++)
        {
            if (_terminals[i].Type == TerminalType.In)
            {
                countIn++;
            }
            else
            {
                countOut++;
            }
        }

        // if a block has less/more in/out terminals, one of the sides will start higher
        countIn = blockSize.Z - countIn;
        countOut = blockSize.Z - countOut;

        int outXPos = (blockSize.X * 8) - 2;

        for (int i = off; i < _terminals.Count - off; i++)
        {
            var terminal = _terminals[i];

            terminals[i] = terminal.Type == TerminalType.In
                ? new TerminalDef(terminal.SignalType, terminal.Type, terminal.Name, i, new byte3(0, 1, (countIn++ * 8) + 3))
                : new TerminalDef(terminal.SignalType, terminal.Type, terminal.Name, i, new byte3(outXPos, 1, (countOut++ * 8) + 3));
        }

        if (blockType == ScriptBlockType.Active)
        {
            terminals[0] = new TerminalDef(_terminals[0].SignalType, _terminals[0].Type, "After", 0, TerminalDef.AfterPosition);
            terminals[^1] = new TerminalDef(_terminals[^1].SignalType, _terminals[^1].Type, "Before", _terminals.Count - 1, new byte3(3, 1, (blockSize.Z * 8) - 2));
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(terminals);
    }

    private record struct TerminalModel(SignalType SignalType, TerminalType Type, string? Name);
}