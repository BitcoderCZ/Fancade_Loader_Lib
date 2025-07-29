// <copyright file="Block.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Editing;

/// <summary>
/// Represents a fancade block instance.
/// </summary>
public record class Block
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Block"/> class.
    /// </summary>
    /// <param name="type">Type of the block.</param>
    /// <param name="position">Position of the block.</param>
    public Block(BlockDef type, int3 position)
    {
        Type = type;
        Position = position;
    }

    /// <summary>
    /// Gets the type of the block.
    /// </summary>
    /// <value>Type of the block.</value>
    public BlockDef Type { get; }

    /// <summary>
    /// Gets or sets the position of the block.
    /// </summary>
    /// <value>Position of the block.</value>
    public int3 Position { get; set; }
}
