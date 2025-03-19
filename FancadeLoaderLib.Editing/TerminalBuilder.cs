// <copyright file="TerminalBuilder.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace FancadeLoaderLib.Editing;

#pragma warning disable CA1815 // Override equals and operator equals on value types
public readonly struct TerminalBuilder
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    public static readonly TerminalBuilder Empty = new TerminalBuilder(0);

    private readonly List<TerminalModel> _terminals = [];

    public TerminalBuilder(int inititalCapacity)
    {
        _terminals = new List<TerminalModel>(inititalCapacity);
    }

    public static TerminalBuilder Create()
        => new TerminalBuilder(8);

    public TerminalBuilder Add(WireType wireType, TerminalType type)
        => Add(wireType, type, null);

    public TerminalBuilder Add(WireType wireType, TerminalType type, string? name)
    {
        _terminals.Add(new TerminalModel(wireType, type, name));

        return this;
    }

    public ImmutableArray<TerminalDef> Build(int3 blockSize, BlockType blockType)
    {
        TerminalDef[] terminals = new TerminalDef[_terminals.Count];

        int off = blockType == BlockType.Active ? 1 : 0;

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
                ? new TerminalDef(terminal.WireType, terminal.Type, terminal.Name, i, new int3(0, 1, (countIn++ * 8) + 3))
                : new TerminalDef(terminal.WireType, terminal.Type, terminal.Name, i, new int3(outXPos, 1, (countOut++ * 8) + 3));
        }

        if (blockType == BlockType.Active)
        {
            terminals[0] = new TerminalDef(_terminals[0].WireType, _terminals[0].Type, "After", 0, new int3(3, 1, 0));
            terminals[^1] = new TerminalDef(_terminals[^1].WireType, _terminals[^1].Type, "Before", _terminals.Count - 1, new int3(3, 1, (blockSize.Z * 8) - 2));
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(terminals);
    }

    private record struct TerminalModel(WireType WireType, TerminalType Type, string? Name);
}