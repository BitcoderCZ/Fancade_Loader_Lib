// <copyright file="LiteralExpressionSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Values;

/// <summary>
/// A <see cref="SyntaxNode"/> for any literal prefab prefab.
/// </summary>
public sealed class LiteralExpressionSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LiteralExpressionSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="type">Type of <paramref name="value"/>.</param>
    /// <param name="value">The value.</param>
    public LiteralExpressionSyntax(ushort prefabId, int3 position, SignalType type, RuntimeValue value)
        : base(prefabId, position)
    {
        if (prefabId is not (36 or 38 or 42 or 449 or 451))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 36 or 38 or 42 or 449 or 451.");
        }

        Type = type;
        Value = value;
    }

    /// <summary>
    /// Gets the type of <see cref="Value"/>.
    /// </summary>
    /// <value>Type of <see cref="Value"/>.</value>
    public SignalType Type { get; }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <value>The value.</value>
    public RuntimeValue Value { get; }
}
