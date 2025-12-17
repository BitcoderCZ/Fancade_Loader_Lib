// <copyright file="RaycastExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Objects;

/// <summary>
/// A <see cref="SyntaxNode"/> for the raycast prefab.
/// </summary>
public sealed class RaycastExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaycastExpressionSyntax"/> class.
    /// </summary>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="from">The from terminal; or <see langword="null"/>, if it is not connected.</param>
    /// <param name="to">The to terminal; or <see langword="null"/>, if it is not connected.</param>
    public RaycastExpressionSyntax(int3 position, SyntaxTerminal? from, SyntaxTerminal? to)
        : base(228, position)
    {
        From = from;
        To = to;
    }

    /// <summary>
    /// Gets the from terminal.
    /// </summary>
    /// <value>The from terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? From { get; }

    /// <summary>
    /// Gets the to terminal.
    /// </summary>
    /// <value>The to terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? To { get; }
}
