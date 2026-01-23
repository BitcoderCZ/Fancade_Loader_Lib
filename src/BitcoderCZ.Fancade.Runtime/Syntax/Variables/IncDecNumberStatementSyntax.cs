// <copyright file="IncDecNumberStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;
using static BitcoderCZ.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Variables;

/// <summary>
/// A <see cref="SyntaxNode"/> for the increase and decrease prefab.
/// </summary>
public sealed class IncDecNumberStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncDecNumberStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="variable">The variable terminal; or <see langword="null"/>, if it is not connected.</param>
    public IncDecNumberStatementSyntax(ushort prefabId, int3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? variable)
        : base(prefabId, position, outVoidConnections)
    {
        if (prefabId is not (556 or 558))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 556 or 558.");
        }

        Variable = variable;
    }

    /// <summary>
    /// Gets the variable terminal.
    /// </summary>
    /// <value>The variable terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Variable { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(1)];
}
