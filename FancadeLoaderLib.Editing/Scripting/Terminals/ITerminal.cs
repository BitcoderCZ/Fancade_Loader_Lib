// <copyright file="ITerminal.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib.Editing.Scripting.Terminals;

public interface ITerminal
{
    int3 BlockPosition { get; }

    int TerminalIndex { get; }

    int3? VoxelPosition { get; }

    WireType WireType { get; }
}
