// <copyright file="TerminalDef.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib.Editing;

public sealed class TerminalDef
{
    public readonly WireType WireType;
    public readonly TerminalType Type;
    public readonly string? Name;
    public readonly int Index;
    public readonly int3 Position;

    public TerminalDef(WireType wireType, TerminalType type, int index, int3 position)
        : this(wireType, type, null, index, position)
    {
        Index = index;
        Position = position;
    }

    public TerminalDef(WireType wireType, TerminalType type, string? name, int index, int3 position)
    {
        WireType = wireType;
        Type = type;
        Name = name;
        Index = index;
        Position = position;
    }
}
