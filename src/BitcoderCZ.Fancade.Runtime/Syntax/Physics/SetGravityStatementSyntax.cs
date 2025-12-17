// <copyright file="SetGravityStatementSyntax.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Immutable;

namespace BitcoderCZ.Fancade.Runtime.Syntax.Physics;

/// <summary>
/// A <see cref="SyntaxNode"/> for the set gravity prefab.
/// </summary>
public sealed class SetGravityStatementSyntax : StatementSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetGravityStatementSyntax"/> class.
    /// </summary>
    /// <param name="position">Position of the prefab this node represents.</param>
    /// <param name="outVoidConnections">Output void connections from this node.</param>
    /// <param name="gravity">The gravity terminal; or <see langword="null"/>, if it is not connected.</param>
    public SetGravityStatementSyntax(int3 position, ImmutableArray<Connection> outVoidConnections, SyntaxTerminal? gravity)
        : base(324, position, outVoidConnections)
    {
        Gravity = gravity;
    }

    /// <summary>
    /// Gets the gravity terminal.
    /// </summary>
    /// <value>The gravity terminal; or <see langword="null"/>, if it is not connected.</value>
    public SyntaxTerminal? Gravity { get; }

    /// <inheritdoc/>
    public override IEnumerable<byte3> InputVoidTerminals => [TerminalDef.GetBeforePosition(2)];
}
