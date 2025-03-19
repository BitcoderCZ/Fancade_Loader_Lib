// <copyright file="Block.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace FancadeLoaderLib.Editing;

public record class Block
{
    public Block(BlockDef type, int3 position)
    {
        Type = type;
        Position = position;
    }

    public BlockDef Type { get; }

    public int3 Position { get; set; }
}
