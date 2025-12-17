// <copyright file="WorldToScreenExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the world to screen prefab.
/// </summary>
public sealed class WorldToScreenExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorldToScreenExpressionSyntax"/> class.
    /// </summary>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="worldPos">The world pos terminal; or <see langword="null"/>, if it is not connected.</param>
    public WorldToScreenExpressionSyntax(int3 position, SyntaxTerminal? worldPos)
        : base(477, position)
    {
        WorldPos = worldPos;
    }

    /// <summary>
    /// Gets the world pos terminal.
    /// </summary>
    /// <value>The world pos terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? WorldPos { get; }
}
