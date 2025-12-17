// <copyright file="CurrentFrameExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Game;

/// <summary>
/// A <see cref="SyntaxNode"/> for the current frame prefab.
/// </summary>
public sealed class CurrentFrameExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentFrameExpressionSyntax"/> class.
    /// </summary>
    /// <param name="position">Position of the prefab this node represents.</param>
    public CurrentFrameExpressionSyntax(int3 position)
        : base(564, position)
    {
    }
}
