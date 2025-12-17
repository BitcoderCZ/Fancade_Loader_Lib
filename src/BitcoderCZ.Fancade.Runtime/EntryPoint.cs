// <copyright file="EntryPoint.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BitcoderCZ.Fancade.Runtime.Compiled")]

namespace BitcoderCZ.Fancade.Runtime;

internal readonly struct EntryPoint
{
    public readonly int EnvironmentIndex;
    public readonly int3 BlockPos;
    public readonly byte3 TerminalPos;

    public EntryPoint(int environmentIndex, int3 blockPos, byte3 terminalPos)
    {
        EnvironmentIndex = environmentIndex;
        BlockPos = blockPos;
        TerminalPos = terminalPos;
    }

    public void Deconstruct(out int environmentIndex, out int3 blockPos, out byte3 terminalPos)
    {
        environmentIndex = EnvironmentIndex;
        blockPos = BlockPos;
        terminalPos = TerminalPos;
    }
}