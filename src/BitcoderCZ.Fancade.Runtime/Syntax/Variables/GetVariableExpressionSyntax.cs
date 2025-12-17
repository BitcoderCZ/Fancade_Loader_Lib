// <copyright file="GetVariableExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Variables;

/// <summary>
/// A <see cref="SyntaxNode"/> for any variable prefab.
/// </summary>
public sealed class GetVariableExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetVariableExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="variable">The variable to get.</param>
    public GetVariableExpressionSyntax(ushort prefabId, int3 position, Variable variable)
        : base(prefabId, position)
    {
        if (prefabId is not (46 or 48 or 50 or 52 or 54 or 56))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 46 or 48 or 50 or 52 or 54 or 56.");
        }

        Variable = variable;
    }

    /// <summary>
    /// Gets the variable to get.
    /// </summary>
    /// <value>The variable to get.</value>
    public Variable Variable { get; }
}
