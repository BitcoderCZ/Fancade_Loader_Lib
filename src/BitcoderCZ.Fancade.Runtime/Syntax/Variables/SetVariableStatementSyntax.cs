// <copyright file="SetVariableStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Variables;

/// <summary>
/// A <see cref="SyntaxNode"/> for any set variable prefab.
/// </summary>
public sealed class SetVariableStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetVariableStatementSyntax"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this node represents.</param>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="variable">The variable to be set.</param>
    /// <param name="value">The value terminal; or <see langword="null"/>, if it is not connected.</param>
    public SetVariableStatementSyntax(ushort prefabId, int3 position, ImmutableArray<Connection> outVoidConnections, Variable variable, SyntaxTerminal? value)
        : base(prefabId, position, outVoidConnections)
    {
        if (prefabId is not (428 or 430 or 432 or 434 or 436 or 438))
        {
            ThrowArgumentOutOfRangeException(nameof(prefabId), $"{nameof(prefabId)} must be 428 or 430 or 432 or 434 or 436 or 438.");
        }

        Variable = variable;
        Value = value;
    }

    /// <summary>
    /// Gets the variable to be set.
    /// </summary>
    /// <value>The variable to be set.</value>
    public Variable Variable { get; }

    /// <summary>
    /// Gets the value terminal.
    /// </summary>
    /// <value>The value terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Value { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(1)];
}
