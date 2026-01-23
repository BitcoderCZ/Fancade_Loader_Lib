// <copyright file="BreakVecRotExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Math;

/// <summary>
/// A <see cref="SyntaxNode"/> for the break vector/rotation prefab.
/// </summary>
public sealed class BreakVecRotExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BreakVecRotExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="vecRot">The vector/rotation terminal; or <see langword="null"/>, if it is not connected.</param>
    public BreakVecRotExpressionSyntax(ushort prefabId, int3 position, SyntaxTerminal? vecRot)
        : base(prefabId, position)
    {
        if (prefabId is not (156 or 442))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 156 or 442.");
        }

        VecRot = vecRot;
    }

    /// <summary>
    /// Gets the vector/rotation terminal.
    /// </summary>
    /// <value>The vector/rotation terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? VecRot { get; }
}
