// <copyright file="UnaryExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

// the operation is determined by PrefabId

/// <summary>
/// A <see cref="SyntaxNode"/> for any unary math prefab.
/// </summary>
public sealed class UnaryExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnaryExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="input">The input terminal; or <see langword="null"/>, if it is not connected.</param>
    public UnaryExpressionSyntax(ushort prefabId, int3 position, SyntaxTerminal? input)
        : base(prefabId, position)
    {
        if (prefabId is not (90 or 144 or 440 or 413 or 453 or 184 or 186 or 188 or 455 or 578))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 90 or 144 or 440 or 413 or 453 or 184 or 186 or 188 or 455 or 578.");
        }

        Input = input;
    }

    /// <summary>
    /// Gets the input terminal.
    /// </summary>
    /// <value>The input terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Input { get; }
}
